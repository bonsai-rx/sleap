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

[assembly: TypeVisualizer(typeof(PoseIdentityCollectionVisualizer), Target = typeof(PoseIdentityCollection))]

namespace Bonsai.Sleap.Design
{
    /// <summary>
    /// Provides a type visualizer that draws a visual representation of the
    /// collection of pose identities extracted from each image in the sequence.
    /// </summary>
    public class PoseIdentityCollectionVisualizer : IplImageVisualizer
    {
        const float BoundingBoxOffset = 0.02f;
        readonly Dictionary<string, int> uniqueLabels = new Dictionary<string, int>();
        PoseIdentityCollection poseIdentities;
        ToolStripButton drawIdentitiesButton;
        LabeledImageLayer labeledImage;

        /// <summary>
        /// Gets or sets a value indicating whether to show the pose identities.
        /// </summary>
        public bool DrawIdentity { get; set; }

        /// <inheritdoc/>
        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            drawIdentitiesButton = new ToolStripButton("Draw Identities");
            drawIdentitiesButton.CheckState = CheckState.Checked;
            drawIdentitiesButton.Checked = DrawIdentity;
            drawIdentitiesButton.CheckOnClick = true;
            drawIdentitiesButton.CheckedChanged += (sender, e) => DrawIdentity = drawIdentitiesButton.Checked;
            StatusStrip.Items.Add(drawIdentitiesButton);

            VisualizerCanvas.Load += (sender, e) =>
            {
                labeledImage = new LabeledImageLayer();
                GL.Enable(EnableCap.PointSmooth);
            };
        }

        /// <inheritdoc/>
        public override void Show(object value)
        {
            poseIdentities = (PoseIdentityCollection)value;
            base.Show(poseIdentities?.Image);
        }

        /// <inheritdoc/>
        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            var image = VisualizerImage;
            if (image != null && poseIdentities != null)
            {
                foreach (var pose in poseIdentities)
                {
                    if (!uniqueLabels.TryGetValue(pose.Identity, out int index))
                    {
                        index = uniqueLabels.Count;
                        uniqueLabels.Add(pose.Identity, index);
                    }
                }

                if (DrawIdentity)
                {
                    labeledImage.UpdateLabels(image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                    {
                        foreach (var pose in poseIdentities)
                        {
                            var position = DrawingHelper.GetBoundingBox(pose, image.Size, BoundingBoxOffset)[2];
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

            if (poseIdentities != null)
            {
                DrawingHelper.SetDrawState(VisualizerCanvas);
                foreach (var pose in poseIdentities)
                {
                    DrawingHelper.DrawPose(pose);
                    DrawingHelper.DrawBoundingBox(pose, uniqueLabels[pose.Identity]);
                }
                labeledImage.Draw();
            }
        }
        /// <inheritdoc/>
        public override void Unload()
        {
            base.Unload();
            uniqueLabels.Clear();
            labeledImage?.Dispose();
            labeledImage = null;
        }
    }
}
