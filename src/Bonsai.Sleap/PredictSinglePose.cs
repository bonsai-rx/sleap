using System;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using System.ComponentModel;
using System.Collections.Generic;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that performs markerless, single instance, pose
    /// estimation for each image in the sequence using a SLEAP model.
    /// </summary>
    /// <seealso cref="PredictCentroids"/>
    /// <seealso cref="PredictPoses"/>
    /// <seealso cref="PredictPoseIdentities"/>
    /// <seealso cref="GetBodyPart"/>
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless, single instance, pose estimation for each image in the sequence using a SLEAP model.")]
    public class PredictSinglePose : Transform<IplImage, Pose>
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

        /// <summary>
        /// Performs markerless, single instance, batched pose estimation for each array
        /// of images in an observable sequence using a SLEAP model.
        /// </summary>
        /// <param name="source">The sequence of image batches from which to extract the poses.</param>
        /// <returns>
        /// A sequence of <see cref="Pose"/> collection objects representing the results
        /// of pose estimation for each image batch in the <paramref name="source"/>
        /// sequence.
        /// </returns>
        public IObservable<IList<Pose>> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                var session = RuntimeHelper.ImportModel(ModelFileName, ExecutionProvider, out var exportMetadata);
                if (exportMetadata.ModelType != ModelType.SingleInstance)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.SingleInstance)} model type but found {exportMetadata.ModelType}.");
                }

                var inputName = session.InputMetadata.Keys.First();
                var frameBatch = new FrameBatch(inputName, InputSize, ColorConversion, exportMetadata);

                return source.Select(frames =>
                {
                    frameBatch.Update(frames);
                    using var output = session.Run(frameBatch.Inputs);

                    // SingleInstance outputs: [batch, 1, n_parts] confidence, [batch, 1, n_parts, 2] positions
                    var poseTensor = output[0].AsTensor<float>();
                    var partConfTensor = output[1].AsTensor<float>();
                    var partCount = partConfTensor.Dimensions[1];

                    var poseCollection = new List<Pose>();
                    var partThreshold = PartMinConfidence;

                    for (int i = 0; i < frames.Length; i++)
                    {
                        var pose = new Pose(frames[i], exportMetadata);
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
                                    x: (float)poseTensor.GetValue(i * partCount * 2 + j * 2),
                                    y: (float)poseTensor.GetValue(i * partCount * 2 + j * 2 + 1));
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
        /// Performs markerless, single instance, pose estimation for each image in
        /// an observable sequence using a SLEAP model.
        /// </summary>
        /// <param name="source">The sequence of images from which to extract the pose.</param>
        /// <returns>
        /// A sequence of <see cref="Pose"/> objects representing the result of pose
        /// estimation for each image in the <paramref name="source"/> sequence.
        /// </returns>
        public override IObservable<Pose> Process(IObservable<IplImage> source)
        {
            return Process(source.Select(frame => new IplImage[] { frame })).Select(result => result[0]);
        }
    }
}
