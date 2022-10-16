using System.Collections.Generic;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    internal class TrainingConfig
    {
        public ModelType ModelType { get; set; }

        public string AnchorName { get; set; }

        public List<string> PartNames { get; } = new List<string>();

        public List<string> ClassNames { get; } = new List<string>();

        public Skeleton Skeleton { get; set; }

        public Size TargetSize { get; set; }

        public float InputScaling { get; set; } = float.NaN;

    }

    internal class Skeleton
    {
        public string Name;
        public bool DirectedEdges;
        public List<Link> Edges;
    }

    internal struct Link
    {
        public int Source;
        public int Target;
    }
}
