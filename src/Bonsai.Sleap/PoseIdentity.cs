using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class PoseIdentity : Pose
    {
        public PoseIdentity(IplImage image)
            : base(image)
        {
        }

        public string Identity { get; set; }

        public float Confidence { get; set; }
    }
}
