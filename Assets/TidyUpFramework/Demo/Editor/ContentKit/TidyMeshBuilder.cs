using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public sealed partial class TidyMeshBuilder
    {
        private const int SixteenBitVertexLimit = 65535;
        private const int SmoothNormalUvChannel = 3;
        private const float MinimumExtent = 1e-5f;
        private const float DegenerateLengthSquared = 1e-12f;

        private static readonly Vector3[] BoxFaceNormals =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
        };

        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> texcoords = new List<Vector2>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<List<int>> subMeshIndices = new List<List<int>>();

        public TidyMeshBuilder(int subMeshCount = 1)
        {
            int count = Mathf.Max(1, subMeshCount);
            for (int i = 0; i < count; i++)
            {
                subMeshIndices.Add(new List<int>());
            }
        }

        public Vector2 UvScale { get; set; } = Vector2.one;

        public Color VertexColor { get; set; } = Color.white;

        public int VertexCount => positions.Count;

        public int SubMeshCount => subMeshIndices.Count;

        public static Matrix4x4 TRS(Vector3 position, Vector3 euler, Vector3 scale)
        {
            return Matrix4x4.TRS(position, Quaternion.Euler(euler), scale);
        }

        public void Clear()
        {
            positions.Clear();
            normals.Clear();
            texcoords.Clear();
            colors.Clear();
            foreach (List<int> indices in subMeshIndices)
            {
                indices.Clear();
            }
        }

        public void AddBox(Vector3 center, Vector3 size, int subMesh = 0, Matrix4x4? transform = null)
        {
            var placement = new TidyPlacement(transform);
            Vector3 half = Abs(size) * 0.5f;
            foreach (Vector3 normal in BoxFaceNormals)
            {
                AddAxisFace(placement, subMesh, center, half, normal, 0f);
            }
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int subMesh = 0, Matrix4x4? transform = null)
        {
            var placement = new TidyPlacement(transform);
            Vector3 normal = SafeNormalize(Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a), Vector3.up);
            Vector3 uAxis = SafeNormalize(d - a, SafeNormalize(c - b, Vector3.right));
            Vector3 vAxis = SafeNormalize(Vector3.Cross(uAxis, normal), Vector3.forward);
            int ia = EmitPlanar(placement, a, normal, a, uAxis, vAxis);
            int ib = EmitPlanar(placement, b, normal, a, uAxis, vAxis);
            int ic = EmitPlanar(placement, c, normal, a, uAxis, vAxis);
            int id = EmitPlanar(placement, d, normal, a, uAxis, vAxis);
            Quad(subMesh, ia, ib, ic, id);
        }

        public void AddPlane(Vector2 size, int segmentsX, int segmentsZ, int subMesh = 0, Matrix4x4? transform = null, bool normalizedUv = false)
        {
            var placement = new TidyPlacement(transform);
            int columns = Mathf.Max(1, segmentsX);
            int rows = Mathf.Max(1, segmentsZ);
            int start = positions.Count;
            for (int z = 0; z <= rows; z++)
            {
                for (int x = 0; x <= columns; x++)
                {
                    float tx = x / (float)columns;
                    float tz = z / (float)rows;
                    var point = new Vector3((tx - 0.5f) * size.x, 0f, (tz - 0.5f) * size.y);
                    Vector2 uv = normalizedUv ? new Vector2(tx, tz) : ScaleUv(new Vector2(point.x, point.z));
                    Emit(placement, point, Vector3.up, uv);
                }
            }

            StitchGrid(subMesh, start, columns + 1, rows + 1);
        }

        public void AddMesh(Mesh source, Matrix4x4 transform, int subMeshOffset = 0)
        {
            if (source == null)
            {
                return;
            }

            var placement = new TidyPlacement(transform);
            bool mirrored = transform.determinant < 0f;
            var sourcePositions = new List<Vector3>();
            var sourceNormals = new List<Vector3>();
            var sourceUvs = new List<Vector2>();
            var sourceColors = new List<Color>();
            source.GetVertices(sourcePositions);
            source.GetNormals(sourceNormals);
            source.GetUVs(0, sourceUvs);
            source.GetColors(sourceColors);
            int baseIndex = positions.Count;
            for (int i = 0; i < sourcePositions.Count; i++)
            {
                Vector3 normal = i < sourceNormals.Count ? sourceNormals[i] : Vector3.up;
                positions.Add(placement.Point(sourcePositions[i]));
                normals.Add(placement.Normal(normal));
                texcoords.Add(i < sourceUvs.Count ? sourceUvs[i] : Vector2.zero);
                colors.Add(i < sourceColors.Count ? sourceColors[i] : VertexColor);
            }

            for (int s = 0; s < source.subMeshCount; s++)
            {
                if (source.GetTopology(s) == MeshTopology.Triangles)
                {
                    AppendTriangles(source.GetTriangles(s), Indices(subMeshOffset + s), baseIndex, mirrored);
                }
            }
        }

        public Mesh Build(string name, bool bakeSmoothNormalsToUv3 = true)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = positions.Count > SixteenBitVertexLimit ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, texcoords);
            mesh.SetColors(colors);
            mesh.subMeshCount = subMeshIndices.Count;
            for (int i = 0; i < subMeshIndices.Count; i++)
            {
                mesh.SetTriangles(subMeshIndices[i], i, false);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (bakeSmoothNormalsToUv3)
            {
                mesh.SetUVs(SmoothNormalUvChannel, TidySmoothNormalBaker.Bake(positions, normals));
            }

            return mesh;
        }

        private static void AppendTriangles(int[] source, List<int> target, int baseIndex, bool mirrored)
        {
            for (int k = 0; k + 2 < source.Length; k += 3)
            {
                target.Add(baseIndex + source[k]);
                target.Add(baseIndex + source[mirrored ? k + 2 : k + 1]);
                target.Add(baseIndex + source[mirrored ? k + 1 : k + 2]);
            }
        }

        private void AddAxisFace(in TidyPlacement placement, int subMesh, Vector3 center, Vector3 innerHalf, Vector3 normal, float outset)
        {
            FaceTangents(normal, out Vector3 tangent, out Vector3 bitangent);
            float halfTangent = Vector3.Dot(innerHalf, Abs(tangent));
            float halfBitangent = Vector3.Dot(innerHalf, Abs(bitangent));
            if (halfTangent < MinimumExtent || halfBitangent < MinimumExtent)
            {
                return;
            }

            Vector3 faceCenter = center + normal * (Vector3.Dot(innerHalf, Abs(normal)) + outset);
            Vector3 t = tangent * halfTangent;
            Vector3 b = bitangent * halfBitangent;
            int i0 = EmitBoxProjected(placement, faceCenter - t - b, normal);
            int i1 = EmitBoxProjected(placement, faceCenter - t + b, normal);
            int i2 = EmitBoxProjected(placement, faceCenter + t + b, normal);
            int i3 = EmitBoxProjected(placement, faceCenter + t - b, normal);
            Quad(subMesh, i0, i1, i2, i3);
        }

        private int EmitBoxProjected(in TidyPlacement placement, Vector3 point, Vector3 normal)
        {
            return Emit(placement, point, normal, BoxProjectedUv(point, normal));
        }

        private int EmitPlanar(in TidyPlacement placement, Vector3 point, Vector3 normal, Vector3 origin, Vector3 uAxis, Vector3 vAxis)
        {
            Vector3 offset = point - origin;
            return Emit(placement, point, normal, ScaleUv(new Vector2(Vector3.Dot(offset, uAxis), Vector3.Dot(offset, vAxis))));
        }

        private int Emit(in TidyPlacement placement, Vector3 point, Vector3 normal, Vector2 uv)
        {
            positions.Add(placement.Point(point));
            normals.Add(placement.Normal(normal));
            texcoords.Add(uv);
            colors.Add(VertexColor);
            return positions.Count - 1;
        }

        private void Triangle(int subMesh, int a, int b, int c)
        {
            List<int> indices = Indices(subMesh);
            Vector3 origin = positions[a];
            Vector3 facing = Vector3.Cross(positions[b] - origin, positions[c] - origin);
            Vector3 expected = normals[a] + normals[b] + normals[c];
            indices.Add(a);
            if (Vector3.Dot(facing, expected) < 0f)
            {
                indices.Add(c);
                indices.Add(b);
            }
            else
            {
                indices.Add(b);
                indices.Add(c);
            }
        }

        private void Quad(int subMesh, int a, int b, int c, int d)
        {
            Triangle(subMesh, a, b, c);
            Triangle(subMesh, a, c, d);
        }

        private void StitchGrid(int subMesh, int start, int columns, int rows)
        {
            for (int row = 0; row < rows - 1; row++)
            {
                for (int column = 0; column < columns - 1; column++)
                {
                    int i0 = start + row * columns + column;
                    int i1 = i0 + 1;
                    int i2 = i1 + columns;
                    int i3 = i0 + columns;
                    Quad(subMesh, i0, i1, i2, i3);
                }
            }
        }

        private List<int> Indices(int subMesh)
        {
            int index = Mathf.Max(0, subMesh);
            while (subMeshIndices.Count <= index)
            {
                subMeshIndices.Add(new List<int>());
            }

            return subMeshIndices[index];
        }

        private Vector2 BoxProjectedUv(Vector3 point, Vector3 normal)
        {
            FaceTangents(DominantAxis(normal), out Vector3 tangent, out Vector3 bitangent);
            return ScaleUv(new Vector2(Vector3.Dot(point, tangent), Vector3.Dot(point, bitangent)));
        }

        private Vector2 ScaleUv(Vector2 uv)
        {
            return Vector2.Scale(uv, UvScale);
        }

        private static void FaceTangents(Vector3 axisNormal, out Vector3 tangent, out Vector3 bitangent)
        {
            bitangent = Mathf.Abs(axisNormal.y) > 0.5f ? new Vector3(0f, 0f, Mathf.Sign(axisNormal.y)) : Vector3.up;
            tangent = Vector3.Cross(axisNormal, bitangent);
        }

        private static Vector3 DominantAxis(Vector3 direction)
        {
            float ax = Mathf.Abs(direction.x);
            float ay = Mathf.Abs(direction.y);
            float az = Mathf.Abs(direction.z);
            if (ax >= ay && ax >= az)
            {
                return new Vector3(Mathf.Sign(direction.x), 0f, 0f);
            }

            return ay >= az ? new Vector3(0f, Mathf.Sign(direction.y), 0f) : new Vector3(0f, 0f, Mathf.Sign(direction.z));
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static float MinComponent(Vector3 value)
        {
            return Mathf.Min(value.x, Mathf.Min(value.y, value.z));
        }

        private static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
        {
            return value.sqrMagnitude > DegenerateLengthSquared ? value.normalized : fallback;
        }
    }
}
