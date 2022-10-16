using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents a center point used for cropping.
    /// </summary>
    public class Centroid : BodyPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Centroid"/> class
        /// extracted from the specified image.
        /// </summary>
        /// <param name="image">The image from which the centroid was extracted.</param>
        public Centroid(IplImage image)
        {
            Image = image;
        }

        /// <summary>
        /// Gets the image from which the centroid was extracted.
        /// </summary>
        public IplImage Image { get; }
    }
}
