using System.Collections.ObjectModel;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class PoseCollection : Collection<Pose>
    {
        public PoseCollection(IplImage image)
        {
            Image = image;
        }

        public IplImage Image { get; }
    }
}
