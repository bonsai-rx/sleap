using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents an operator that returns the body part with the specified
    /// name for each pose in the sequence.
    /// </summary>
    [Description("Returns the body part with the specified name for each pose in the sequence.")]
    public class GetBodyPart : Transform<Pose, BodyPart>
    {
        /// <summary>
        /// Gets or sets the name of the body part to locate in each pose object.
        /// </summary>
        [Description("The name of the body part to locate in each pose object.")]
        public string Name { get; set; }

        /// <summary>
        /// Returns the body part with the specified name for each pose in an
        /// observable sequence.
        /// </summary>
        /// <param name="source">The sequence of poses for which to locate the body part.</param>
        /// <returns>
        /// A sequence of <see cref="BodyPart"/> objects representing the location
        /// of the body part with the specified name. If no body part with the
        /// specified name is found, a default value is returned.
        /// </returns>
        public override IObservable<BodyPart> Process(IObservable<Pose> source)
        {
            return source.Select(pose =>
            {
                var name = Name;
                return pose.Contains(name) ? pose[name] : DefaultBodyPart(name);
            });
        }

        internal static BodyPart DefaultBodyPart(string name)
        {
            return new BodyPart
            {
                Name = name,
                Position = new Point2f(float.NaN, float.NaN),
                Confidence = 0
            };
        }
    }
}
