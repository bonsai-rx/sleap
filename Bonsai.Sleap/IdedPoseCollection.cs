using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class IdedPoseCollection : Collection<IdedPose>
    {

        public IdedPoseCollection(IList<IdedPose> idedposes, bool wasBatch)
            : base(idedposes)
        {
            RanBatch = wasBatch;
        }

        public IdedPoseCollection(bool wasBatch)
        {
            RanBatch = wasBatch;
        }

        public bool RanBatch { get; private set; }
    }

}
