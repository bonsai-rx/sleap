using System.Collections.Generic;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Provides information about the model used for inference.
    /// </summary>
    public interface IModelInfo
    {
        /// <summary>
        /// Gets the type of SLEAP model used for inference.
        /// </summary>
        ModelType ModelType { get; }

        /// <summary>
        /// Gets the name of the anchor part.
        /// </summary>
        string AnchorName { get; }

        /// <summary>
        /// Gets the collection of body part names.
        /// </summary>
        IReadOnlyList<string> PartNames { get; }

        /// <summary>
        /// Gets the collection of class names used to assign pose identities.
        /// </summary>
        IReadOnlyList<string> ClassNames { get; }
    }
}
