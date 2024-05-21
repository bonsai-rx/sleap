using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using TensorFlow;

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
        /// Gets or sets a value specifying the path to the exported Protocol Buffer
        /// file containing the pretrained SLEAP model.
        /// </summary>
        [FileNameFilter("Protocol Buffer Files(*.pb)|*.pb")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("Specifies the path to the exported Protocol Buffer file containing the pretrained SLEAP model.")]
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

        private IObservable<CentroidCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadTrainingConfig(TrainingConfig);
                var ragged = graph["Identity_6"] != null;

                if (config.ModelType != ModelType.Centroid)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.Centroid)} model type but found {config.ModelType} .");
                }

                return source.Select(input =>
                {
                    var poseScale = 1.0;
                    int colorChannels = (ColorConversion is null) ? input[0].Channels : ExtensionMethods.GetConversionNumChannels((ColorConversion)ColorConversion);
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

                    if (tensor == null || tensor.Shape[0] != batchSize || tensor.Shape[1] != tensorSize.Height || tensor.Shape[2] != tensorSize.Width)
                    {
                        tensor?.Dispose();
                        runner = session.GetRunner();
                        tensor = TensorHelper.CreatePlaceholder(graph, runner, tensorSize, batchSize, colorChannels);

                        if (ragged)
                        {
                            // ragged version of the frozen graph
                            runner.Fetch(graph["Identity"][0]);
                            runner.Fetch(graph["Identity_2"][0]);
                        }
                        else
                        {
                            // unragged version of the frozen graph
                            runner.Fetch(graph["Identity"][0]);
                            runner.Fetch(graph["Identity_1"][0]);
                        }
                    }

                    var frames = Array.ConvertAll(input, frame =>
                    {
                        frame = TensorHelper.EnsureFrameSize(frame, tensorSize, ref resizeTemp);
                        frame = TensorHelper.EnsureColorFormat(frame, ColorConversion, ref colorTemp, colorChannels);
                        return frame;
                    });
                    TensorHelper.UpdateTensor(tensor, colorChannels, frames);
                    var output = runner.Run();

                    var shapeIdx = ragged ? 0 : 1;
                    var centroidCollection = new CentroidCollection(input[0]);
                    if (output[0].Shape[shapeIdx] == 0) return centroidCollection;
                    else
                    {
                        // Fetch the results from output
                        var centroidConfidenceTensor = output[0];
                        float[] centroidConfArr = new float[centroidConfidenceTensor.Shape[shapeIdx]];
                        centroidConfidenceTensor.GetValue(centroidConfArr);

                        var centroidTensor = output[1];
                        float[,] centroidArr = new float[centroidTensor.Shape[shapeIdx], centroidTensor.Shape[shapeIdx + 1]];
                        centroidTensor.GetValue(centroidArr);

                        var confidenceThreshold = CentroidMinConfidence;
                        for (int i = 0; i < centroidConfArr.GetLength(0); i++)
                        {
                            //TODO: batch centroid estimation is not currently supported
                            var centroid = new Centroid(input[0]);
                            centroid.Name = config.AnchorName;
                            centroid.Confidence = centroidConfArr[i];

                            if (centroid.Confidence < confidenceThreshold)
                            {
                                centroid.Position = new Point2f(float.NaN, float.NaN);
                            }
                            else
                            {
                                centroid.Position = new Point2f(
                                    (float)(centroidArr[i, 0] * poseScale),
                                    (float)(centroidArr[i, 1] * poseScale));
                            }
                            centroidCollection.Add(centroid);
                        };
                        return centroidCollection;
                    }
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
