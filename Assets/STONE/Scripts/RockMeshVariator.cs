
using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class RockMeshVariator : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private bool randomSeedOnStart = true;
    [SerializeField] private int seed = 1234;

    [Header("Surface")]
    [Range(0f, 0.5f)]
    public float roughness = 0.2f;

    [Range(0.2f, 12f)]
    public float noiseFrequency = 3f;

    [Header("Dents")]
    [Range(0, 8)]
    public int dentCount = 3;

    [Range(0f, 0.5f)]
    public float dentDepth = 0.2f;

    [Header("Spikes")]
    [Range(0, 6)]
    public int spikeCount = 2;

    [Range(0f, 1f)]
    public float spikeHeight = 0.35f;

    [Header("Flattened Faces")]
    [Range(0, 3)]
    public int flatFaceCount = 1;

    [Range(0.25f, 1f)]
    public float cutPlaneDistance = 0.65f;

    private Mesh generatedMesh;

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh source = filter.sharedMesh;

        if (source == null || !source.isReadable)
        {
            Debug.LogError(
                "Rock mesh missing or Read/Write disabled.",
                this
            );
            return;
        }

        int currentSeed = randomSeedOnStart
            ? UnityEngine.Random.Range(1, int.MaxValue)
            : seed;

        System.Random rng = new System.Random(currentSeed);

        Vector3[] dents = CreateDirections(rng, dentCount);
        Vector3[] spikes = CreateDirections(rng, spikeCount);
        Vector3[] cuts = CreateDirections(rng, flatFaceCount);

        float offset = (float)rng.NextDouble() * 1000f;

        generatedMesh = Instantiate(source);
        generatedMesh.name = source.name + "_Variant";

        Vector3[] vertices = generatedMesh.vertices;

        Vector3 center = source.bounds.center;
        float radius = Mathf.Max(
            source.bounds.extents.magnitude,
            0.0001f
        );

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 point = vertices[i];
            Vector3 radial = (point - center).normalized;

            if (radial.sqrMagnitude < 0.0001f)
                continue;

            Vector3 sample =
                (point - center) / radius * noiseFrequency;

            float noise =
                Mathf.PerlinNoise(
                    sample.x + offset,
                    sample.y + offset
                );

            noise += Mathf.PerlinNoise(
                sample.y + offset,
                sample.z + offset
            );

            noise += Mathf.PerlinNoise(
                sample.z + offset,
                sample.x + offset
            );

            noise = (noise / 3f - 0.5f) * 2f;

            point += radial * noise * roughness * radius;

            foreach (Vector3 direction in dents)
            {
                float dot = Vector3.Dot(radial, direction);

                float influence = Mathf.InverseLerp(
                    0.55f, 1f, dot
                );

                influence = Mathf.SmoothStep(
                    0f, 1f, influence
                );

                point -= radial *
                    influence * dentDepth * radius;
            }

            foreach (Vector3 direction in spikes)
            {
                float dot = Vector3.Dot(radial, direction);

                float influence = Mathf.InverseLerp(
                    0.65f, 1f, dot
                );

                influence = Mathf.Pow(influence, 3f);

                point += radial *
                    influence * spikeHeight * radius;
            }

            foreach (Vector3 normal in cuts)
            {
                float distance =
                    Vector3.Dot(point - center, normal)
                    - cutPlaneDistance * radius;

                if (distance > 0f)
                    point -= normal * distance;
            }

            vertices[i] = point;
        }

        generatedMesh.vertices = vertices;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        filter.sharedMesh = generatedMesh;

        MeshCollider meshCollider =
            GetComponent<MeshCollider>();

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = generatedMesh;
        }
    }

    private static Vector3[] CreateDirections(
        System.Random rng, int count)
    {
        Vector3[] directions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float y = (float)(rng.NextDouble() * 2 - 1);
            float angle = (float)(
                rng.NextDouble() * Math.PI * 2
            );

            float r = Mathf.Sqrt(1f - y * y);

            directions[i] = new Vector3(
                r * Mathf.Cos(angle),
                y,
                r * Mathf.Sin(angle)
            );
        }

        return directions;
    }

    private void OnDestroy()
    {
        if (generatedMesh != null)
            Destroy(generatedMesh);
    }
}
