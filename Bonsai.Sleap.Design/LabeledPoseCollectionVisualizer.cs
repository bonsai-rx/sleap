using Bonsai;
using Bonsai.Vision.Design;
using Bonsai.Sleap;
using Bonsai.Sleap.Design;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;

[assembly: TypeVisualizer(typeof(LabeledPoseCollectionVisualizer), Target = typeof(LabeledPoseCollection))]

namespace Bonsai.Sleap.Design
{
    public class LabeledPoseCollectionVisualizer : IplImageVisualizer
    {
        const float BoundingBoxLineWidth = 3;
        const float BoundingBoxOffset = 0.02f;
        readonly Dictionary<string, int> uniqueLabels = new Dictionary<string, int>();
        LabeledPoseCollection labeledPoses;
        LabeledImageLayer labeledImage;

        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            VisualizerCanvas.Load += (sender, e) =>
            {
                labeledImage = new LabeledImageLayer();
                GL.Enable(EnableCap.PointSmooth);
                GL.LineWidth(BoundingBoxLineWidth);
            };
        }

        public override void Show(object value)
        {
            labeledPoses = (LabeledPoseCollection)value;
            if (labeledPoses != null && labeledPoses.Count > 0)
            {
                base.Show(labeledPoses[0].Image);
            }
        }

        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            var image = VisualizerImage;
            if (image != null && labeledPoses != null && labeledPoses.Count > 0)
            {
                labeledImage.UpdateLabels(image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                {
                    foreach (var labeledPose in labeledPoses)
                    {
                        if (!uniqueLabels.TryGetValue(labeledPose.Label, out int index))
                        {
                            index = uniqueLabels.Count;
                            uniqueLabels.Add(labeledPose.Label, index);
                        }

                        var position = DrawingHelper.GetBoundingBox(labeledPose, image.Size, BoundingBoxOffset)[2];
                        graphics.DrawString(labeledPose.Label, labelFont, Brushes.White, position.X, position.Y);
                    }
                });
            }
        }

        protected override void RenderFrame()
        {
            GL.Color4(Color4.White);
            base.RenderFrame();

            if (labeledPoses != null && labeledPoses.Count > 0)
            {
                var image = VisualizerImage;
                GL.PointSize(5 * VisualizerCanvas.Height / 480f);
                GL.Disable(EnableCap.Texture2D);
                foreach (var labeledPose in labeledPoses)
                {
                    // Draw all body parts
                    GL.Begin(PrimitiveType.Points);
                    for (int i = 0; i < labeledPose.Count; i++)
                    {
                        var position = labeledPose[i].Position;
                        GL.Color3(ColorPalette.GetColor(i));
                        GL.Vertex2(DrawingHelper.NormalizePoint(position, image.Size));
                    }
                    GL.End();

                    // Draw bounding box
                    var roiLimits = DrawingHelper.GetBoundingBox(labeledPose, image.Size, BoundingBoxOffset);
                    GL.Color3(ColorPalette.GetColor(uniqueLabels[labeledPose.Label]));
                    GL.Begin(PrimitiveType.LineLoop);
                    for (int i = 0; i < roiLimits.Length; i++)
                    {
                        GL.Vertex2(DrawingHelper.NormalizePoint(roiLimits[i], image.Size));
                    }
                    GL.End();
                }
                labeledImage.Draw();
            }
        }
        public override void Unload()
        {
            base.Unload();
            uniqueLabels.Clear();
            labeledImage?.Dispose();
            labeledImage = null;
        }
    }
}
