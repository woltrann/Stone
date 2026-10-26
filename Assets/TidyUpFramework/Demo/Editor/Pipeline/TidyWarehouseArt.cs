using System;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyWarehouseArt
    {
        public static readonly string[] LabelColors = { "Red", "Blue", "Green", "Yellow" };
        public static readonly string[] SizeCodes = { "S", "M", "L" };

        public static void Build(TidyDemoContext context)
        {
            BuildSurfaces(context);
            BuildPaints(context);
            BuildPrints(context);
            BuildGlows(context);
            AssetDatabase.SaveAssets();
        }

        static void BuildSurfaces(TidyDemoContext context)
        {
            Surface(context, "Warehouse_Floor", ConcreteSlabs(1024, 41), 0.42f, new Vector2(1f, 1f), 0.35f, 0f);
            Surface(context, "Warehouse_Cladding", Corrugated(512, new Color32(196, 204, 210, 255), 42), 0.45f, new Vector2(1f, 1f), 1.4f, 0.25f);
            Surface(context, "Warehouse_Roof", Corrugated(512, new Color32(168, 174, 182, 255), 43), 0.35f, new Vector2(1f, 1f), 1.2f, 0.3f);
            Surface(context, "Warehouse_Block", BlockWall(512, 44), 0.12f, new Vector2(0.5f, 0.5f), 1.1f, 0f);
            Surface(context, "Warehouse_Pegboard", Pegboard(512, 45), 0.25f, new Vector2(1f, 1f), 0.8f, 0f);
            Surface(context, "Warehouse_Hazard", Hazard(256, 46), 0.35f, new Vector2(1f, 1f), 0.2f, 0f);
            var cardboard = AssetDatabase.LoadAssetAtPath<Texture2D>(TidyDemoPaths.Textures + "/T_Cardboard.png");
            TidyArtLibrary.Textured(context, "Warehouse_CardboardLight", cardboard, 0.15f, new Color(1.25f, 1.2f, 1.12f));
        }

        static void BuildPaints(TidyDemoContext context)
        {
            TidyArtLibrary.Plain(context, "Warehouse_PaintYellow", new Color32(244, 196, 48, 255), 0.35f);
            TidyArtLibrary.Plain(context, "Warehouse_PaintWhite", new Color32(236, 236, 230, 255), 0.35f);
            TidyArtLibrary.Plain(context, "Warehouse_BeamOrange", new Color32(232, 112, 36, 255), 0.55f, 0.35f);
            TidyArtLibrary.Plain(context, "Warehouse_SteelDark", new Color32(64, 68, 74, 255), 0.5f, 0.6f);
            TidyArtLibrary.Plain(context, "Warehouse_DrumBlue", new Color32(36, 92, 168, 255), 0.6f, 0.3f);
            TidyArtLibrary.Plain(context, "Warehouse_DrumRed", new Color32(184, 48, 40, 255), 0.6f, 0.3f);
            TidyArtLibrary.Plain(context, "Warehouse_LampShade", new Color32(46, 84, 72, 255), 0.65f, 0.4f);
            TidyArtLibrary.Plain(context, "Warehouse_OfficeWall", new Color32(226, 222, 212, 255), 0.25f);
            TidyArtLibrary.Plain(context, "Warehouse_SignPanel", new Color32(34, 40, 46, 255), 0.4f);
            TidyArtLibrary.Plain(context, "Warehouse_Screen", new Color32(20, 26, 34, 255), 0.85f);
        }

        static void BuildPrints(TidyDemoContext context)
        {
            for (var index = 0; index < LabelColors.Length; index++)
            {
                var band = TidyArtLibrary.PaletteColor(LabelColors[index]);
                var label = TidyTextureBuilder.Label(256, 176, new Color(0.97f, 0.96f, 0.93f), band, new Color(0.16f, 0.16f, 0.18f), index, 500 + index);
                TidyArtLibrary.Textured(context, "Warehouse_Label_" + LabelColors[index], TidyArtLibrary.SaveTexture("T_Warehouse_Label_" + LabelColors[index], label, false, 256), 0.3f);
            }

            for (var index = 0; index < SizeCodes.Length; index++)
            {
                Cutout(context, "Warehouse_Print_" + SizeCodes[index], CartonPrint(256, index), new Color(0.2f, 0.17f, 0.15f));
            }

            Cutout(context, "Warehouse_StencilFragile", Stencil(256, FragileDistance), new Color(0.72f, 0.14f, 0.12f));
            Cutout(context, "Warehouse_StencilParts", Stencil(256, GearDistance), new Color(0.1f, 0.24f, 0.5f));
            Cutout(context, "Warehouse_StencilHazmat", Stencil(256, FlameDiamondDistance), new Color(0.12f, 0.12f, 0.12f));
        }

        static void BuildGlows(TidyDemoContext context)
        {
            Emissive(context, "Warehouse_Skylight", new Color(0.86f, 0.92f, 1f), new Color(2.2f, 2.5f, 2.9f));
            Emissive(context, "Warehouse_Daylight", new Color(1f, 0.95f, 0.85f), new Color(4.2f, 3.8f, 3.1f));
            Emissive(context, "Warehouse_SignGreen", new Color(0.3f, 0.9f, 0.45f), new Color(0.22f, 1.25f, 0.42f));
            Emissive(context, "Warehouse_SignAmber", new Color(1f, 0.6f, 0.2f), new Color(1.1f, 0.5f, 0.08f));
            Emissive(context, "Warehouse_Bulb", new Color(1f, 0.9f, 0.7f), new Color(5f, 3.9f, 2.4f));
            var dust = AssetDatabase.LoadAssetAtPath<Texture2D>(TidyDemoPaths.Textures + "/T_Dust.png");
            var floorDust = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_Warehouse_FloorDust.mat", new TidyMaterialSpec { BaseMap = dust, BaseColor = new Color(0.46f, 0.42f, 0.37f), Smoothness = 0.05f, Transparent = true, Alpha = 0.7f });
            context.AddMaterial("Warehouse_FloorDust", floorDust);
        }

        static void Surface(TidyDemoContext context, string key, Texture2D albedo, float smoothness, Vector2 tiling, float normalStrength, float metallic)
        {
            var normal = TidyTextureBuilder.NormalFromHeight(albedo, 2f);
            var size = albedo.width;
            var albedoAsset = TidyArtLibrary.SaveTexture("T_" + key, albedo, false, size);
            var normalAsset = TidyArtLibrary.SaveTexture("T_" + key + "_N", normal, true, size);
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec
            {
                BaseMap = albedoAsset,
                NormalMap = normalAsset,
                NormalStrength = normalStrength,
                Smoothness = smoothness,
                Metallic = metallic,
                Tiling = tiling
            });
            context.AddMaterial(key, material);
        }

        static void Cutout(TidyDemoContext context, string key, Texture2D texture, Color ink)
        {
            var asset = TidyArtLibrary.SaveTexture("T_" + key, texture, false, 256, true);
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseMap = asset, BaseColor = ink, Smoothness = 0.2f });
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = 2450;
            material.SetOverrideTag("RenderType", "TransparentCutout");
            EditorUtility.SetDirty(material);
            context.AddMaterial(key, material);
        }

        static void Emissive(TidyDemoContext context, string key, Color color, Color emission)
        {
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseColor = color, Emission = emission, Smoothness = 0.6f });
            context.AddMaterial(key, material);
        }

        static Texture2D Paint(int width, int height, Func<float, float, Color> shader)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = shader((x + 0.5f) / width, (y + 0.5f) / height);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D ConcreteSlabs(int size, int seed)
        {
            var light = new Color(0.66f, 0.66f, 0.64f);
            var dark = new Color(0.5f, 0.5f, 0.49f);
            return Paint(size, size, (u, v) =>
            {
                var cloud = TidyNoise.Fbm(u * 6f, v * 6f, seed, 5, 6, 6);
                var fine = TidyNoise.Value(u * size * 0.5f, v * size * 0.5f, seed + 1, size / 2, size / 2);
                var stain = TidyMath.SmoothStep(0.55f, 0.8f, TidyNoise.Fbm(u * 3f, v * 3f, seed + 2, 4, 3, 3));
                var color = Color.Lerp(light, dark, cloud * 0.7f);
                color = TidyColor.Shade(color, 0.94f + fine * 0.1f - stain * 0.12f);
                var jointU = Mathf.Abs(TidyMath.Frac(u * 2f + 0.5f) - 0.5f) * size / 2f;
                var jointV = Mathf.Abs(TidyMath.Frac(v * 2f + 0.5f) - 0.5f) * size / 2f;
                var joint = Mathf.Min(jointU, jointV);
                var cut = 1f - TidyMath.SmoothStep(0.6f, 1.8f, joint);
                return TidyColor.Opaque(Color.Lerp(color, new Color(0.3f, 0.3f, 0.3f), cut * 0.8f));
            });
        }

        static Texture2D Corrugated(int size, Color baseColor, int seed)
        {
            return Paint(size, size, (u, v) =>
            {
                var rib = Mathf.Sin(u * Mathf.PI * 2f * 16f);
                var shade = 0.82f + 0.18f * rib;
                var streak = TidyNoise.Fbm(u * 40f, v * 2f, seed, 3, 40, 2);
                var seam = 1f - TidyMath.SmoothStep(0f, 0.004f, Mathf.Abs(v - 0.5f));
                var color = TidyColor.Shade(baseColor, shade * (0.94f + streak * 0.1f) - seam * 0.25f);
                return TidyColor.Opaque(color);
            });
        }

        static Texture2D BlockWall(int size, int seed)
        {
            var block = new Color(0.78f, 0.77f, 0.74f);
            var mortar = new Color(0.6f, 0.59f, 0.57f);
            return Paint(size, size, (u, v) =>
            {
                var rows = 10f;
                var row = Mathf.Floor(v * rows);
                var offset = row % 2f * 0.5f;
                var columnU = TidyMath.Frac(u * 5f + offset);
                var rowV = TidyMath.Frac(v * rows);
                var edge = Mathf.Min(Mathf.Min(columnU, 1f - columnU) * 0.4f, Mathf.Min(rowV, 1f - rowV) * 0.1f) * size;
                var mortarMask = 1f - TidyMath.SmoothStep(0.4f, 1.4f, edge);
                var pores = TidyNoise.Value(u * size * 0.6f, v * size * 0.6f, seed, (int)(size * 0.6f), (int)(size * 0.6f));
                var tone = TidyNoise.Hash01((int)Mathf.Floor(u * 5f + offset), (int)row, seed);
                var color = TidyColor.Shade(block, 0.92f + tone * 0.08f + pores * 0.06f);
                return TidyColor.Opaque(Color.Lerp(color, mortar, mortarMask));
            });
        }

        static Texture2D Pegboard(int size, int seed)
        {
            var board = new Color(0.86f, 0.82f, 0.74f);
            return Paint(size, size, (u, v) =>
            {
                var cellU = TidyMath.Frac(u * 16f) - 0.5f;
                var cellV = TidyMath.Frac(v * 16f) - 0.5f;
                var hole = Mathf.Sqrt(cellU * cellU + cellV * cellV) * size / 16f;
                var holeMask = 1f - TidyMath.SmoothStep(4f, 5.5f, hole);
                var grain = TidyNoise.Fbm(u * 8f, v * 30f, seed, 3, 8, 30);
                var color = TidyColor.Shade(board, 0.93f + grain * 0.1f);
                return TidyColor.Opaque(Color.Lerp(color, new Color(0.18f, 0.15f, 0.12f), holeMask));
            });
        }

        static Texture2D Hazard(int size, int seed)
        {
            var yellow = new Color(0.96f, 0.76f, 0.12f);
            var black = new Color(0.1f, 0.1f, 0.1f);
            return Paint(size, size, (u, v) =>
            {
                var stripe = TidyMath.Frac((u + v) * 4f);
                var mask = TidyMath.SmoothStep(0.48f, 0.52f, stripe) * (1f - TidyMath.SmoothStep(0.98f, 1f, stripe));
                var wear = TidyNoise.Fbm(u * 10f, v * 10f, seed, 4, 10, 10);
                var color = Color.Lerp(yellow, black, mask);
                color = Color.Lerp(color, new Color(0.55f, 0.53f, 0.5f), TidyMath.SmoothStep(0.62f, 0.78f, wear) * 0.6f);
                return TidyColor.Opaque(color);
            });
        }

        static Texture2D CartonPrint(int size, int sizeIndex)
        {
            return Paint(size, size, (u, v) =>
            {
                var p = new Vector2(u * 2f - 1f, v * 2f - 1f);
                var frame = Mathf.Abs(TidySdf.Box(p, new Vector2(0.9f, 0.9f))) - 0.025f;
                var letter = LetterDistance(p - new Vector2(-0.3f, -0.1f), sizeIndex);
                var arrows = Mathf.Min(ArrowDistance(p - new Vector2(0.38f, 0.32f)), ArrowDistance(p - new Vector2(0.62f, 0.32f)));
                var bars = TidySdf.Box(p - new Vector2(0.5f, -0.45f), new Vector2(0.28f, 0.035f));
                bars = Mathf.Min(bars, TidySdf.Box(p - new Vector2(0.5f, -0.6f), new Vector2(0.28f, 0.035f)));
                var distance = Mathf.Min(Mathf.Min(frame, letter), Mathf.Min(arrows, bars));
                var alpha = TidySdf.Coverage(distance, 2f / size);
                return new Color(1f, 1f, 1f, alpha);
            });
        }

        static float LetterDistance(Vector2 p, int sizeIndex)
        {
            const float stroke = 0.075f;
            var w = 0.32f;
            var h = 0.5f;
            switch (sizeIndex)
            {
                case 0:
                    var top = TidySdf.Segment(p, new Vector2(w, h), new Vector2(-w, h));
                    var upper = TidySdf.Segment(p, new Vector2(-w, h), new Vector2(-w, 0f));
                    var middle = TidySdf.Segment(p, new Vector2(-w, 0f), new Vector2(w, 0f));
                    var lower = TidySdf.Segment(p, new Vector2(w, 0f), new Vector2(w, -h));
                    var bottom = TidySdf.Segment(p, new Vector2(w, -h), new Vector2(-w, -h));
                    return Mathf.Min(Mathf.Min(top, upper), Mathf.Min(middle, Mathf.Min(lower, bottom))) - stroke;
                case 1:
                    var left = TidySdf.Segment(p, new Vector2(-w, -h), new Vector2(-w, h));
                    var right = TidySdf.Segment(p, new Vector2(w, -h), new Vector2(w, h));
                    var diagonalA = TidySdf.Segment(p, new Vector2(-w, h), new Vector2(0f, 0f));
                    var diagonalB = TidySdf.Segment(p, new Vector2(w, h), new Vector2(0f, 0f));
                    return Mathf.Min(Mathf.Min(left, right), Mathf.Min(diagonalA, diagonalB)) - stroke;
                default:
                    var stem = TidySdf.Segment(p, new Vector2(-w, -h), new Vector2(-w, h));
                    var foot = TidySdf.Segment(p, new Vector2(-w, -h), new Vector2(w, -h));
                    return Mathf.Min(stem, foot) - stroke;
            }
        }

        static float ArrowDistance(Vector2 p)
        {
            var shaft = TidySdf.Segment(p, new Vector2(0f, -0.2f), new Vector2(0f, 0.15f)) - 0.035f;
            var headLeft = TidySdf.Segment(p, new Vector2(0f, 0.2f), new Vector2(-0.09f, 0.08f)) - 0.035f;
            var headRight = TidySdf.Segment(p, new Vector2(0f, 0.2f), new Vector2(0.09f, 0.08f)) - 0.035f;
            return Mathf.Min(shaft, Mathf.Min(headLeft, headRight));
        }

        static Texture2D Stencil(int size, Func<Vector2, float> shape)
        {
            return Paint(size, size, (u, v) =>
            {
                var p = new Vector2(u * 2f - 1f, v * 2f - 1f);
                var ring = Mathf.Abs(TidySdf.RoundedBox(p, new Vector2(0.86f, 0.86f), 0.12f)) - 0.035f;
                var distance = Mathf.Min(ring, shape(p));
                var speckle = TidyNoise.Value(u * 90f, v * 90f, 77, 90, 90);
                var alpha = TidySdf.Coverage(distance, 2f / size) * (speckle > 0.18f ? 1f : 0f);
                return new Color(1f, 1f, 1f, alpha);
            });
        }

        static float FragileDistance(Vector2 p)
        {
            var bowl = TidySdf.Subtract(TidySdf.Ellipse(p - new Vector2(0f, 0.28f), new Vector2(0.3f, 0.34f)), TidySdf.Box(p - new Vector2(0f, 0.72f), new Vector2(0.4f, 0.12f)));
            var stem = TidySdf.Box(p - new Vector2(0f, -0.25f), new Vector2(0.045f, 0.25f));
            var foot = TidySdf.RoundedBox(p - new Vector2(0f, -0.52f), new Vector2(0.24f, 0.05f), 0.03f);
            var crack = TidySdf.Segment(p, new Vector2(-0.05f, 0.6f), new Vector2(0.08f, 0.38f)) - 0.025f;
            crack = Mathf.Min(crack, TidySdf.Segment(p, new Vector2(0.08f, 0.38f), new Vector2(-0.06f, 0.2f)) - 0.025f);
            return TidySdf.Subtract(Mathf.Min(bowl, Mathf.Min(stem, foot)), crack);
        }

        static float GearDistance(Vector2 p)
        {
            var angle = Mathf.Atan2(p.y, p.x);
            var radius = p.magnitude;
            var teeth = 0.46f + 0.09f * Mathf.Clamp(Mathf.Sin(angle * 8f) * 3f, -1f, 1f);
            var body = radius - teeth;
            return TidySdf.Subtract(body, TidySdf.Circle(p, 0.18f));
        }

        static float FlameDiamondDistance(Vector2 p)
        {
            var diamond = Mathf.Abs(TidySdf.Rhombus(p, new Vector2(0.62f, 0.62f))) - 0.04f;
            var flame = TidySdf.SmoothUnion(TidySdf.Circle(p - new Vector2(0f, -0.12f), 0.18f), TidySdf.Segment(p, new Vector2(0f, -0.1f), new Vector2(0.04f, 0.3f)) - 0.07f, 0.12f);
            return Mathf.Min(diamond, flame);
        }
    }
}
