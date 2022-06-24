using System.Collections.Generic;
using OpenCV.Net;

namespace Bonsai.Sleap
{
    public class NetworkConfig
    {
        public List<string> part_names { get; } = new List<string>();

        public List<string> classes_names { get; } = new List<string>();

        public Skeleton Skeleton { get; set; } = new Skeleton();

        public Size original_input_size { get; set; } = new Size();
        public float original_input_scaling { get; set; } = float.NaN;

    }

    public struct Skeleton
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
