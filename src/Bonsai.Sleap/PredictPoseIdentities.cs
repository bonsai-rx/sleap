using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using OpenCV.Net;
using TensorFlow;
using System.ComponentModel;

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

        private IObservable<PoseIdentityCollection> Process(IObservable<IplImage[]> source)
        {
            return Observable.Defer(() =>
            {
                IplImage resizeTemp = null;
                IplImage colorTemp = null;
                TFTensor tensor = null;
                TFSession.Runner runner = null;
                var graph = TensorHelper.ImportModel(ModelFileName, out TFSession session);
                var config = ConfigHelper.LoadTrainingConfig(TrainingConfig);

                if (config.ModelType != ModelType.MultiClass)
                {
                    throw new UnexpectedModelTypeException($"Expected {nameof(ModelType.MultiClass)} model type but found {config.ModelType} .");
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
                        runner.Fetch(graph["Identity_5"][0]);
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

                    var identityCollection = new PoseIdentityCollection(input[0]);
                    if (output[0].Shape[0] == 0) return identityCollection;
                    else
                    {
                        // Fetch the results from output
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

                        var idTensor = output[4];
                        float[,] idArr = new float[idTensor.Shape[0], idTensor.Shape[1]];
                        idTensor.GetValue(idArr);

                        var partThreshold = PartMinConfidence;
                        var idThreshold = IdentityMinConfidence;
                        var centroidThreshold = CentroidMinConfidence;

                        for (int iid = 0; iid < idArr.GetLength(0); iid++)
                        {
                            // Find the class with max score
                            var pose = new PoseIdentity(input.Length == 1 ? input[0] : input[iid]);
                            var maxIndex = ArgMax(idArr, iid, Comparer<float>.Default, out float maxScore);
                            pose.Confidence = maxScore;
                            if (maxScore < idThreshold || maxIndex < 0)
                            {
                                pose.Identity = string.Empty;
                            }
                            else pose.Identity = config.ClassNames[maxIndex];

                            var centroid = new BodyPart();
                            centroid.Name = config.AnchorName;
                            centroid.Confidence = centroidConfArr[0];
                            if (centroid.Confidence < centroidThreshold)
                            {
                                centroid.Position = new Point2f(float.NaN, float.NaN);
                            }
                            else
                            {
                                centroid.Position = new Point2f(
                                    x: (float)(centroidArr[iid, 0] * poseScale),
                                    y: (float)(centroidArr[iid, 1] * poseScale));
                            }
                            pose.Centroid = centroid;

                            // Iterate on the body parts
                            for (int bodyPartIdx = 0; bodyPartIdx < poseArr.GetLength(1); bodyPartIdx++)
                            {
                                var bodyPart = new BodyPart();
                                bodyPart.Name = config.PartNames[bodyPartIdx];
                                bodyPart.Confidence = partConfArr[iid, bodyPartIdx];
                                if (bodyPart.Confidence < partThreshold)
                                {
                                    bodyPart.Position = new Point2f(float.NaN, float.NaN);
                                }
                                else
                                {
                                    bodyPart.Position = new Point2f(
                                        x: (float)(poseArr[iid, bodyPartIdx, 0] * poseScale),
                                        y: (float)(poseArr[iid, bodyPartIdx, 1] * poseScale));
                                }
                                pose.Add(bodyPart);
                            }
                            identityCollection.Add(pose);
                        };
                        return identityCollection;
                    }
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
