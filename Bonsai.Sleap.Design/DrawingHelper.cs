using Bonsai.Vision.Design;
using OpenCV.Net;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Font = System.Drawing.Font;
using Graphics = System.Drawing.Graphics;
using Brushes = System.Drawing.Brushes;

namespace Bonsai.Sleap.Design
{
    internal static class DrawingHelper
    {
        public static Vector2 NormalizePoint(Point2f point, Size imageSize)
        {
            return new Vector2(
                (point.X * 2f / imageSize.Width) - 1,
              -((point.Y * 2f / imageSize.Height) - 1));
        }

        public static Point2f[] GetBoundingBox(Pose pose, Size imageSize, float offsetScale)
        {
            var minX = float.NaN;
            var maxX = float.NaN;
            var minY = float.NaN;
            var maxY = float.NaN;
            var unitOffsetX = imageSize.Width * offsetScale;
            var unitOffsetY = imageSize.Height * offsetScale;

            for (int j = 0; j < pose.Count; j++)
            {
                var position = pose[j].Position;
                if (j == 0)
                {
                    minX = maxX = position.X;
                    minY = maxY = position.Y;
                }

                minX = position.X < minX ? position.X : minX;
                maxX = position.X > maxX ? position.X : maxX;
                minY = position.Y < minY ? position.Y : minY;
                maxY = position.Y > maxY ? position.Y : maxY;
            }

            minX -= unitOffsetX;
            maxX += unitOffsetX;
            minY -= unitOffsetY;
            maxY += unitOffsetY;

            var points = new Point2f[] {
                new Point2f(minX, minY),
                new Point2f(maxX, minY),
                new Point2f(maxX, maxY),
                new Point2f(minX, maxY)
            };
            return points;
        }

        public static void SetDrawState(VisualizerCanvas canvas)
        {
            const float BoundingBoxLineWidth = 3;
            GL.PointSize(5 * canvas.Height / 480f);
            GL.LineWidth(BoundingBoxLineWidth);
            GL.Disable(EnableCap.Texture2D);
        }

        public static void DrawPose(Pose pose)
        {
            GL.Begin(PrimitiveType.Points);
            for (int i = 0; i < pose.Count; i++)
            {
                var position = pose[i].Position;
                GL.Color3(ColorPalette.GetColor(i));
                GL.Vertex2(NormalizePoint(position, pose.Image.Size));
            }
            GL.End();
        }

        public static void DrawCentroid(Centroid centroid)
        {
            GL.Begin(PrimitiveType.Points);
            GL.Color3(ColorPalette.GetColor(0));
            GL.Vertex2(NormalizePoint(centroid.Position, centroid.Image.Size));
            GL.End();
        }

        public static void DrawBoundingBox(Pose pose, int colorIndex = 0)
        {
            const float BoundingBoxOffset = 0.02f;
            var imageSize = pose.Image.Size;
            var roiLimits = GetBoundingBox(pose, imageSize, BoundingBoxOffset);
            GL.Color3(ColorPalette.GetColor(colorIndex));
            GL.Begin(PrimitiveType.LineLoop);
            for (int i = 0; i < roiLimits.Length; i++)
            {
                GL.Vertex2(NormalizePoint(roiLimits[i], imageSize));
            }
            GL.End();
        }

        public static void DrawLabels(Graphics graphics, Font font, Pose pose)
        {
            for (int i = 0; i < pose.Count; i++)
            {
                var bodyPart = pose[i];
                var position = bodyPart.Position;
                graphics.DrawString(bodyPart.Name, font, Brushes.White, position.X, position.Y);
            }
        }

        public static void DrawLabels(Graphics graphics, Font font, Centroid centroid)
        {
            if (!string.IsNullOrEmpty(centroid.Name))
            {
                var position = centroid.Position;
                graphics.DrawString(centroid.Name, font, Brushes.White, position.X, position.Y);
            }
        }
    }
}
