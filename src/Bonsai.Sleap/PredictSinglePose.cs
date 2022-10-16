using System;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;
using TensorFlow;
using System.ComponentModel;
using System.Collections.Generic;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that performs markerless, single instance, pose
    /// estimation for each image in the sequence using a SLEAP model.
    /// </summary>
    [DefaultProperty(nameof(ModelFileName))]
    [Description("Performs markerless, single instance, pose estimation for each image in the sequence using a SLEAP model.")]
    public class PredictSinglePose : Transform<IplImage, Pose>
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
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadTrainingConfig(TrainingConfig);

                if (config.ModelType != ModelType.SingleInstance)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.SingleInstance)} model type but found {config.ModelType} .");
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
                        runner.Fetch(graph["Identity_1"][0]);
                    }

                    var frames = Array.ConvertAll(input, frame => 
                    {
                        frame = TensorHelper.EnsureFrameSize(frame, tensorSize, ref resizeTemp);
                        frame = TensorHelper.EnsureColorFormat(frame, ColorConversion, ref colorTemp, colorChannels);
                        return frame;
                    });
                    TensorHelper.UpdateTensor(tensor, colorChannels, frames);
                    var output = runner.Run();

                    var partConfTensor = output[0];
                    float[,,] partConfArr = new float[partConfTensor.Shape[0], partConfTensor.Shape[1], partConfTensor.Shape[2]];
                    partConfTensor.GetValue(partConfArr);

                    var poseTensor = output[1];
                    float[,,,] poseArr = new float[poseTensor.Shape[0], poseTensor.Shape[1], poseTensor.Shape[2], poseTensor.Shape[3]];
                    poseTensor.GetValue(poseArr);

                    var poseCollection = new List<Pose>();
                    var partThreshold = PartMinConfidence;

                    //Loop the available identifications
                    for (int i = 0; i < input.Length; i++)
                    {
                        var pose = new Pose(input[i]);
                        // Iterate on the body parts
                        for (int bodyPartIdx = 0; bodyPartIdx < poseArr.GetLength(2); bodyPartIdx++)
                        {
                            var bodyPart = new BodyPart();
                            bodyPart.Name = config.PartNames[bodyPartIdx];
                            bodyPart.Confidence = partConfArr[i,0, bodyPartIdx];
                            if (bodyPart.Confidence < partThreshold)
                            {
                                bodyPart.Position = new Point2f(float.NaN, float.NaN);
                            }
                            else
                            {
                                bodyPart.Position = new Point2f(
                                    x: (float)(poseArr[i, 0, bodyPartIdx, 0] * poseScale),
                                    y: (float)(poseArr[i, 0, bodyPartIdx, 1] * poseScale));
                            }
                            pose.Add(bodyPart);
                        }
                        poseCollection.Add(pose);
                    };
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
