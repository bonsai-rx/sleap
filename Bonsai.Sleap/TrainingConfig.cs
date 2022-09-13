using System.Collections.Generic;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class TrainingConfig
    {
        public ModelType ModelType { get; set; }

        public List<string> PartNames { get; } = new List<string>();

        public List<string> ClassNames { get; } = new List<string>();

        public Skeleton Skeleton { get; set; }

        public Size TargetSize { get; set; }

        public float InputScaling { get; set; } = float.NaN;

    }

    public class Skeleton
    {
        public string Name;
        public bool DirectedEdges;
        public List<Link> Edges;
    }

    public struct Link
    {
        public int Source;
        public int Target;
    }
}
