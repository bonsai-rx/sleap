﻿using OpenCV.Net;
using System.Collections.ObjectModel;

namespace Bonsai.Sleap
{
    public class Pose : KeyedCollection<string, BodyPart>
    {
        public Pose(IplImage image)
        {
            Image = image;
        }

        public IplImage Image { get; private set; }

        public InferedCentroid InferedCentroid { get; set; }

        protected override string GetKeyForItem(BodyPart item)
        {
            return item.Name;
        }
    }

    public struct BodyPart
    {
        public string Name;
        public Point2f Position;
        public float Confidence;
    }
}
