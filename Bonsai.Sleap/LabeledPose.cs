using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class LabeledPose : Pose
    {
        public LabeledPose(IplImage image)
            : base(image)
        {
        }

        public string Label { get; set; }

        public float Confidence { get; set; }
    }
}
