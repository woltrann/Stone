using System;
using System.Collections.Generic;
using ProceduralRocks;
using UnityEditor;
using UnityEngine;

/// <summary>Execute from Tools > Procedural Rocks > Run Cut Self-Test.</summary>
public static class RockGeneratorSelfTest
{
    [MenuItem("Tools/Procedural Rocks/Run Cut Self-Test")]
    public static void Run()
    {
        Mesh source = MakeClosedCube();
        Mesh cut = null;
        Mesh generated = null;
        try
        {
            Vector3 normal = new Vector3(0.83f, 0.27f, -0.48f).normalized;
            if (!RockMeshSlicer.TryKeepNegative(source, new Vector3(0.13f, 0.02f, 0.11f),
                normal, out cut, out string error))
                throw new Exception("Slicer failed on closed cube: " + error);

            if (cut.subMeshCount != 2 || cut.GetTriangles(1).Length == 0)
                throw new Exception("No cap submesh was generated.");

            if (!HasClosedPositionalEdges(cut))
                throw new Exception("Cut mesh has open edges after welding matching positions.");

            var settings = new RockMeshBuilder.Options
            {
                Shape = RockShape.Natural,
                Roughness = 0.45f,
                Dents = 0.35f,
                Spikes = 0.25f,
                Cuts = 0.40f,
                Seed = 1234
            };
            generated = RockMeshBuilder.Build(source, settings, out int count, out string message);
            if (count < 1 || generated.subMeshCount < 2)
                throw new Exception("Generator did not produce at least one capped cut: " + message);
            if (!HasClosedPositionalEdges(generated))
                throw new Exception("Generated mesh has open edges after welding matching positions.");

            Debug.Log("[Procedural Rocks] PASS: closed cube slice, cap material submesh, " +
                      "default seeded generation, positional edge closure.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Procedural Rocks] FAIL: " + e.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            if (cut != null) UnityEngine.Object.DestroyImmediate(cut);
            if (generated != null) UnityEngine.Object.DestroyImmediate(generated);
        }
    }

    private static Mesh MakeClosedCube()
    {
        var mesh = new Mesh { name = "ClosedTestCube" };
        mesh.vertices = new[]
        {
            new Vector3(-1,-1,-1), new Vector3(1,-1,-1),
            new Vector3(1,-1,1), new Vector3(-1,-1,1),
            new Vector3(-1,1,-1), new Vector3(1,1,-1),
            new Vector3(1,1,1), new Vector3(-1,1,1)
        };
        mesh.triangles = new[]
        {
            0,1,2, 0,2,3, // bottom
            4,6,5, 4,7,6, // top
            0,5,1, 0,4,5, // front
            3,2,6, 3,6,7, // back
            0,3,7, 0,7,4, // left
            1,5,6, 1,6,2  // right
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    // Position-welded edge count: every undirected edge on a closed surface
    // must appear exactly twice. This also detects many T-junctions / missing caps.
    private static bool HasClosedPositionalEdges(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        var counts = new Dictionary<string, int>();
        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            int[] triangles = mesh.GetTriangles(submesh);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    string a = Key(vertices[triangles[i + e]]);
                    string b = Key(vertices[triangles[i + (e + 1) % 3]]);
                    string edge = string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
                    counts.TryGetValue(edge, out int current);
                    counts[edge] = current + 1;
                }
            }
        }

        foreach (int value in counts.Values)
            if (value != 2) return false;
        return counts.Count > 0;
    }

    private static string Key(Vector3 v)
    {
        return Mathf.RoundToInt(v.x * 10000f) + "," +
               Mathf.RoundToInt(v.y * 10000f) + "," +
               Mathf.RoundToInt(v.z * 10000f);
    }
}
