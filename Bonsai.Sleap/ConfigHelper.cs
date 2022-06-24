using System;
using System.IO;
using System.Linq;
using OpenCV.Net;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Bonsai.Sleap
{
    static class ConfigHelper
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

        public static NetworkConfig LoadPoseConfig(string fileName)
        {
            var mapping = OpenFile(fileName);
            return LoadPoseConfig(mapping);
        }

        public static NetworkConfig LoadPoseConfig(YamlMappingNode mapping)
        {
            var config = new NetworkConfig();

            // Get the part names
            var partNames = (YamlSequenceNode) mapping["model"]["heads"]["multi_class_topdown"]["confmaps"]["part_names"];
            foreach (var part in partNames.Children)
            {
                config.part_names.Add((string)part);

            }

            //Get the class names
            var identityNames = (YamlSequenceNode)mapping["model"]["heads"]["multi_class_topdown"]["class_vectors"]["classes"];
            foreach (var id in identityNames.Children)
            {
                config.classes_names.Add((string)id);

            }


            //Build the skeleton
            var skel = new Skeleton();
            skel.DirectedEdges = ((string)mapping["data"]["labels"]["skeletons"][0]["directed"] == "true");
            skel.Name = (string) mapping["data"]["labels"]["skeletons"][0]["graph"]["name"];
            //TODO
            var edges = new List<Link>();
            skel.Edges = edges;

            // set the Skeleton
            config.Skeleton = skel;


            // Network metadata
            config.original_input_size = new Size(
                Int32.Parse((string)mapping["data"]["preprocessing"]["target_width"]),
                Int32.Parse((string)mapping["data"]["preprocessing"]["target_height"]));

            config.original_input_scaling = float.Parse((string)mapping["data"]["preprocessing"]["input_scaling"]);

            return config;

        }
    }
}
