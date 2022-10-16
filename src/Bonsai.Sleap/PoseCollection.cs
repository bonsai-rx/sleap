using System.Collections.ObjectModel;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents a collection of poses extracted from a specified image.
    /// </summary>
    public class PoseCollection : Collection<Pose>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoseCollection"/> class
        /// extracted from the specified image.
        /// </summary>
        /// <param name="image">The image from which the poses were extracted.</param>
        public PoseCollection(IplImage image)
        {
            Image = image;
        }

        /// <summary>
        /// Gets the image from which the poses were extracted.
        /// </summary>
        public IplImage Image { get; }
    }
}
