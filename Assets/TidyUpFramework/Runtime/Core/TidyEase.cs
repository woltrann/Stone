using UnityEngine;

namespace TidyUpFramework.Core
{
    public enum TidyEaseType
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        OutCubic,
        InOutCubic,
        OutBack,
        OutElastic,
        InOutSine,
        OutBounce
    }

    public static class TidyEase
    {
        const float BackOvershoot = 1.70158f;
        const float BackOvershootPlusOne = BackOvershoot + 1f;
        const float ElasticFrequency = 2f * Mathf.PI / 3f;
        const float BounceDivisor = 2.75f;
        const float BounceScale = 7.5625f;

        public static float Evaluate(TidyEaseType ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case TidyEaseType.InQuad:
                    return t * t;
                case TidyEaseType.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case TidyEaseType.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case TidyEaseType.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case TidyEaseType.InOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                case TidyEaseType.OutBack:
                    return 1f + BackOvershootPlusOne * Mathf.Pow(t - 1f, 3f) + BackOvershoot * Mathf.Pow(t - 1f, 2f);
                case TidyEaseType.OutElastic:
                    return EvaluateOutElastic(t);
                case TidyEaseType.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case TidyEaseType.OutBounce:
                    return EvaluateOutBounce(t);
                default:
                    return t;
            }
        }

        static float EvaluateOutElastic(float t)
        {
            if (t <= 0f || t >= 1f)
            {
                return t;
            }

            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * ElasticFrequency) + 1f;
        }

        static float EvaluateOutBounce(float t)
        {
            if (t < 1f / BounceDivisor)
            {
                return BounceScale * t * t;
            }

            if (t < 2f / BounceDivisor)
            {
                t -= 1.5f / BounceDivisor;
                return BounceScale * t * t + 0.75f;
            }

            if (t < 2.5f / BounceDivisor)
            {
                t -= 2.25f / BounceDivisor;
                return BounceScale * t * t + 0.9375f;
            }

            t -= 2.625f / BounceDivisor;
            return BounceScale * t * t + 0.984375f;
        }
    }
}
