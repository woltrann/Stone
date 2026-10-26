using System;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidySpriteBuilder
    {
        private const float EdgeMarginPixels = 1f;
        private const float BorderPaddingPixels = 2f;

        public static Texture2D RoundedRect(int size, float radius, float borderWidth = 0f)
        {
            float half = size * 0.5f - EdgeMarginPixels;
            float corner = Mathf.Clamp(radius, 0f, half);
            return RenderPixels(size, "RoundedRect", p =>
            {
                float distance = TidySdf.RoundedBox(p, new Vector2(half, half), corner);
                if (borderWidth > 0f)
                {
                    distance = TidySdf.Outline(distance + borderWidth * 0.5f, borderWidth * 0.5f);
                }

                return TidySdf.Coverage(distance, 1f);
            });
        }

        public static Texture2D Circle(int size)
        {
            float radius = size * 0.5f - EdgeMarginPixels;
            return RenderPixels(size, "Circle", p => TidySdf.Coverage(TidySdf.Circle(p, radius), 1f));
        }

        public static Texture2D Ring(int size, float thickness)
        {
            float halfThickness = Mathf.Max(0.5f, thickness * 0.5f);
            float radius = size * 0.5f - EdgeMarginPixels - halfThickness;
            return RenderPixels(size, "Ring", p => TidySdf.Coverage(TidySdf.Ring(p, radius, halfThickness), 1f));
        }

        public static Texture2D Shadow(int size, float radius, float blur)
        {
            float softness = Mathf.Max(1f, blur);
            float half = size * 0.5f - EdgeMarginPixels - softness;
            float corner = Mathf.Clamp(radius, 0f, Mathf.Max(0f, half));
            return RenderPixels(size, "Shadow", p =>
            {
                float distance = TidySdf.RoundedBox(p, new Vector2(half, half), corner);
                return 1f - TidyMath.SmoothStep(-softness, softness, distance);
            });
        }

        public static Vector4 RoundedRectBorder(float radius, float borderWidth = 0f)
        {
            float border = Mathf.Ceil(Mathf.Max(radius, borderWidth) + BorderPaddingPixels);
            return new Vector4(border, border, border, border);
        }

        public static Vector4 ShadowBorder(float radius, float blur)
        {
            float border = Mathf.Ceil(radius + blur * 2f + BorderPaddingPixels);
            return new Vector4(border, border, border, border);
        }

        public static Sprite SaveSprite(Texture2D texture, string assetPath, Vector4 border, float pixelsPerUnit = 100f)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            string path = TidyAssetPaths.Normalize(assetPath);
            TidyAssetPaths.WriteAndImport(path, texture.EncodeToPNG());
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            importer.SetTextureSettings(settings);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D RenderPixels(int size, string name, Func<Vector2, float> alpha)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.FillCentered(p => new Color(1f, 1f, 1f, TidyMath.Saturate(alpha(p))));
            Texture2D texture = canvas.ToTexture(name, false, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static Texture2D RenderGlyph(int size, string name, Func<Vector2, float> distanceInUnits)
        {
            float pixel = 1f / Mathf.Max(1, size);
            return RenderPixels(size, name, p => TidySdf.Coverage(distanceInUnits(p * pixel), pixel));
        }
    }
}
