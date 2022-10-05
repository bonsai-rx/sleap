﻿using OpenCV.Net;
using OpenTK;

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
    }
}