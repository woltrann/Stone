using System;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyArtLibrary
    {
        const int SurfaceSize = 1024;
        const int PropSize = 512;
        const int SmallSize = 256;

        public static void Build(TidyDemoContext context)
        {
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Textures);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Materials);
            BuildSurfaces(context);
            BuildPropMaterials(context);
            AssetDatabase.SaveAssets();
        }

        static void BuildSurfaces(TidyDemoContext context)
        {
            Surface(context, "Floor_Herringbone", () => TidyTextureBuilder.WoodPlanks(SurfaceSize, new Color32(176, 120, 74, 255), new Color32(110, 66, 38, 255), 6, 11), 0.45f, new Vector2(3f, 3f), 0.8f);
            Surface(context, "Floor_LightOak", () => TidyTextureBuilder.WoodPlanks(SurfaceSize, new Color32(214, 176, 128, 255), new Color32(160, 118, 76, 255), 5, 12), 0.4f, new Vector2(3f, 3f), 0.6f);
            Surface(context, "Wood_Walnut", () => TidyTextureBuilder.WoodGrain(PropSize, new Color32(122, 74, 44, 255), new Color32(70, 38, 22, 255), 13), 0.5f, Vector2.one, 0.5f);
            Surface(context, "Wood_Mahogany", () => TidyTextureBuilder.WoodGrain(PropSize, new Color32(146, 66, 40, 255), new Color32(86, 32, 20, 255), 14), 0.55f, Vector2.one, 0.5f);
            Surface(context, "Wood_Oak", () => TidyTextureBuilder.WoodGrain(PropSize, new Color32(206, 160, 108, 255), new Color32(150, 104, 62, 255), 15), 0.4f, Vector2.one, 0.4f);
            Surface(context, "Wood_Painted", () => TidyTextureBuilder.WoodGrain(PropSize, new Color32(236, 228, 214, 255), new Color32(214, 204, 188, 255), 16), 0.35f, Vector2.one, 0.2f);
            Surface(context, "Wood_Crate", () => TidyTextureBuilder.WoodPlanks(PropSize, new Color32(214, 170, 112, 255), new Color32(158, 112, 66, 255), 3, 17), 0.25f, Vector2.one, 0.7f);
            Surface(context, "Stone_Wall", () => TidyTextureBuilder.StoneWall(SurfaceSize, new Color32(70, 62, 58, 255), new Color32(134, 124, 114, 255), 8, 18), 0.2f, new Vector2(2f, 2f), 1.2f);
            Surface(context, "Stone_Floor", () => TidyTextureBuilder.FloorTiles(SurfaceSize, new Color32(122, 112, 104, 255), new Color32(98, 90, 84, 255), 6, 0.025f, 19), 0.35f, new Vector2(3f, 3f), 0.8f);
            Surface(context, "Wallpaper_Library", () => TidyTextureBuilder.Wallpaper(SurfaceSize, new Color32(46, 70, 58, 255), new Color32(170, 140, 82, 255), 20), 0.3f, new Vector2(2f, 2f), 0.3f);
            Surface(context, "Wallpaper_Room", () => TidyTextureBuilder.Wallpaper(SurfaceSize, new Color32(232, 214, 196, 255), new Color32(208, 160, 150, 255), 21), 0.25f, new Vector2(2f, 2f), 0.2f);
            Surface(context, "Plaster_Cream", () => TidyTextureBuilder.Plaster(SurfaceSize, new Color32(236, 226, 208, 255), 22), 0.15f, new Vector2(2f, 2f), 0.4f);
            Surface(context, "Plaster_Store", () => TidyTextureBuilder.Plaster(SurfaceSize, new Color32(250, 236, 214, 255), 23), 0.15f, new Vector2(2f, 2f), 0.3f);
            Surface(context, "Tiles_Store", () => TidyTextureBuilder.FloorTiles(SurfaceSize, new Color32(238, 230, 214, 255), new Color32(110, 170, 168, 255), 8, 0.02f, 24), 0.6f, new Vector2(4f, 4f), 0.4f);
            Surface(context, "Concrete", () => TidyTextureBuilder.Plaster(SurfaceSize, new Color32(150, 150, 146, 255), 25), 0.3f, new Vector2(4f, 4f), 0.5f);
            Surface(context, "Metal_Rack", () => TidyTextureBuilder.Metal(PropSize, new Color32(70, 108, 150, 255), 26), 0.55f, Vector2.one, 0.2f, 0.6f);
            Surface(context, "Metal_Steel", () => TidyTextureBuilder.Metal(PropSize, new Color32(170, 172, 176, 255), 27), 0.6f, Vector2.one, 0.2f, 0.85f);
            Surface(context, "Metal_Brass", () => TidyTextureBuilder.Metal(PropSize, new Color32(204, 160, 82, 255), 28), 0.7f, Vector2.one, 0.2f, 0.9f);
            Surface(context, "Fabric_Red", () => TidyTextureBuilder.Fabric(PropSize, new Color32(150, 46, 46, 255), 29), 0.1f, new Vector2(2f, 2f), 0.6f);
            Surface(context, "Fabric_Bed", () => TidyTextureBuilder.Fabric(PropSize, new Color32(140, 170, 200, 255), 30), 0.1f, new Vector2(2f, 2f), 0.5f);
            Surface(context, "Fabric_Cream", () => TidyTextureBuilder.Fabric(PropSize, new Color32(232, 222, 204, 255), 31), 0.1f, new Vector2(2f, 2f), 0.5f);
            Surface(context, "Paper_Parchment", () => TidyTextureBuilder.Paper(PropSize, new Color32(236, 220, 186, 255), 32), 0.15f, Vector2.one, 0.2f);
            Surface(context, "Cardboard", () => TidyTextureBuilder.Cardboard(PropSize, 33), 0.15f, Vector2.one, 0.4f);
            Surface(context, "Rug_Library", () => TidyTextureBuilder.Rug(SurfaceSize, new Color32(120, 34, 40, 255), new Color32(214, 170, 90, 255), new Color32(40, 50, 80, 255), 34), 0.05f, Vector2.one, 0.5f);
            Surface(context, "Rug_Room", () => TidyTextureBuilder.Rug(SurfaceSize, new Color32(110, 150, 170, 255), new Color32(240, 220, 190, 255), new Color32(210, 120, 110, 255), 35), 0.05f, Vector2.one, 0.4f);
            Surface(context, "Wicker", () => TidyTextureBuilder.Fabric(PropSize, new Color32(196, 150, 92, 255), 36), 0.2f, new Vector2(3f, 3f), 0.9f);
            Decal(context, "Grime", TidyTextureBuilder.Grime(PropSize, 37), new Color32(90, 70, 50, 255));
            Decal(context, "Dust", TidyTextureBuilder.Dust(PropSize, 38), new Color32(200, 190, 175, 255));
        }

        static void BuildPropMaterials(TidyDemoContext context)
        {
            Plain(context, "Paper_Pages", new Color32(240, 230, 206, 255), 0.15f);
            Plain(context, "Wax_Cream", new Color32(244, 232, 204, 255), 0.35f);
            Plain(context, "Wick", new Color32(40, 32, 28, 255), 0.1f);
            Plain(context, "Cork", new Color32(176, 136, 90, 255), 0.15f);
            Plain(context, "Dark", new Color32(28, 24, 26, 255), 0.6f);
            Plain(context, "White", new Color32(244, 240, 232, 255), 0.4f);
            Plain(context, "Ceramic_White", new Color32(246, 242, 236, 255), 0.75f);
            Plain(context, "Rubber_Dark", new Color32(46, 42, 44, 255), 0.25f);
            Plain(context, "Tape", new Color32(196, 160, 104, 255), 0.45f);
            Plain(context, "Plant_Green", new Color32(78, 140, 74, 255), 0.35f);
            Plain(context, "Terracotta", new Color32(196, 104, 70, 255), 0.25f);
            Plain(context, "Ceiling_Dark", new Color32(46, 36, 34, 255), 0.2f);
            Emissive(context, "Flame", new Color32(255, 196, 110, 255), new Color(6f, 3.2f, 0.9f));
            Emissive(context, "Window_Glow", new Color32(255, 226, 180, 255), new Color(1.5f, 1.25f, 0.9f));
            Emissive(context, "Lamp_Glow", new Color32(255, 220, 160, 255), new Color(3.2f, 2.4f, 1.3f));
            Glass(context, "Glass_Clear", new Color(0.86f, 0.94f, 1f), 0.25f);
            foreach (var (tagName, color) in Palette)
            {
                Plain(context, "Plastic_" + tagName, color, 0.55f);
                Plain(context, "Matte_" + tagName, color, 0.2f);
                Emissive(context, "Liquid_" + tagName, color, new Color(color.r, color.g, color.b) * 1.4f);
                Glass(context, "Glass_" + tagName, Color.Lerp(color, Color.white, 0.35f), 0.45f);
            }
        }

        public static readonly (string name, Color color)[] Palette =
        {
            ("Red", new Color32(214, 74, 64, 255)),
            ("Blue", new Color32(70, 120, 214, 255)),
            ("Green", new Color32(86, 170, 92, 255)),
            ("Yellow", new Color32(240, 196, 66, 255)),
            ("Purple", new Color32(146, 92, 196, 255)),
            ("Orange", new Color32(236, 132, 54, 255)),
            ("Pink", new Color32(236, 130, 170, 255)),
            ("Brown", new Color32(138, 92, 60, 255)),
            ("Teal", new Color32(60, 170, 168, 255)),
            ("White", new Color32(236, 232, 222, 255))
        };

        public static Color PaletteColor(string tagName)
        {
            foreach (var (name, color) in Palette)
            {
                if (name == tagName)
                {
                    return color;
                }
            }

            return Color.white;
        }

        public static Texture2D SaveTexture(string textureName, Texture2D texture, bool isNormal = false, int maxSize = PropSize, bool alpha = false)
        {
            var path = TidyDemoPaths.Textures + "/" + textureName + ".png";
            TidyTextureBuilder.SavePng(texture, path, isNormal, !isNormal, alpha, maxSize, true, alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat);
            UnityEngine.Object.DestroyImmediate(texture);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void Surface(TidyDemoContext context, string key, Func<Texture2D> generate, float smoothness, Vector2 tiling, float normalStrength, float metallic = 0f)
        {
            var albedo = generate();
            var normal = TidyTextureBuilder.NormalFromHeight(albedo, 2f);
            var maxSize = albedo.width;
            var albedoAsset = SaveTexture("T_" + key, albedo, false, maxSize);
            var normalAsset = SaveTexture("T_" + key + "_N", normal, true, maxSize);
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

        static void Decal(TidyDemoContext context, string key, Texture2D texture, Color tint)
        {
            var asset = SaveTexture("T_" + key, texture, false, PropSize, true);
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec
            {
                BaseMap = asset,
                BaseColor = tint,
                Smoothness = 0.1f,
                Transparent = true,
                Alpha = 1f
            });
            context.AddMaterial(key, material);
        }

        public static Material Plain(TidyDemoContext context, string key, Color color, float smoothness, float metallic = 0f)
        {
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseColor = color, Smoothness = smoothness, Metallic = metallic });
            context.AddMaterial(key, material);
            return material;
        }

        public static Material Textured(TidyDemoContext context, string key, Texture2D texture, float smoothness, Color? tint = null)
        {
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseMap = texture, BaseColor = tint ?? Color.white, Smoothness = smoothness });
            context.AddMaterial(key, material);
            return material;
        }

        static void Emissive(TidyDemoContext context, string key, Color color, Color emission)
        {
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseColor = color, Emission = emission, Smoothness = 0.7f });
            context.AddMaterial(key, material);
        }

        static void Glass(TidyDemoContext context, string key, Color color, float alpha)
        {
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseColor = color, Smoothness = 0.95f, Transparent = true, Alpha = alpha });
            context.AddMaterial(key, material);
        }

        public static int SmallTextureSize => SmallSize;
    }
}
