using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal struct TidyBiquad
    {
        public const float ButterworthQ = 0.7071f;

        private const float MinimumFrequency = 10f;
        private const float NyquistSafety = 0.45f;

        private float b0;
        private float b1;
        private float b2;
        private float a1;
        private float a2;
        private float z1;
        private float z2;

        public static TidyBiquad LowPass(float cutoff, float q = ButterworthQ)
        {
            var filter = new TidyBiquad();
            filter.SetLowPass(cutoff, q);
            return filter;
        }

        public static TidyBiquad HighPass(float cutoff, float q = ButterworthQ)
        {
            var filter = new TidyBiquad();
            filter.SetHighPass(cutoff, q);
            return filter;
        }

        public static TidyBiquad BandPass(float center, float q = 1f)
        {
            var filter = new TidyBiquad();
            filter.SetBandPass(center, q);
            return filter;
        }

        public void SetLowPass(float cutoff, float q)
        {
            Prepare(cutoff, q, out float cos, out float alpha);
            float a0 = 1f + alpha;
            SetCoefficients((1f - cos) * 0.5f, 1f - cos, (1f - cos) * 0.5f, a0, -2f * cos, 1f - alpha);
        }

        public void SetHighPass(float cutoff, float q)
        {
            Prepare(cutoff, q, out float cos, out float alpha);
            float a0 = 1f + alpha;
            SetCoefficients((1f + cos) * 0.5f, -(1f + cos), (1f + cos) * 0.5f, a0, -2f * cos, 1f - alpha);
        }

        public void SetBandPass(float center, float q)
        {
            Prepare(center, q, out float cos, out float alpha);
            float a0 = 1f + alpha;
            SetCoefficients(alpha, 0f, -alpha, a0, -2f * cos, 1f - alpha);
        }

        public float Process(float input)
        {
            float output = b0 * input + z1;
            z1 = b1 * input - a1 * output + z2;
            z2 = b2 * input - a2 * output;
            return output;
        }

        private static void Prepare(float frequency, float q, out float cos, out float alpha)
        {
            float clamped = Mathf.Clamp(frequency, MinimumFrequency, TidyDsp.SampleRate * NyquistSafety);
            float omega = TidyMath.TwoPi * clamped / TidyDsp.SampleRate;
            cos = Mathf.Cos(omega);
            alpha = Mathf.Sin(omega) / (2f * Mathf.Max(0.05f, q));
        }

        private void SetCoefficients(float n0, float n1, float n2, float d0, float d1, float d2)
        {
            b0 = n0 / d0;
            b1 = n1 / d0;
            b2 = n2 / d0;
            a1 = d1 / d0;
            a2 = d2 / d0;
        }
    }
}
