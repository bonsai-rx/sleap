using System.Xml.Serialization;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents the result of identity estimation as a collection of body parts
    /// and a predicted pose identity.
    /// </summary>
    public class PoseIdentity : Pose
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoseIdentity"/> class
        /// extracted from the specified image.
        /// </summary>
        /// <param name="image">The image from which the pose identity was extracted.</param>
        public PoseIdentity(IplImage image)
            : base(image)
        {
        }

        /// <summary>
        /// Gets or sets the maximum likelihood predicted pose identity.
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        /// Gets or sets the maximum likelihood confidence score for the predicted identity.
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Gets or sets the maximum likelihood predicted pose identity index.
        /// </summary>
        [XmlIgnore]
        public int IdentityIndex { get; set; }

        /// <summary>
        /// Gets or sets the predicted identity confidence scores for this instance.
        /// </summary>
        [XmlIgnore]
        public float[] IdentityScores { get; set; }
    }
}
