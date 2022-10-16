using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class PoseIdentityCollection : Collection<PoseIdentity>
    {
        public PoseIdentityCollection(IplImage image)
        {
            Image = image;
        }

        public PoseIdentityCollection(IList<PoseIdentity> list, IplImage image)
            : base(list)
        {
            Image = image;
        }

        public IplImage Image { get; }
    }

}
