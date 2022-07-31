﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
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
        public string PoseConfigFileName { get; set; }

        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("The optional confidence threshold used to discard position values.")]
        public float? PartMinConfidence { get; set; }

        [Description("The optional scale factor used to resize video frames for inference.")]
        public float? ScaleFactor { get; set; }

        [Description("The optional color conversion used to prepare RGB video frames for inference.")]
        public ColorConversion? ColorConversion { get; set; }

        IObservable<PoseCollection> Process<TSource>(IObservable<TSource> source, Func<TSource, (IplImage[], Rect)> roiSelector)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                //var config = SingleInstance_ConfigHelper.LoadTrainingConfig(PoseConfigFileName);
                var config = ConfigHelper.LoadTrainingConfig(PoseConfigFileName);

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
                        runner.Fetch(graph["Identity_1"][0]); 
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

                    var partConfTensor = output[0];
                    float[,,] partConfArr = new float[partConfTensor.Shape[0], partConfTensor.Shape[1], partConfTensor.Shape[2]];
                    partConfTensor.GetValue(partConfArr);

                    var poseTensor = output[1];
                    float[,,,] poseArr = new float[poseTensor.Shape[0], poseTensor.Shape[1], poseTensor.Shape[2], poseTensor.Shape[3]];
                    poseTensor.GetValue(poseArr);

                    var PoseCollection = new PoseCollection();
                    var partThreshold = PartMinConfidence;

                    //Loop the available identifications
                    for (int i = 0; i < input.Length; i++)
                    {
                        // Find the class with max score
                        var pose = new Pose(input[i]);

                        // Iterate on the body parts
                        for (int bodyPartIdx = 0; bodyPartIdx < poseArr.GetLength(1); bodyPartIdx++)
                        {
                            BodyPart bodyPart;
                            bodyPart.Name = config.PartNames[bodyPartIdx];
                            bodyPart.Confidence = partConfArr[i,0, bodyPartIdx];
                            if (bodyPart.Confidence < partThreshold)
                            {
                                bodyPart.Position = new Point2f(float.NaN, float.NaN);
                            }
                            else
                            {
                                bodyPart.Position.X = (float)(poseArr[i,0, bodyPartIdx, 0] * poseScale) + offset.X;
                                bodyPart.Position.Y = (float)(poseArr[i,0, bodyPartIdx, 1] * poseScale) + offset.Y;
                            }
                            pose.Add(bodyPart);
                        }
                        PoseCollection.Add(pose);
                    };
                    return PoseCollection;
                });
            });

        }

        public override IObservable<PoseCollection> Process(IObservable<IplImage> source)
        {
            return Process(source, frame => (new IplImage[] { frame }, new Rect(0, 0, 0, 0)));
        }

        public IObservable<PoseCollection> Process(IObservable<IplImage[]> source)
        {
            return Process(source, frame => (frame , new Rect(0, 0, 0, 0)));
        }

        public IObservable<PoseCollection> Process(IObservable<Tuple<IplImage, Rect>> source)
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