using System;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using TensorFlow;
using System.ComponentModel;

namespace Bonsai.Sleap
{
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless, single instance, pose estimation using a SLEAP model on the input image sequence.")]
    public class PredictPoses : Transform<IplImage, PoseCollection>
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

        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("The optional confidence threshold used to discard position values.")]
        public float? PartMinConfidence { get; set; }

        [Description("The optional scale factor used to resize video frames for inference.")]
        public float? ScaleFactor { get; set; }

        [Description("The optional color conversion used to prepare RGB video frames for inference.")]
        public ColorConversion? ColorConversion { get; set; }

        public IObservable<PoseCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadTrainingConfig(TrainingConfig);

                if (config.ModelType != ModelType.CenteredInstance)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.CenteredInstance)} model type but found {config.ModelType} .");
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

                    if (tensor == null || tensor.Shape[0] != batchSize || tensor.Shape[1] != tensorSize.Height || tensor.Shape[2] != tensorSize.Width )
                    {
                        tensor?.Dispose();
                        runner = session.GetRunner();
                        tensor = TensorHelper.CreatePlaceholder(graph, runner, tensorSize, batchSize, colorChannels);

                        runner.Fetch(graph["Identity"][0]);
                        runner.Fetch(graph["Identity_2"][0]);
                        runner.Fetch(graph["Identity_4"][0]);
                        runner.Fetch(graph["Identity_6"][0]);

                    }

                    var frames = Array.ConvertAll(input, frame => 
                    {
                        frame = TensorHelper.EnsureFrameSize(frame, tensorSize, ref resizeTemp);
                        frame = TensorHelper.EnsureColorFormat(frame, ColorConversion, ref colorTemp, colorChannels);
                        return frame;

                    });
                    TensorHelper.UpdateTensor(tensor, colorChannels, frames);
                    var output = runner.Run();

                    var poseCollection = new PoseCollection(input[0]);
                    if (output[0].Shape[0] == 0) return poseCollection;
                    else
                    {
                        var centroidConfidenceTensor = output[0];
                        float[] centroidConfArr = new float[centroidConfidenceTensor.Shape[0]];
                        centroidConfidenceTensor.GetValue(centroidConfArr);

                        var centroidTensor = output[1];
                        float[,] centroidArr = new float[centroidTensor.Shape[0], centroidTensor.Shape[1]];
                        centroidTensor.GetValue(centroidArr);

                        var partConfTensor = output[2];
                        float[,] partConfArr = new float[partConfTensor.Shape[0], partConfTensor.Shape[1]];
                        partConfTensor.GetValue(partConfArr);

                        var poseTensor = output[3];
                        float[,,] poseArr = new float[poseTensor.Shape[0], poseTensor.Shape[1], poseTensor.Shape[2]];
                        poseTensor.GetValue(poseArr);

                        var partThreshold = PartMinConfidence;
                        var centroidThreshold = CentroidMinConfidence;

                        //Loop the available identifications
                        for (int i = 0; i < input.Length; i++)
                        {
                            var pose = new Pose(input[0]);
                            var centroid = new BodyPart();
                            centroid.Name = string.Empty;
                            centroid.Confidence = centroidConfArr[0];
                            if (centroid.Confidence < centroidThreshold)
                            {
                                centroid.Position = new Point2f(float.NaN, float.NaN);
                            }
                            else
                            {
                                centroid.Position = new Point2f(
                                    (float)(centroidArr[i, 0] * poseScale),
                                    (float)(centroidArr[i, 1] * poseScale));
                            }
                            pose.Centroid = centroid;

                            // Iterate on the body parts
                            for (int bodyPartIdx = 0; bodyPartIdx < poseArr.GetLength(1); bodyPartIdx++)
                            {
                                var bodyPart = new BodyPart();
                                bodyPart.Name = config.PartNames[bodyPartIdx];
                                bodyPart.Confidence = partConfArr[i, bodyPartIdx];
                                if (bodyPart.Confidence < partThreshold)
                                {
                                    bodyPart.Position = new Point2f(float.NaN, float.NaN);
                                }
                                else
                                {
                                    bodyPart.Position.X = (float)(poseArr[i, bodyPartIdx, 0] * poseScale);
                                    bodyPart.Position.Y = (float)(poseArr[i, bodyPartIdx, 1] * poseScale);
                                }
                                pose.Add(bodyPart);
                            }
                            poseCollection.Add(pose);
                        };
                        return poseCollection;
                    }
                });
            });
        }

        public override IObservable<PoseCollection> Process(IObservable<IplImage> source)
        {
            return Process(source.Select(frame => new IplImage[] { frame }));
        }
    }
}
