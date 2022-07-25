using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class LabeledPoseCollection : Collection<LabeledPose>
    {
        public LabeledPoseCollection()
        {
        }

        public LabeledPoseCollection(IList<LabeledPose> list)
            : base(list)
        {
        }
    }

}
