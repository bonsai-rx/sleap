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
    /// <summary>
    /// Provides a type visualizer that draws a visual representation of the
    /// estimated pose and identity extracted from each image in the sequence.
    /// </summary>
    public class PoseIdentityVisualizer : IplImageVisualizer
    {
        PoseIdentity pose;
        LabeledImageLayer labeledImage;
        ToolStripButton drawLabelsButton;
        ToolStripButton drawIdentityButton;

        /// <summary>
        /// Gets or sets a value indicating whether to show the names of body parts.
        /// </summary>
        public bool DrawLabels { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the pose identity.
        /// </summary>
        public bool DrawIdentity { get; set; }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override void Show(object value)
        {
            pose = (PoseIdentity)value;
            base.Show(pose?.Image);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override void Unload()
        {
            base.Unload();
            labeledImage?.Dispose();
            labeledImage = null;
        }
    }
}
