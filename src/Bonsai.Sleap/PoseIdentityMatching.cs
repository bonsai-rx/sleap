using System;
using System.Collections.Generic;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    /// <summary>
    /// Represents a collection of identity classes matched to poses extracted from a
    /// specified image.
    /// </summary>
    public class PoseIdentityMatching : PoseIdentityCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PoseIdentityMatching"/> class
        /// extracted from the specified image.
        /// </summary>
        /// <inheritdoc/>
        public PoseIdentityMatching(IplImage image, IModelInfo model)
            : base(image, model)
        {
        }

        /// <summary>
        /// Gets the pose identity matching the specified class label.
        /// </summary>
        /// <param name="key">
        /// The label of the identity class to get.
        /// </param>
        /// <returns>
        /// The pose identity matching the specified class label. If a pose identity
        /// matching the specified key is not found, an exception is thrown.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> is null.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// A pose identity matching the specified <paramref name="key"/>
        /// does not exist in the collection.
        /// </exception>
        public PoseIdentity this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                foreach (var item in Items)
                {
                    if (item.Identity == key)
                    {
                        return item;
                    }
                }

                throw new KeyNotFoundException();
            }
        }
    }
}
