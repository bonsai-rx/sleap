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

[assembly: TypeVisualizer(typeof(PoseIdentityVisualizer), Target = typeof(PoseIdentity))]

namespace Bonsai.Sleap.Design
{
    public class PoseIdentityVisualizer : IplImageVisualizer
    {
        PoseIdentity pose;
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

            drawIdentityButton = new ToolStripButton("Draw Identity");
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
            pose = (PoseIdentity)value;
            base.Show(pose?.Image);
        }

        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            if (pose != null)
            {
                if (DrawLabels || DrawIdentity)
                {
                    labeledImage.UpdateLabels(pose.Image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                    {
                        if (DrawLabels)
                        {
                            DrawingHelper.DrawLabels(graphics, labelFont, pose);
                        }

                        if (DrawIdentity && pose.Count > 0)
                        {
                            var position = pose[0].Position;
                            graphics.DrawString(pose.Identity, labelFont, Brushes.White, position.X, position.Y);
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

            if (pose != null)
            {
                DrawingHelper.SetDrawState(VisualizerCanvas);
                DrawingHelper.DrawPose(pose);
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
