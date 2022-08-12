using Bonsai;
using Bonsai.Vision.Design;
using Bonsai.Sleap;
using Bonsai.Sleap.Design;
using OpenCV.Net;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Font = System.Drawing.Font;

[assembly: TypeVisualizer(typeof(InferedCentroidVisualizer), Target = typeof(InferedCentroid))]

namespace Bonsai.Sleap.Design
{
    public class InferedCentroidVisualizer : IplImageVisualizer
    {
        InferedCentroid inferedCentroid;
        IplImage labelImage;
        IplImageTexture labelTexture;
        ToolStripButton drawLabelsButton;
        Font labelFont;

        public bool LabelCentroidAnchor { get; set; } = false;

        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            drawLabelsButton = new ToolStripButton("Label anchor");
            drawLabelsButton.CheckState = CheckState.Checked;
            drawLabelsButton.Checked = LabelCentroidAnchor;
            drawLabelsButton.CheckOnClick = true;
            drawLabelsButton.CheckedChanged += (sender, e) => LabelCentroidAnchor = drawLabelsButton.Checked;
            StatusStrip.Items.Add(drawLabelsButton);

            VisualizerCanvas.Load += (sender, e) =>
            {
                labelTexture = new IplImageTexture();
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.PointSmooth);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            };
        }

        public override void Show(object value)
        {
            inferedCentroid = (InferedCentroid)value;
            if (inferedCentroid != null)
            {
                base.Show(inferedCentroid.Image);
            }
        }

        public static Vector2 NormalizePoint(Point2f point, OpenCV.Net.Size imageSize)
        {
            return new Vector2(
                (point.X * 2f / imageSize.Width) - 1,
                -((point.Y * 2f / imageSize.Height) - 1));
        }

        protected override void RenderFrame()
        {
            var drawLabels = LabelCentroidAnchor;
            GL.Color4(Color4.White);
            base.RenderFrame();

            if (inferedCentroid != null)
            {
                GL.PointSize(5 * VisualizerCanvas.Height / 480f);
                GL.Disable(EnableCap.Texture2D);
                GL.Begin(PrimitiveType.Points);

                GL.Color3(ColorPalette.GetColor(0));
                GL.Vertex2(NormalizePoint(inferedCentroid.Centroid, inferedCentroid.Image.Size));
                
                GL.End();

                if (drawLabels)
                {
                    if (labelImage == null || labelImage.Size != inferedCentroid.Image.Size)
                    {
                        const float LabelFontScale = 0.02f;
                        labelImage = new IplImage(inferedCentroid.Image.Size, IplDepth.U8, 4);
                        var emSize = VisualizerCanvas.Font.SizeInPoints * (labelImage.Height * LabelFontScale) / VisualizerCanvas.Font.Height;
                        labelFont = new Font(VisualizerCanvas.Font.FontFamily, emSize);
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
                        
                        graphics.DrawString(inferedCentroid.AnchorName, labelFont, Brushes.White, inferedCentroid.Centroid.X, inferedCentroid.Centroid.Y);
                    }

                    GL.Color4(Color4.White);
                    GL.Enable(EnableCap.Texture2D);
                    labelTexture.Update(labelImage);
                    labelTexture.Draw();
                }
            }
        }
    }
}
