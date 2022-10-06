using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
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
                if (string.IsNullOrEmpty(label))
                {
                    var topConfIdx = ArgMax(poses.Select(x => x.Confidence).ToArray(), Comparer<float>.Default, out float maxScore);
                    return topConfIdx >= 0 ? poses[topConfIdx] : DefaultPose(poses.Image, string.Empty);
                }
                else // do a search for the label
                {
                    var filtposes = poses.Where(x => x.Label == label).ToList();
                    if (!filtposes.Any()){return DefaultPose(poses.Image, label);}
                    else
                    {
                        var topConfIdx = ArgMax(filtposes.Select(x => x.Confidence).ToArray(), Comparer<float>.Default, out float _);
                        return topConfIdx >= 0 ? filtposes[topConfIdx] : DefaultPose(poses.Image, label);
                    }
                }
            });
        }

        static int ArgMax<TElement>(TElement[] array, IComparer<TElement> comparer, out TElement maxValue)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            int maxIndex = -1;
            maxValue = default;
            for (int i = 0; i < array.Count(); i++)
            {
                if (i == 0 || comparer.Compare(array[i], maxValue) > 0)
                {
                    maxIndex = i;
                    maxValue = array[i];
                }
            }
            return maxIndex;
        }

        private static LabeledPose DefaultPose(IplImage image, String label)
        {
            var pose = new LabeledPose(image)
            {
                Confidence = float.NaN,
                Label = label,
                Centroid = new Centroid(image)
                {
                    Position = new Point2f(float.NaN, float.NaN),
                    Confidence = float.NaN,
                    Name = null
                }
            };
            return pose;
        }
    }
}
