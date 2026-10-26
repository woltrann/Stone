using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidySdf
    {
        private static readonly Vector2 StarFold = new Vector2(0.809016994375f, -0.587785252292f);

        public static float Circle(Vector2 p, float radius)
        {
            return p.magnitude - radius;
        }

        public static float Ring(Vector2 p, float radius, float halfThickness)
        {
            return Mathf.Abs(p.magnitude - radius) - halfThickness;
        }

        public static float Box(Vector2 p, Vector2 halfSize)
        {
            return RoundedBox(p, halfSize, 0f);
        }

        public static float RoundedBox(Vector2 p, Vector2 halfSize, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x) - halfSize.x + radius, Mathf.Abs(p.y) - halfSize.y + radius);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        public static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float lengthSquared = Vector2.Dot(ba, ba);
            float h = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(pa, ba) / lengthSquared) : 0f;
            return (pa - ba * h).magnitude;
        }

        public static float Capsule(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            return Segment(p, a, b) - radius;
        }

        public static float Ellipse(Vector2 p, Vector2 radii)
        {
            float k = new Vector2(p.x / radii.x, p.y / radii.y).magnitude;
            return (k - 1f) * Mathf.Min(radii.x, radii.y);
        }

        public static float Star5(Vector2 p, float radius, float innerRatio)
        {
            Vector2 k1 = StarFold;
            var k2 = new Vector2(-k1.x, k1.y);
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= radius;
            Vector2 ba = innerRatio * new Vector2(-k1.y, k1.x) - Vector2.up;
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, radius);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        public static float Rhombus(Vector2 p, Vector2 halfSize)
        {
            p = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
            Vector2 b = halfSize;
            Vector2 offset = b - 2f * p;
            float h = Mathf.Clamp((offset.x * b.x - offset.y * b.y) / Vector2.Dot(b, b), -1f, 1f);
            float d = (p - 0.5f * new Vector2(b.x * (1f - h), b.y * (1f + h))).magnitude;
            return d * Mathf.Sign(p.x * b.y + p.y * b.x - b.x * b.y);
        }

        public static float Union(float a, float b)
        {
            return Mathf.Min(a, b);
        }

        public static float Subtract(float shape, float cutter)
        {
            return Mathf.Max(shape, -cutter);
        }

        public static float Intersect(float a, float b)
        {
            return Mathf.Max(a, b);
        }

        public static float SmoothUnion(float a, float b, float smoothness)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / smoothness);
            return Mathf.Lerp(b, a, h) - smoothness * h * (1f - h);
        }

        public static float Outline(float distance, float halfThickness)
        {
            return Mathf.Abs(distance) - halfThickness;
        }

        public static float Coverage(float distance, float pixelSize)
        {
            return TidyMath.Saturate(0.5f - distance / pixelSize);
        }

        public static Vector2 Rotate(Vector2 p, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
        }
    }
}
