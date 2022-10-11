using Bonsai;
using Bonsai.Vision.Design;
using Bonsai.Sleap;
using Bonsai.Sleap.Design;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Windows.Forms;
using System.Collections.Generic;

[assembly: TypeVisualizer(typeof(CentroidCollectionVisualizer), Target = typeof(CentroidCollection))]

namespace Bonsai.Sleap.Design
{
    public class CentroidCollectionVisualizer : IplImageVisualizer
    {
        CentroidCollection centroids;
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
            centroids = (CentroidCollection)value;
            base.Show(centroids?.Image);
        }

        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            if (centroids != null)
            {
                if (DrawLabels)
                {
                    labeledImage.UpdateLabels(centroids.Image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                    {
                        DrawingHelper.DrawLabels(graphics, labelFont, centroids);
                    });
                }
                else labeledImage.ClearLabels();
            }
        }

        protected override void RenderFrame()
        {
            GL.Color4(Color4.White);
            base.RenderFrame();

            if (centroids != null)
            {
                DrawingHelper.SetDrawState(VisualizerCanvas);
                DrawingHelper.DrawCentroids(centroids);
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
