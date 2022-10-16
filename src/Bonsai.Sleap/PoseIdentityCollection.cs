using System.Collections.ObjectModel;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents a collection of pose identities extracted from a specified image.
    /// </summary>
    public class PoseIdentityCollection : Collection<PoseIdentity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoseIdentityCollection"/> class
        /// extracted from the specified image.
        /// </summary>
        /// <param name="image">The image from which the pose identities were extracted.</param>
        public PoseIdentityCollection(IplImage image)
        {
            Image = image;
        }

        /// <summary>
        /// Gets the image from which the pose identities were extracted.
        /// </summary>
        public IplImage Image { get; }
    }

}
