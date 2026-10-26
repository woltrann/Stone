using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidyMeshAssets
    {
        public static Mesh SaveMesh(Mesh mesh, string assetPath)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            string path = TidyAssetPaths.Normalize(assetPath);
            EnsureFolder(TidyAssetPaths.FolderOf(path));
            string assetName = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return ReplaceInPlace(existing, mesh, assetName);
            }

            Mesh target = AssetDatabase.Contains(mesh) ? UnityEngine.Object.Instantiate(mesh) : mesh;
            target.name = assetName;
            AssetDatabase.CreateAsset(target, path);
            AssetDatabase.SaveAssetIfDirty(target);
            return target;
        }

        public static void EnsureFolder(string assetFolderPath)
        {
            TidyAssetPaths.EnsureFolder(assetFolderPath);
        }

        private static Mesh ReplaceInPlace(Mesh existing, Mesh source, string assetName)
        {
            if (existing != source)
            {
                EditorUtility.CopySerialized(source, existing);
            }

            existing.name = assetName;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }
    }
}
