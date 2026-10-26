using System;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomProps
    {
        public static GameObject Interactive(string prefabPath, Mesh mesh, Material[] materials, TidyObjectDefinition definition, TidyColliderShape colliderShape, bool requiresDiscovery, bool countsAsClutter, Action<GameObject> configure = null)
        {
            var root = TidyPropFactory.Visual(definition.DisplayName, mesh, materials);
            var renderer = root.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = mesh.bounds.size.magnitude < 0.12f ? ShadowCastingMode.Off : ShadowCastingMode.On;
            TidyPropFactory.AddCollider(root, mesh, colliderShape);
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            var tidyObject = root.AddComponent<TidyObject>();
            tidyObject.AssignDefinition(definition);
            tidyObject.ConfigureFlags(requiresDiscovery, countsAsClutter);
            configure?.Invoke(root);
            var prefab = TidyAssetFactory.SavePrefab(root, prefabPath);
            definition.AssignPrefab(prefab);
            EditorUtility.SetDirty(definition);
            return prefab;
        }

        public static GameObject Static(TidyDemoContext context, string folder, string prefabName, Mesh mesh, Material[] materials, TidyColliderShape collider)
        {
            var prefab = TidyPropFactory.Static(folder + "/" + prefabName + ".prefab", mesh, materials, collider);
            context.AddPrefab(prefab);
            return prefab;
        }

        public static string PrefabName(string id)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var part in id.Split('_'))
            {
                if (part.Length > 0)
                {
                    builder.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
                }
            }

            return builder.ToString();
        }
    }
}
