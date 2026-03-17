using OpenCV.Net;
using System;
using System.Runtime.InteropServices;
using System.Numerics.Tensors;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Bonsai.Sleap
{
    static class TensorHelper
    {
        public static DenseTensor<byte> CreateInputBuffer(ReadOnlySpan<int> tensorSize)
        {
            if (tensorSize.Length != 4)
            {
                throw new ArgumentException("Expected tensor size to have 4 dimensions (batch, height, width, channels).", nameof(tensorSize));
            }
            Memory<byte> totalSize = new byte[tensorSize[0] * tensorSize[1] * tensorSize[2] * tensorSize[3]];
            return new DenseTensor<byte>(totalSize, tensorSize);
        }

        public static void UpdateInputBuffer(DenseTensor<byte> inputBuffer, Size tensorSize, params IplImage[] frames)
        {
            var batchSize = frames.Length;
            var tensorRows = tensorSize.Height;
            var tensorCols = tensorSize.Width;
            if (!MemoryMarshal.TryGetArray<byte>(inputBuffer.Buffer, out var segment))
            {
                throw new InvalidOperationException("Unable to pin tensor buffer.");
            }

            var handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
            try
            {
                var basePtr = IntPtr.Add(handle.AddrOfPinnedObject(), segment.Offset * sizeof(byte));
                using var data = new Mat(new Size(tensorCols, batchSize * tensorRows), Depth.U8, 1, basePtr);
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
            finally
            {
                handle.Free();
            }
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

        public static IplImage EnsureGrayscale(IplImage frame, ref IplImage colorTemp)
        {
            if (frame.Channels != 1)
            {
                if (colorTemp == null || colorTemp.Size != frame.Size)
                {
                    colorTemp = new IplImage(frame.Size, frame.Depth, 1);
                }

                CV.CvtColor(frame, colorTemp, ColorConversion.Bgr2Gray);
                frame = colorTemp;
            }

            return frame;
        }
    }
}
