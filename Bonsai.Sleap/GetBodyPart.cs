using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    [Description("Returns the body part with the specified name for each pose in the sequence.")]
    public class GetBodyPart : Transform<Pose, BodyPart>
    {
        [Description("The name of the body part.")]
        public string Name { get; set; }

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
