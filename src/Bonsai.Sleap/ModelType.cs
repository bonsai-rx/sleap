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
        InvalidModel,

        /// <summary>
        /// A model for single instance pose estimation.
        /// </summary>
        SingleInstance,

        /// <summary>
        /// A model for centroid-only pose estimation.
        /// </summary>
        Centroid,

        /// <summary>
        /// A model for centered instance pose estimation.
        /// </summary>
        CenteredInstance,

        /// <summary>
        /// A model for bottom-up pose estimation.
        /// </summary>
        BottomUp,

        /// <summary>
        /// A model for top-down pose estimation.
        /// </summary>
        TopDown,

        /// <summary>
        /// A model for multi-class bottom-up pose estimation.
        /// </summary>
        MultiClassBottomUp,

        /// <summary>
        /// A model for multi-class top-down pose estimation.
        /// </summary>
        MultiClassTopDown,

        /// <summary>
        /// A model for combined centroid and multi-class top-down pose estimation.
        /// </summary>
        MultiClassTopDownCombined
    }
}
