using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidyMath
    {
        public const float TwoPi = Mathf.PI * 2f;

        public static float Saturate(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }

        public static float SmoothStep(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge0, edge1))
            {
                return x < edge0 ? 0f : 1f;
            }

            float t = Saturate((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        public static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }

        public static int Wrap(int value, int period)
        {
            return period > 0 ? ((value % period) + period) % period : value;
        }

        public static float Quintic(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
        }
    }
}
