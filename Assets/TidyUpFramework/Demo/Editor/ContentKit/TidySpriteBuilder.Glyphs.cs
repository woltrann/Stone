using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidySpriteBuilder
    {
        private const float GlyphStroke = 0.06f;
        private const float StarRounding = 0.02f;
        private const float GearBaseRadius = 0.33f;
        private const float GearToothHeight = 0.09f;
        private const float GearToothSharpness = 2.2f;
        private const float GearHoleRadius = 0.12f;
        private const float GearDistanceScale = 0.8f;
        private const int MinimumGearTeeth = 4;

        public static Texture2D Star(int size)
        {
            return RenderGlyph(size, "Star", p => TidySdf.Star5(p + new Vector2(0f, 0.02f), 0.4f, 0.5f) - StarRounding);
        }

        public static Texture2D Check(int size)
        {
            return RenderGlyph(size, "Check", p => TidySdf.Union(
                TidySdf.Capsule(p, new Vector2(-0.26f, 0f), new Vector2(-0.08f, -0.18f), GlyphStroke),
                TidySdf.Capsule(p, new Vector2(-0.08f, -0.18f), new Vector2(0.28f, 0.22f), GlyphStroke)));
        }

        public static Texture2D Cross(int size)
        {
            return RenderGlyph(size, "Cross", p => TidySdf.Union(
                TidySdf.Capsule(p, new Vector2(-0.25f, -0.25f), new Vector2(0.25f, 0.25f), GlyphStroke),
                TidySdf.Capsule(p, new Vector2(-0.25f, 0.25f), new Vector2(0.25f, -0.25f), GlyphStroke)));
        }

        public static Texture2D Chevron(int size)
        {
            return RenderGlyph(size, "Chevron", p => TidySdf.Union(
                TidySdf.Capsule(p, new Vector2(-0.1f, 0.25f), new Vector2(0.12f, 0f), GlyphStroke),
                TidySdf.Capsule(p, new Vector2(0.12f, 0f), new Vector2(-0.1f, -0.25f), GlyphStroke)));
        }

        public static Texture2D Diamond(int size)
        {
            return RenderGlyph(size, "Diamond", p => TidySdf.Rhombus(p, new Vector2(0.32f, 0.44f)));
        }

        public static Texture2D Lock(int size)
        {
            return RenderGlyph(size, "Lock", LockDistance);
        }

        public static Texture2D Hourglass(int size)
        {
            return RenderGlyph(size, "Hourglass", HourglassDistance);
        }

        public static Texture2D Lightbulb(int size)
        {
            return RenderGlyph(size, "Lightbulb", LightbulbDistance);
        }

        public static Texture2D Gear(int size, int teeth = 8)
        {
            int toothCount = Mathf.Max(MinimumGearTeeth, teeth);
            return RenderGlyph(size, "Gear", p => GearDistance(p, toothCount));
        }

        public static Texture2D Crosshair(int size)
        {
            float pixel = 1f / Mathf.Max(1, size);
            return RenderPixels(size, "Crosshair", p =>
            {
                Vector2 q = p * pixel;
                float core = TidySdf.Coverage(TidySdf.Circle(q, 0.16f), pixel);
                float halo = 0.45f * (1f - TidyMath.SmoothStep(0.16f, 0.48f, q.magnitude));
                return Mathf.Max(core, halo);
            });
        }

        public static Texture2D SoftGlow(int size)
        {
            float pixel = 1f / Mathf.Max(1, size);
            return RenderPixels(size, "SoftGlow", p =>
            {
                float radius = (p * pixel).magnitude / 0.22f;
                float window = 1f - TidyMath.SmoothStep(0.42f, 0.5f, (p * pixel).magnitude);
                return Mathf.Exp(-radius * radius) * window;
            });
        }

        private static float LockDistance(Vector2 p)
        {
            float body = TidySdf.RoundedBox(p - new Vector2(0f, -0.12f), new Vector2(0.28f, 0.2f), 0.05f);
            float shackle = TidySdf.Intersect(TidySdf.Ring(p - new Vector2(0f, 0.08f), 0.17f, 0.045f), 0.08f - p.y);
            float keyhole = TidySdf.Union(
                TidySdf.Circle(p - new Vector2(0f, -0.08f), 0.05f),
                TidySdf.Box(p - new Vector2(0f, -0.17f), new Vector2(0.02f, 0.06f)));
            return TidySdf.Union(TidySdf.Subtract(body, keyhole), shackle);
        }

        private static float HourglassDistance(Vector2 p)
        {
            const float waist = 0.05f;
            const float slope = 0.8f;
            const float halfHeight = 0.36f;
            float slanted = (Mathf.Abs(p.x) - waist - slope * Mathf.Abs(p.y)) / Mathf.Sqrt(1f + slope * slope);
            float glass = TidySdf.Intersect(slanted, Mathf.Abs(p.y) - halfHeight);
            float caps = TidySdf.RoundedBox(new Vector2(p.x, Mathf.Abs(p.y) - 0.41f), new Vector2(0.38f, 0.035f), 0.02f);
            return TidySdf.Union(glass, caps);
        }

        private static float LightbulbDistance(Vector2 p)
        {
            float bulb = TidySdf.Circle(p - new Vector2(0f, 0.1f), 0.26f);
            float neck = TidySdf.RoundedBox(p - new Vector2(0f, -0.17f), new Vector2(0.12f, 0.08f), 0.02f);
            float glass = TidySdf.SmoothUnion(bulb, neck, 0.08f);
            float baseUpper = TidySdf.Capsule(p, new Vector2(-0.09f, -0.27f), new Vector2(0.09f, -0.27f), 0.03f);
            float baseLower = TidySdf.Capsule(p, new Vector2(-0.06f, -0.34f), new Vector2(0.06f, -0.34f), 0.025f);
            return TidySdf.Union(glass, TidySdf.Union(baseUpper, baseLower));
        }

        private static float GearDistance(Vector2 p, int teeth)
        {
            float angle = Mathf.Atan2(p.y, p.x);
            float tooth = TidyMath.Saturate(Mathf.Cos(teeth * angle) * GearToothSharpness * 0.5f + 0.5f);
            float outline = (p.magnitude - (GearBaseRadius + GearToothHeight * tooth)) * GearDistanceScale;
            return TidySdf.Subtract(outline, TidySdf.Circle(p, GearHoleRadius));
        }
    }
}
