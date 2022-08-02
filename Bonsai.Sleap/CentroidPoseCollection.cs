using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class CentroidPoseCollection : Collection<InferedCentroid>
    {
        public CentroidPoseCollection()
        {
        }

        public CentroidPoseCollection(IList<InferedCentroid> list)
            : base(list)
        {
        }
    }

}
