using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class IdedPoseCollection : Collection<IdedPose>
    {

        public IdedPoseCollection(IList<IdedPose> idedposes)
            : base(idedposes)
        {
        }
        public IdedPoseCollection()
        {
        }
    }

}
