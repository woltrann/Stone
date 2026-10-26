using TidyUpFramework.Core;
using TidyUpFramework.Objects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public enum TidyColliderShape
    {
        Box,
        Convex,
        Mesh,
        None
    }

    public static class TidyPropFactory
    {
        public static GameObject Visual(string objectName, Mesh mesh, params Material[] materials)
        {
            var root = new GameObject(objectName);
            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return root;
        }

        public static GameObject Interactive(string prefabPath, Mesh mesh, Material[] materials, TidyObjectDefinition definition, TidyColliderShape colliderShape = TidyColliderShape.Box, Vector3 placementEuler = default)
        {
            var root = Visual(definition != null ? definition.DisplayName : mesh.name, mesh, materials);
            var renderer = root.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = mesh.bounds.size.magnitude < 0.12f ? ShadowCastingMode.Off : ShadowCastingMode.On;
            AddCollider(root, mesh, colliderShape);
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            var tidyObject = root.AddComponent<TidyObject>();
            tidyObject.AssignDefinition(definition);
            if (placementEuler != default)
            {
                TidySerialization.SetVector3(tidyObject, "placementEulerOffset", placementEuler);
            }

            var prefab = TidyAssetFactory.SavePrefab(root, prefabPath);
            if (definition != null)
            {
                definition.AssignPrefab(prefab);
                EditorUtility.SetDirty(definition);
            }

            return prefab;
        }

        public static GameObject Static(string prefabPath, Mesh mesh, Material[] materials, TidyColliderShape colliderShape = TidyColliderShape.Mesh)
        {
            var root = Visual(System.IO.Path.GetFileNameWithoutExtension(prefabPath), mesh, materials);
            AddCollider(root, mesh, colliderShape);
            return TidyAssetFactory.SavePrefab(root, prefabPath);
        }

        public static void AddCollider(GameObject target, Mesh mesh, TidyColliderShape shape)
        {
            switch (shape)
            {
                case TidyColliderShape.Box:
                    var box = target.AddComponent<BoxCollider>();
                    box.center = mesh.bounds.center;
                    box.size = Vector3.Max(mesh.bounds.size, Vector3.one * 0.01f);
                    break;
                case TidyColliderShape.Convex:
                    var convex = target.AddComponent<MeshCollider>();
                    convex.sharedMesh = mesh;
                    convex.convex = true;
                    break;
                case TidyColliderShape.Mesh:
                    var meshCollider = target.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = mesh;
                    break;
            }
        }

        public static BoxCollider AimTrigger(GameObject target, Vector3 center, Vector3 size)
        {
            var trigger = target.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = center;
            trigger.size = size;
            return trigger;
        }

        public static Bounds BoundsOf(GameObject root)
        {
            return TidyBoundsUtility.CalculateLocalBounds(root.transform);
        }
    }
}
