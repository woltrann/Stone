using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyTextureBuilder
    {
        private const int WallpaperColumns = 4;
        private const int WallpaperRows = 4;
        private const float WallpaperMotifStrength = 0.9f;
        private const float WallpaperStripeStrength = 0.55f;
        private const float WallpaperStripeOffset = 0.035f;
        private const float WallpaperStripeHalfWidth = 0.006f;

        private const int FabricThreadPixels = 6;
        private const int FabricMinimumThreads = 16;
        private const int FabricFuzzCells = 32;
        private const int FabricWeftSalt = 1000;

        private const int CardboardFlutes = 24;
        private const float CardboardSpeckThreshold = 0.996f;

        private const int MetalBrushCellsAlong = 2;
        private const int MetalBrushLines = 256;
        private const int MetalFineLines = 512;

        public static Texture2D Wallpaper(int size, Color background, Color pattern, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => WallpaperPixel(u, v, canvas.Width, background, pattern, seed));
            return canvas.ToTexture("Wallpaper");
        }

        public static Texture2D Fabric(int size, Color baseColor, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            int threads = Mathf.Max(FabricMinimumThreads, canvas.Width / FabricThreadPixels);
            threads += threads % 2;
            canvas.Fill((u, v) => FabricPixel(u, v, threads, baseColor, seed));
            return canvas.ToTexture("Fabric");
        }

        public static Texture2D Paper(int size, Color baseColor, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => PaperPixel(u, v, baseColor, seed));
            return canvas.ToTexture("Paper");
        }

        public static Texture2D Plaster(int size, Color baseColor, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => PlasterPixel(u, v, canvas.Width, baseColor, seed));
            return canvas.ToTexture("Plaster");
        }

        public static Texture2D Metal(int size, Color baseColor, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => MetalPixel(u, v, baseColor, seed));
            return canvas.ToTexture("Metal");
        }

        public static Texture2D Cardboard(int size, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            var kraft = new Color(0.64f, 0.48f, 0.31f, 1f);
            canvas.Fill((u, v) => CardboardPixel(u, v, canvas.Width, kraft, seed));
            return canvas.ToTexture("Cardboard");
        }

        private static Color WallpaperPixel(float u, float v, int size, Color background, Color pattern, int seed)
        {
            float cx = u * WallpaperColumns;
            int column = Mathf.Min(Mathf.FloorToInt(cx), WallpaperColumns - 1);
            float cy = v * WallpaperRows + ((column & 1) == 0 ? 0f : 0.5f);
            var local = new Vector2(Mathf.Abs(TidyMath.Frac(cx) - 0.5f), TidyMath.Frac(cy) - 0.5f);
            float pixel = WallpaperColumns / (float)size;
            float motif = TidySdf.Coverage(DamaskMotif(local), pixel);
            float edgeDistance = 0.5f - local.x;
            float stripeDistance = Mathf.Abs(edgeDistance - WallpaperStripeOffset) - WallpaperStripeHalfWidth;
            float stripe = TidySdf.Coverage(stripeDistance, pixel);
            float paper = TidyNoise.Fbm(u * 6f, v * 6f, seed, 4, 6, 6);
            float streak = TidyNoise.Value(u * 128f, v * 2f, seed + 1, 128, 2);
            Color paperColor = TidyColor.Shade(background, 0.94f + 0.08f * paper + 0.03f * (streak - 0.5f));
            float mask = Mathf.Max(motif * WallpaperMotifStrength, stripe * WallpaperStripeStrength);
            return TidyColor.Opaque(Color.Lerp(paperColor, pattern, mask));
        }

        private static float DamaskMotif(Vector2 p)
        {
            float body = TidySdf.Ellipse(p - new Vector2(0f, 0.02f), new Vector2(0.07f, 0.15f));
            Vector2 leafSpace = TidySdf.Rotate(p - new Vector2(0.13f, -0.04f), 35f);
            float leaf = TidySdf.Ellipse(leafSpace, new Vector2(0.04f, 0.11f));
            float crown = TidySdf.Circle(p - new Vector2(0f, 0.26f), 0.035f);
            float scroll = TidySdf.Ring(p - new Vector2(0f, -0.23f), 0.06f, 0.012f);
            float berries = TidySdf.Circle(p - new Vector2(0.2f, 0.16f), 0.02f);
            float stem = TidySdf.Capsule(p, new Vector2(0f, -0.17f), new Vector2(0.11f, -0.06f), 0.01f);
            float shape = TidySdf.Union(TidySdf.Union(body, leaf), TidySdf.Union(crown, scroll));
            return TidySdf.Union(shape, TidySdf.Union(berries, stem));
        }

        private static Color FabricPixel(float u, float v, int threads, Color baseColor, int seed)
        {
            float tx = u * threads;
            float ty = v * threads;
            int ix = Mathf.Min(Mathf.FloorToInt(tx), threads - 1);
            int iy = Mathf.Min(Mathf.FloorToInt(ty), threads - 1);
            float fx = tx - ix;
            float fy = ty - iy;
            bool warpOnTop = ((ix + iy) & 1) == 0;
            float across = warpOnTop ? fx : fy;
            float along = warpOnTop ? fy : fx;
            float profile = Mathf.Pow(Mathf.Sin(Mathf.PI * across), 0.6f);
            float dip = 0.85f + 0.15f * Mathf.Sin(Mathf.PI * along);
            int thread = warpOnTop ? ix : iy + FabricWeftSalt;
            float slub = 0.9f + 0.2f * TidyNoise.Hash01(thread, warpOnTop ? 0 : 1, seed);
            float fuzz = TidyNoise.Fbm(u * FabricFuzzCells, v * FabricFuzzCells, seed + 3, 3, FabricFuzzCells, FabricFuzzCells);
            float value = profile * dip * slub * (0.85f + 0.3f * fuzz);
            return TidyColor.Opaque(TidyColor.Shade(baseColor, Mathf.Lerp(0.55f, 1.08f, value)));
        }

        private static Color PaperPixel(float u, float v, Color baseColor, int seed)
        {
            float blotch = TidyNoise.Fbm(u * 3f, v * 3f, seed, 5, 3, 3);
            float fibre = TidyNoise.Fbm(u * 48f, v * 48f, seed + 1, 2, 48, 48);
            float stain = TidyNoise.Ridged(u * 4f, v * 4f, seed + 2, 3, 4, 4);
            float tone = 0.9f + (blotch - 0.5f) * 0.22f + (fibre - 0.5f) * 0.08f - TidyMath.SmoothStep(0.75f, 0.95f, stain) * 0.06f;
            Color paper = TidyColor.Shade(baseColor, tone);
            Color aged = TidyColor.Multiply(paper, new Color(0.9f, 0.8f, 0.65f, 1f));
            return TidyColor.Opaque(Color.Lerp(paper, aged, (1f - blotch) * 0.25f));
        }

        private static Color PlasterPixel(float u, float v, int size, Color baseColor, int seed)
        {
            float broad = TidyNoise.Fbm(u * 4f, v * 4f, seed, 5, 4, 4);
            float trowel = TidyNoise.Ridged(u * 6f + broad * 2f, v * 3f, seed + 1, 3, 6, 3);
            float speck = TidyNoise.Hash01(Mathf.FloorToInt(u * size), Mathf.FloorToInt(v * size), seed + 2) - 0.5f;
            float tone = 0.92f + (broad - 0.5f) * 0.12f + (trowel - 0.5f) * 0.06f + speck * 0.04f;
            return TidyColor.Opaque(TidyColor.Shade(baseColor, tone));
        }

        private static Color MetalPixel(float u, float v, Color baseColor, int seed)
        {
            float streak = TidyNoise.Value(u * MetalBrushCellsAlong, v * MetalBrushLines, seed, MetalBrushCellsAlong, MetalBrushLines);
            float fine = TidyNoise.Value(u * MetalBrushCellsAlong * 3f, v * MetalFineLines, seed + 1, MetalBrushCellsAlong * 3, MetalFineLines);
            float blotch = TidyNoise.Fbm(u * 3f, v * 3f, seed + 2, 4, 3, 3);
            float tone = 0.82f + (streak - 0.5f) * 0.18f + (fine - 0.5f) * 0.1f + (blotch - 0.5f) * 0.12f;
            return TidyColor.Opaque(TidyColor.Shade(baseColor, tone));
        }

        private static Color CardboardPixel(float u, float v, int size, Color kraft, int seed)
        {
            float fibre = TidyNoise.Fbm(u * 24f, v * 24f, seed, 4, 24, 24);
            float blotch = TidyNoise.Fbm(u * 3f, v * 3f, seed + 1, 4, 3, 3);
            float corrugation = 0.5f + 0.5f * Mathf.Sin(u * CardboardFlutes * TidyMath.TwoPi);
            bool speck = TidyNoise.Hash01(Mathf.FloorToInt(u * size), Mathf.FloorToInt(v * size), seed + 2) > CardboardSpeckThreshold;
            float tone = 0.88f + (fibre - 0.5f) * 0.12f + (blotch - 0.5f) * 0.14f + corrugation * 0.03f;
            if (speck)
            {
                tone *= 0.7f;
            }

            return TidyColor.Opaque(TidyColor.Shade(kraft, tone));
        }
    }
}
