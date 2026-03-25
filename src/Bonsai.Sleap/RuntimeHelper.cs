using Microsoft.ML.OnnxRuntime;
using System.IO;

namespace Bonsai.Sleap
{
    static class RuntimeHelper
    {
        public static InferenceSession ImportModel(string modelPath, ExecutionProvider provider, out ExportMetadata exportMetadata)
        {
            exportMetadata = LoadExportMetadata(modelPath);
            var sessionOptions = new SessionOptions();
            if (provider >= ExecutionProvider.Cuda)
            {
                if (provider == ExecutionProvider.TensorRT)
                {
                    var tensorRtOptions = new OrtTensorRTProviderOptions();
                    tensorRtOptions.UpdateOptions(new()
                    {
                        { "trt_fp16_enable", exportMetadata.Precision == ModelPrecision.FP16 ? "true" : "false" },
                        { "trt_engine_cache_enable", "true" },
                        { "trt_engine_cache_path", PathHelper.GetOnnxCacheDirectory() }
                    });
                    sessionOptions.AppendExecutionProvider_Tensorrt(tensorRtOptions);
                }
                sessionOptions.AppendExecutionProvider_CUDA();
            }

            return new InferenceSession(modelPath, sessionOptions);
        }

        static ExportMetadata LoadExportMetadata(string modelPath)
        {
            var baseDirectory = Path.GetDirectoryName(modelPath);
            var exportMetadataFileName = Path.Combine(baseDirectory, "export_metadata.json");
            var contents = File.ReadAllText(exportMetadataFileName);
            return ExportMetadata.Deserializer.Deserialize<ExportMetadata>(contents);
        }
    }
}
