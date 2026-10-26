using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyAudioSynth
    {
        private const float LoopCrossfadeSeconds = 0.15f;
        private const float AmbienceCrossfadeSeconds = 1f;
        private const float MinimumAmbienceSeconds = 4f;
        private const float ScrubLoopSeconds = 2f;
        private const int ScrubStrokes = 8;
        private const float VacuumLoopSeconds = 3f;
        private const float VacuumFundamental = 118f;
        private const int VacuumHarmonics = 8;
        private const float BrownStep = 0.02f;
        private const float BrownLeak = 0.999f;
        private const float BedDecibels = -20f;
        private const float CreakInterval = 9f;
        private const float PageTurnInterval = 13f;
        private const float ChimeInterval = 15f;
        private const float FootstepInterval = 11f;
        private const float EventMarginSeconds = 1.5f;

        public static float[] ScrubLoop()
        {
            float[] buffer = TidyDsp.Allocate(ScrubLoopSeconds + LoopCrossfadeSeconds);
            var random = new TidyRandom(131);
            var bright = TidyBiquad.BandPass(2800f, 0.9f);
            var body = TidyBiquad.BandPass(900f, 0.8f);
            int loopSamples = TidyDsp.ToSamples(ScrubLoopSeconds);
            for (int i = 0; i < buffer.Length; i++)
            {
                float phase = (i % loopSamples) / (float)loopSamples * ScrubStrokes;
                float stroke = Mathf.Sin(Mathf.PI * TidyMath.Frac(phase));
                float strength = 0.75f + 0.25f * Mathf.Sin(Mathf.Floor(phase) * 2.3f);
                float envelope = (0.35f + 0.65f * stroke * stroke) * strength;
                float noise = random.Signed();
                buffer[i] = (bright.Process(noise) + 0.6f * body.Process(noise)) * envelope;
            }

            return FinishLoop(TidyDsp.CrossfadeLoop(buffer, LoopCrossfadeSeconds));
        }

        public static float[] VacuumLoop()
        {
            float[] buffer = TidyDsp.Allocate(VacuumLoopSeconds + LoopCrossfadeSeconds);
            var random = new TidyRandom(137);
            float fundamental = LoopAlignedFrequency(VacuumFundamental, VacuumLoopSeconds);
            float wobble = LoopAlignedFrequency(0.7f, VacuumLoopSeconds);
            var roar = TidyBiquad.LowPass(1200f);
            var whine = TidyBiquad.BandPass(2500f, 4f);
            var phases = new float[VacuumHarmonics];
            float wobblePhase = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                float motor = 0f;
                float drift = 1f + 0.004f * TidyDsp.Sine(wobblePhase);
                for (int h = 0; h < VacuumHarmonics; h++)
                {
                    motor += TidyDsp.Sine(phases[h]) / Mathf.Pow(h + 1, 1.2f);
                    phases[h] = TidyDsp.Advance(phases[h], fundamental * (h + 1) * drift);
                }

                wobblePhase = TidyDsp.Advance(wobblePhase, wobble);
                float noise = random.Signed();
                buffer[i] = motor * 0.25f + roar.Process(noise) * 0.9f + whine.Process(noise) * 0.15f;
            }

            TidyDsp.LowPass(buffer, 6000f);
            return FinishLoop(TidyDsp.CrossfadeLoop(buffer, LoopCrossfadeSeconds));
        }

        public static float[] RoomToneLoop(float seconds)
        {
            float length = Mathf.Max(MinimumAmbienceSeconds, seconds);
            float[] buffer = BrownNoise(length + AmbienceCrossfadeSeconds, 101, 380f);
            return FinishLoop(TidyDsp.CrossfadeLoop(buffer, AmbienceCrossfadeSeconds));
        }

        public static float[] LibraryAmbienceLoop(float seconds)
        {
            float length = Mathf.Max(MinimumAmbienceSeconds, seconds);
            float total = length + AmbienceCrossfadeSeconds;
            float[] buffer = BrownNoise(total, 211, 420f);
            TidyDsp.Normalize(buffer, BedDecibels);
            var random = new TidyRandom(223);
            foreach (float time in EventTimes(total, CreakInterval, ref random))
            {
                AddCreak(buffer, time, ref random);
            }

            foreach (float time in EventTimes(total, PageTurnInterval, ref random))
            {
                AddPageTurn(buffer, time, ref random);
            }

            return FinishLoop(TidyDsp.CrossfadeLoop(buffer, AmbienceCrossfadeSeconds));
        }

        public static float[] StoreAmbienceLoop(float seconds)
        {
            float length = Mathf.Max(MinimumAmbienceSeconds, seconds);
            float total = length + AmbienceCrossfadeSeconds;
            float[] buffer = BrownNoise(total, 307, 700f);
            TidyDsp.Normalize(buffer, BedDecibels);
            var random = new TidyRandom(311);
            AddMurmur(buffer, total, ref random);
            foreach (float time in EventTimes(total, ChimeInterval, ref random))
            {
                AddDistantChime(buffer, time, ref random);
            }

            foreach (float time in EventTimes(total, FootstepInterval, ref random))
            {
                AddFootsteps(buffer, time, ref random);
            }

            return FinishLoop(TidyDsp.CrossfadeLoop(buffer, AmbienceCrossfadeSeconds));
        }

        private static float[] BrownNoise(float seconds, int seed, float lowPass)
        {
            float[] buffer = TidyDsp.Allocate(seconds);
            var random = new TidyRandom(seed);
            float state = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                state = (state + random.Signed() * BrownStep) * BrownLeak;
                buffer[i] = state;
            }

            TidyDsp.HighPass(buffer, 25f);
            TidyDsp.LowPass(buffer, lowPass);
            return buffer;
        }

        private static float[] EventTimes(float total, float interval, ref TidyRandom random)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(total / interval));
            var times = new float[count];
            float slot = (total - EventMarginSeconds) / count;
            for (int k = 0; k < count; k++)
            {
                times[k] = k * slot + random.Range(0.1f, Mathf.Max(0.2f, slot - 0.9f));
            }

            return times;
        }

        private static void AddCreak(float[] buffer, float start, ref TidyRandom random)
        {
            float duration = random.Range(0.35f, 0.8f);
            float rateStart = random.Range(18f, 35f);
            float rateEnd = random.Range(40f, 70f);
            float resonance = random.Range(480f, 900f);
            float[] creak = TidyDsp.Allocate(duration);
            var low = TidyBiquad.BandPass(resonance, 8f);
            var high = TidyBiquad.BandPass(resonance * 2.1f, 6f);
            float phase = 0f;
            for (int i = 0; i < creak.Length; i++)
            {
                float t = i / (float)creak.Length;
                phase += Mathf.Lerp(rateStart, rateEnd, t) * TidyDsp.SecondsPerSample;
                float impulse = 0f;
                if (phase >= 1f)
                {
                    phase -= 1f;
                    impulse = random.Range(0.6f, 1f);
                }

                float envelope = Mathf.Sin(Mathf.PI * t);
                creak[i] = (low.Process(impulse) + 0.5f * high.Process(impulse)) * envelope;
            }

            TidyDsp.Normalize(creak, -14f);
            TidyDsp.Mix(buffer, creak, TidyDsp.ToSamples(start), 1f);
        }

        private static void AddPageTurn(float[] buffer, float start, ref TidyRandom random)
        {
            float[] page = TidyDsp.Allocate(0.6f);
            var filter = TidyBiquad.BandPass(1800f, 0.8f);
            for (int i = 0; i < page.Length; i++)
            {
                float t = i / (float)page.Length;
                if (i % FilterUpdateInterval == 0)
                {
                    filter.SetBandPass(Mathf.Lerp(1800f, 3500f, t), 0.8f);
                }

                float swish = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.6f));
                float flop = t > 0.62f ? Mathf.Exp(-(t - 0.62f) * 40f) : 0f;
                page[i] = filter.Process(random.Signed()) * (swish * swish * 0.6f + flop);
            }

            TidyDsp.LowPass(page, 6000f);
            TidyDsp.Normalize(page, -16f);
            TidyDsp.Mix(buffer, page, TidyDsp.ToSamples(start), 1f);
        }

        private static void AddMurmur(float[] buffer, float total, ref TidyRandom random)
        {
            var voice = TidyBiquad.BandPass(500f, 1.2f);
            float[] murmur = new float[buffer.Length];
            float slow = LoopAlignedFrequency(0.13f, total);
            float faster = LoopAlignedFrequency(0.41f, total);
            for (int i = 0; i < murmur.Length; i++)
            {
                float t = i * TidyDsp.SecondsPerSample;
                float swell = 0.5f + 0.25f * Mathf.Sin(TidyMath.TwoPi * slow * t) + 0.25f * Mathf.Sin(TidyMath.TwoPi * faster * t + 1.3f);
                murmur[i] = voice.Process(random.Signed()) * swell;
            }

            TidyDsp.Normalize(murmur, -24f);
            TidyDsp.Mix(buffer, murmur, 0, 1f);
        }

        private static void AddDistantChime(float[] buffer, float start, ref TidyRandom random)
        {
            float[] chime = TidyDsp.Allocate(1.6f);
            float root = random.Chance(0.5f) ? 84f : 81f;
            TidyVoices.Bell(chime, 0f, Note(root), 1.4f, 1f);
            TidyVoices.Bell(chime, 0.22f, Note(root - 4f), 1.3f, 0.8f);
            TidyDsp.LowPass(chime, 3000f);
            TidyDsp.Normalize(chime, -22f);
            TidyDsp.Mix(buffer, chime, TidyDsp.ToSamples(start), 1f);
        }

        private static void AddFootsteps(float[] buffer, float start, ref TidyRandom random)
        {
            int steps = random.Range(4, 7);
            float[] walk = TidyDsp.Allocate(steps * 0.5f + 0.3f);
            for (int k = 0; k < steps; k++)
            {
                float time = k * 0.5f + random.Range(-0.03f, 0.03f) + 0.05f;
                TidyVoices.Tone(walk, time, 95f, 70f, 0.2f, random.Range(0.6f, 1f), 0.004f, 0.04f);
                TidyVoices.FilteredNoise(walk, time, 0.08f, 0.3f, 0.002f, 0.02f, TidyBiquad.LowPass(900f), ref random);
            }

            TidyDsp.Normalize(walk, -26f);
            TidyDsp.Mix(buffer, walk, TidyDsp.ToSamples(start), 1f);
        }

        private static float LoopAlignedFrequency(float frequency, float loopSeconds)
        {
            return Mathf.Max(1f, Mathf.Round(frequency * loopSeconds)) / loopSeconds;
        }

        private static float[] FinishLoop(float[] buffer)
        {
            TidyDsp.Normalize(buffer, PeakDecibels);
            return buffer;
        }
    }
}
