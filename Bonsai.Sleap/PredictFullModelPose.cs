using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using OpenCV.Net;
using TensorFlow;
using System.ComponentModel;


// TODO:
// CHECK if array is empty and return an empty collection
// CHECK that all images have the same size in the array

namespace Bonsai.Sleap
{
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless pose estimation using a DeepLabCut model on the input image sequence.")]
    public class PredictFullModelPose : Transform<IplImage, IdedPoseCollection>
    {
        [FileNameFilter("Protocol Buffer Files(*.pb)|*.pb")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the exported Protocol Buffer file containing the pretrained DeepLabCut model.")]
        public string ModelFileName { get; set; }

        [FileNameFilter("Config Files(*.json)|*.json|All Files|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        [Description("The path to the configuration json file containing joint labels.")]
        public string PoseConfigFileName { get; set; }

        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("The optional confidence threshold used to assign an identity.")]
        public float? IdentityMinConfidence { get; set; }

        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("The optional confidence threshold used to discard position values.")]
        public float? PartMinConfidence { get; set; }

        [Description("The optional scale factor used to resize video frames for inference.")]
        public float? ScaleFactor { get; set; }

        [Description("The optional color conversion used to prepare RGB video frames for inference.")]
        public ColorConversion? ColorConversion { get; set; } = OpenCV.Net.ColorConversion.Bgr2Gray;

        IObservable<IdedPoseCollection> Process<TSource>(IObservable<TSource> source, Func<TSource, (IplImage[], Rect)> roiSelector)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                
                var config = ConfigHelper.LoadPoseConfig(PoseConfigFileName);

                return source.Select(value =>
                {

                    var poseScale = 1.0;
                    var (input, roi) = roiSelector(value);
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

                    if (tensor == null || tensor.Shape[1] != tensorSize.Height || tensor.Shape[2] != tensorSize.Width)
                    {
                        tensor?.Dispose();
                        runner = session.GetRunner();
                        tensor = TensorHelper.CreatePlaceholder(graph, runner, tensorSize, batchSize);

                        //Batch processing 
                        runner.Fetch(graph["Identity"][0]); //Class identification confidence [batch x trained classes]
                        runner.Fetch(graph["Identity_1"][0]); //Part confidence [batch x parts]
                        runner.Fetch(graph["Identity_2"][0]); //Part position [batch x parts x 2]
                    }

                    // Run 

                    var _frame = TensorHelper.GetRegionOfInterest(input[0], roi, out Point offset);
                    IplImage[] frame = input.Select(im => 
                    {

                        var cFrame = TensorHelper.GetRegionOfInterest(im, roi, out Point _);
                        cFrame = TensorHelper.EnsureFrameSize(cFrame, tensorSize, ref resizeTemp);
                        cFrame = TensorHelper.EnsureColorFormat(cFrame, ColorConversion, ref colorTemp);
                        return cFrame;

                    }).ToArray();

                    TensorHelper.UpdateTensor(tensor, frame);


                    var output = runner.Run();

                    // Fetch the results from output

                    // Class identification
                    var idTensor = output[0];
                    float[,] idArr = new float[idTensor.Shape[0], idTensor.Shape[1]];
                    idTensor.GetValue(idArr);

                    var partConfTensor = output[1];
                    float[,] partConfArr = new float[partConfTensor.Shape[0], partConfTensor.Shape[1]];
                    partConfTensor.GetValue(partConfArr);

                    var poseTensor = output[2];
                    float[,,] poseArr = new float[poseTensor.Shape[0], poseTensor.Shape[1], poseTensor.Shape[2]];
                    poseTensor.GetValue(poseArr);

                    var partThreshold = PartMinConfidence;
                    var idThreshold = IdentityMinConfidence;

                    var identityCollection = new IdedPoseCollection();
                    //Loop the available identifications
                    for (int iid = 0; iid < idArr.GetLength(0); iid++)
                    {

                        IdedPose idedPose;
                        // Collect the confidence on the identity
                        var conf = new float[idArr.GetLength(1)];
                        for (int trainedClass = 0; trainedClass < idArr.GetLength(1); trainedClass++)
                        {
                            conf[trainedClass] = idArr[iid, trainedClass];
                        }
                        idedPose.IdConfidence = conf;
                        // Find the argmax of the confidence tensor
                        var argMaxConfidence = argmax_confidence(conf);
                        if ((conf[argMaxConfidence] < idThreshold) | (argMaxConfidence == -1))
                        {
                            idedPose.IdArgMax = -1;
                            idedPose.IdName = "";
                        }
                        else
                        {
                            idedPose.IdArgMax = argMaxConfidence;
                            
                            idedPose.IdName = config.classes_names[idedPose.IdArgMax];
                        }

                        var result = new Pose(input[iid]);
                        // Iterate on the body parts
                        for (int iBodyPart = 0; iBodyPart < poseArr.GetLength(1); iBodyPart++)
                        {
                            BodyPart bodyPart;
                            bodyPart.Name = config.part_names[iBodyPart];
                            bodyPart.Confidence = (float)partConfArr[iid, iBodyPart];
                            if (bodyPart.Confidence < partThreshold)
                            {
                                bodyPart.Position = new Point2f(float.NaN, float.NaN);
                            }
                            else
                            {
                                bodyPart.Position.X = (float)(poseArr[iid, iBodyPart, 0] * poseScale) + offset.X;
                                bodyPart.Position.Y = (float)(poseArr[iid, iBodyPart, 1] * poseScale) + offset.Y;
                            }
                            result.Add(bodyPart);
                        }
                        idedPose.Pose = result;
                        identityCollection.Add(idedPose);

                    };
                    return identityCollection;

                });
            });

        }


        public override IObservable<IdedPoseCollection> Process(IObservable<IplImage> source)
        {
            return Process(source, frame => (new IplImage[] { frame }, new Rect(0, 0, 0, 0)));
        }

        public IObservable<IdedPoseCollection> Process(IObservable<IplImage[]> source)
        {
            return Process(source, frame => (frame , new Rect(0, 0, 0, 0)));
        }

        public IObservable<IdedPoseCollection> Process(IObservable<Tuple<IplImage, Rect>> source)
        {
            return Process(source, input => (new IplImage[] { input.Item1 }, input.Item2));
        }

        public static int argmax_confidence<T>(IEnumerable<T> seq) where T : IComparable<T>
        {
            if (!seq.Any()) return -1;
            T max = seq.First();
            int maxIdx = 0;
            int idx = 1;

            foreach (T item in seq.Skip(1))
            {
                if (max.CompareTo(item) < 0)
                {
                    max = item;
                    maxIdx = idx;
                }
                ++idx;
            }

            return maxIdx;
        }
    }


}
