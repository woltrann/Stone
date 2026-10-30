using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralRocks
{
    /// <summary>
    /// Keeps the negative half-space of a triangulated closed surface and fills
    /// every simple intersection contour with oriented, planar-UV cap triangles.
    /// Supports multiple separate contours and concave (hole-free) contours.
    /// Does NOT support contour holes/nested loops or non-manifold source meshes.
    /// </summary>
    public static class RockMeshSlicer
    {
        private struct PointUV
        {
            public Vector3 Position;
            public Vector2 UV;
            public Vector3 Normal;
            public Color Color;

            public PointUV(Vector3 position, Vector2 uv)
            {
                Position = position;
                UV = uv;
                Normal = Vector3.zero;
                Color = Color.white;
            }
        }

        private struct Segment
        {
            public Vector3 A;
            public Vector3 B;
            public Segment(Vector3 a, Vector3 b) { A = a; B = b; }
        }

        private sealed class MeshWriter
        {
            internal readonly List<Vector3> Positions = new List<Vector3>();
            internal readonly List<Vector2> UVs = new List<Vector2>();
            internal readonly List<Vector3> Normals = new List<Vector3>();
            internal readonly List<Color> Colors = new List<Color>();
            internal readonly List<int>[] Triangles;

            internal MeshWriter(int submeshCount)
            {
                Triangles = new List<int>[submeshCount];
                for (int i = 0; i < submeshCount; i++)
                    Triangles[i] = new List<int>();
            }

            internal void AddTriangle(PointUV a, PointUV b, PointUV c, int submesh, float minAreaSquared)
            {
                Vector3 cross = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
                if (cross.sqrMagnitude <= minAreaSquared)
                    return;

                int first = Positions.Count;
                Positions.Add(a.Position);
                Positions.Add(b.Position);
                Positions.Add(c.Position);
                UVs.Add(a.UV);
                UVs.Add(b.UV);
                UVs.Add(c.UV);
                Normals.Add(a.Normal); Normals.Add(b.Normal); Normals.Add(c.Normal);
                Colors.Add(a.Color); Colors.Add(b.Color); Colors.Add(c.Color);
                Triangles[submesh].Add(first);
                Triangles[submesh].Add(first + 1);
                Triangles[submesh].Add(first + 2);
            }

            internal Mesh ToMesh(string name, bool preserveNormals)
            {
                var mesh = new Mesh { name = name };
                if (Positions.Count > 65535)
                    mesh.indexFormat = IndexFormat.UInt32;

                mesh.SetVertices(Positions);
                mesh.SetUVs(0, UVs);
                mesh.subMeshCount = Triangles.Length;
                for (int i = 0; i < Triangles.Length; i++)
                    mesh.SetTriangles(Triangles[i], i, false);

                if (preserveNormals) mesh.SetNormals(Normals);
                else mesh.RecalculateNormals(); // Preserve the generator's original flat-shading behaviour.
                mesh.SetColors(Colors);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        public static bool TryKeepNegative(Mesh input, Vector3 planePoint, Vector3 planeNormal,
            out Mesh result, out string error, float relativeTolerance = 0.00001f,
            bool preserveNormals = false, int maximumContourVertices = 2048)
        {
            result = null;
            error = string.Empty;
            if (input == null || !input.isReadable || planeNormal.sqrMagnitude < 0.000001f)
            {
                error = "Input must be a readable mesh and plane normal must be nonzero.";
                return false;
            }

            planeNormal.Normalize();
            Vector3[] vertices = input.vertices;
            Vector2[] uvs = input.uv;
            Vector3[] normals = input.normals;
            Color[] colors = input.colors;
            bool hasUV = uvs.Length == vertices.Length;
            bool hasNormals = normals.Length == vertices.Length;
            bool hasColors = colors.Length == vertices.Length;
            float epsilon = Mathf.Max(input.bounds.size.magnitude * relativeTolerance, 0.0000001f);
            float minAreaSquared = epsilon * epsilon * epsilon * epsilon;

            var writer = new MeshWriter(input.subMeshCount + 1);
            var segments = new List<Segment>();
            int clippedCount = 0;
            var tri = new PointUV[3];
            var d = new float[3];
            var polygon = new List<PointUV>(4);
            var intersectionPoints = new List<Vector3>(3);

            for (int submesh = 0; submesh < input.subMeshCount; submesh++)
            {
                int[] indices = input.GetTriangles(submesh);
                for (int j = 0; j + 2 < indices.Length; j += 3)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int idx = indices[j + k];
                        tri[k] = new PointUV(vertices[idx], hasUV ? uvs[idx] : Vector2.zero);
                        tri[k].Normal = hasNormals ? normals[idx] : Vector3.zero;
                        tri[k].Color = hasColors ? colors[idx] : Color.white;
                    }

                    if (!hasNormals)
                    {
                        var normal = Vector3.Cross(tri[1].Position-tri[0].Position, tri[2].Position-tri[0].Position).normalized;
                        for (int k=0;k<3;k++) tri[k].Normal = normal;
                    }
                    bool positive = false;
                    bool negative = false;
                    int onPlane = 0;
                    for (int k = 0; k < 3; k++)
                    {
                        d[k] = Vector3.Dot(tri[k].Position - planePoint, planeNormal);
                        if (d[k] > epsilon) positive = true;
                        else if (d[k] < -epsilon) negative = true;
                        else
                        {
                            // The same near-plane vertex must be shared by the clipped skin and cap.
                            // Merely classifying it as on-plane creates tiny T-junctions at shallow cuts.
                            tri[k].Position -= planeNormal * d[k];
                            d[k] = 0f;
                            onPlane++;
                        }
                    }

                    if (!positive)
                    {
                        writer.AddTriangle(tri[0], tri[1], tri[2], submesh, minAreaSquared);
                        continue;
                    }

                    clippedCount++;
                    if (negative || onPlane > 0)
                    {
                        ClipTriangle(tri, d, epsilon, polygon);
                        for (int k = 1; k + 1 < polygon.Count; k++)
                            writer.AddTriangle(polygon[0], polygon[k], polygon[k + 1], submesh, minAreaSquared);

                        // Include an edge that lies exactly on the cutting plane only
                        // on its discarded-side triangle (avoids duplicate segments).
                        if (negative || onPlane >= 2)
                            CollectIntersectionSegment(tri, d, epsilon, segments, intersectionPoints);
                    }
                }
            }

            if (clippedCount == 0 || segments.Count == 0)
            {
                error = "Plane did not intersect a closed surface.";
                return false;
            }

            if (!TryBuildLoops(segments, input.bounds.center, epsilon * 2f,
                    out List<List<Vector3>> loops, out error))
                return false;

            Vector3 axisU = Vector3.Cross(
                Mathf.Abs(planeNormal.y) < 0.95f ? Vector3.up : Vector3.right,
                planeNormal).normalized;
            Vector3 axisV = Vector3.Cross(planeNormal, axisU).normalized;
            if (HasNestedLoops(loops, axisU, axisV))
            {
                error = "Nested cap contours (holes) are not supported.";
                return false;
            }
            float uvScale = 1f / Mathf.Max(input.bounds.size.magnitude, 0.001f);

            int capSubmesh = input.subMeshCount;
            foreach (List<Vector3> loop in loops)
            {
                if (loop.Count > maximumContourVertices)
                { error = $"Cut contour exceeds the configured limit of {maximumContourVertices} vertices."; return false; }
                if (!TryAddCap(loop, writer, capSubmesh, axisU, axisV,
                        uvScale, epsilon, minAreaSquared, planeNormal, out error))
                    return false;
            }

            result = writer.ToMesh(input.name + "_Sliced", preserveNormals);
            return true;
        }

        private static void ClipTriangle(PointUV[] tri, float[] d, float epsilon, List<PointUV> output)
        {
            output.Clear();
            for (int i = 0; i < 3; i++)
            {
                int previous = (i + 2) % 3;
                bool inPrevious = d[previous] <= epsilon;
                bool inCurrent = d[i] <= epsilon;

                if (inPrevious != inCurrent)
                {
                    float denominator = d[previous] - d[i];
                    float t = Mathf.Abs(denominator) < 1e-20f ? 0f :
                        Mathf.Clamp01(d[previous] / denominator);
                    output.Add(Lerp(tri[previous], tri[i], t));
                }
                if (inCurrent)
                    output.Add(tri[i]);
            }

            // Remove coincident neighbors caused by vertices lying on the plane.
            for (int i = output.Count - 1; i >= 0 && output.Count > 1; i--)
            {
                int previous = (i + output.Count - 1) % output.Count;
                if ((output[i].Position - output[previous].Position).sqrMagnitude <= epsilon * epsilon)
                    output.RemoveAt(i);
            }
        }

        private static PointUV Lerp(PointUV a, PointUV b, float t)
        {
            return new PointUV(Vector3.LerpUnclamped(a.Position, b.Position, t),
                Vector2.LerpUnclamped(a.UV, b.UV, t)) {
                Normal = Vector3.LerpUnclamped(a.Normal,b.Normal,t).normalized,
                Color = Color.LerpUnclamped(a.Color,b.Color,t) };
        }

        private static void CollectIntersectionSegment(PointUV[] tri, float[] d,
            float epsilon, List<Segment> segments, List<Vector3> points)
        {
            points.Clear();
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                if (Mathf.Abs(d[i]) <= epsilon)
                    AddUnique(points, tri[i].Position, epsilon);

                if ((d[i] < -epsilon && d[next] > epsilon) ||
                    (d[i] > epsilon && d[next] < -epsilon))
                {
                    float t = Mathf.Clamp01(d[i] / (d[i] - d[next]));
                    AddUnique(points, Vector3.LerpUnclamped(tri[i].Position,
                        tri[next].Position, t), epsilon);
                }
            }

            if (points.Count == 2)
                segments.Add(new Segment(points[0], points[1]));
        }

        private static void AddUnique(List<Vector3> points, Vector3 point, float epsilon)
        {
            foreach (Vector3 existing in points)
                if ((existing - point).sqrMagnitude <= epsilon * epsilon)
                    return;
            points.Add(point);
        }

        private static bool TryBuildLoops(List<Segment> segments, Vector3 origin, float tolerance,
            out List<List<Vector3>> loops, out string error)
        {
            loops = new List<List<Vector3>>();
            error = string.Empty;
            var nodeIDs = new Dictionary<Vector3Int, List<int>>();
            var points = new List<Vector3>();
            var adjacency = new Dictionary<int, List<int>>();
            var uniqueEdges = new HashSet<ulong>();

            foreach (Segment segment in segments)
            {
                int a = GetNode(segment.A, origin, tolerance, nodeIDs, points);
                int b = GetNode(segment.B, origin, tolerance, nodeIDs, points);
                if (a == b) continue;

                uint lo = (uint)Mathf.Min(a, b);
                uint hi = (uint)Mathf.Max(a, b);
                ulong key = ((ulong)lo << 32) | hi;
                if (!uniqueEdges.Add(key)) continue;

                if (!adjacency.ContainsKey(a)) adjacency[a] = new List<int>(2);
                if (!adjacency.ContainsKey(b)) adjacency[b] = new List<int>(2);
                adjacency[a].Add(b);
                adjacency[b].Add(a);
            }

            foreach (KeyValuePair<int, List<int>> pair in adjacency)
            {
                if (pair.Value.Count != 2)
                {
                    error = "Open/branching cut contour. Use a closed, manifold rock mesh without holes.";
                    return false;
                }
            }

            var visited = new HashSet<int>();
            foreach (int start in adjacency.Keys)
            {
                if (visited.Contains(start)) continue;
                var loop = new List<Vector3>();
                int previous = -1;
                int current = start;
                int safety = 0;
                do
                {
                    if (visited.Contains(current))
                    {
                        error = "Cut contour intersects itself or branches.";
                        return false;
                    }
                    visited.Add(current);
                    loop.Add(points[current]);
                    List<int> neighbors = adjacency[current];
                    int next = neighbors[0] == previous ? neighbors[1] : neighbors[0];
                    previous = current;
                    current = next;
                    if (++safety > adjacency.Count + 1)
                    {
                        error = "Could not close the cut contour.";
                        return false;
                    }
                } while (current != start);

                if (loop.Count < 3)
                {
                    error = "Degenerate cap contour.";
                    return false;
                }
                loops.Add(loop);
            }
            if (loops.Count == 0)
            {
                error = "No closed cut contours were found.";
                return false;
            }
            return true;
        }

        private static int GetNode(Vector3 p, Vector3 origin, float tolerance,
            Dictionary<Vector3Int, List<int>> lookup, List<Vector3> points)
        {
            Vector3 v = (p - origin) / tolerance;
            var key = new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
            // Also search adjacent buckets: mathematically identical edge intersections
            // can round to opposite sides of a hash grid boundary.
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                var neighbor = new Vector3Int(key.x + x, key.y + y, key.z + z);
                if (lookup.TryGetValue(neighbor, out var bucket))
                    foreach (int existing in bucket)
                        if ((points[existing] - p).sqrMagnitude <= tolerance * tolerance) return existing;
            }
            int id = points.Count;
            if (!lookup.TryGetValue(key,out var cell)) lookup[key] = cell = new List<int>();
            cell.Add(id);
            points.Add(p);
            return id;
        }

        private static bool HasNestedLoops(List<List<Vector3>> loops, Vector3 axisU, Vector3 axisV)
        {
            for (int i = 0; i < loops.Count; i++)
            {
                var polygon = new List<Vector2>();
                foreach (Vector3 v in loops[i])
                    polygon.Add(new Vector2(Vector3.Dot(v, axisU), Vector3.Dot(v, axisV)));

                for (int j = 0; j < loops.Count; j++)
                {
                    if (i == j) continue;
                    Vector3 sample = loops[j][0];
                    if (PointInPolygon(new Vector2(Vector3.Dot(sample, axisU),
                        Vector3.Dot(sample, axisV)), polygon))
                        return true;
                }
            }
            return false;
        }

        private static bool PointInPolygon(Vector2 p, List<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) &&
                    p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private static bool TryAddCap(List<Vector3> loop, MeshWriter writer, int submesh,
            Vector3 axisU, Vector3 axisV, float uvScale, float epsilon,
            float minAreaSquared, Vector3 normal, out string error)
        {
            error = string.Empty;
            var points2 = new List<Vector2>(loop.Count);
            foreach (Vector3 p in loop)
                points2.Add(new Vector2(Vector3.Dot(p, axisU), Vector3.Dot(p, axisV)));

            float area = SignedArea(points2);
            if (Mathf.Abs(area) <= epsilon * epsilon)
            {
                error = "Cut contour has near-zero area.";
                return false;
            }
            if (area < 0f)
            {
                loop.Reverse();
                points2.Reverse();
            }

            var remaining = new List<int>(loop.Count);
            for (int i = 0; i < loop.Count; i++) remaining.Add(i);
            int guard = 0;
            while (remaining.Count > 3)
            {
                bool found = false;
                for (int k = 0; k < remaining.Count; k++)
                {
                    int a = remaining[(k + remaining.Count - 1) % remaining.Count];
                    int b = remaining[k];
                    int c = remaining[(k + 1) % remaining.Count];
                    if (Cross(points2[b] - points2[a], points2[c] - points2[a]) <= epsilon * epsilon)
                        continue;

                    bool containsOther = false;
                    for (int j = 0; j < remaining.Count; j++)
                    {
                        int p = remaining[j];
                        if (p == a || p == b || p == c) continue;
                        if (InsideTriangle(points2[p], points2[a], points2[b], points2[c],
                                epsilon * epsilon))
                        {
                            containsOther = true;
                            break;
                        }
                    }
                    if (containsOther) continue;

                    writer.AddTriangle(CapPoint(loop[a], points2[a], uvScale, normal),
                        CapPoint(loop[b], points2[b], uvScale, normal),
                        CapPoint(loop[c], points2[c], uvScale, normal), submesh, minAreaSquared);
                    remaining.RemoveAt(k);
                    found = true;
                    break;
                }

                if (!found || ++guard > loop.Count * loop.Count)
                {
                    error = "Cap triangulation failed (self-intersecting or non-simple contour).";
                    return false;
                }
            }

            if (remaining.Count == 3)
            {
                int a = remaining[0], b = remaining[1], c = remaining[2];
                writer.AddTriangle(CapPoint(loop[a], points2[a], uvScale, normal),
                    CapPoint(loop[b], points2[b], uvScale, normal),
                    CapPoint(loop[c], points2[c], uvScale, normal), submesh, minAreaSquared);
            }
            return true;
        }

        private static PointUV CapPoint(Vector3 p, Vector2 projected, float scale, Vector3 normal)
        {
            return new PointUV(p, projected * scale + Vector2.one * 0.5f) { Normal = normal };
        }

        private static float SignedArea(List<Vector2> poly)
        {
            float area = 0f;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Count];
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c, float epsilon)
        {
            float x = Cross(b - a, p - a);
            float y = Cross(c - b, p - b);
            float z = Cross(a - c, p - c);
            return x >= -epsilon && y >= -epsilon && z >= -epsilon;
        }
    }
}
