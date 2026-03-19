using System;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using System.ComponentModel;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that performs markerless multi-pose estimation
    /// for each image in the sequence using a SLEAP model.
    /// </summary>
    /// <seealso cref="PredictCentroids"/>
    /// <seealso cref="PredictPoseIdentities"/>
    /// <seealso cref="PredictSinglePose"/>
    /// <seealso cref="GetBodyPart"/>
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless multi-pose estimation for each image in the sequence using a SLEAP model.")]
    public class PredictPoses : Transform<IplImage, PoseCollection>
    {
        /// <summary>
        /// Gets or sets a value specifying the path to the exported ONNX
        /// file containing the pretrained SLEAP model.
        /// </summary>
        [FileNameFilter("ONNX Files(*.onnx)|*.onnx")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("Specifies the path to the exported ONNX file containing the pretrained SLEAP model.")]
        public string ModelFileName { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the confidence threshold used to discard centroid
        /// predictions. If no value is specified, all estimated centroid positions are returned.
        /// </summary>
        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the confidence threshold used to discard centroid predictions. If no value is specified, all estimated centroid positions are returned.")]
        public float? CentroidMinConfidence { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the confidence threshold used to discard predicted
        /// body part positions. If no value is specified, all estimated positions are returned.
        /// </summary>
        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the confidence threshold used to discard predicted body part positions. If no value is specified, all estimated positions are returned.")]
        public float? PartMinConfidence { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the scale factor used to resize video frames
        /// for inference. If no value is specified, no resizing is performed.
        /// </summary>
        [Description("Specifies the scale factor used to resize video frames for inference. If no value is specified, no resizing is performed.")]
        public float? ScaleFactor { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the optional color conversion used to prepare
        /// RGB video frames for inference. If no value is specified, no color conversion
        /// is performed.
        /// </summary>
        [Description("Specifies the optional color conversion used to prepare RGB video frames for inference. If no value is specified, no color conversion is performed.")]
        public ColorConversion? ColorConversion { get; set; }

        /// <summary>
        /// Gets or sets the ONNX runtime execution provider used to perform model inference.
        /// </summary>
        [Description("The ONNX runtime execution provider used to perform model inference.")]
        public ExecutionProvider ExecutionProvider { get; set; } = ExecutionProvider.Cpu;

        private IObservable<PoseCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                Size currentTensorSize = default;
                DenseTensor<byte> tensor = null;
                var colorConversion = ColorConversion;
                var modelPath = ModelFileName;
                int currentBatchSize = 0;

                var session = TensorHelper.ImportModel(modelPath, ExecutionProvider, out var exportMetadata);
                var inputName = session.InputMetadata.Keys.First();
                if (exportMetadata.ModelType != ModelType.TopDown)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.CenteredInstance)} model type but found {exportMetadata.ModelType}.");
                }

                return source.Select(input =>
                {
                    var colorChannels = (colorConversion?.GetConversionNumChannels()) ?? input[0].Channels;
                    var tensorSize = input[0].Size;
                    var batchSize = input.Length;
                    var scaleFactor = ScaleFactor;
                    var poseScale = (double)scaleFactor.GetValueOrDefault(exportMetadata.InputScale);

                    if (poseScale < 1)
                    {
                        tensorSize.Width = (int)(tensorSize.Width * poseScale);
                        tensorSize.Height = (int)(tensorSize.Height * poseScale);
                        poseScale = 1.0 / poseScale;
                    }

                    if (tensor == null || currentBatchSize != batchSize || currentTensorSize != tensorSize)
                    {
                        ReadOnlySpan<int> dimensions = stackalloc int[] { batchSize, colorChannels, tensorSize.Height, tensorSize.Width };
                        tensor = new DenseTensor<byte>(dimensions);
                        currentTensorSize = tensorSize;
                        currentBatchSize = batchSize;
                    }

                    var frames = Array.ConvertAll(input, frame =>
                    {
                        frame = TensorHelper.EnsureFrameSize(frame, tensorSize, ref resizeTemp);
                        frame = TensorHelper.EnsureColorFormat(frame, colorConversion, ref colorTemp, colorChannels);
                        return frame;
                    });

                    TensorHelper.UpdateTensor(tensor, tensorSize, frames);
                    var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };
                    using var output = session.Run(inputs);

                    var poseCollection = new PoseCollection(input[0], exportMetadata);
                    var centroidTensor = output[0].AsTensor<float>();
                    var instanceCount = centroidTensor.Dimensions[1];
                    if (instanceCount == 0)
                        return poseCollection;

                    var centroidConfidenceTensor = output[1].AsTensor<float>();
                    var poseTensor = output[2].AsTensor<float>();
                    var partConfTensor = output[3].AsTensor<float>();
                    var instanceValidTensor = output[4].AsTensor<bool>();
                    var partCount = partConfTensor.Dimensions[2];

                    var partThreshold = PartMinConfidence;
                    var centroidThreshold = CentroidMinConfidence;

                    for (int i = 0; i < instanceCount; i++)
                    {
                        var centroidConfidence = centroidConfidenceTensor.GetValue(i);
                        if (centroidConfidence < centroidThreshold || !instanceValidTensor.GetValue(i))
                            continue;

                        var pose = new Pose(input[0], exportMetadata);
                        var centroid = new BodyPart();
                        centroid.Confidence = centroidConfidence;
                        centroid.Position = new Point2f(
                            x: (float)(centroidTensor.GetValue(i * 2) * poseScale),
                            y: (float)(centroidTensor.GetValue(i * 2 + 1) * poseScale));
                        pose.Centroid = centroid;

                        for (int j = 0; j < partCount; j++)
                        {
                            var bodyPart = new BodyPart();
                            bodyPart.Name = exportMetadata.PartNames[j];
                            bodyPart.Confidence = partConfTensor.GetValue(i * partCount + j);
                            if (bodyPart.Confidence < partThreshold)
                            {
                                bodyPart.Position = new Point2f(float.NaN, float.NaN);
                            }
                            else
                            {
                                bodyPart.Position = new Point2f(
                                    x: (float)(poseTensor.GetValue(i * partCount * 2 + j * 2) * poseScale),
                                    y: (float)(poseTensor.GetValue(i * partCount * 2 + j * 2 + 1) * poseScale));
                            }
                            pose.Add(bodyPart);
                        }
                        poseCollection.Add(pose);
                    }
                    return poseCollection;
                });
            });
        }

        /// <summary>
        /// Performs markerless multi-pose estimation for each image in an observable
        /// sequence using a SLEAP model.
        /// </summary>
        /// <param name="source">The sequence of images from which to extract the poses.</param>
        /// <returns>
        /// A sequence of <see cref="PoseCollection"/> objects representing the poses
        /// extracted from each image in the <paramref name="source"/> sequence.
        /// </returns>
        public override IObservable<PoseCollection> Process(IObservable<IplImage> source)
        {
            return Process(source.Select(frame => new IplImage[] { frame }));
        }
    }
}
