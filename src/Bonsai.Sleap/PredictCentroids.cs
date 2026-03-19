using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that performs multi-instance centroid detection for each
    /// image in the sequence using a SLEAP model.
    /// </summary>
    /// <seealso cref="PredictPoses"/>
    /// <seealso cref="PredictPoseIdentities"/>
    /// <seealso cref="PredictSinglePose"/>
    /// <seealso cref="GetBodyPart"/>
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs multi-instance centroid detection for each image in the sequence using a SLEAP model.")]
    public class PredictCentroids : Transform<IplImage, CentroidCollection>
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

        private IObservable<CentroidCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                Size currentImageSize = default;
                DenseTensor<byte> tensor = null;
                var colorConversion = ColorConversion;
                var modelPath = ModelFileName;

                var session = TensorHelper.ImportModel(modelPath, ExecutionProvider, out var exportMetadata);
                var inputName = session.InputMetadata.Keys.First();
                if (exportMetadata.ModelType != ModelType.Centroid)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.Centroid)} model type but found {exportMetadata.ModelType}.");
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

                    if (tensor == null || currentImageSize != tensorSize)
                    {
                        ReadOnlySpan<int> dimensions = stackalloc int[] { batchSize, colorChannels, tensorSize.Height, tensorSize.Width };
                        tensor = new DenseTensor<byte>(dimensions);
                        currentImageSize = tensorSize;
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

                    var centroidCollection = new CentroidCollection(input[0]);
                    var centroidTensor = output[0].AsTensor<float>();
                    var centroidConfidenceTensor = output[1].AsTensor<float>();
                    var centroidValidTensor = output[2].AsTensor<bool>();

                    var instanceCount = centroidConfidenceTensor.Dimensions[1];
                    if (instanceCount == 0)
                        return centroidCollection;

                    var centroidThreshold = CentroidMinConfidence ?? 0;

                    for (int i = 0; i < instanceCount; i++)
                    {
                        if (centroidValidTensor[0, i] && centroidConfidenceTensor[0, i] >= centroidThreshold)
                        {
                            centroidCollection.Add(new Centroid(frames[0])
                            {
                                Position = new Point2f(
                                    (float)(centroidTensor[0, i, 0] * poseScale),
                                    (float)(centroidTensor[0, i, 1] * poseScale)),
                                Confidence = centroidConfidenceTensor[0, i]
                            });
                        }
                    }
                    return centroidCollection;
                });
            });
        }

        /// <summary>
        /// Performs multi-instance centroid detection for each image in an observable
        /// sequence using a SLEAP model.
        /// </summary>
        /// <param name="source">The sequence of images from which to extract the centroids.</param>
        /// <returns>
        /// A sequence of <see cref="CentroidCollection"/> objects representing the
        /// centroids extracted from each image in the <paramref name="source"/> sequence.
        /// </returns>
        public override IObservable<CentroidCollection> Process(IObservable<IplImage> source)
        {
            return Process(source.Select(frame => new IplImage[] { frame }));
        }
    }
}
