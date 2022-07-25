using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class PoseCollection : Collection<Pose>
    {
        public PoseCollection()
        {
        }
         
        public PoseCollection(IList<Pose> list)
            : base(list)
        {
        }
    }

}
