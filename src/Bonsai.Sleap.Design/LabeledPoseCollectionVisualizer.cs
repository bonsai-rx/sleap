using Bonsai;
using Bonsai.Vision.Design;
using Bonsai.Sleap;
using Bonsai.Sleap.Design;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

[assembly: TypeVisualizer(typeof(LabeledPoseCollectionVisualizer), Target = typeof(LabeledPoseCollection))]

namespace Bonsai.Sleap.Design
{
    public class LabeledPoseCollectionVisualizer : IplImageVisualizer
    {
        const float BoundingBoxOffset = 0.02f;
        readonly Dictionary<string, int> uniqueLabels = new Dictionary<string, int>();
        LabeledPoseCollection labeledPoses;
        LabeledImageLayer labeledImage;
        ToolStripButton drawLabelsButton;

        public bool DrawLabels { get; set; }

        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            drawLabelsButton = new ToolStripButton("Draw Labels");
            drawLabelsButton.CheckState = CheckState.Checked;
            drawLabelsButton.Checked = DrawLabels;
            drawLabelsButton.CheckOnClick = true;
            drawLabelsButton.CheckedChanged += (sender, e) => DrawLabels = drawLabelsButton.Checked;
            StatusStrip.Items.Add(drawLabelsButton);

            VisualizerCanvas.Load += (sender, e) =>
            {
                labeledImage = new LabeledImageLayer();
                GL.Enable(EnableCap.PointSmooth);
            };
        }

        public override void Show(object value)
        {
            labeledPoses = (LabeledPoseCollection)value;
            base.Show(labeledPoses?.Image);
        }

        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            var image = VisualizerImage;
            if (image != null && labeledPoses != null)
            {
                foreach (var labeledPose in labeledPoses)
                {
                    if (!uniqueLabels.TryGetValue(labeledPose.Label, out int index))
                    {
                        index = uniqueLabels.Count;
                        uniqueLabels.Add(labeledPose.Label, index);
                    }
                }

                if (DrawLabels)
                {
                    labeledImage.UpdateLabels(image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                    {
                        foreach (var labeledPose in labeledPoses)
                        {
                            var position = DrawingHelper.GetBoundingBox(labeledPose, image.Size, BoundingBoxOffset)[2];
                            graphics.DrawString(labeledPose.Label, labelFont, Brushes.White, position.X, position.Y);
                        }
                    });
                }
                else labeledImage.ClearLabels();
            }
        }

        protected override void RenderFrame()
        {
            GL.Color4(Color4.White);
            base.RenderFrame();

            if (labeledPoses != null)
            {
                DrawingHelper.SetDrawState(VisualizerCanvas);
                foreach (var labeledPose in labeledPoses)
                {
                    DrawingHelper.DrawPose(labeledPose);
                    DrawingHelper.DrawBoundingBox(labeledPose, uniqueLabels[labeledPose.Label]);
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
