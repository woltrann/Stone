using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyTextureBuilder
    {
        private const float GrimeShapeWarp = 0.9f;
        private const float GrimeRingRadius = 0.62f;
        private const float GrimeRingWidth = 0.05f;
        private const float DecalWindowStart = 0.85f;
        private const float DustWindowStart = 0.55f;
        private const int DustSpeckCells = 40;

        private static readonly Color GrimeColor = new Color(0.22f, 0.17f, 0.12f, 1f);
        private static readonly Color DustColor = new Color(0.78f, 0.75f, 0.7f, 1f);

        public static Texture2D Grime(int size, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => GrimePixel(u, v, seed));
            return canvas.ToTexture("Grime");
        }

        public static Texture2D Dust(int size, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => DustPixel(u, v, seed));
            return canvas.ToTexture("Dust");
        }

        public static Texture2D NormalFromHeight(Texture2D height, float strength)
        {
            int width = height.width;
            int rows = height.height;
            Color[] source = height.GetPixels();
            var heights = new float[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                heights[i] = source[i].grayscale;
            }

            var canvas = new TidyCanvas(width, rows);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    canvas.Pixels[y * width + x] = EncodeNormal(SobelGradient(heights, width, rows, x, y), strength);
                }
            }

            return canvas.ToTexture(height.name + "_Normal", true);
        }

        private static Vector2 SobelGradient(float[] heights, int width, int rows, int x, int y)
        {
            float Sample(int dx, int dy)
            {
                return heights[TidyMath.Wrap(y + dy, rows) * width + TidyMath.Wrap(x + dx, width)];
            }

            float gx = (Sample(1, -1) + 2f * Sample(1, 0) + Sample(1, 1)) - (Sample(-1, -1) + 2f * Sample(-1, 0) + Sample(-1, 1));
            float gy = (Sample(-1, 1) + 2f * Sample(0, 1) + Sample(1, 1)) - (Sample(-1, -1) + 2f * Sample(0, -1) + Sample(1, -1));
            return new Vector2(gx, gy);
        }

        private static Color EncodeNormal(Vector2 gradient, float strength)
        {
            Vector3 normal = new Vector3(-gradient.x * strength, -gradient.y * strength, 1f).normalized;
            return new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
        }

        private static Color GrimePixel(float u, float v, int seed)
        {
            var p = new Vector2(u * 2f - 1f, v * 2f - 1f);
            float radius = p.magnitude;
            float warp = TidyNoise.Fbm(u * 4f, v * 4f, seed, 5);
            float shape = radius + (warp - 0.5f) * GrimeShapeWarp;
            float body = 1f - TidyMath.SmoothStep(0.35f, 0.75f, shape);
            float ringOffset = (shape - GrimeRingRadius) / GrimeRingWidth;
            float ring = Mathf.Exp(-ringOffset * ringOffset) * 0.5f;
            float mottling = TidyNoise.Fbm(u * 10f, v * 10f, seed + 1, 4);
            float window = 1f - TidyMath.SmoothStep(DecalWindowStart, 1f, radius);
            float alpha = TidyMath.Saturate(body * (0.55f + 0.45f * mottling) * 0.85f + ring * 0.4f) * window;
            Color tint = TidyColor.Shade(GrimeColor, 0.85f + 0.3f * mottling);
            return TidyColor.WithAlpha(tint, alpha);
        }

        private static Color DustPixel(float u, float v, int seed)
        {
            var p = new Vector2(u * 2f - 1f, v * 2f - 1f);
            float patches = TidyNoise.Fbm(u * 6f, v * 6f, seed, 4);
            TidyCellularSample cell = TidyNoise.Cellular(u * DustSpeckCells, v * DustSpeckCells, seed + 1);
            float speckChance = (cell.CellHash & 0xFF) / 255f;
            float speck = speckChance > 0.5f ? 1f - TidyMath.SmoothStep(0f, 0.12f, cell.F1) : 0f;
            float edgeWarp = TidyNoise.Fbm(u * 3f, v * 3f, seed + 2, 3);
            float window = 1f - TidyMath.SmoothStep(DustWindowStart, 1f, p.magnitude + (edgeWarp - 0.5f) * 0.3f);
            float alpha = TidyMath.Saturate(TidyMath.SmoothStep(0.35f, 0.8f, patches) * 0.55f + speck * 0.5f) * window;
            return TidyColor.WithAlpha(TidyColor.Shade(DustColor, 0.9f + 0.2f * patches), alpha);
        }
    }
}
