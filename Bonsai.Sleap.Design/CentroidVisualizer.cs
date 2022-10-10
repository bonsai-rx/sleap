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
    public class CentroidVisualizer : IplImageVisualizer
    {
        Centroid centroid;
        LabeledImageLayer labeledImage;
        ToolStripButton drawLabelsButton;

        public bool DrawLabels { get; set; }

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

        public override void Show(object value)
        {
            centroid = (Centroid)value;
            base.Show(centroid?.Image);
        }

        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            if (centroid != null)
            {
                labeledImage.UpdateLabels(centroid.Image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                {
                    if (DrawLabels)
                    {
                        DrawingHelper.DrawLabels(graphics, labelFont, centroid);
                    }
                });
            }
        }

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

        public override void Unload()
        {
            base.Unload();
            labeledImage?.Dispose();
            labeledImage = null;
        }
    }
}
