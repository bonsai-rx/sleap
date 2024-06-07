using System.Collections.Generic;
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
        /// <param name="model">Information about the model used to extract the pose identities.</param>
        public PoseIdentityCollection(IplImage image, IModelInfo model)
        {
            Image = image;
            Model = model;
        }

        /// <summary>
        /// Gets the image from which the pose identities were extracted.
        /// </summary>
        public IplImage Image { get; }

        /// <summary>
        /// Gets information about the model used to extract the pose identities.
        /// </summary>
        public IModelInfo Model { get; }
    }

}
