using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyDecorBooks
    {
        static readonly string[] Colors = { "Red", "Blue", "Green", "Purple", "Brown", "Teal", "Orange", "Yellow" };
        const int BrassSubMesh = 8;
        const int PagesSubMesh = 9;
        const string Folder = TidyDemoPaths.Meshes + "/Decor";

        public static Material[] Materials(TidyDemoContext context)
        {
            var materials = new Material[PagesSubMesh + 1];
            for (var index = 0; index < Colors.Length; index++)
            {
                var color = Color.Lerp(TidyArtLibrary.PaletteColor(Colors[index]), new Color(0.1f, 0.07f, 0.06f), 0.45f);
                materials[index] = TidyArtLibrary.Plain(context, "Leather_Decor_" + Colors[index], color, 0.3f);
            }

            materials[BrassSubMesh] = context.Material("Metal_Brass");
            materials[PagesSubMesh] = context.Material("Paper_Pages");
            return materials;
        }

        public static Mesh Row(float length, float maxHeight, int seed, float fill = 0.92f)
        {
            var path = Folder + "/SM_DecorBooks_" + Mathf.RoundToInt(length * 100f) + "_" + Mathf.RoundToInt(maxHeight * 100f) + "_" + seed + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                return existing;
            }

            TidyAssetFactory.EnsureFolder(Folder);
            var random = new System.Random(seed);
            var builder = new TidyMeshBuilder(PagesSubMesh + 1);
            var cursor = 0.01f;
            var limit = length * fill;
            while (cursor < limit)
            {
                var thickness = Range(random, 0.022f, 0.06f);
                var height = Mathf.Min(maxHeight - 0.02f, Range(random, 0.19f, 0.31f));
                var depth = Range(random, 0.15f, 0.2f);
                if (cursor + thickness > length - 0.01f)
                {
                    break;
                }

                var lean = random.NextDouble() < 0.06 && cursor > 0.2f ? Range(random, 8f, 16f) : 0f;
                var center = new Vector3(cursor + thickness * 0.5f, 0f, 0f);
                var matrix = TidyMeshBuilder.TRS(center + new Vector3(lean > 0f ? height * Mathf.Sin(lean * Mathf.Deg2Rad) * 0.5f : 0f, 0f, 0f), new Vector3(0f, 0f, -lean), Vector3.one);
                var colorIndex = random.Next(Colors.Length);
                builder.AddBeveledBox(new Vector3(0f, height * 0.5f, 0f), new Vector3(thickness, height, depth), 0.003f, 1, colorIndex, matrix);
                builder.AddBox(new Vector3(0f, height * 0.5f, -0.004f), new Vector3(thickness - 0.006f, height - 0.01f, depth - 0.004f), PagesSubMesh, matrix);
                if (random.NextDouble() < 0.7)
                {
                    var band = Range(random, 0.006f, 0.012f);
                    builder.AddBox(new Vector3(0f, height * 0.82f, depth * 0.5f + 0.0005f), new Vector3(thickness * 0.94f, band, 0.002f), BrassSubMesh, matrix);
                    builder.AddBox(new Vector3(0f, height * 0.18f, depth * 0.5f + 0.0005f), new Vector3(thickness * 0.94f, band, 0.002f), BrassSubMesh, matrix);
                }

                cursor += thickness + (lean > 0f ? height * Mathf.Sin(lean * Mathf.Deg2Rad) : 0.002f);
                if (random.NextDouble() < 0.05)
                {
                    cursor += Range(random, 0.04f, 0.12f);
                }
            }

            return TidyMeshAssets.SaveMesh(builder.Build("SM_DecorBooks_" + seed), path);
        }

        static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
