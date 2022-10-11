using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class LabeledPoseCollection : Collection<LabeledPose>
    {
        public LabeledPoseCollection(IplImage image)
        {
            Image = image;
        }

        public LabeledPoseCollection(IList<LabeledPose> list, IplImage image)
            : base(list)
        {
            Image = image;
        }

        public IplImage Image { get; }
    }

}
