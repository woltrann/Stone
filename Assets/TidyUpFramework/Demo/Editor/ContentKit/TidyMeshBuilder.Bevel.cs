using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public sealed partial class TidyMeshBuilder
    {
        private const float MaxBevelFraction = 0.999f;
        private const int BoxCornerCount = 8;
        private const int EdgeSignCombinations = 4;

        private static readonly Vector3[] PrincipalAxes = { Vector3.right, Vector3.up, Vector3.forward };

        public void AddBeveledBox(Vector3 center, Vector3 size, float bevel, int bevelSegments, int subMesh = 0, Matrix4x4? transform = null)
        {
            Vector3 half = Abs(size) * 0.5f;
            float radius = Mathf.Min(Mathf.Max(0f, bevel), MinComponent(half) * MaxBevelFraction);
            if (radius < MinimumExtent)
            {
                AddBox(center, size, subMesh, transform);
                return;
            }

            var placement = new TidyPlacement(transform);
            var shape = new BevelShape(center, half - Vector3.one * radius, radius, Mathf.Max(1, bevelSegments));
            foreach (Vector3 normal in BoxFaceNormals)
            {
                AddAxisFace(placement, subMesh, center, shape.Inner, normal, radius);
            }

            AddBevelEdges(placement, subMesh, shape);
            AddBevelCorners(placement, subMesh, shape);
        }

        private void AddBevelEdges(in TidyPlacement placement, int subMesh, in BevelShape shape)
        {
            for (int along = 0; along < PrincipalAxes.Length; along++)
            {
                Vector3 axisAlong = PrincipalAxes[along];
                Vector3 axisA = PrincipalAxes[(along + 1) % PrincipalAxes.Length];
                Vector3 axisB = PrincipalAxes[(along + 2) % PrincipalAxes.Length];
                float halfLength = Vector3.Dot(shape.Inner, axisAlong);
                if (halfLength < MinimumExtent)
                {
                    continue;
                }

                for (int signs = 0; signs < EdgeSignCombinations; signs++)
                {
                    Vector3 normalA = axisA * SignFromBit(signs, 0);
                    Vector3 normalB = axisB * SignFromBit(signs, 1);
                    Vector3 edgeCenter = shape.Center + Vector3.Scale(shape.Inner, normalA + normalB);
                    AddBevelEdgeStrip(placement, subMesh, shape, edgeCenter, axisAlong * halfLength, normalA, normalB);
                }
            }
        }

        private void AddBevelEdgeStrip(in TidyPlacement placement, int subMesh, in BevelShape shape, Vector3 edgeCenter, Vector3 halfAlong, Vector3 normalA, Vector3 normalB)
        {
            for (int k = 0; k < shape.Segments; k++)
            {
                Vector3 d0 = ArcDirection(normalA, normalB, k / (float)shape.Segments);
                Vector3 d1 = ArcDirection(normalA, normalB, (k + 1) / (float)shape.Segments);
                Vector3 start = edgeCenter - halfAlong;
                Vector3 end = edgeCenter + halfAlong;
                AddSmoothQuad(placement, subMesh,
                    start + d0 * shape.Radius, end + d0 * shape.Radius, end + d1 * shape.Radius, start + d1 * shape.Radius,
                    d0, d0, d1, d1);
            }
        }

        private void AddBevelCorners(in TidyPlacement placement, int subMesh, in BevelShape shape)
        {
            for (int corner = 0; corner < BoxCornerCount; corner++)
            {
                var signs = new Vector3(SignFromBit(corner, 0), SignFromBit(corner, 1), SignFromBit(corner, 2));
                var patch = new BevelCorner(
                    shape.Center + Vector3.Scale(shape.Inner, signs),
                    new Vector3(signs.x, 0f, 0f),
                    new Vector3(0f, signs.y, 0f),
                    new Vector3(0f, 0f, signs.z),
                    shape.Radius,
                    shape.Segments);
                AddBevelCornerPatch(placement, subMesh, patch);
            }
        }

        private void AddBevelCornerPatch(in TidyPlacement placement, int subMesh, in BevelCorner patch)
        {
            int segments = patch.Segments;
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments - i; j++)
                {
                    AddCornerTriangle(placement, subMesh, patch, new Vector2Int(i, j), new Vector2Int(i + 1, j), new Vector2Int(i, j + 1));
                    if (i + j < segments - 1)
                    {
                        AddCornerTriangle(placement, subMesh, patch, new Vector2Int(i + 1, j), new Vector2Int(i + 1, j + 1), new Vector2Int(i, j + 1));
                    }
                }
            }
        }

        private void AddCornerTriangle(in TidyPlacement placement, int subMesh, in BevelCorner patch, Vector2Int a, Vector2Int b, Vector2Int c)
        {
            Vector3 da = patch.Direction(a);
            Vector3 db = patch.Direction(b);
            Vector3 dc = patch.Direction(c);
            AddSmoothTriangle(placement, subMesh, patch.Surface(da), patch.Surface(db), patch.Surface(dc), da, db, dc);
        }

        private void AddSmoothQuad(in TidyPlacement placement, int subMesh, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3)
        {
            Vector3 projection = n0 + n1 + n2 + n3;
            int i0 = Emit(placement, p0, n0, BoxProjectedUv(p0, projection));
            int i1 = Emit(placement, p1, n1, BoxProjectedUv(p1, projection));
            int i2 = Emit(placement, p2, n2, BoxProjectedUv(p2, projection));
            int i3 = Emit(placement, p3, n3, BoxProjectedUv(p3, projection));
            Quad(subMesh, i0, i1, i2, i3);
        }

        private void AddSmoothTriangle(in TidyPlacement placement, int subMesh, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 n0, Vector3 n1, Vector3 n2)
        {
            Vector3 projection = n0 + n1 + n2;
            int i0 = Emit(placement, p0, n0, BoxProjectedUv(p0, projection));
            int i1 = Emit(placement, p1, n1, BoxProjectedUv(p1, projection));
            int i2 = Emit(placement, p2, n2, BoxProjectedUv(p2, projection));
            Triangle(subMesh, i0, i1, i2);
        }

        private static Vector3 ArcDirection(Vector3 from, Vector3 to, float t)
        {
            return Vector3.Lerp(from, to, t).normalized;
        }

        private static float SignFromBit(int value, int bit)
        {
            return ((value >> bit) & 1) == 0 ? 1f : -1f;
        }

        private readonly struct BevelShape
        {
            public BevelShape(Vector3 center, Vector3 inner, float radius, int segments)
            {
                Center = center;
                Inner = inner;
                Radius = radius;
                Segments = segments;
            }

            public Vector3 Center { get; }

            public Vector3 Inner { get; }

            public float Radius { get; }

            public int Segments { get; }
        }

        private readonly struct BevelCorner
        {
            public BevelCorner(Vector3 center, Vector3 axisA, Vector3 axisB, Vector3 axisC, float radius, int segments)
            {
                Center = center;
                AxisA = axisA;
                AxisB = axisB;
                AxisC = axisC;
                Radius = radius;
                Segments = segments;
            }

            public Vector3 Center { get; }

            public Vector3 AxisA { get; }

            public Vector3 AxisB { get; }

            public Vector3 AxisC { get; }

            public float Radius { get; }

            public int Segments { get; }

            public Vector3 Direction(Vector2Int grid)
            {
                int remaining = Segments - grid.x - grid.y;
                return (AxisA * grid.x + AxisB * grid.y + AxisC * remaining).normalized;
            }

            public Vector3 Surface(Vector3 direction)
            {
                return Center + direction * Radius;
            }
        }
    }
}
