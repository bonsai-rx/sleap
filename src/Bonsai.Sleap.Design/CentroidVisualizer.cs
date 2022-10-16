using Bonsai;
using Bonsai.Vision.Design;
using Bonsai.Sleap;
using Bonsai.Sleap.Design;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Windows.Forms;
using System.Collections.Generic;

[assembly: TypeVisualizer(typeof(CentroidVisualizer), Target = typeof(Centroid))]

namespace Bonsai.Sleap.Design
{
    /// <summary>
    /// Provides a type visualizer that draws a visual representation of a
    /// detected image centroid.
    /// </summary>
    public class CentroidVisualizer : IplImageVisualizer
    {
        Centroid centroid;
        LabeledImageLayer labeledImage;
        ToolStripButton drawLabelsButton;

        /// <summary>
        /// Gets or sets a value indicating whether to show the centroid anchor name.
        /// </summary>
        public bool DrawLabels { get; set; }

        /// <inheritdoc/>
        public override void Load(IServiceProvider provider)
        {
            base.Load(provider);
            drawLabelsButton = new ToolStripButton("Draw Label");
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

        /// <inheritdoc/>
        public override void Show(object value)
        {
            centroid = (Centroid)value;
            base.Show(centroid?.Image);
        }

        /// <inheritdoc/>
        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            if (centroid != null)
            {
                if (DrawLabels)
                {
                    labeledImage.UpdateLabels(centroid.Image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                    {
                        DrawingHelper.DrawLabels(graphics, labelFont, centroid);
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

            if (centroid != null)
            {
                DrawingHelper.SetDrawState(VisualizerCanvas);
                DrawingHelper.DrawCentroid(centroid);
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
