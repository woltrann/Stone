using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralRocks
{
    /// <summary>
    /// Deforms the input mesh in local space, then optionally clips it against
    /// cutting planes. The source asset is NEVER mutated.
    /// </summary>
    public static class RockMeshBuilder
    {
        public struct Options
        {
            public RockShape Shape;
            public float Roughness;
            public float Dents;
            public float Spikes;
            public float Cuts;
            public int Seed;
        }

        public static Mesh Build(Mesh source, Options settings, out int completedCuts, out string message)
        {
            if (source == null || !source.isReadable)
                throw new ArgumentException("A readable source mesh is required.");

            System.Random random = new System.Random(settings.Seed);
            Mesh current = UnityEngine.Object.Instantiate(source);
            current.name = source.name + "_Deformed";
            completedCuts = 0;
            message = string.Empty;

            Vector3 center = source.bounds.center;
            float radius = Mathf.Max(source.bounds.extents.magnitude, 0.0001f);

            try
            {
                Vector3[] vertices = current.vertices;
                var dentDirections = CreateDirections(random, settings.Shape == RockShape.Jagged ? 4 : 3);
                var spikeDirections = CreateDirections(random, settings.Shape == RockShape.Jagged ? 5 : 2);
                float noiseOffset = (float)random.NextDouble() * 1000f;

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 p = vertices[i];
                    Vector3 outward = (p - center).normalized;
                    if (outward.sqrMagnitude < 0.001f)
                        continue;

                    Vector3 q = (p - center) / radius * 4.5f;
                    float noise = (Mathf.PerlinNoise(q.x + noiseOffset, q.y + noiseOffset) +
                                   Mathf.PerlinNoise(q.y + noiseOffset, q.z + noiseOffset) +
                                   Mathf.PerlinNoise(q.z + noiseOffset, q.x + noiseOffset)) / 3f;
                    p += outward * (noise * 2f - 1f) * radius * settings.Roughness * 0.18f;

                    foreach (Vector3 direction in dentDirections)
                    {
                        float influence = Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(0.45f, 0.96f, Vector3.Dot(outward, direction)));
                        p -= outward * radius * settings.Dents * 0.45f * influence;
                    }

                    foreach (Vector3 direction in spikeDirections)
                    {
                        float influence = Mathf.Pow(Mathf.InverseLerp(
                            0.58f, 0.98f, Vector3.Dot(outward, direction)), 3f);
                        float boost = settings.Shape == RockShape.Jagged ? 1.6f : 1f;
                        p += outward * radius * settings.Spikes * 0.55f * boost * influence;
                    }

                    vertices[i] = p;
                }

                current.vertices = vertices;
                current.RecalculateNormals();
                current.RecalculateBounds();

                int attempts = CalculateCutCount(settings);
                Vector3 slabAxis = RandomUnitVector(random);
                var failures = new List<string>();

                for (int i = 0; i < attempts; i++)
                {
                    Vector3 normal;
                    if (settings.Shape == RockShape.Slab)
                        normal = i == 0 ? slabAxis : (i == 1 ? -slabAxis : RandomUnitVector(random));
                    else
                        normal = RandomUnitVector(random);

                    Vector3[] currentVertices = current.vertices;
                    float farthest = float.NegativeInfinity;
                    float nearest = float.PositiveInfinity;
                    foreach (Vector3 p in currentVertices)
                    {
                        float d = Vector3.Dot(p - center, normal);
                        farthest = Mathf.Max(farthest, d);
                        nearest = Mathf.Min(nearest, d);
                    }

                    if (farthest - nearest < radius * 0.01f)
                        continue;

                    // A plane inside the actual projected bounds guarantees a geometric cut.
                    float inset = Mathf.Lerp(0.10f, 0.43f, settings.Cuts);
                    if (settings.Shape == RockShape.Fractured)
                        inset = Mathf.Min(0.55f, inset + 0.07f);
                    float distance = Mathf.Lerp(farthest, nearest, inset);
                    Vector3 planePoint = center + normal * distance;

                    if (RockMeshSlicer.TryKeepNegative(current, planePoint, normal,
                            out Mesh next, out string error))
                    {
                        DestroyMesh(current);
                        current = next;
                        completedCuts++;
                    }
                    else
                    {
                        failures.Add("Cut " + (i + 1) + ": " + error);
                    }
                }

                message = "Generated " + current.vertexCount + " vertices; " + completedCuts +
                          "/" + attempts + " real cuts with caps.";
                if (failures.Count > 0)
                    message += " Failed cuts: " + string.Join("; ", failures);

                return current;
            }
            catch
            {
                DestroyMesh(current);
                throw;
            }
        }

        private static int CalculateCutCount(Options o)
        {
            if (o.Cuts <= 0.001f)
                return 0;

            switch (o.Shape)
            {
                case RockShape.Jagged: return 1;
                case RockShape.Fractured: return o.Cuts > 0.55f ? 3 : 2;
                case RockShape.Slab: return o.Cuts > 0.65f ? 3 : 2;
                default: return o.Cuts > 0.7f ? 2 : 1;
            }
        }

        private static Vector3[] CreateDirections(System.Random rng, int count)
        {
            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
                result[i] = RandomUnitVector(rng);
            return result;
        }

        private static Vector3 RandomUnitVector(System.Random rng)
        {
            float y = (float)(rng.NextDouble() * 2.0 - 1.0);
            float radians = (float)(rng.NextDouble() * Math.PI * 2.0);
            float radius = Mathf.Sqrt(1f - y * y);
            return new Vector3(radius * Mathf.Cos(radians), y, radius * Mathf.Sin(radians));
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
            else UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
