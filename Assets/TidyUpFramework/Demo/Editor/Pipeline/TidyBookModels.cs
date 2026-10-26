using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public readonly struct TidyBookShape
    {
        public readonly string Key;
        public readonly float Thickness;
        public readonly float Height;
        public readonly float Depth;

        public TidyBookShape(string key, float thickness, float height, float depth)
        {
            Key = key;
            Thickness = thickness;
            Height = height;
            Depth = depth;
        }
    }

    public static class TidyBookModels
    {
        public const float SpineAtlasFraction = 0.25f;
        const float CoverThickness = 0.004f;
        const float DecalLift = 0.0006f;
        const float PageInset = 0.006f;

        public static readonly TidyBookShape[] Shapes =
        {
            new TidyBookShape("Slim", 0.028f, 0.22f, 0.16f),
            new TidyBookShape("Standard", 0.04f, 0.24f, 0.17f),
            new TidyBookShape("Thick", 0.058f, 0.25f, 0.18f),
            new TidyBookShape("Tall", 0.042f, 0.3f, 0.2f),
            new TidyBookShape("Tome", 0.075f, 0.29f, 0.21f),
            new TidyBookShape("Pocket", 0.03f, 0.18f, 0.12f)
        };

        public static Mesh Build(TidyBookShape shape, string folder)
        {
            var builder = new TidyMeshBuilder(3);
            var halfT = shape.Thickness * 0.5f;
            var halfD = shape.Depth * 0.5f;
            var height = shape.Height;
            var bevel = CoverThickness * 0.45f;
            builder.AddBeveledBox(new Vector3(halfT - CoverThickness * 0.5f, height * 0.5f, 0f), new Vector3(CoverThickness, height, shape.Depth), bevel, 1, 0);
            builder.AddBeveledBox(new Vector3(-halfT + CoverThickness * 0.5f, height * 0.5f, 0f), new Vector3(CoverThickness, height, shape.Depth), bevel, 1, 0);
            builder.AddBeveledBox(new Vector3(0f, height * 0.5f, halfD - CoverThickness * 0.5f), new Vector3(shape.Thickness, height, CoverThickness * 1.5f), CoverThickness * 0.7f, 2, 0);
            builder.UvScale = new Vector2(4f, 4f);
            builder.AddBox(new Vector3(0f, height * 0.5f, -PageInset * 0.5f), new Vector3(shape.Thickness - CoverThickness * 2f, height - PageInset * 2f, shape.Depth - PageInset - CoverThickness), 2);
            builder.UvScale = Vector2.one;
            builder.AddMesh(Decals(shape), Matrix4x4.identity, 1);
            return TidyMeshUtility.SaveBottomCentered(builder, "SM_Book_" + shape.Key, folder);
        }

        static Mesh Decals(TidyBookShape shape)
        {
            var quads = new TidyQuadMesh();
            var halfT = shape.Thickness * 0.5f + DecalLift;
            var halfD = shape.Depth * 0.5f;
            var height = shape.Height;
            var spineZ = halfD + CoverThickness * 0.25f + DecalLift;
            var coverUv = new Rect(SpineAtlasFraction, 0f, 1f - SpineAtlasFraction, 1f);
            var spineUv = new Rect(0f, 0f, SpineAtlasFraction, 1f);
            quads.AddQuad(new Vector3(halfT, 0f, -halfD), new Vector3(halfT, 0f, halfD), new Vector3(halfT, height, halfD), new Vector3(halfT, height, -halfD), coverUv);
            quads.AddQuad(new Vector3(-halfT, 0f, halfD), new Vector3(-halfT, 0f, -halfD), new Vector3(-halfT, height, -halfD), new Vector3(-halfT, height, halfD), coverUv);
            var spineHalf = shape.Thickness * 0.5f - CoverThickness * 0.2f;
            quads.AddQuad(new Vector3(spineHalf, 0f, spineZ), new Vector3(-spineHalf, 0f, spineZ), new Vector3(-spineHalf, height, spineZ), new Vector3(spineHalf, height, spineZ), spineUv);
            return quads.ToMesh("BookDecals");
        }

        public static Texture2D BuildAtlas(Color cover, Color accent, int style, int seed)
        {
            const int size = 512;
            var spineWidth = Mathf.RoundToInt(size * SpineAtlasFraction);
            var spine = TidyTextureBuilder.BookCover(spineWidth, size, cover, accent, (style + 3) % 6, seed + 1);
            var front = TidyTextureBuilder.BookCover(size - spineWidth, size, cover, accent, style, seed);
            var atlas = new Texture2D(size, size, TextureFormat.RGBA32, true);
            atlas.SetPixels(0, 0, spineWidth, size, spine.GetPixels());
            atlas.SetPixels(spineWidth, 0, size - spineWidth, size, front.GetPixels());
            atlas.Apply();
            Object.DestroyImmediate(spine);
            Object.DestroyImmediate(front);
            return atlas;
        }
    }
}
