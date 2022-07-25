using System;
using System.IO;
using System.Linq;
using OpenCV.Net;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Bonsai.Sleap
{
    static class SingleInstance_ConfigHelper
    {
        static YamlMappingNode OpenFile(string fileName)
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

            var partNames = (YamlSequenceNode)mapping["model"]["heads"]["single_instance"]["part_names"];
            foreach (var part in partNames.Children)
            {
                config.PartNames.Add((string)part);
            }

            var skeleton = new Skeleton();
            skeleton.DirectedEdges = (string)mapping["data"]["labels"]["skeletons"][0]["directed"] == "true";
            skeleton.Name = (string)mapping["data"]["labels"]["skeletons"][0]["graph"]["name"];

            //TODO: fill edges
            var edges = new List<Link>();
            skeleton.Edges = edges;
            config.Skeleton = skeleton;

            config.TargetSize = new Size(
                int.Parse((string)mapping["data"]["preprocessing"]["target_width"]),
                int.Parse((string)mapping["data"]["preprocessing"]["target_height"]));
            config.InputScaling = float.Parse((string)mapping["data"]["preprocessing"]["input_scaling"]);
            return config;

        }
    }
}
