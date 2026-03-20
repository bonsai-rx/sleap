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
        Size tensorSize;
        int batchSize;
        DenseTensor<byte> tensor = null;
        IReadOnlyCollection<NamedOnnxValue> inputs;
        readonly string inputName = inputName;
        readonly ColorConversion? colorConversion = colorConversion;
        readonly int? colorChannels = colorConversion?.GetConversionNumChannels();

        public Size TensorSize => tensorSize;

        public IReadOnlyCollection<NamedOnnxValue> Inputs => inputs;

        public unsafe void Update(params IplImage[] frames)
        {
            var frameSize = frames[0].Size;
            if (frameSize != tensorSize || frames.Length != batchSize || tensor is null)
            {
                tensorSize = frameSize;
                batchSize = frames.Length;
                var channels = colorChannels ?? frames[0].Channels;
                ReadOnlySpan<int> dimensions = stackalloc int[] { batchSize, channels, tensorSize.Height, tensorSize.Width };
                tensor = new DenseTensor<byte>(dimensions);
                inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) };
            }

            var tensorRows = tensorSize.Height;
            var tensorCols = tensorSize.Width;

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
