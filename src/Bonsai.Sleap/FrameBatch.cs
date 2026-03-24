using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCV.Net;
using System;
using System.Collections.Generic;

namespace Bonsai.Sleap
{
    internal class FrameBatch(string inputName, ColorConversion? colorConversion)
    {
        IplImage colorTemp;
        Size frameSize;
        int batchSize;
        DenseTensor<byte> tensor = null;
        IReadOnlyCollection<NamedOnnxValue> inputs;
        readonly string inputName = inputName;
        readonly ColorConversion? colorConversion = colorConversion;
        readonly int? colorChannels = colorConversion?.GetConversionNumChannels();

        public IReadOnlyCollection<NamedOnnxValue> Inputs => inputs;

        public unsafe void Update(params IplImage[] frames)
        {
            if (frames is null || frames.Length == 0)
                throw new ArgumentException("Frame batch must have at least one frame.", nameof(frames));

            if (frames[0].Size != frameSize || frames.Length != batchSize || tensor is null)
            {
                frameSize = frames[0].Size;
                batchSize = frames.Length;
                var channels = colorChannels ?? frames[0].Channels;
                ReadOnlySpan<int> dimensions = stackalloc int[] { batchSize, channels, frameSize.Height, frameSize.Width };
                tensor = new DenseTensor<byte>(dimensions);
                inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };
            }

            var tensorRows = frameSize.Height;
            var tensorCols = frameSize.Width;
            using var handle = tensor.Buffer.Pin();
            using var data = new Mat(batchSize * tensorRows, tensorCols, Depth.U8, 1, (IntPtr)handle.Pointer);
            {
                if (frames.Length == 1)
                {
                    CV.Copy(EnsureColorFormat(frames[0]), data);
                }
                else
                {
                    for (int i = 0; i < frames.Length; i++)
                    {
                        var startRow = i * tensorRows;
                        var image = data.GetRows(startRow, startRow + tensorRows);
                        CV.Copy(EnsureColorFormat(frames[i]), image);
                    }
                }
            }
        }

        IplImage EnsureColorFormat(IplImage frame)
        {
            if (colorConversion != null)
            {
                if (colorTemp is null || colorTemp.Size != frame.Size)
                {
                    colorTemp = new IplImage(frame.Size, frame.Depth, colorChannels ?? frame.Channels);
                }

                CV.CvtColor(frame, colorTemp, colorConversion.Value);
                frame = colorTemp;
            }

            return frame;
        }
    }
}
