using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyTextureBuilder
    {
        private const int BookStyleCount = 6;
        private const float BookMarginFraction = 0.08f;
        private const float BookFrameHalfWidth = 0.008f;
        private const float BookInnerMarginFraction = 0.13f;
        private const float BookInnerFrameHalfWidth = 0.004f;
        private const float BookOrnamentScale = 0.3f;
        private const float BookLeatherCellFraction = 0.035f;
        private const float BookEmbossOffset = 1.5f;
        private const float BookEmbossStrength = 0.45f;
        private const float BookDebossShadow = 0.25f;

        private const float LabelLineHeightFraction = 0.018f;
        private const float LabelWordGapFraction = 0.03f;
        private const int LabelMaxWords = 5;

        private const float RugBandOuter = 0.05f;
        private const float RugBandInner = 0.16f;
        private const float RugLineInner = 0.19f;
        private const int RugBorderMotifs = 8;
        private const float RugLatticeFrequency = 6f;

        public static Texture2D BookCover(int width, int height, Color cover, Color accent, int style, int seed)
        {
            var canvas = new TidyCanvas(width, height);
            var layout = new BookLayout(canvas.Width, canvas.Height, TidyMath.Wrap(style, BookStyleCount));
            canvas.FillCentered(p => BookPixel(p, layout, cover, accent, seed));
            return canvas.ToTexture("BookCover");
        }

        public static Texture2D Label(int width, int height, Color background, Color band, Color text, int style, int seed)
        {
            var canvas = new TidyCanvas(width, height);
            var layout = new LabelLayout(canvas.Width, canvas.Height, Mathf.Abs(style), seed);
            canvas.FillCentered(p => LabelPixel(p, layout, background, band, text, seed));
            return canvas.ToTexture("Label");
        }

        public static Texture2D Rug(int size, Color a, Color b, Color c, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => RugPixel(u, v, canvas.Width, a, b, c, seed));
            return canvas.ToTexture("Rug");
        }

        private static Color BookPixel(Vector2 p, BookLayout layout, Color cover, Color accent, int seed)
        {
            float u = p.x / layout.Width + 0.5f;
            float v = p.y / layout.Height + 0.5f;
            TidyCellularSample leather = TidyNoise.Cellular(p.x / layout.LeatherCell, p.y / layout.LeatherCell, seed);
            float crease = TidyMath.SmoothStep(0f, 0.35f, leather.Edge);
            float blotch = TidyNoise.Fbm(u * 3f, v * 3f, seed + 1, 4);
            float edgeDistance = Mathf.Min(layout.Width * 0.5f - Mathf.Abs(p.x), layout.Height * 0.5f - Mathf.Abs(p.y)) / layout.Unit;
            float vignette = Mathf.Lerp(0.7f, 1f, TidyMath.SmoothStep(0f, 0.08f, edgeDistance));
            Color leatherColor = TidyColor.Shade(cover, (0.82f + 0.18f * crease) * (0.9f + 0.2f * blotch) * vignette);
            float design = BookDesign(p, layout);
            float coverage = TidySdf.Coverage(design, 1f);
            float shadow = TidySdf.Coverage(design - BookEmbossOffset * 1.5f, 1f) * (1f - coverage);
            Vector2 light = new Vector2(-1f, 1f) * BookEmbossOffset;
            float emboss = TidySdf.Coverage(BookDesign(p - light, layout), 1f) - TidySdf.Coverage(BookDesign(p + light, layout), 1f);
            float sheen = 0.5f + 0.5f * Mathf.Sin((p.x + p.y) / layout.Unit * 6f);
            Color gilt = TidyColor.Shade(accent, (0.85f + 0.3f * sheen) * (1f - emboss * BookEmbossStrength));
            Color debossed = TidyColor.Shade(leatherColor, 1f - shadow * BookDebossShadow);
            return TidyColor.Opaque(Color.Lerp(debossed, gilt, coverage));
        }

        private static float BookDesign(Vector2 p, BookLayout layout)
        {
            var outerHalf = new Vector2(layout.Width * 0.5f - layout.Margin, layout.Height * 0.5f - layout.Margin);
            float frame = TidySdf.Outline(TidySdf.RoundedBox(p, outerHalf, layout.Margin * 0.5f), layout.Unit * BookFrameHalfWidth);
            var innerHalf = new Vector2(layout.Width * 0.5f - layout.InnerMargin, layout.Height * 0.5f - layout.InnerMargin);
            float innerFrame = TidySdf.Outline(TidySdf.Box(p, innerHalf), layout.Unit * BookInnerFrameHalfWidth);
            float ornamentScale = layout.Unit * BookOrnamentScale;
            float ornament = BookOrnament(p / ornamentScale, layout.Style) * ornamentScale;
            if (layout.Style == BookStyleCount - 1)
            {
                ornament = TidySdf.Union(ornament, SpineBands(p, layout));
            }

            return TidySdf.Union(TidySdf.Union(frame, innerFrame), ornament);
        }

        private static float SpineBands(Vector2 p, BookLayout layout)
        {
            float bandOffset = layout.Height * 0.32f;
            float band = Mathf.Min(Mathf.Abs(p.y - bandOffset), Mathf.Abs(p.y + bandOffset)) - layout.Unit * 0.03f;
            return TidySdf.Intersect(band, Mathf.Abs(p.x) - (layout.Width * 0.5f - layout.InnerMargin));
        }

        private static float BookOrnament(Vector2 q, int style)
        {
            switch (style)
            {
                case 0:
                    return CircleSigil(q);
                case 1:
                    return TidySdf.Union(TidySdf.Star5(q, 0.85f, 0.45f), TidySdf.Ring(q, 1f, 0.03f));
                case 2:
                    return TidySdf.Union(TidySdf.Outline(TidySdf.Rhombus(q, new Vector2(0.6f, 0.9f)), 0.05f), TidySdf.Rhombus(q, new Vector2(0.25f, 0.4f)));
                case 3:
                    return TidySdf.Union(TidySdf.Subtract(TidySdf.Circle(q, 0.75f), TidySdf.Circle(q - new Vector2(0.3f, 0.15f), 0.65f)), TidySdf.Circle(q - new Vector2(0.45f, 0.45f), 0.08f));
                case 4:
                    return RuneLines(q);
                default:
                    return TidySdf.Rhombus(q, new Vector2(0.18f, 0.28f));
            }
        }

        private static float CircleSigil(Vector2 q)
        {
            float rings = TidySdf.Union(TidySdf.Ring(q, 0.8f, 0.06f), TidySdf.Ring(q, 0.55f, 0.03f));
            float spokes = float.MaxValue;
            for (int k = 0; k < 6; k++)
            {
                float angle = k * Mathf.PI / 3f;
                var tip = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.55f;
                spokes = Mathf.Min(spokes, TidySdf.Capsule(q, Vector2.zero, tip, 0.03f));
            }

            return TidySdf.Union(TidySdf.Union(rings, spokes), TidySdf.Circle(q, 0.12f));
        }

        private static float RuneLines(Vector2 q)
        {
            const float stroke = 0.06f;
            float stem = TidySdf.Capsule(q, new Vector2(0f, -0.8f), new Vector2(0f, 0.8f), stroke);
            float upper = TidySdf.Capsule(q, new Vector2(0f, 0.2f), new Vector2(0.45f, 0.6f), stroke);
            float middle = TidySdf.Capsule(q, new Vector2(0f, -0.1f), new Vector2(-0.45f, 0.3f), stroke);
            float lower = TidySdf.Capsule(q, new Vector2(0f, -0.4f), new Vector2(0.4f, -0.75f), stroke);
            return TidySdf.Union(TidySdf.Union(stem, upper), TidySdf.Union(middle, lower));
        }

        private static Color LabelPixel(Vector2 p, LabelLayout layout, Color background, Color band, Color text, int seed)
        {
            float u = p.x / layout.Width + 0.5f;
            float v = p.y / layout.Height + 0.5f;
            float paper = TidyNoise.Fbm(u * 8f, v * 8f, seed, 3);
            Color color = TidyColor.Shade(background, 0.96f + 0.06f * paper);
            color = Color.Lerp(color, band, TidySdf.Coverage(LabelBands(p, layout), 1f));
            color = Color.Lerp(color, band, TidySdf.Coverage(LabelLogo(p - layout.LogoCenter, layout), 1f));
            color = Color.Lerp(color, text, TidySdf.Coverage(layout.TextDistance(p), 1f));
            return TidyColor.Opaque(color);
        }

        private static float LabelBands(Vector2 p, LabelLayout layout)
        {
            float halfHeight = layout.Height * 0.5f;
            switch (layout.Style % 3)
            {
                case 0:
                    return Mathf.Min(halfHeight * 0.56f - p.y, p.y + halfHeight * 0.72f);
                case 1:
                    return Mathf.Abs((p.x - p.y) * 0.7071f + layout.Width * 0.18f) - layout.Height * 0.09f;
                default:
                    return Mathf.Abs(p.y + layout.Height * 0.02f) - layout.Height * 0.11f;
            }
        }

        private static float LabelLogo(Vector2 p, LabelLayout layout)
        {
            float r = layout.LogoRadius;
            switch ((layout.Style / 3) % 3)
            {
                case 0:
                    return TidySdf.Subtract(TidySdf.Circle(p, r), TidySdf.Star5(p, r * 0.7f, 0.5f));
                case 1:
                    return TidySdf.Subtract(TidySdf.Rhombus(p, new Vector2(r, r * 1.2f)), TidySdf.Rhombus(p, new Vector2(r * 0.45f, r * 0.55f)));
                default:
                    return TidySdf.Subtract(TidySdf.RoundedBox(p, new Vector2(r, r), r * 0.3f), TidySdf.Circle(p, r * 0.45f));
            }
        }

        private static Color RugPixel(float u, float v, int size, Color a, Color b, Color c, int seed)
        {
            var p = new Vector2(u * 2f - 1f, v * 2f - 1f);
            float pixel = 2f / size;
            float inset = 1f - Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
            Color color = Color.Lerp(a, TidyColor.Shade(b, 0.8f), TidySdf.Coverage(RugLattice(p), pixel) * 0.5f);
            color = Color.Lerp(color, b, TidySdf.Coverage(RugMedallion(p), pixel));
            color = Color.Lerp(color, a, TidySdf.Coverage(TidySdf.Rhombus(p, new Vector2(0.28f, 0.4f)), pixel));
            color = Color.Lerp(color, c, TidySdf.Coverage(TidySdf.Star5(p, 0.16f, 0.5f), pixel));
            color = Color.Lerp(color, b, TidySdf.Coverage(RugCorners(p), pixel));
            color = Color.Lerp(color, c, TidySdf.Coverage(Mathf.Abs(inset - RugLineInner) - 0.008f, pixel));
            color = Color.Lerp(color, b, TidySdf.Coverage(Mathf.Max(RugBandOuter - inset, inset - RugBandInner), pixel));
            color = Color.Lerp(color, c, TidySdf.Coverage(RugBorderMotif(p, inset), pixel));
            color = Color.Lerp(color, TidyColor.Shade(c, 0.7f), TidySdf.Coverage(inset - RugBandOuter * 0.6f, pixel));
            float weave = TidyNoise.Value(u * size * 0.25f, v * size * 0.25f, seed, Mathf.Max(1, size / 4), Mathf.Max(1, size / 4));
            float wear = TidyNoise.Fbm(u * 4f, v * 4f, seed + 1, 4, 4, 4);
            return TidyColor.Opaque(TidyColor.Shade(color, (0.9f + 0.1f * weave) * (0.9f + 0.15f * wear)));
        }

        private static float RugLattice(Vector2 p)
        {
            Vector2 rotated = TidySdf.Rotate(p, 45f) * RugLatticeFrequency;
            float lines = Mathf.Min(Mathf.Abs(TidyMath.Frac(rotated.x) - 0.5f), Mathf.Abs(TidyMath.Frac(rotated.y) - 0.5f));
            return (lines - 0.03f) / RugLatticeFrequency;
        }

        private static float RugMedallion(Vector2 p)
        {
            return TidySdf.Union(TidySdf.Rhombus(p, new Vector2(0.42f, 0.58f)), TidySdf.Circle(p, 0.3f));
        }

        private static float RugCorners(Vector2 p)
        {
            var corner = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(1f - RugLineInner, 1f - RugLineInner);
            return TidySdf.Outline(TidySdf.Circle(corner, 0.22f), 0.03f);
        }

        private static float RugBorderMotif(Vector2 p, float inset)
        {
            float along = Mathf.Abs(p.x) > Mathf.Abs(p.y) ? p.y : p.x;
            float phase = (TidyMath.Frac(along * RugBorderMotifs * 0.5f) - 0.5f) * 2f / RugBorderMotifs;
            float across = inset - (RugBandOuter + RugBandInner) * 0.5f;
            return TidySdf.Rhombus(new Vector2(phase, across), new Vector2(0.06f, 0.035f));
        }

        private readonly struct BookLayout
        {
            public BookLayout(int width, int height, int style)
            {
                Width = width;
                Height = height;
                Style = style;
                Unit = Mathf.Min(width, height);
                Margin = Unit * BookMarginFraction;
                InnerMargin = Unit * BookInnerMarginFraction;
                LeatherCell = Mathf.Max(2f, Mathf.Max(width, height) * BookLeatherCellFraction);
            }

            public float Width { get; }

            public float Height { get; }

            public int Style { get; }

            public float Unit { get; }

            public float Margin { get; }

            public float InnerMargin { get; }

            public float LeatherCell { get; }
        }

        private sealed class LabelLayout
        {
            private readonly List<Vector4> words = new List<Vector4>();
            private readonly float lineRadius;

            public LabelLayout(int width, int height, int style, int seed)
            {
                Width = width;
                Height = height;
                Style = style;
                LogoRadius = Mathf.Min(width, height) * 0.16f;
                bool bandInMiddle = style % 3 == 2;
                LogoCenter = new Vector2(0f, height * (bandInMiddle ? 0.28f : 0.08f));
                lineRadius = Mathf.Max(1f, height * LabelLineHeightFraction);
                float firstLine = bandInMiddle ? -height * 0.24f : -height * 0.14f;
                AddLine(firstLine, width * 0.62f, seed, 0);
                AddLine(firstLine - lineRadius * 3.5f, width * 0.48f, seed, 1);
                AddLine(firstLine - lineRadius * 6.5f, width * 0.36f, seed, 2);
            }

            public float Width { get; }

            public float Height { get; }

            public int Style { get; }

            public float LogoRadius { get; }

            public Vector2 LogoCenter { get; }

            public float TextDistance(Vector2 p)
            {
                float distance = float.MaxValue;
                foreach (Vector4 word in words)
                {
                    distance = Mathf.Min(distance, TidySdf.Capsule(p, new Vector2(word.x, word.z), new Vector2(word.y, word.z), word.w));
                }

                return distance;
            }

            private void AddLine(float y, float maxWidth, int seed, int line)
            {
                float gap = Width * LabelWordGapFraction;
                var widths = new List<float>();
                float total = 0f;
                for (int k = 0; k < LabelMaxWords; k++)
                {
                    float wordWidth = Width * Mathf.Lerp(0.06f, 0.16f, TidyNoise.Hash01(k, line, seed + Style));
                    if (total + wordWidth > maxWidth && widths.Count > 0)
                    {
                        break;
                    }

                    widths.Add(wordWidth);
                    total += wordWidth + gap;
                }

                float x = -(total - gap) * 0.5f;
                float radius = line == 0 ? lineRadius * 1.4f : lineRadius;
                foreach (float wordWidth in widths)
                {
                    words.Add(new Vector4(x + radius, x + wordWidth - radius, y, radius));
                    x += wordWidth + gap;
                }
            }
        }
    }
}
