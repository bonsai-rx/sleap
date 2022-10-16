using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    [WorkflowElementCategory(ElementCategory.Transform)]
    [Description("Returns the labeled pose with the highest identity confidence, using the optional class label.")]
    public class GetMaximumConfidenceLabel : Transform<LabeledPoseCollection, LabeledPose>
    {
        [Description("The class label used to filter the labeled pose collection.")]
        public string Label { get; set; }

        public override IObservable<LabeledPose> Process(IObservable<LabeledPoseCollection> source)
        {
            return source.Select(poses =>
            {
                var label = Label;
                int maxIndex = -1;
                for (int i = 0; i < poses.Count; i++)
                {
                    var pose = poses[i];
                    if (!string.IsNullOrEmpty(label) && pose.Label != label)
                    {
                        continue;
                    }

                    if (maxIndex < 0 || pose.Confidence > poses[maxIndex].Confidence)
                    {
                        maxIndex = i;
                    }
                }

                return maxIndex < 0 ? DefaultPose(poses.Image, label) : poses[maxIndex];
            });
        }

        static LabeledPose DefaultPose(IplImage image, string label)
        {
            return new LabeledPose(image)
            {
                Label = label,
                Confidence = float.NaN,
                Centroid = GetBodyPart.DefaultBodyPart(string.Empty)
            };
        }
    }
}
