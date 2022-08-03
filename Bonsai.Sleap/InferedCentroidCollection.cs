using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class InferedCentroidCollection : Collection<InferedCentroid>
    {
        public InferedCentroidCollection()
        {
        }

        public InferedCentroidCollection(IList<InferedCentroid> list)
            : base(list)
        {
        }
    }

}
