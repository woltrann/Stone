using System.IO;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal static class TidyAssetPaths
    {
        private const string RootFolder = "Assets";

        public static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/').TrimEnd('/');
        }

        public static string FolderOf(string assetPath)
        {
            string normalized = Normalize(assetPath);
            int slash = normalized.LastIndexOf('/');
            return slash > 0 ? normalized.Substring(0, slash) : RootFolder;
        }

        public static string ToAbsolute(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, Normalize(assetPath)));
        }

        public static void EnsureFolder(string assetFolderPath)
        {
            string normalized = Normalize(assetFolderPath);
            if (string.IsNullOrEmpty(normalized) || normalized == RootFolder || AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string parent = FolderOf(normalized);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, normalized.Substring(parent.Length + 1));
        }

        public static void WriteAndImport(string assetPath, byte[] bytes)
        {
            string normalized = Normalize(assetPath);
            EnsureFolder(FolderOf(normalized));
            File.WriteAllBytes(ToAbsolute(normalized), bytes);
            AssetDatabase.ImportAsset(normalized, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
