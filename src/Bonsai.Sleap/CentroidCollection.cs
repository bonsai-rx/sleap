using System.Collections.ObjectModel;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents a collection of image centroids.
    /// </summary>
    public class CentroidCollection : Collection<Centroid>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CentroidCollection"/> class
        /// extracted from the specified image.
        /// </summary>
        /// <param name="image">The image from which the centroids were extracted.</param>
        public CentroidCollection(IplImage image)
        {
            Image = image;
        }

        /// <summary>
        /// Gets the image from which the centroids were extracted.
        /// </summary>
        public IplImage Image { get; }
    }
}
