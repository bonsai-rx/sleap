using System.Collections.ObjectModel;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class CentroidCollection : Collection<Centroid>
    {
        public CentroidCollection(IplImage image)
        {
            Image = image;
        }

        public IplImage Image { get; }
    }
}
