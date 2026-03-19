using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCV.Net;
using System;
using System.IO;

namespace Bonsai.Sleap
{
    static class TensorHelper
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
                        { "trt_fp16_enable", "true" },
                        { "trt_engine_cache_enable", "true" },
                        { "trt_engine_cache_path", ".bonsai/onnx" }
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

        public static IplImage GetRegionOfInterest(IplImage frame, Rect rect, out Point offset)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                frame = frame.GetSubRect(rect);
                offset = new Point(rect.X, rect.Y);
            }
            else offset = Point.Zero;
            return frame;
        }

        public static IplImage EnsureFrameSize(IplImage frame, Size tensorSize, ref IplImage resizeTemp)
        {
            if (tensorSize != frame.Size)
            {
                if (resizeTemp == null || resizeTemp.Size != tensorSize)
                {
                    resizeTemp = new IplImage(tensorSize, frame.Depth, frame.Channels);
                }

                CV.Resize(frame, resizeTemp);
                frame = resizeTemp;
            }

            return frame;
        }

        public static IplImage EnsureColorFormat(IplImage frame, ColorConversion? colorConversion, ref IplImage colorTemp, int channels = 1)
        {
            if (colorConversion != null)
            {
                if (colorTemp == null || colorTemp.Size != frame.Size)
                {
                    colorTemp = new IplImage(frame.Size, frame.Depth, channels);
                }

                CV.CvtColor(frame, colorTemp, colorConversion.Value);
                frame = colorTemp;
            }

            return frame;
        }

        public static unsafe void UpdateTensor(DenseTensor<byte> tensor, Size tensorSize, params IplImage[] frames)
        {
            var batchSize = frames.Length;
            var tensorRows = tensorSize.Height;
            var tensorCols = tensorSize.Width;

            using var handle = tensor.Buffer.Pin();
            using var data = new Mat(batchSize * tensorRows, tensorCols, Depth.U8, 1, (IntPtr)handle.Pointer);
            {
                if (frames.Length == 1)
                {
                    CV.Convert(frames[0], data);
                }
                else
                {
                    for (int i = 0; i < frames.Length; i++)
                    {
                        var startRow = i * tensorRows;
                        var image = data.GetRows(startRow, startRow + tensorRows);
                        CV.Convert(frames[i], image);
                    }
                }
            }
        }
    }
}
