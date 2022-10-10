using System;
using Bonsai.Vision.Design;
using OpenCV.Net;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Font = System.Drawing.Font;
using Size = OpenCV.Net.Size;

namespace Bonsai.Sleap.Design
{
    internal class LabeledImageLayer : IDisposable
    {
        const float LabelFontScale = 0.02f;
        readonly IplImageTexture labelTexture;
        IplImage labelImage;
        Font labelFont;
        bool hasLabels;
        bool disposed;

        public LabeledImageLayer()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            labelTexture = new IplImageTexture();
        }

        public void ClearLabels()
        {
            hasLabels = false;
        }

        public void UpdateLabels(Size size, Font font, Action<Graphics, Font> draw)
        {
            if (labelImage == null || labelImage.Size != size)
            {
                labelImage = new IplImage(size, IplDepth.U8, 4);
                var emSize = font.SizeInPoints * (labelImage.Height * LabelFontScale) / font.Height;
                labelFont = new Font(font.FontFamily, emSize);
            }

            labelImage.SetZero();
            using (var labelBitmap = new Bitmap(labelImage.Width, labelImage.Height, labelImage.WidthStep, System.Drawing.Imaging.PixelFormat.Format32bppArgb, labelImage.ImageData))
            using (var graphics = Graphics.FromImage(labelBitmap))
            using (var format = new StringFormat())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                draw(graphics, labelFont);
            }

            labelTexture.Update(labelImage);
            hasLabels = true;
        }

        public void Draw()
        {
            if (hasLabels)
            {
                GL.Color4(Color4.White);
                GL.Enable(EnableCap.Texture2D);
                labelTexture.Draw();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    labelTexture.Dispose();
                    labelImage?.Dispose();
                    labelFont?.Dispose();
                    labelImage = null;
                    labelFont = null;
                    hasLabels = false;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
