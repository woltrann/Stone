using System.Collections.Generic;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyMeshUtility
    {
        public static Mesh SaveBottomCentered(TidyMeshBuilder builder, string meshName, string folder)
        {
            var mesh = builder.Build(meshName);
            RecenterBottom(mesh);
            return TidyMeshAssets.SaveMesh(mesh, folder + "/" + meshName + ".asset");
        }

        public static Mesh SaveAsIs(TidyMeshBuilder builder, string meshName, string folder)
        {
            return TidyMeshAssets.SaveMesh(builder.Build(meshName), folder + "/" + meshName + ".asset");
        }

        public static void RecenterBottom(Mesh mesh)
        {
            var bounds = mesh.bounds;
            var offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            if (offset.sqrMagnitude < 1e-10f)
            {
                return;
            }

            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            for (var index = 0; index < vertices.Count; index++)
            {
                vertices[index] += offset;
            }

            mesh.SetVertices(vertices);
            mesh.RecalculateBounds();
        }

        public static void Jitter(Mesh mesh, float amount, int seed)
        {
            var random = new System.Random(seed);
            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            var offsets = new Dictionary<Vector3Int, Vector3>();
            for (var index = 0; index < vertices.Count; index++)
            {
                var key = Vector3Int.RoundToInt(vertices[index] * 10000f);
                if (!offsets.TryGetValue(key, out var offset))
                {
                    offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * amount;
                    offsets[key] = offset;
                }

                vertices[index] += offset;
            }

            mesh.SetVertices(vertices);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
