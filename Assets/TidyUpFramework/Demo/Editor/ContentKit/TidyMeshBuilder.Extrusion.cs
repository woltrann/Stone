using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public sealed partial class TidyMeshBuilder
    {
        private const float MaxExtrusionBevelFraction = 0.5f;

        public void AddExtrusion(Vector2[] closedOutline, float depth, int subMesh = 0, Matrix4x4? transform = null, float bevel = 0f)
        {
            Vector2[] outline = TidyPolygon.Prepare(closedOutline);
            if (outline.Length < 3)
            {
                return;
            }

            var placement = new TidyPlacement(transform);
            float thickness = Mathf.Abs(depth);
            float radius = Mathf.Clamp(bevel, 0f, thickness * MaxExtrusionBevelFraction);
            if (radius < MinimumExtent)
            {
                AddExtrusionWall(placement, subMesh, outline, 0f, thickness);
                AddExtrusionCap(placement, subMesh, outline, 0f, -1f);
                AddExtrusionCap(placement, subMesh, outline, thickness, 1f);
                return;
            }

            Vector2[] inset = TidyPolygon.Inset(outline, radius);
            AddExtrusionCap(placement, subMesh, inset, 0f, -1f);
            AddExtrusionChamfer(placement, subMesh, inset, 0f, outline, radius, -1f);
            AddExtrusionWall(placement, subMesh, outline, radius, thickness - radius);
            AddExtrusionChamfer(placement, subMesh, outline, thickness - radius, inset, thickness, 1f);
            AddExtrusionCap(placement, subMesh, inset, thickness, 1f);
        }

        private void AddExtrusionWall(in TidyPlacement placement, int subMesh, Vector2[] outline, float zStart, float zEnd)
        {
            if (zEnd - zStart < MinimumExtent)
            {
                return;
            }

            float perimeter = 0f;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i];
                Vector2 b = outline[(i + 1) % outline.Length];
                float edgeLength = Vector2.Distance(a, b);
                Vector3 normal = TidyPolygon.OutwardEdgeNormal(a, b);
                float u0 = perimeter * UvScale.x;
                float u1 = (perimeter + edgeLength) * UvScale.x;
                int i0 = Emit(placement, new Vector3(a.x, a.y, zStart), normal, new Vector2(u0, zStart * UvScale.y));
                int i1 = Emit(placement, new Vector3(b.x, b.y, zStart), normal, new Vector2(u1, zStart * UvScale.y));
                int i2 = Emit(placement, new Vector3(b.x, b.y, zEnd), normal, new Vector2(u1, zEnd * UvScale.y));
                int i3 = Emit(placement, new Vector3(a.x, a.y, zEnd), normal, new Vector2(u0, zEnd * UvScale.y));
                Quad(subMesh, i0, i1, i2, i3);
                perimeter += edgeLength;
            }
        }

        private void AddExtrusionChamfer(in TidyPlacement placement, int subMesh, Vector2[] loopA, float zA, Vector2[] loopB, float zB, float facing)
        {
            Vector2[] outer = facing < 0f ? loopB : loopA;
            for (int i = 0; i < loopA.Length; i++)
            {
                int next = (i + 1) % loopA.Length;
                Vector3 edgeNormal = TidyPolygon.OutwardEdgeNormal(outer[i], outer[next]);
                Vector3 normal = (edgeNormal + Vector3.forward * facing).normalized;
                var a0 = new Vector3(loopA[i].x, loopA[i].y, zA);
                var a1 = new Vector3(loopA[next].x, loopA[next].y, zA);
                var b1 = new Vector3(loopB[next].x, loopB[next].y, zB);
                var b0 = new Vector3(loopB[i].x, loopB[i].y, zB);
                int i0 = Emit(placement, a0, normal, BoxProjectedUv(a0, normal));
                int i1 = Emit(placement, a1, normal, BoxProjectedUv(a1, normal));
                int i2 = Emit(placement, b1, normal, BoxProjectedUv(b1, normal));
                int i3 = Emit(placement, b0, normal, BoxProjectedUv(b0, normal));
                Quad(subMesh, i0, i1, i2, i3);
            }
        }

        private void AddExtrusionCap(in TidyPlacement placement, int subMesh, Vector2[] outline, float z, float facing)
        {
            List<int> triangles = TidyTriangulator.Triangulate(outline);
            Vector3 normal = Vector3.forward * facing;
            int start = positions.Count;
            foreach (Vector2 point in outline)
            {
                Emit(placement, new Vector3(point.x, point.y, z), normal, ScaleUv(point));
            }

            for (int k = 0; k + 2 < triangles.Count; k += 3)
            {
                Triangle(subMesh, start + triangles[k], start + triangles[k + 1], start + triangles[k + 2]);
            }
        }
    }
}
