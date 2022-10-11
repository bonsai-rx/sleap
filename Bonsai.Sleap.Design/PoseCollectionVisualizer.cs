using Bonsai;
using Bonsai.Vision.Design;
using Bonsai.Sleap;
using Bonsai.Sleap.Design;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

[assembly: TypeVisualizer(typeof(PoseCollectionVisualizer), Target = typeof(PoseCollection))]

namespace Bonsai.Sleap.Design
{
    public class PoseCollectionVisualizer : IplImageVisualizer
    {
        PoseCollection poses;
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
            poses = (PoseCollection)value;
            base.Show(poses?.Image);
        }

        protected override void ShowMashup(IList<object> values)
        {
            base.ShowMashup(values);
            var image = VisualizerImage;
            if (image != null && poses != null)
            {
                if (DrawLabels)
                {
                    labeledImage.UpdateLabels(image.Size, VisualizerCanvas.Font, (graphics, labelFont) =>
                    {
                        foreach (var pose in poses)
                        {
                            DrawingHelper.DrawLabels(graphics, labelFont, pose);
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

            if (poses != null)
            {
                DrawingHelper.SetDrawState(VisualizerCanvas);
                foreach (var pose in poses)
                {
                    DrawingHelper.DrawPose(pose);
                    DrawingHelper.DrawBoundingBox(pose, 0);
                }
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
