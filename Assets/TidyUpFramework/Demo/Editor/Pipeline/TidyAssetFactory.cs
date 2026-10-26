using System.IO;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyAssetFactory
    {
        public static T CreateOrLoad<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            var created = ScriptableObject.CreateInstance<T>();
            created.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static GameObject SavePrefab(GameObject root, string assetPath)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath, out var success);
            if (!success)
            {
                Debug.LogError("Tidy Up pipeline: failed to save prefab " + assetPath);
            }

            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void MarkDirty(Object asset)
        {
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
