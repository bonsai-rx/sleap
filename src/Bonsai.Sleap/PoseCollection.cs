using System.Collections.Generic;
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
        /// <param name="model">Information about the model used to extract the poses.</param>
        public PoseCollection(IplImage image, IModelInfo model)
        {
            Image = image;
            Model = model;
        }

        /// <summary>
        /// Gets the image from which the poses were extracted.
        /// </summary>
        public IplImage Image { get; }

        /// <summary>
        /// Gets information about the model used to extract the poses.
        /// </summary>
        public IModelInfo Model { get; }
    }
}
