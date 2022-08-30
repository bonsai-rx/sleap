using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class CentroidCollection : Collection<Centroid>
    {
        public CentroidCollection()
        {
        }

        public CentroidCollection(IList<Centroid> list)
            : base(list)
        {
        }
    }
}
