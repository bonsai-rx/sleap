using OpenCV.Net;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public struct IdedPose
    {
        public int IdArgMax;
        public string IdName;
        public float[] IdConfidence;
        public Pose Pose;

    }
}
