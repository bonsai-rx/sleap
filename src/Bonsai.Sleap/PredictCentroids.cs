using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

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

        private IObservable<CentroidCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                var session = RuntimeHelper.ImportModel(ModelFileName, ExecutionProvider, out var exportMetadata);
                if (exportMetadata.ModelType != ModelType.Centroid)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.Centroid)} model type but found {exportMetadata.ModelType}.");
                }

                var inputName = session.InputMetadata.Keys.First();
                var frameBatch = new FrameBatch(inputName, InputSize, ColorConversion, exportMetadata);

                return source.Select(frames =>
                {
                    frameBatch.Update(frames);
                    using var output = session.Run(frameBatch.Inputs);

                    var centroidCollection = new CentroidCollection(frames[0]);
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
                                Name = exportMetadata.AnchorPart,
                                Position = new Point2f(
                                    (float)centroidTensor[0, i, 0],
                                    (float)centroidTensor[0, i, 1]),
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
