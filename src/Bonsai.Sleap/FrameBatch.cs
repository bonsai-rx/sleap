using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCV.Net;
using System;
using System.Collections.Generic;

namespace Bonsai.Sleap
{
    internal class FrameBatch(string inputName, Size? inputSize, ColorConversion? colorConversion, ExportMetadata exportMetadata)
    {
        IplImage colorTemp;
        Size frameSize;
        int batchSize;
        Point2f poseScale = new(1, 1);
        DenseTensor<byte> tensor = null;
        IReadOnlyCollection<NamedOnnxValue> inputs;
        readonly string inputName = inputName;
        readonly Size? inputSize = inputSize;
        readonly ColorConversion? colorConversion = colorConversion;
        readonly int? colorChannels = colorConversion?.GetConversionNumChannels();
        readonly ExportMetadata exportMetadata = exportMetadata;

        public IReadOnlyCollection<NamedOnnxValue> Inputs => inputs;

        public Point2f PoseScale => poseScale;

        public unsafe void Update(params IplImage[] frames)
        {
            if (frames is null || frames.Length == 0)
                throw new ArgumentException("Frame batch must have at least one frame.", nameof(frames));

            if (frames.Length > exportMetadata.MaxBatchSize)
                throw new ArgumentException($"Frame batch exceeded maximum batch size of {exportMetadata.MaxBatchSize} frames.");

            var channels = colorChannels ?? frames[0].Channels;
            var currentSize = inputSize.HasValue ? inputSize.GetValueOrDefault() : frames[0].Size;
            if (currentSize != frameSize || frames.Length != batchSize || tensor is null)
            {
                frameSize = currentSize;
                batchSize = frames.Length;
                if (channels != exportMetadata.InputChannels)
                    throw new InvalidOperationException(
                        $"The current model expects {exportMetadata.InputChannels}-channel images, but a {channels}-channel frame was received.");

                ReadOnlySpan<int> dimensions = stackalloc int[] { batchSize, channels, frameSize.Height, frameSize.Width };
                tensor = new DenseTensor<byte>(dimensions);
                inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };
                poseScale = inputSize.HasValue
                    ? new(frames[0].Size.Width / (float)currentSize.Height, frames[0].Size.Height / (float)currentSize.Height)
                    : new(1, 1);
            }

            var tensorRows = frameSize.Height;
            var tensorCols = frameSize.Width;
            using var handle = tensor.Buffer.Pin();
            using var data = new Mat(batchSize * tensorRows, tensorCols, Depth.U8, channels, (IntPtr)handle.Pointer);
            {
                if (frames.Length == 1)
                {
                    CopyResize(EnsureColorFormat(frames[0]), data);
                }
                else
                {
                    for (int i = 0; i < frames.Length; i++)
                    {
                        var startRow = i * tensorRows;
                        var image = data.GetRows(startRow, startRow + tensorRows);
                        CopyResize(EnsureColorFormat(frames[i]), image);
                    }
                }
            }
        }

        IplImage EnsureColorFormat(IplImage frame)
        {
            if (colorConversion.HasValue)
            {
                if (colorTemp is null || colorTemp.Size != frame.Size)
                {
                    colorTemp = new IplImage(frame.Size, frame.Depth, colorChannels ?? frame.Channels);
                }

                CV.CvtColor(frame, colorTemp, colorConversion.GetValueOrDefault());
                frame = colorTemp;
            }

            return frame;
        }

        void CopyResize(IplImage frame, Arr destination)
        {
            if (inputSize.HasValue && inputSize.GetValueOrDefault() != frame.Size)
                CV.Resize(frame, destination);
            else
                CV.Copy(frame, destination);
        }
    }
}
