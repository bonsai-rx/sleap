using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that returns a collection of poses where each distinct identity class
    /// has been matched to a single high confidence pose.
    /// </summary>
    /// <remarks>
    /// Each pose can be matched to only one identity class. If no poses are found for a given identity,
    /// a default pose is returned as representative of that identity class.
    /// </remarks>
    [WorkflowElementCategory(ElementCategory.Transform)]
    [Description("Returns a collection of poses where each distinct identity class has been matched to a single high confidence pose.")]
    public class FindPoseIdentityMatching : Transform<PoseIdentityCollection, PoseIdentityCollection>
    {
        /// <summary>
        /// Gets or sets a value specifying the minimum confidence value used to match an identity
        /// class. If no value is specified, identity classes will be matched to poses regardless
        /// of the identity confidence value.
        /// </summary>
        [Range(0, 1)]
        [Editor(DesignTypes.SliderEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the minimum confidence value used to match an identity class. " +
                     "If no value is specified, identity classes will be matched to poses regardless " +
                     "of the identity confidence value.")]
        public float? IdentityMinConfidence { get; set; }

        /// <summary>
        /// Returns a collection of poses where each distinct model class has been matched to
        /// a single high confidence pose.
        /// </summary>
        /// <param name="source">
        /// The sequence of identified poses from which to find the highest confidence identities
        /// for each distinct model class.
        /// </param>
        /// <returns>
        /// A sequence of <see cref="PoseIdentityCollection"/> objects representing
        /// the poses matched to each distinct model class.
        /// </returns>
        public override IObservable<PoseIdentityCollection> Process(IObservable<PoseIdentityCollection> source)
        {
            return source.Select(poses =>
            {
                var model = poses.Model;
                var identityThreshold = IdentityMinConfidence;
                var matchedPoses = new HashSet<PoseIdentity>();
                var bestPoses = new PoseIdentity[model.ClassNames.Count];
                var result = new PoseIdentityCollection(poses.Image, model);

                static float GetMaxConfidence(PoseIdentity pose, PoseIdentity[] bestPoses, out int maxClass)
                {
                    if (bestPoses[pose.IdentityIndex] == null)
                    {
                        maxClass = pose.IdentityIndex;
                        return pose.Confidence;
                    }

                    maxClass = -1;
                    float? maxConfidence = null;
                    for (int i = 0; i < pose.IdentityScores.Length; i++)
                    {
                        if (bestPoses[i] != null)
                            continue;

                        if (maxConfidence == null || pose.IdentityScores[i] > maxConfidence.GetValueOrDefault())
                        {
                            maxClass = i;
                            maxConfidence = pose.IdentityScores[i];
                        }
                    }

                    return maxConfidence.GetValueOrDefault();
                }

                // loop over poses until all classes have been matched or no poses are left
                var loopCount = Math.Min(poses.Count, bestPoses.Length);
                for (int i = 0; i < loopCount; i++)
                {
                    var maxClass = -1;
                    var maxPose = default(PoseIdentity);
                    foreach (var pose in poses)
                    {
                        if (matchedPoses.Contains(pose))
                            continue;

                        var poseConfidence = GetMaxConfidence(pose, bestPoses, out int poseClass);
                        if (!(poseConfidence < identityThreshold) &&
                            (maxPose == null || poseConfidence > maxPose.IdentityScores[maxClass]))
                        {
                            maxClass = poseClass;
                            maxPose = pose;
                        }
                    }

                    if (maxPose != null)
                    {
                        matchedPoses.Add(maxPose);
                        bestPoses[maxClass] = maxPose;
                    }
                }

                for (int i = 0; i < bestPoses.Length; i++)
                {
                    var pose = CreatePose(poses.Image, model, i, bestPoses[i]);
                    result.Add(pose);
                }

                return result;
            });
        }

        static PoseIdentity CreatePose(IplImage image, IModelInfo model, int identityIndex, PoseIdentity poseData)
        {
            var pose = new PoseIdentity(image, model)
            {
                IdentityIndex = identityIndex,
                Identity = model.ClassNames[identityIndex]
            };

            if (poseData != null)
            {
                pose.Confidence = poseData.IdentityScores[identityIndex];
                pose.IdentityScores = poseData.IdentityScores;
                pose.Centroid = poseData.Centroid;
                foreach (var bodyPart in poseData)
                {
                    pose.Add(bodyPart);
                }
            }
            else
            {
                pose.Confidence = float.NaN;
                pose.IdentityScores = new float[model.ClassNames.Count];
                pose.Centroid = GetBodyPart.DefaultBodyPart(model.AnchorName);
                foreach (var partName in model.PartNames)
                {
                    pose.Add(GetBodyPart.DefaultBodyPart(partName));
                }
            }

            return pose;
        }
    }
}
