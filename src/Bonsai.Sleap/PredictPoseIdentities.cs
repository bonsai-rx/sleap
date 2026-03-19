using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using OpenCV.Net;
using System.ComponentModel;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that performs markerless multi-pose and identity
    /// estimation for each image in the sequence using a SLEAP model.
    /// </summary>
    /// <seealso cref="PredictCentroids"/>
    /// <seealso cref="PredictPoses"/>
    /// <seealso cref="PredictSinglePose"/>
    /// <seealso cref="GetBodyPart"/>
    /// <seealso cref="GetMaximumConfidencePoseIdentity"/>
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless multi-pose and identity estimation for each image in the sequence using a SLEAP model.")]
    public class PredictPoseIdentities : Transform<IplImage, PoseIdentityCollection>
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
        /// Gets or sets a value specifying the path to the configuration JSON file
        /// containing training metadata.
        /// </summary>
        [FileNameFilter("Config Files(*.json)|*.json|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("Specifies the path to the configuration JSON file containing training metadata.")]
        public string TrainingConfig { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the confidence threshold used to discard centroid
        /// predictions. If no value is specified, all estimated centroid positions are returned.
        /// </summary>
        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the confidence threshold used to discard centroid predictions. If no value is specified, all estimated centroid positions are returned.")]
        public float? CentroidMinConfidence { get; set; }

        /// <summary>
        /// Gets or sets a value specifying the confidence threshold used to assign an identity
        /// class. If no value is specified, the identity with highest confidence will be
        /// assigned to each pose.
        /// </summary>
        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the confidence threshold used to assign an identity class. If no value is specified, the identity with highest confidence will be assigned to each pose.")]
        public float? IdentityMinConfidence { get; set; }

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

        private IObservable<PoseIdentityCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Using(() => TensorHelper.ImportModel(ModelFileName, ExecutionProvider), session =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                Size currentTensorSize = default;
                DenseTensor<byte> tensor = null;
                var colorConversion = ColorConversion;
                int currentBatchSize = 0;

                var inputName = session.InputMetadata.Keys.First();
                var config = ConfigHelper.LoadTrainingConfig(TrainingConfig);
                if (config.ModelType != ModelType.MultiClassBottomUp)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.MultiClassBottomUp)} model type but found {config.ModelType}.");
                }

                return source.Select(input =>
                {
                    var poseScale = 1.0;
                    var colorChannels = (colorConversion?.GetConversionNumChannels()) ?? input[0].Channels;
                    var tensorSize = input[0].Size;
                    var batchSize = input.Length;
                    var scaleFactor = ScaleFactor;

                    if (scaleFactor.HasValue)
                    {
                        poseScale = scaleFactor.Value;
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

                    var identityCollection = new PoseIdentityCollection(input[0], config);
                    var centroidConfidenceTensor = output[0].AsTensor<float>();
                    var instanceCount = centroidConfidenceTensor.Dimensions[0];
                    if (instanceCount == 0)
                        return identityCollection;

                    var centroidTensor = output[1].AsTensor<float>();
                    var partConfTensor = output[2].AsTensor<float>();
                    var poseTensor = output[3].AsTensor<float>();
                    var idTensor = output[4].AsTensor<float>();
                    var partCount = partConfTensor.Dimensions[1];
                    var classCount = idTensor.Dimensions[1];

                    var partThreshold = PartMinConfidence;
                    var idThreshold = IdentityMinConfidence;
                    var centroidThreshold = CentroidMinConfidence;

                    for (int i = 0; i < instanceCount; i++)
                    {
                        var pose = new PoseIdentity(input.Length == 1 ? input[0] : input[i], config);
                        pose.IdentityScores = GetIdentityScores(idTensor, i, classCount, Comparer<float>.Default, out float maxScore, out int maxIndex);

                        if (maxScore < idThreshold || maxIndex < 0)
                        {
                            pose.IdentityIndex = -1;
                            pose.Confidence = float.NaN;
                            pose.Identity = string.Empty;
                        }
                        else
                        {
                            pose.IdentityIndex = maxIndex;
                            pose.Confidence = maxScore;
                            pose.Identity = config.ClassNames[maxIndex];
                        }

                        var centroid = new BodyPart();
                        centroid.Name = config.AnchorName;
                        centroid.Confidence = centroidConfidenceTensor.GetValue(i);
                        if (centroid.Confidence < centroidThreshold)
                        {
                            centroid.Position = new Point2f(float.NaN, float.NaN);
                        }
                        else
                        {
                            centroid.Position = new Point2f(
                                x: (float)(centroidTensor.GetValue(i * 2) * poseScale),
                                y: (float)(centroidTensor.GetValue(i * 2 + 1) * poseScale));
                        }
                        pose.Centroid = centroid;

                        for (int j = 0; j < partCount; j++)
                        {
                            var bodyPart = new BodyPart();
                            bodyPart.Name = config.PartNames[j];
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
                        identityCollection.Add(pose);
                    }
                    return identityCollection;
                });
            });
        }

        /// <summary>
        /// Performs markerless multi-pose and identity estimation for each image in
        /// an observable sequence using a SLEAP model.
        /// </summary>
        /// <param name="source">
        /// The sequence of images from which to extract the pose identities.
        /// </param>
        /// <returns>
        /// A sequence of <see cref="PoseIdentityCollection"/> objects representing
        /// the pose identities extracted from each image in the <paramref name="source"/>
        /// sequence.
        /// </returns>
        public override IObservable<PoseIdentityCollection> Process(IObservable<IplImage> source)
        {
            return Process(source.Select(frame => new IplImage[] { frame }));
        }

        static float[] GetIdentityScores(
            Tensor<float> tensor,
            int rowIndex,
            int classCount,
            IComparer<float> comparer,
            out float maxValue,
            out int maxIndex)
        {
            maxIndex = -1;
            maxValue = default;
            var values = new float[classCount];
            for (int i = 0; i < classCount; i++)
            {
                values[i] = tensor.GetValue(rowIndex * classCount + i);
                if (i == 0 || comparer.Compare(values[i], maxValue) > 0)
                {
                    maxIndex = i;
                    maxValue = values[i];
                }
            }
            return values;
        }
    }
}
