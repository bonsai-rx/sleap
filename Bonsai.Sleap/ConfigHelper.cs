using System;
using System.IO;
using System.Linq;
using OpenCV.Net;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Bonsai.Sleap
{
    public static class ConfigHelper
    {
        public static YamlMappingNode OpenFile(string fileName)
        {
            var yaml = new YamlStream();
            var reader = new StringReader(File.ReadAllText(fileName));
            yaml.Load(reader);

            var document = yaml.Documents.FirstOrDefault();
            if (document == null)
            {
                throw new ArgumentException("The specified pose config file is empty.", nameof(fileName));
            }

            var mapping = document.RootNode as YamlMappingNode;
            return mapping;
        }

        public static TrainingConfig LoadTrainingConfig(string fileName)
        {
            var mapping = OpenFile(fileName);
            return LoadTrainingConfig(mapping);
        }

        public static TrainingConfig LoadTrainingConfig(YamlMappingNode mapping)
        {
            var config = new TrainingConfig();
            config.ModelType = GetModelType(mapping);
            ParseModel(config, mapping);

            config.TargetSize = new Size(
                int.Parse((string)mapping["data"]["preprocessing"]["target_width"]),
                int.Parse((string)mapping["data"]["preprocessing"]["target_height"]));
            config.InputScaling = float.Parse((string)mapping["data"]["preprocessing"]["input_scaling"]);
            return config;

        }

        public static ModelType GetModelType(YamlMappingNode mapping)
        {
            int modelCount = 0;
            var availableModels = mapping["model"]["heads"];
            var outArg = ModelType.InvalidModel;

            if (availableModels["single_instance"].AllNodes.Count() > 1)
            {
                modelCount++;
                outArg = ModelType.SingleInstance;
            }
            if (availableModels["centroid"].AllNodes.Count() > 1)
            {
                modelCount++;
                outArg = ModelType.Centroid;
            }
            if (availableModels["centered_instance"].AllNodes.Count() > 1)
            {
                modelCount++;
                outArg = ModelType.CenteredInstance;
            }
            if (availableModels["multi_instance"].AllNodes.Count() > 1)
            {
                modelCount++;
                outArg = ModelType.MultiInstance;
            }
            if (modelCount == 0)
            {
                //TODO: Sometimes it does not appear in the json, might need a try/catch
                if (availableModels["multi_class_topdown"].AllNodes.Count() > 1)
                {
                    modelCount++;
                    outArg = ModelType.MultiClass;
                }
            }

            if (modelCount == 0)
            {
                throw new InvalidDataException("No models found in training_config.json file.");
            }
            if (modelCount > 1)
            {
                throw new InvalidDataException("Multiple models found in training_config.json file.");
            }
            return outArg;
        }

        public static void ParseModel(TrainingConfig config, YamlMappingNode mapping)
        {
            switch (config.ModelType)
            {
                case ModelType.SingleInstance:
                    Parse_SingleInstance_Model(config, mapping);
                    break;
                case ModelType.Centroid:
                    Parse_Centroid_Model(config, mapping);
                    break;
                case ModelType.CenteredInstance:
                    Parse_CenteredInstance_Model(config, mapping);
                    break;
                case ModelType.MultiInstance:
                    Parse_MultiInstance_Model(config, mapping);
                    break;
                case ModelType.MultiClass:
                    Parse_MultiClass_Model(config, mapping);
                    break;
            }
        }

        public static void Parse_SingleInstance_Model(TrainingConfig config, YamlMappingNode mapping)
        {
            var partNames = (YamlSequenceNode)mapping["model"]["heads"]["single_instance"]["part_names"];
            foreach (var part in partNames.Children)
            {
                config.PartNames.Add((string)part);
            }
            AddSkeleton(config, mapping);
        }

        public static void Parse_Centroid_Model(TrainingConfig config, YamlMappingNode mapping)
        {
            var anchor = (string)mapping["model"]["heads"]["centroid"]["anchor_part"];
            config.PartNames.Add(anchor);
            AddSkeleton(config, mapping);
        }

        public static void Parse_CenteredInstance_Model(TrainingConfig config, YamlMappingNode mapping)
        {
            var partNames = (YamlSequenceNode)mapping["model"]["heads"]["centered_instance"]["part_names"];
            foreach (var part in partNames.Children)
            {
                config.PartNames.Add((string)part);
            }
            AddSkeleton(config, mapping);
        }

        public static void Parse_MultiClass_Model(TrainingConfig config, YamlMappingNode mapping)
        {
            var partNames = (YamlSequenceNode) mapping["model"]["heads"]["multi_class_topdown"]["confmaps"]["part_names"];
            foreach (var part in partNames.Children)
            {
                config.PartNames.Add((string) part);
            }
            var classNames = (YamlSequenceNode)mapping["model"]["heads"]["multi_class_topdown"]["class_vectors"]["classes"];
            foreach (var id in classNames.Children)
            {
                config.ClassNames.Add((string) id);
            }
            AddSkeleton(config, mapping);
        }

        public static void Parse_MultiInstance_Model(TrainingConfig config, YamlMappingNode mapping)
        {
            var partNames = (YamlSequenceNode)mapping["model"]["heads"]["multi_instance"]["confmaps"]["part_names"];
            foreach (var part in partNames.Children)
            {
                config.PartNames.Add((string)part);
            }
            var classNames = (YamlSequenceNode)mapping["model"]["heads"]["multi_instance"]["class_vectors"]["classes"];
            foreach (var id in classNames.Children)
            {
                config.ClassNames.Add((string)id);
            }
            AddSkeleton(config, mapping);
        }

        public static void AddSkeleton(TrainingConfig config, YamlMappingNode mapping)
        {
            var skeleton = new Skeleton();
            skeleton.DirectedEdges = (string)mapping["data"]["labels"]["skeletons"][0]["directed"] == "true";
            skeleton.Name = (string)mapping["data"]["labels"]["skeletons"][0]["graph"]["name"];

            //TODO: fill edges
            var edges = new List<Link>();
            skeleton.Edges = edges;
            config.Skeleton = skeleton;
        }
    }
}
