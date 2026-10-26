using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public readonly struct TidyCellularSample
    {
        public TidyCellularSample(float nearest, float secondNearest, int cellHash)
        {
            F1 = nearest;
            F2 = secondNearest;
            CellHash = cellHash;
        }

        public float F1 { get; }

        public float F2 { get; }

        public int CellHash { get; }

        public float Edge => F2 - F1;
    }

    public static class TidyNoise
    {
        private const int HashMask = 0x7FFFFFFF;
        private const int UnitMask = 0xFFFFFF;
        private const float UnitScale = 1f / UnitMask;
        private const float GradientRangeScale = 0.7071068f;
        private const int OctaveSeedStep = 1013;
        private const int JitterSeedX = 7919;
        private const int JitterSeedY = 6151;
        private const float CellCenter = 0.5f;

        private static readonly Vector2[] Gradients =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(0.7071068f, 0.7071068f), new Vector2(-0.7071068f, 0.7071068f),
            new Vector2(0.7071068f, -0.7071068f), new Vector2(-0.7071068f, -0.7071068f)
        };

        public static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 0x27D4EB2Du;
                h ^= (uint)x * 0x85EBCA6Bu;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xC2B2AE35u;
                h *= 0x9E3779B1u;
                h ^= h >> 15;
                h *= 0x85EBCA77u;
                h ^= h >> 13;
                h *= 0xC2B2AE3Du;
                h ^= h >> 16;
                return (int)(h & HashMask);
            }
        }

        public static float Hash01(int x, int y, int seed)
        {
            return (Hash(x, y, seed) & UnitMask) * UnitScale;
        }

        public static float Value(float x, float y, int seed, int periodX = 0, int periodY = 0)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float sx = TidyMath.Quintic(x - ix);
            float sy = TidyMath.Quintic(y - iy);
            int x0 = TidyMath.Wrap(ix, periodX);
            int x1 = TidyMath.Wrap(ix + 1, periodX);
            int y0 = TidyMath.Wrap(iy, periodY);
            int y1 = TidyMath.Wrap(iy + 1, periodY);
            float bottom = Mathf.Lerp(Hash01(x0, y0, seed), Hash01(x1, y0, seed), sx);
            float top = Mathf.Lerp(Hash01(x0, y1, seed), Hash01(x1, y1, seed), sx);
            return Mathf.Lerp(bottom, top, sy);
        }

        public static float Gradient(float x, float y, int seed, int periodX = 0, int periodY = 0)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;
            int x0 = TidyMath.Wrap(ix, periodX);
            int x1 = TidyMath.Wrap(ix + 1, periodX);
            int y0 = TidyMath.Wrap(iy, periodY);
            int y1 = TidyMath.Wrap(iy + 1, periodY);
            float n00 = Vector2.Dot(GradientAt(x0, y0, seed), new Vector2(fx, fy));
            float n10 = Vector2.Dot(GradientAt(x1, y0, seed), new Vector2(fx - 1f, fy));
            float n01 = Vector2.Dot(GradientAt(x0, y1, seed), new Vector2(fx, fy - 1f));
            float n11 = Vector2.Dot(GradientAt(x1, y1, seed), new Vector2(fx - 1f, fy - 1f));
            float sx = TidyMath.Quintic(fx);
            float sy = TidyMath.Quintic(fy);
            float n = Mathf.Lerp(Mathf.Lerp(n00, n10, sx), Mathf.Lerp(n01, n11, sx), sy);
            return TidyMath.Saturate(0.5f + n * GradientRangeScale);
        }

        public static float Fbm(float x, float y, int seed, int octaves = 4, int periodX = 0, int periodY = 0, float gain = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float normalization = 0f;
            float frequency = 1f;
            int px = periodX;
            int py = periodY;
            for (int octave = 0; octave < Mathf.Max(1, octaves); octave++)
            {
                sum += amplitude * Gradient(x * frequency, y * frequency, seed + octave * OctaveSeedStep, px, py);
                normalization += amplitude;
                amplitude *= gain;
                frequency *= 2f;
                px *= 2;
                py *= 2;
            }

            return sum / normalization;
        }

        public static float Ridged(float x, float y, int seed, int octaves = 4, int periodX = 0, int periodY = 0, float gain = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float normalization = 0f;
            float frequency = 1f;
            int px = periodX;
            int py = periodY;
            for (int octave = 0; octave < Mathf.Max(1, octaves); octave++)
            {
                float signal = 1f - Mathf.Abs(2f * Gradient(x * frequency, y * frequency, seed + octave * OctaveSeedStep, px, py) - 1f);
                sum += amplitude * signal * signal;
                normalization += amplitude;
                amplitude *= gain;
                frequency *= 2f;
                px *= 2;
                py *= 2;
            }

            return sum / normalization;
        }

        public static TidyCellularSample Cellular(float x, float y, int seed, int periodX = 0, int periodY = 0, float jitter = 1f)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float nearest = float.MaxValue;
            float second = float.MaxValue;
            int nearestHash = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx;
                    int cy = iy + dy;
                    int wx = TidyMath.Wrap(cx, periodX);
                    int wy = TidyMath.Wrap(cy, periodY);
                    float featureX = cx + CellCenter + (Hash01(wx, wy, seed + JitterSeedX) - CellCenter) * jitter;
                    float featureY = cy + CellCenter + (Hash01(wx, wy, seed + JitterSeedY) - CellCenter) * jitter;
                    float distance = new Vector2(featureX - x, featureY - y).magnitude;
                    if (distance < nearest)
                    {
                        second = nearest;
                        nearest = distance;
                        nearestHash = Hash(wx, wy, seed);
                    }
                    else if (distance < second)
                    {
                        second = distance;
                    }
                }
            }

            return new TidyCellularSample(nearest, second, nearestHash);
        }

        public static float Worley(float x, float y, int seed, int periodX = 0, int periodY = 0)
        {
            return Cellular(x, y, seed, periodX, periodY).F1;
        }

        private static Vector2 GradientAt(int x, int y, int seed)
        {
            return Gradients[Hash(x, y, seed) & (Gradients.Length - 1)];
        }
    }
}
