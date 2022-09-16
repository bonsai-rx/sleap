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
using Font = System.Drawing.Font;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Collections.Generic;

[assembly: TypeVisualizer(typeof(LabeledPoseCollectionVisualizer), Target = typeof(LabeledPoseCollection))]

namespace Bonsai.Sleap.Design
{
    public class LabeledPoseCollectionVisualizer : IplImageVisualizer
    {
        IplImage labelImage;
        LabeledPose labeledPose;
        IplImageTexture labelTexture;
        LabeledPoseCollection labeledPoseCollection;
        Font labelFont;
        float offsetBoundingBox = 0.02f;
        Dictionary<string, int> uniqueLabels = new Dictionary<string, int>();

        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            VisualizerCanvas.Load += (sender, e) =>
            {
                labelTexture = new IplImageTexture();
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.PointSmooth);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.LineWidth(3);
            };
        }

        public override void Show(object value)
        {
            labeledPoseCollection = (LabeledPoseCollection)value;
            if (labeledPoseCollection != null)
            {
                if (labeledPoseCollection.Count > 0)
                {
                    uniqueLabels = updateUniqueLabels(labeledPoseCollection, uniqueLabels);
                    base.Show(labeledPoseCollection[0].Image);
                }
            }
        }

        public static Vector2 NormalizePoint(Point2f point, OpenCV.Net.Size imageSize)
        {
            return new Vector2(
                (point.X * 2f / imageSize.Width) - 1,
                -((point.Y * 2f / imageSize.Height) - 1));
        }

        private static Point2f[] GetBoundingBox(Pose inPose, OpenCV.Net.Size imageSize, float offsetScale)
        {
            float minX = float.NaN;
            float maxX = float.NaN;
            float minY = float.NaN;
            float maxY = float.NaN;

            float unitOffset_w = imageSize.Width * offsetScale;
            float unitOffset_h = imageSize.Height * offsetScale;

            for (int j = 0; j < inPose.Count; j++)
            {
                var position = inPose[j].Position;
                if (float.IsNaN(minX)) { minX = position.X; maxX = position.X; };
                if (float.IsNaN(minY)) { minY = position.Y; maxY = position.Y; };

                minX = position.X < minX ? position.X : minX;
                maxX = position.X > maxX ? position.X : maxX;
                minY = position.Y < minY ? position.Y : minY;
                maxY = position.Y > maxY ? position.Y : maxY;
            }

            minX = minX - unitOffset_w;
            maxX = maxX + unitOffset_w;
            minY = minY - unitOffset_h;
            maxY = maxY + unitOffset_h;

            var points = new Point2f[] {
                new Point2f(minX, minY),
                new Point2f(maxX, minY),
                new Point2f(maxX, maxY),
                new Point2f(minX, maxY)
            };
            return points;
        }

        protected override void RenderFrame()
        {
            GL.Color4(Color4.White);
            base.RenderFrame();
            if (labeledPoseCollection != null)
            {
                GL.PointSize(5 * VisualizerCanvas.Height / 480f);
                GL.Disable(EnableCap.Texture2D);
                for (int j = 0; j < labeledPoseCollection.Count; j++) { 

                    labeledPose = labeledPoseCollection[j];
                    if (labeledPose != null)
                    {
                        // Draw all body parts
                        GL.PointSize(5 * VisualizerCanvas.Height / 480f);
                        GL.Disable(EnableCap.Texture2D);
                        GL.Begin(PrimitiveType.Points);
                        for (int i = 0; i < labeledPose.Count; i++)
                        {
                            var position = labeledPose[i].Position;
                            GL.Color3(ColorPalette.GetColor(i));
                            GL.Vertex2(NormalizePoint(position, labeledPoseCollection[0].Image.Size));
                        }
                        GL.End();

                        var roiLimits = GetBoundingBox(labeledPose, labeledPoseCollection[0].Image.Size, offsetBoundingBox);
                        GL.Disable(EnableCap.Texture2D);
                        GL.Color3(ColorPalette.GetColor(uniqueLabels[labeledPoseCollection[j].Label]));
                        GL.Begin(PrimitiveType.LineLoop);
                        for (int i = 0; i < roiLimits.Length; i++)
                        {
                            GL.Vertex2(NormalizePoint(roiLimits[i], labeledPoseCollection[0].Image.Size));
                        }
                        GL.End();

                        if (labelImage == null || labelImage.Size != labeledPoseCollection[0].Image.Size)
                        {
                            const float LabelFontScale = 0.03f;
                            labelImage = new IplImage(labeledPoseCollection[0].Image.Size, IplDepth.U8, 4);
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
                            var position = roiLimits[2];
                            graphics.DrawString(labeledPose.Label, labelFont, Brushes.White, position.X, position.Y);
                        }
                        GL.Color4(Color4.White);
                        GL.Enable(EnableCap.Texture2D);
                        labelTexture.Update(labelImage);
                        labelTexture.Draw();
                    }
                }
            }
        }
        public override void Unload()
        {
            base.Unload();
            uniqueLabels.Clear();
        }

        private static Dictionary<string, int> updateUniqueLabels(LabeledPoseCollection labeledPoseCollection, Dictionary<string, int> currentLabels)
        {
            int nElements = currentLabels.Count;// Assume the only unique keys are present
            foreach (var labeledPose in labeledPoseCollection)
            {
                if (!(currentLabels.ContainsKey(labeledPose.Label)))
                {
                    currentLabels[labeledPose.Label] = nElements;
                    nElements++;
                }
            }
            return currentLabels;
        }
    }
}
