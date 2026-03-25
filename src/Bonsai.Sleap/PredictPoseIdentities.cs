using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using OpenCV.Net;
using System.ComponentModel;
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
        /// Gets or sets a value specifying a target size used to resize video frames
        /// for inference. If no value is specified, no resizing is performed.
        /// </summary>
        [TypeConverter(typeof(NumericRecordConverter))]
        [Description("Specifies the target size used to resize video frames for inference. If no value is specified, no resizing is performed.")]
        public Size? InputSize { get; set; }

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
            return Observable.Defer(() =>
            {
                var session = RuntimeHelper.ImportModel(ModelFileName, ExecutionProvider, out var exportMetadata);
                if (exportMetadata.ModelType != ModelType.MultiClassTopDownCombined)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.MultiClassTopDownCombined)} model type but found {exportMetadata.ModelType}.");
                }

                var inputName = session.InputMetadata.Keys.First();
                var frameBatch = new FrameBatch(inputName, InputSize, ColorConversion, exportMetadata);

                return source.Select(frames =>
                {
                    frameBatch.Update(frames);
                    using var output = session.Run(frameBatch.Inputs);

                    var identityCollection = new PoseIdentityCollection(frames[0], exportMetadata);
                    var centroidTensor = output[0].AsTensor<float>();
                    var instanceCount = centroidTensor.Dimensions[1];
                    if (instanceCount == 0)
                        return identityCollection;

                    var centroidConfidenceTensor = output[1].AsTensor<float>();
                    var poseTensor = output[2].AsTensor<float>();
                    var partConfTensor = output[3].AsTensor<float>();
                    var idTensor = output[4].AsTensor<float>();
                    var instanceValidTensor = output[5].AsTensor<bool>();

                    var partCount = partConfTensor.Dimensions[2];
                    var classCount = idTensor.Dimensions[2];

                    var partThreshold = PartMinConfidence;
                    var idThreshold = IdentityMinConfidence;
                    var centroidThreshold = CentroidMinConfidence;
                    var poseScale = frameBatch.PoseScale;

                    for (int i = 0; i < instanceCount; i++)
                    {
                        var centroidConfidence = centroidConfidenceTensor.GetValue(i);
                        if (centroidConfidence < centroidThreshold || !instanceValidTensor.GetValue(i))
                            continue;

                        var pose = new PoseIdentity(frames.Length == 1 ? frames[0] : frames[i], exportMetadata);
                        var centroid = new BodyPart();
                        centroid.Name = exportMetadata.AnchorPart;
                        centroid.Confidence = centroidConfidence;
                        centroid.Position = new Point2f(
                            x: (float)centroidTensor.GetValue(i * 2) * poseScale.X,
                            y: (float)centroidTensor.GetValue(i * 2 + 1) * poseScale.Y);
                        pose.Centroid = centroid;
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
                            pose.Identity = exportMetadata.ClassNames[maxIndex];
                        }

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
                                    x: (float)poseTensor.GetValue(i * partCount * 2 + j * 2) * poseScale.X,
                                    y: (float)poseTensor.GetValue(i * partCount * 2 + j * 2 + 1) * poseScale.Y);
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
