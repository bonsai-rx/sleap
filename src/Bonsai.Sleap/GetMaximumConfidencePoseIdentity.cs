using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that returns the pose with the highest identity
    /// confidence for each collection in the sequence, using the optional class label.
    /// </summary>
    [WorkflowElementCategory(ElementCategory.Transform)]
    [Description("Returns the pose with the highest identity confidence, using the optional class label.")]
    public class GetMaximumConfidencePoseIdentity : Transform<PoseIdentityCollection, PoseIdentity>
    {
        /// <summary>
        /// Gets or sets the optional class label used to filter the pose collection.
        /// </summary>
        [Description("The optional class label used to filter the pose collection.")]
        public string Identity { get; set; }

        /// <summary>
        /// Returns the pose with the highest identity confidence for each pose collection
        /// in an observable sequence, using the optional class label.
        /// </summary>
        /// <param name="source">
        /// The sequence of identified poses for which to extract the identity with highest
        /// confidence score.
        /// </param>
        /// <returns>
        /// A sequence of the poses with highest identity confidence for each pose collection
        /// in the <paramref name="source"/> sequence.
        /// </returns>
        public override IObservable<PoseIdentity> Process(IObservable<PoseIdentityCollection> source)
        {
            return source.Select(poses =>
            {
                var identity = Identity;
                int maxIndex = -1;
                for (int i = 0; i < poses.Count; i++)
                {
                    var pose = poses[i];
                    if (!string.IsNullOrEmpty(identity) && pose.Identity != identity)
                    {
                        continue;
                    }

                    if (maxIndex < 0 || pose.Confidence > poses[maxIndex].Confidence)
                    {
                        maxIndex = i;
                    }
                }

                return maxIndex < 0 ? DefaultPose(poses.Image, identity, poses.Model) : poses[maxIndex];
            });
        }

        static PoseIdentity DefaultPose(IplImage image, string identity, IModelInfo model)
        {
            var pose = new PoseIdentity(image, model)
            {
                Identity = identity,
                Confidence = float.NaN,
                IdentityIndex = -1,
                IdentityScores = new float[model.ClassNames.Count],
                Centroid = GetBodyPart.DefaultBodyPart(model.AnchorName)
            };

            foreach (var partName in model.PartNames)
            {
                pose.Add(GetBodyPart.DefaultBodyPart(partName));
            }

            return pose;
        }
    }
}
