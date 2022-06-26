using OpenCV.Net;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public struct IdedPose
    {
        public int IdArgMax;
        public float MaxIdConfidence;
        public string IdName;
        public float[] IdLayerOutput;
        public Pose Pose;

    }
}
