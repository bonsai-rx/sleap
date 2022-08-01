using OpenCV.Net;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class InferedCentroid
    {
        public InferedCentroid(IplImage image)
        {
            Image = image;
        }

        public IplImage Image { get; private set; }

        public Point2f Centroid { get; set; }

        public float Confidence { get; set; }
    }

}



