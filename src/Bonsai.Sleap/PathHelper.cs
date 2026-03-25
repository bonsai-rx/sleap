using System;
using System.IO;
using System.Text;

namespace Bonsai.Sleap
{
    internal class PathHelper
    {
        internal const string BonsaiExtension = ".bonsai";
        internal const string OnnxCacheDirectory = "onnx";

        private static string GetParentRelativePath(DirectoryInfo appDirectory, string path)
        {
            var relativeToDirectory = appDirectory.Parent;
            var fullPathName = Path.GetFullPath(path);
            var relativePathRoot = Path.GetPathRoot(relativeToDirectory.FullName);
            var pathRoot = Path.GetPathRoot(fullPathName);
            var pathComparison = StringComparison.OrdinalIgnoreCase;
            if (!string.Equals(relativePathRoot, pathRoot, pathComparison))
                return path;

            var relativeToComponents = relativeToDirectory.FullName.Split(Path.DirectorySeparatorChar);
            var pathComponents = fullPathName.Split(Path.DirectorySeparatorChar);
            if (pathComponents.Length < relativeToComponents.Length)
                return path;

            var stringBuilder = new StringBuilder();
            for (int i = 0; i < pathComponents.Length; i++)
            {
                if (i >= relativeToComponents.Length)
                {
                    if (stringBuilder.Length > 0)
                        stringBuilder.Append(Path.DirectorySeparatorChar);
                    stringBuilder.Append(pathComponents[i]);
                }
                else if (!string.Equals(pathComponents[i], relativeToComponents[i], pathComparison))
                    return path;
            }

            return stringBuilder.ToString();
        }

        public static string GetOnnxCacheDirectory()
        {
            var baseDirectory = Environment.CurrentDirectory;
            var appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(appBaseDirectory))
            {
                var appDirectoryInfo = new DirectoryInfo(appBaseDirectory);
                var parentRelativePath = GetParentRelativePath(appDirectoryInfo, baseDirectory);
                if (!ReferenceEquals(parentRelativePath, baseDirectory))
                {
                    return Path.Combine(appBaseDirectory, OnnxCacheDirectory, parentRelativePath);
                }
            }

            return Path.Combine(baseDirectory, BonsaiExtension, OnnxCacheDirectory);
        }
    }
}
