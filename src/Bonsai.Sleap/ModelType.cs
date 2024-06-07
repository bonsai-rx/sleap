namespace Bonsai.Sleap
{
    /// <summary>
    /// Specifies the type of SLEAP model.
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// A model type which is unsupported by this package.
        /// </summary>
        InvalidModel = 0,

        /// <summary>
        /// A model for single instance pose estimation.
        /// </summary>
        SingleInstance = 1,

        /// <summary>
        /// A model for centroid-only pose estimation.
        /// </summary>
        Centroid = 2,

        /// <summary>
        /// A model for centered instance pose estimation.
        /// </summary>
        CenteredInstance = 3,

        /// <summary>
        /// A model for multi instance pose estimation.
        /// </summary>
        MultiInstance = 4,

        /// <summary>
        /// A model for multi-class multi-instance pose estimation.
        /// </summary>
        MultiClass = 5
    }
}
