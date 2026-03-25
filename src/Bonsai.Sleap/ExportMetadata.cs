using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bonsai.Sleap
{
    internal record class ExportMetadata : IModelInfo
    {
        public static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        // Version info
        [YamlMember(Alias = "sleap_nn_version")] public string SleapVersion = "";
        public DateTime ExportTimestamp = default;
        public string ExportFormat = "";

        // Model info
        public ModelType ModelType = ModelType.InvalidModel;
        public string ModelName = "";
        public string CheckpointPath = "";

        // Architecture
        public string Backbone = "";
        [YamlMember(Alias = "n_nodes")] public int NodeCount = default;
        [YamlMember(Alias = "n_edges")] public int EdgeCount = default;
        [YamlMember(Alias = "node_names")] public List<string> PartNames = [];
        [YamlMember(Alias = "edge_inds")] public List<List<int>> EdgeIndices = [];

        // Input/output spec
        public float InputScale = 1.0f;
        public int InputChannels = default;
        public int OutputStride = default;
        public List<int> CropSize = [];

        // Export parameters
        public int? MaxInstances = null;
        public int? MaxPeaksPerNode = null;
        public int MaxBatchSize = 1;
        public ModelPrecision Precision = ModelPrecision.FP32;
        public float? PeakThreshold = null;

        // Preprocessing - input is uint8 [0,255], normalized internally to float32 [0,1]
        public string InputDtype = "uint8";
        public string Normalization = "0_to_1";

        // Multiclass model fields (optional)
        [YamlMember(Alias = "n_classes")] public int? ClassCount = null;
        public List<string>? ClassNames = null;

        // Centroid/top-down anchor point
        public string? AnchorPart = null;

        // Training config reference
        public bool TrainingConfigEmbedded = default;
        public string TrainingConfigHash = "";

        ModelType IModelInfo.ModelType => ModelType;

        string IModelInfo.AnchorName => string.Empty;

        IReadOnlyList<string> IModelInfo.PartNames => PartNames;

        IReadOnlyList<string> IModelInfo.ClassNames => ClassNames ?? [];
    }
}
