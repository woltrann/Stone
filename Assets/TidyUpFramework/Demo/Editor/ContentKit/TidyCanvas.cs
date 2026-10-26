using System;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal sealed class TidyCanvas
    {
        public TidyCanvas(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            Pixels = new Color[Width * Height];
        }

        public int Width { get; }

        public int Height { get; }

        public Color[] Pixels { get; }

        public void Fill(Func<float, float, Color> shader)
        {
            for (int y = 0; y < Height; y++)
            {
                float v = (y + 0.5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    Pixels[y * Width + x] = shader((x + 0.5f) / Width, v);
                }
            }
        }

        public void FillCentered(Func<Vector2, Color> shader)
        {
            float halfWidth = Width * 0.5f;
            float halfHeight = Height * 0.5f;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Pixels[y * Width + x] = shader(new Vector2(x + 0.5f - halfWidth, y + 0.5f - halfHeight));
                }
            }
        }

        public Texture2D ToTexture(string name, bool linear = false, bool mipChain = true)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, mipChain, linear)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat
            };
            texture.SetPixels(Pixels);
            texture.Apply(mipChain, false);
            return texture;
        }
    }
}
