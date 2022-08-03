using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using TensorFlow;

namespace Bonsai.Sleap
{
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs multi- markerless pose estimation using a SLEAP model on the input image sequence.")]
    public class PredictCentroids : Transform<IplImage, InferedCentroidCollection>
    {
        [FileNameFilter("Protocol Buffer Files(*.pb)|*.pb")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the exported Protocol Buffer file containing the pretrained SLEAP model.")]
        public string ModelFileName { get; set; }

        [FileNameFilter("Config Files(*.json)|*.json|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the configuration json file containing joint labels.")]
        public string TrainingConfig { get; set; }

        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("The optional confidence threshold used to discard position values.")]
        public float? CentroidMinConfidence { get; set; }

        [Description("The optional scale factor used to resize video frames for inference.")]
        public float? ScaleFactor { get; set; }

        [Description("The optional color conversion used to prepare RGB video frames for inference.")]
        public ColorConversion? ColorConversion { get; set; }

        IObservable<InferedCentroidCollection> Process<TSource>(IObservable<TSource> source, Func<TSource, (IplImage[], Rect)> roiSelector)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadTrainingConfig(TrainingConfig);

                if (config.ModelType != ConfigHelper.ModelType.Centroid)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ConfigHelper.ModelType.Centroid)} model type but found {config.ModelType} .");
                }

                return source.Select(value =>
                {
                    var poseScale = 1.0;
                    var (input, roi) = roiSelector(value);
                    int colorChannels = (ColorConversion is null) ? input[0].Channels : ExtensionMethods.GetConversionNumChannels((ColorConversion)ColorConversion);
                    var tensorSize = roi.Width > 0 && roi.Height > 0 ? new Size(roi.Width, roi.Height) : input[0].Size;
                    var batchSize = input.Length;
                    var scaleFactor = ScaleFactor;
                    
                    if (scaleFactor.HasValue)
                    {
                        poseScale = scaleFactor.Value;
                        tensorSize.Width = (int)(tensorSize.Width * poseScale);
                        tensorSize.Height = (int)(tensorSize.Height * poseScale);
                        poseScale = 1.0 / poseScale;
                    }

                    if (tensor == null || tensor.Shape[0] != batchSize || tensor.Shape[1] != tensorSize.Height || tensor.Shape[2] != tensorSize.Width )
                    {
                        tensor?.Dispose();
                        runner = session.GetRunner();
                        tensor = TensorHelper.CreatePlaceholder(graph, runner, tensorSize, batchSize, colorChannels);

                        runner.Fetch(graph["Identity"][0]);
                        runner.Fetch(graph["Identity_2"][0]);
                    }

                    var _frame = TensorHelper.GetRegionOfInterest(input[0], roi, out Point offset);
                    IplImage[] frame = input.Select(im => 
                    {
                        var cFrame = TensorHelper.GetRegionOfInterest(im, roi, out Point _);
                        cFrame = TensorHelper.EnsureFrameSize(cFrame, tensorSize, ref resizeTemp);
                        cFrame = TensorHelper.EnsureColorFormat(cFrame, ColorConversion, ref colorTemp, colorChannels);
                        return cFrame;

                    }).ToArray();
                    TensorHelper.UpdateTensor(tensor, colorChannels, frame);
                    var output = runner.Run();

                    // Fetch the results from output
                    var centroidConfidenceTensor = output[0];
                    float[] centroidConfArr = new float[centroidConfidenceTensor.Shape[0]];
                    centroidConfidenceTensor.GetValue(centroidConfArr);

                    var centroidTensor = output[1];
                    float[,] centroidArr = new float[centroidTensor.Shape[0], centroidTensor.Shape[1]];
                    centroidTensor.GetValue(centroidArr);

                    var centroidPoseCollection = new InferedCentroidCollection();
                    var confidenceThreshold = CentroidMinConfidence;

                    for (int i = 0; i < centroidConfArr.GetLength(0); i++)
                    {
                        //TODO not sure what to do here if multiple images are given....
                        var centroid = new InferedCentroid(input[0]);
                        centroid.Confidence = centroidConfArr[i];

                        if (centroid.Confidence < confidenceThreshold)
                        {
                            centroid.Centroid = new Point2f(float.NaN, float.NaN);
                        }
                        else
                        {
                            centroid.Centroid = new Point2f(
                                 (float)(centroidArr[i, 0] * poseScale) + offset.X,
                                 (float)(centroidArr[i, 1] * poseScale) + offset.Y
                                );
                        }
                        centroidPoseCollection.Add(centroid);
                    };
                    return centroidPoseCollection;
                });
            });
        }

        public override IObservable<InferedCentroidCollection> Process(IObservable<IplImage> source)
        {
            return Process(source, frame => (new IplImage[] { frame }, new Rect(0, 0, 0, 0)));
        }

        public IObservable<InferedCentroidCollection> Process(IObservable<IplImage[]> source)
        {
            return Process(source, frame => (frame , new Rect(0, 0, 0, 0)));
        }

        public IObservable<InferedCentroidCollection> Process(IObservable<Tuple<IplImage, Rect>> source)
        {
            return Process(source, input => (new IplImage[] { input.Item1 }, input.Item2));
        }

        static int ArgMax<TElement>(TElement[,] array, int instance, IComparer<TElement> comparer, out TElement maxValue)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            int maxIndex = -1;
            maxValue = default;
            for (int i = 0; i < array.GetLength(1); i++)
            {
                if (i == 0 || comparer.Compare(array[instance, i], maxValue) > 0)
                {
                    maxIndex = i;
                    maxValue = array[instance, i];
                }
            }
            return maxIndex;
        }
    }
}
