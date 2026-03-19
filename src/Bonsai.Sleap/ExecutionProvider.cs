namespace Bonsai.Sleap
{
    /// <summary>
    /// Specifies the ONNX runtime execution provider used to perform model inference.
    /// </summary>
    public enum ExecutionProvider
    {
        /// <summary>
        /// Specifies the CPU execution provider.
        /// </summary>
        Cpu,

        /// <summary>
        /// Specifies the CUDA execution provider.
        /// </summary>
        Cuda,

        /// <summary>
        /// Specifies the TensorRT execution provider.
        /// </summary>
        TensorRT
    }
}
