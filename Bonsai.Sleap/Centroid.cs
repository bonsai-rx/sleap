using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class Centroid
    {
        public Centroid(IplImage image)
        {
            Image = image;
        }

        public IplImage Image { get; private set; }

        public string Name { get; set; }

        public Point2f Position { get; set; }

        public float Confidence { get; set; }

    }

}



