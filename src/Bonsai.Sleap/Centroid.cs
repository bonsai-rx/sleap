using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class Centroid : BodyPart
    {
        public Centroid(IplImage image)
        {
            Image = image;
        }

        public IplImage Image { get; }
    }
}
