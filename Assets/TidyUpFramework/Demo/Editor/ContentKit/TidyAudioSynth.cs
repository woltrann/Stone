using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyAudioSynth
    {
        public const int SampleRate = TidyDsp.SampleRate;

        private const float PeakDecibels = -3f;
        private const float EdgeFadeIn = 0.002f;
        private const float EdgeFadeOut = 0.015f;
        private const float SoftTopCutoff = 9000f;
        private const int FilterUpdateInterval = 16;
        private const int RustleGrains = 16;

        public static float[] PickUpPop()
        {
            float[] buffer = TidyDsp.Allocate(0.22f);
            var random = new TidyRandom(11);
            TidyVoices.Tone(buffer, 0f, 380f, 820f, 0.16f, 0.9f, 0.002f, 0.045f);
            TidyVoices.Tone(buffer, 0.004f, 760f, 1640f, 0.1f, 0.22f, 0.002f, 0.025f);
            TidyVoices.FilteredNoise(buffer, 0f, 0.03f, 0.12f, 0.001f, 0.008f, TidyBiquad.BandPass(2500f, 0.8f), ref random);
            return Finish(buffer);
        }

        public static float[] PlaceWoodThunk()
        {
            float[] buffer = TidyDsp.Allocate(0.38f);
            var random = new TidyRandom(23);
            TidyVoices.Tone(buffer, 0f, 150f, 100f, 0.35f, 1f, 0.001f, 0.07f);
            TidyVoices.Tone(buffer, 0f, 330f, 300f, 0.2f, 0.45f, 0.001f, 0.035f);
            TidyVoices.Tone(buffer, 0f, 620f, 600f, 0.12f, 0.22f, 0.001f, 0.02f);
            TidyVoices.FilteredNoise(buffer, 0f, 0.05f, 0.35f, 0.0005f, 0.012f, TidyBiquad.LowPass(1800f), ref random);
            TidyDsp.LowPass(buffer, 4000f);
            return Finish(buffer);
        }

        public static float[] PlaceSoftThud()
        {
            float[] buffer = TidyDsp.Allocate(0.32f);
            var random = new TidyRandom(37);
            TidyVoices.Tone(buffer, 0f, 110f, 70f, 0.3f, 1f, 0.003f, 0.06f);
            TidyVoices.Tone(buffer, 0f, 220f, 180f, 0.15f, 0.2f, 0.002f, 0.03f);
            TidyVoices.FilteredNoise(buffer, 0f, 0.12f, 0.5f, 0.002f, 0.04f, TidyBiquad.LowPass(500f), ref random);
            return Finish(buffer);
        }

        public static float[] PlaceGlassClink()
        {
            float[] buffer = TidyDsp.Allocate(0.65f);
            var random = new TidyRandom(41);
            AddClink(buffer, 0f, 1f, 1f);
            AddClink(buffer, 0.035f, 1.03f, 0.35f);
            TidyVoices.FilteredNoise(buffer, 0f, 0.01f, 0.1f, 0.0003f, 0.003f, TidyBiquad.BandPass(6000f, 1.2f), ref random);
            TidyDsp.LowPass(buffer, SoftTopCutoff);
            return Finish(buffer);
        }

        public static float[] PaperRustle()
        {
            float[] buffer = TidyDsp.Allocate(0.5f);
            var random = new TidyRandom(53);
            for (int grain = 0; grain < RustleGrains; grain++)
            {
                float start = random.Range(0f, 0.4f);
                float length = random.Range(0.015f, 0.05f);
                float center = random.Range(2200f, 4200f);
                TidyVoices.FilteredNoise(buffer, start, length * 3f, random.Range(0.3f, 1f), 0.002f, length, TidyBiquad.BandPass(center, 0.7f), ref random);
            }

            ApplyHumpEnvelope(buffer, 0.08f);
            TidyDsp.LowPass(buffer, 7000f);
            return Finish(buffer);
        }

        public static float[] SnapClick()
        {
            float[] buffer = TidyDsp.Allocate(0.12f);
            var random = new TidyRandom(61);
            TidyVoices.FilteredNoise(buffer, 0f, 0.03f, 1f, 0.0003f, 0.006f, TidyBiquad.BandPass(2200f, 1.2f), ref random);
            TidyVoices.Tone(buffer, 0f, 900f, 700f, 0.05f, 0.4f, 0.0005f, 0.012f);
            TidyVoices.Tone(buffer, 0f, 180f, 170f, 0.06f, 0.3f, 0.001f, 0.015f);
            return Finish(buffer);
        }

        public static float[] Whoosh()
        {
            const float duration = 0.65f;
            float[] buffer = TidyDsp.Allocate(duration);
            var random = new TidyRandom(71);
            var filter = TidyBiquad.BandPass(400f, 0.9f);
            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)buffer.Length;
                if (i % FilterUpdateInterval == 0)
                {
                    filter.SetBandPass(WhooshCenter(t), 0.9f);
                }

                float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.8f)), 2f);
                buffer[i] = filter.Process(random.Signed()) * envelope;
            }

            TidyDsp.LowPass(buffer, 5000f);
            return Finish(buffer);
        }

        private static float WhooshCenter(float t)
        {
            return t < 0.45f ? Mathf.Lerp(350f, 2200f, t / 0.45f) : Mathf.Lerp(2200f, 700f, (t - 0.45f) / 0.55f);
        }

        private static void AddClink(float[] buffer, float start, float pitch, float amplitude)
        {
            TidyVoices.Partial(buffer, start, 2350f * pitch, 0.6f, amplitude, 0.0005f, 0.22f);
            TidyVoices.Partial(buffer, start, 3480f * pitch, 0.5f, amplitude * 0.55f, 0.0005f, 0.15f);
            TidyVoices.Partial(buffer, start, 5120f * pitch, 0.3f, amplitude * 0.25f, 0.0005f, 0.08f);
        }

        private static void ApplyHumpEnvelope(float[] buffer, float attackFraction)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                float t = i / (float)buffer.Length;
                float rise = TidyMath.SmoothStep(0f, attackFraction, t);
                float fall = 1f - TidyMath.SmoothStep(0.5f, 1f, t);
                buffer[i] *= rise * fall;
            }
        }

        private static float[] Finish(float[] buffer)
        {
            TidyDsp.RemoveDc(buffer);
            TidyDsp.FadeEdges(buffer, EdgeFadeIn, EdgeFadeOut);
            TidyDsp.Normalize(buffer, PeakDecibels);
            return buffer;
        }

        private static float Note(float midi)
        {
            return TidyDsp.MidiToFrequency(midi);
        }
    }
}
