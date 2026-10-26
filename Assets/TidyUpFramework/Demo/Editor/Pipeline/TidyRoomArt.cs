using System;
using TidyUpFramework.Demo.Editor.ContentKit;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyRoomArt
    {
        const int PaintingSize = 256;
        const int FabricSize = 256;

        public static Color Pastel(string colorTag, float amount = 0.28f)
        {
            return Color.Lerp(TidyArtLibrary.PaletteColor(colorTag), Color.white, amount);
        }

        public static void Build(TidyDemoContext context)
        {
            Fabric(context, "Room_Curtain", new Color32(214, 160, 150, 255), 501);
            Fabric(context, "Room_Throw", new Color32(236, 196, 120, 255), 502);
            Fabric(context, "Room_Pouf", new Color32(150, 178, 150, 255), 503);
            Fabric(context, "Room_Velvet", new Color32(120, 60, 110, 255), 504);
            TidyArtLibrary.Plain(context, "Room_DustFluff", new Color32(150, 144, 138, 255), 0.02f);
            TidyArtLibrary.Plain(context, "Room_RubberWhite", new Color32(244, 240, 232, 255), 0.25f);
            TidyArtLibrary.Plain(context, "Room_Foil", new Color32(206, 210, 216, 255), 0.75f, 0.85f);
            TidyArtLibrary.Plain(context, "Room_LeafDark", new Color32(52, 104, 62, 255), 0.4f);
            TidyArtLibrary.Plain(context, "Room_LeafLight", new Color32(118, 170, 92, 255), 0.4f);
            TidyArtLibrary.Plain(context, "Room_Stem", new Color32(96, 132, 70, 255), 0.2f);
            TidyArtLibrary.Plain(context, "Room_Soil", new Color32(70, 50, 38, 255), 0.05f);
            TidyArtLibrary.Plain(context, "Room_Nose", new Color32(60, 42, 40, 255), 0.6f);
            TidyArtLibrary.Plain(context, "Room_Sage", new Color32(150, 176, 150, 255), 0.35f);
            TidyArtLibrary.Plain(context, "Room_Blush", new Color32(232, 176, 168, 255), 0.35f);
            TidyArtLibrary.Plain(context, "Room_Mustard", new Color32(224, 176, 82, 255), 0.35f);
            TidyArtLibrary.Plain(context, "Room_Wire", new Color32(70, 60, 50, 255), 0.4f);
            Emissive(context, "Room_FairyGlow", new Color32(255, 214, 150, 255), new Color(4.2f, 2.9f, 1.2f));
            Emissive(context, "Room_Sunset", new Color32(255, 200, 150, 255), new Color(2.2f, 1.45f, 0.85f));
            foreach (var colorTag in new[] { "Red", "Blue", "Green", "Yellow", "Purple", "Orange", "Pink", "Teal", "Brown", "White" })
            {
                Fabric(context, "Room_Knit_" + colorTag, Pastel(colorTag), 600 + colorTag.Length * 7 + colorTag[0]);
                TidyArtLibrary.Plain(context, "Room_Glaze_" + colorTag, Pastel(colorTag, 0.2f), 0.82f);
            }

            Painting(context, "Room_Painting_Sunset", SunsetPixel);
            Painting(context, "Room_Painting_Shapes", ShapesPixel);
            Painting(context, "Room_Painting_Botanical", BotanicalPixel);
            Painting(context, "Room_Photo", PhotoPixel);
        }

        static void Fabric(TidyDemoContext context, string key, Color color, int seed)
        {
            var texture = TidyArtLibrary.SaveTexture("T_" + key, TidyTextureBuilder.Fabric(FabricSize, color, seed), false, FabricSize);
            TidyArtLibrary.Textured(context, key, texture, 0.08f);
        }

        static void Emissive(TidyDemoContext context, string key, Color color, Color emission)
        {
            var material = TidyMaterialFactory.Lit(TidyDemoPaths.Materials + "/M_" + key + ".mat", new TidyMaterialSpec { BaseColor = color, Emission = emission, Smoothness = 0.6f });
            context.AddMaterial(key, material);
        }

        static void Painting(TidyDemoContext context, string key, Func<float, float, Color> shader)
        {
            var texture = new Texture2D(PaintingSize, PaintingSize, TextureFormat.RGBA32, true);
            var pixels = new Color[PaintingSize * PaintingSize];
            for (var y = 0; y < PaintingSize; y++)
            {
                for (var x = 0; x < PaintingSize; x++)
                {
                    var u = (x + 0.5f) / PaintingSize;
                    var v = (y + 0.5f) / PaintingSize;
                    var grain = (TidyNoise.Hash01(x, y, key.Length) - 0.5f) * 0.05f;
                    var color = shader(u, v);
                    pixels[y * PaintingSize + x] = new Color(color.r + grain, color.g + grain, color.b + grain, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            var asset = TidyArtLibrary.SaveTexture("T_" + key, texture, false, PaintingSize);
            TidyArtLibrary.Textured(context, key, asset, 0.2f);
        }

        static Color SunsetPixel(float u, float v)
        {
            var sky = Color.Lerp(new Color(1f, 0.78f, 0.55f), new Color(0.62f, 0.55f, 0.78f), Mathf.SmoothStep(0.45f, 1f, v));
            var sunDistance = Vector2.Distance(new Vector2(u, v), new Vector2(0.62f, 0.5f));
            sky = Color.Lerp(new Color(1f, 0.93f, 0.7f), sky, Mathf.SmoothStep(0.08f, 0.1f, sunDistance));
            var far = 0.42f + TidyNoise.Fbm(u * 3f, 0.5f, 11, 3) * 0.12f;
            var near = 0.26f + TidyNoise.Fbm(u * 4f, 2.5f, 12, 3) * 0.14f;
            if (v < near)
            {
                return Color.Lerp(new Color(0.36f, 0.46f, 0.36f), new Color(0.46f, 0.56f, 0.42f), v / Mathf.Max(near, 0.01f));
            }

            return v < far ? new Color(0.62f, 0.56f, 0.66f) : sky;
        }

        static Color ShapesPixel(float u, float v)
        {
            var color = new Color(0.95f, 0.9f, 0.82f);
            if (Vector2.Distance(new Vector2(u, v), new Vector2(0.36f, 0.6f)) < 0.22f)
            {
                color = new Color(0.86f, 0.5f, 0.38f);
            }

            var archCenter = new Vector2(0.64f, 0.38f);
            var arch = Vector2.Distance(new Vector2(u, Mathf.Max(v, archCenter.y)), archCenter);
            if (arch < 0.2f && arch > 0.11f && v > 0.12f)
            {
                color = new Color(0.56f, 0.66f, 0.54f);
            }

            if (v < 0.2f && u > 0.12f && u < 0.52f)
            {
                color = new Color(0.9f, 0.72f, 0.36f);
            }

            return color;
        }

        static Color BotanicalPixel(float u, float v)
        {
            var color = new Color(0.93f, 0.9f, 0.84f);
            var stemX = 0.5f + Mathf.Sin(v * 3f) * 0.03f;
            if (Mathf.Abs(u - stemX) < 0.008f && v > 0.08f && v < 0.9f)
            {
                return new Color(0.3f, 0.42f, 0.3f);
            }

            for (var leaf = 0; leaf < 6; leaf++)
            {
                var side = leaf % 2 == 0 ? -1f : 1f;
                var anchor = new Vector2(stemX, 0.2f + leaf * 0.12f);
                var direction = new Vector2(side * 0.8f, 0.6f).normalized;
                var local = new Vector2(u, v) - anchor;
                var along = Vector2.Dot(local, direction);
                var across = Vector2.Dot(local, new Vector2(-direction.y, direction.x));
                var length = 0.2f - leaf * 0.015f;
                if (along > 0f && along < length && Mathf.Abs(across) < Mathf.Sin(along / length * Mathf.PI) * 0.05f)
                {
                    color = leaf % 3 == 0 ? new Color(0.36f, 0.52f, 0.38f) : new Color(0.46f, 0.62f, 0.44f);
                }
            }

            return color;
        }

        static Color PhotoPixel(float u, float v)
        {
            var horizon = 0.42f;
            if (v > horizon)
            {
                var sky = Color.Lerp(new Color(1f, 0.74f, 0.56f), new Color(0.5f, 0.66f, 0.86f), (v - horizon) / (1f - horizon));
                var sun = Vector2.Distance(new Vector2(u, v), new Vector2(0.3f, 0.5f));
                return Color.Lerp(new Color(1f, 0.9f, 0.7f), sky, Mathf.SmoothStep(0.06f, 0.09f, sun));
            }

            var sand = v < 0.18f;
            if (sand)
            {
                return new Color(0.9f, 0.8f, 0.62f);
            }

            var figure = Mathf.Abs(u - 0.7f) < 0.03f && v < 0.36f || Vector2.Distance(new Vector2(u, v), new Vector2(0.7f, 0.39f)) < 0.03f;
            return figure ? new Color(0.25f, 0.2f, 0.22f) : Color.Lerp(new Color(0.3f, 0.52f, 0.62f), new Color(0.88f, 0.66f, 0.56f), (v - 0.18f) / (horizon - 0.18f));
        }
    }
}
