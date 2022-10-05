using Bonsai;
using Bonsai.Vision.Design;
using Bonsai.Sleap;
using Bonsai.Sleap.Design;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

[assembly: TypeVisualizer(typeof(LabeledPoseVisualizer), Target = typeof(LabeledPose))]

namespace Bonsai.Sleap.Design
{
    public class LabeledPoseVisualizer : IplImageVisualizer
    {
        LabeledPose labeledPose;
        LabeledImageLayer labeledImage;
        ToolStripButton drawLabelsButton;
        ToolStripButton drawIdentityButton;

        public bool DrawLabels { get; set; }

        public bool DrawIdentity { get; set; }

        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            drawLabelsButton = new ToolStripButton("Draw Labels");
            drawLabelsButton.CheckState = CheckState.Checked;
            drawLabelsButton.Checked = DrawLabels;
            drawLabelsButton.CheckOnClick = true;
            drawLabelsButton.CheckedChanged += (sender, e) => DrawLabels = drawLabelsButton.Checked;
            StatusStrip.Items.Add(drawLabelsButton);

            drawIdentityButton = new ToolStripButton("Label Identity");
            drawIdentityButton.CheckState = CheckState.Checked;
            drawIdentityButton.Checked = DrawIdentity;
            drawIdentityButton.CheckOnClick = true;
            drawIdentityButton.CheckedChanged += (sender, e) => DrawIdentity = drawIdentityButton.Checked;
            StatusStrip.Items.Add(drawIdentityButton);

            VisualizerCanvas.Load += (sender, e) =>
            {
                labeledImage = new LabeledImageLayer();
                GL.Enable(EnableCap.PointSmooth);
            };
        }

        public override void Show(object value)
        {
            labeledPose = (LabeledPose)value;
            base.Show(labeledPose?.Image);
        }

        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            if (labeledPose != null)
            {
                labeledImage.UpdateLabels(labeledPose.Image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                {
                    if (DrawLabels)
                    {
                        DrawingHelper.DrawLabels(graphics, labelFont, labeledPose);
                    }

                    if (DrawIdentity && labeledPose.Count > 0)
                    {
                        var position = labeledPose[0].Position;
                        graphics.DrawString(labeledPose.Label, labelFont, Brushes.White, position.X, position.Y);
                    }
                });
            }
        }

        protected override void RenderFrame()
        {
            GL.Color4(Color4.White);
            base.RenderFrame();

            if (labeledPose != null)
            {
                DrawingHelper.SetDrawState(VisualizerCanvas);
                DrawingHelper.DrawPose(labeledPose);
                labeledImage.Draw();
            }
        }

        public override void Unload()
        {
            base.Unload();
            labeledImage?.Dispose();
            labeledImage = null;
        }
    }
}
