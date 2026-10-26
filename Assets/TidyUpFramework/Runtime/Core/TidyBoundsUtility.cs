using UnityEngine;

namespace TidyUpFramework.Core
{
    public static class TidyBoundsUtility
    {
        public static Bounds CalculateLocalBounds(Transform root)
        {
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var worldToRoot = root.worldToLocalMatrix;
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var toRoot = worldToRoot * meshFilter.transform.localToWorldMatrix;
                EncapsulateTransformedBounds(ref bounds, ref hasBounds, mesh.bounds, toRoot);
            }

            var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var skinned in skinnedRenderers)
            {
                var toRoot = worldToRoot * skinned.transform.localToWorldMatrix;
                EncapsulateTransformedBounds(ref bounds, ref hasBounds, skinned.localBounds, toRoot);
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one * 0.1f);
        }

        static void EncapsulateTransformedBounds(ref Bounds bounds, ref bool hasBounds, Bounds source, Matrix4x4 matrix)
        {
            var center = source.center;
            var extents = source.extents;
            for (var corner = 0; corner < 8; corner++)
            {
                var offset = new Vector3(
                    (corner & 1) == 0 ? -extents.x : extents.x,
                    (corner & 2) == 0 ? -extents.y : extents.y,
                    (corner & 4) == 0 ? -extents.z : extents.z);
                var point = matrix.MultiplyPoint3x4(center + offset);
                if (!hasBounds)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }
    }
}
