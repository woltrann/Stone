using System;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyTextureBuilder
    {
        private const int SurfaceAnisoLevel = 4;
        private const int MinimumTextureSize = 32;
        private const int MaximumTextureSize = 8192;

        public static void SavePng(Texture2D texture, string assetPath, bool isNormalMap = false, bool sRGB = true, bool alphaIsTransparency = false, int maxSize = 1024, bool mipmaps = true, TextureWrapMode wrap = TextureWrapMode.Repeat)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            string path = TidyAssetPaths.Normalize(assetPath);
            TidyAssetPaths.WriteAndImport(path, texture.EncodeToPNG());
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return;
            }

            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = sRGB && !isNormalMap;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = alphaIsTransparency;
            importer.maxTextureSize = Mathf.Clamp(Mathf.ClosestPowerOfTwo(maxSize), MinimumTextureSize, MaximumTextureSize);
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = wrap;
            importer.filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.anisoLevel = wrap == TextureWrapMode.Repeat ? SurfaceAnisoLevel : 1;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }
}
