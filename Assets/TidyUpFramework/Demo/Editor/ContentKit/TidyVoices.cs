using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal static class TidyVoices
    {
        private const float MaxPartialFrequency = 16000f;
        private const float BellDecayFraction = 0.32f;
        private const float BellAttack = 0.0015f;
        private const float PhaseModulationScale = 1f / TidyMath.TwoPi;
        private const float ReleaseFadeSeconds = 0.02f;
        private const float PadDetune = 0.0035f;
        private const float PadMaxFrequency = 12000f;
        private const float PadVoicePhaseOffset = 0.37f;

        private static readonly float[] BellRatios = { 1f, 2.01f, 2.76f, 4.07f, 5.43f };
        private static readonly float[] BellLevels = { 1f, 0.45f, 0.3f, 0.16f, 0.07f };
        private static readonly float[] BellDecayScales = { 1f, 0.6f, 0.42f, 0.3f, 0.2f };

        public static void Partial(float[] buffer, float startSeconds, float frequency, float durationSeconds, float amplitude, float attack, float decay)
        {
            if (frequency >= MaxPartialFrequency)
            {
                return;
            }

            NoteSpan span = NoteSpan.Create(buffer, startSeconds, durationSeconds);
            int attackSamples = Mathf.Max(1, TidyDsp.ToSamples(attack));
            float decayFactor = TidyDsp.DecayFactor(decay);
            float level = 1f;
            float phase = 0f;
            for (int i = 0; i < span.Length; i++)
            {
                float envelope = i < attackSamples ? i / (float)attackSamples : level;
                if (i >= attackSamples)
                {
                    level *= decayFactor;
                }

                buffer[span.Start + i] += TidyDsp.Sine(phase) * amplitude * envelope * span.ReleaseGain(i);
                phase = TidyDsp.Advance(phase, frequency);
            }
        }

        public static void Tone(float[] buffer, float startSeconds, float startFrequency, float endFrequency, float durationSeconds, float amplitude, float attack, float decay)
        {
            NoteSpan span = NoteSpan.Create(buffer, startSeconds, durationSeconds);
            int attackSamples = Mathf.Max(1, TidyDsp.ToSamples(attack));
            int totalSamples = Mathf.Max(1, TidyDsp.ToSamples(durationSeconds));
            float glide = Mathf.Exp(Mathf.Log(Mathf.Max(1f, endFrequency) / Mathf.Max(1f, startFrequency)) / totalSamples);
            float decayFactor = TidyDsp.DecayFactor(decay);
            float frequency = startFrequency;
            float level = 1f;
            float phase = 0f;
            for (int i = 0; i < span.Length; i++)
            {
                float envelope = i < attackSamples ? i / (float)attackSamples : level;
                if (i >= attackSamples)
                {
                    level *= decayFactor;
                }

                buffer[span.Start + i] += TidyDsp.Sine(phase) * amplitude * envelope * span.ReleaseGain(i);
                phase = TidyDsp.Advance(phase, frequency);
                frequency *= glide;
            }
        }

        public static void Bell(float[] buffer, float startSeconds, float frequency, float durationSeconds, float amplitude)
        {
            for (int p = 0; p < BellRatios.Length; p++)
            {
                float decay = durationSeconds * BellDecayFraction * BellDecayScales[p];
                Partial(buffer, startSeconds, frequency * BellRatios[p], durationSeconds, amplitude * BellLevels[p], BellAttack, decay);
            }
        }

        public static void Fm(float[] buffer, float startSeconds, float frequency, float durationSeconds, float amplitude, TidyFmPatch patch)
        {
            NoteSpan span = NoteSpan.Create(buffer, startSeconds, durationSeconds);
            int attackSamples = Mathf.Max(1, TidyDsp.ToSamples(patch.Attack));
            float levelFactor = TidyDsp.DecayFactor(durationSeconds * patch.DecayFraction);
            float indexFactor = TidyDsp.DecayFactor(patch.IndexDecay);
            float overtoneFactor = TidyDsp.DecayFactor(patch.OvertoneDecay);
            float modulatorFrequency = frequency * patch.ModulatorRatio;
            float overtoneFrequency = frequency * patch.OvertoneRatio;
            bool useOvertone = overtoneFrequency < MaxPartialFrequency;
            float level = 1f;
            float index = patch.ModulationIndex;
            float overtone = patch.OvertoneLevel;
            float carrierPhase = 0f;
            float modulatorPhase = 0f;
            float overtonePhase = 0f;
            for (int i = 0; i < span.Length; i++)
            {
                float attack = i < attackSamples ? i / (float)attackSamples : 1f;
                float modulation = TidyDsp.Sine(modulatorPhase) * index * PhaseModulationScale;
                float sample = TidyDsp.Sine(carrierPhase + modulation);
                if (useOvertone)
                {
                    sample += TidyDsp.Sine(overtonePhase) * overtone;
                }

                buffer[span.Start + i] += sample * amplitude * level * attack * span.ReleaseGain(i);
                carrierPhase = TidyDsp.Advance(carrierPhase, frequency);
                modulatorPhase = TidyDsp.Advance(modulatorPhase, modulatorFrequency);
                overtonePhase = TidyDsp.Advance(overtonePhase, overtoneFrequency);
                level *= levelFactor;
                index *= indexFactor;
                overtone *= overtoneFactor;
            }
        }

        public static void Pad(float[] buffer, float startSeconds, float frequency, float sustainSeconds, float releaseSeconds, float attackSeconds, float amplitude, float brightness, int harmonics)
        {
            NoteSpan span = NoteSpan.Create(buffer, startSeconds, sustainSeconds + releaseSeconds);
            if (span.Length <= 0)
            {
                return;
            }

            float[] envelope = PadEnvelope(span.Length, attackSeconds, sustainSeconds, releaseSeconds);
            float[] wave = TidyDsp.CreateHarmonicTable(harmonics, brightness, frequency, PadMaxFrequency);
            float lowFrequency = frequency * (1f - PadDetune);
            float highFrequency = frequency * (1f + PadDetune);
            float lowPhase = 0f;
            float highPhase = PadVoicePhaseOffset;
            float level = amplitude * 0.5f;
            for (int i = 0; i < span.Length; i++)
            {
                float sample = TidyDsp.ReadTable(wave, lowPhase) + TidyDsp.ReadTable(wave, highPhase);
                buffer[span.Start + i] += sample * level * envelope[i];
                lowPhase = TidyDsp.Advance(lowPhase, lowFrequency);
                highPhase = TidyDsp.Advance(highPhase, highFrequency);
            }
        }

        public static void FilteredNoise(float[] buffer, float startSeconds, float durationSeconds, float amplitude, float attack, float decay, TidyBiquad filter, ref TidyRandom random)
        {
            NoteSpan span = NoteSpan.Create(buffer, startSeconds, durationSeconds);
            int attackSamples = Mathf.Max(1, TidyDsp.ToSamples(attack));
            float decayFactor = TidyDsp.DecayFactor(decay);
            float level = 1f;
            for (int i = 0; i < span.Length; i++)
            {
                float envelope = i < attackSamples ? i / (float)attackSamples : level;
                if (i >= attackSamples)
                {
                    level *= decayFactor;
                }

                buffer[span.Start + i] += filter.Process(random.Signed()) * amplitude * envelope * span.ReleaseGain(i);
            }
        }

        public static void SoftKick(float[] buffer, float startSeconds, float amplitude)
        {
            Tone(buffer, startSeconds, 110f, 45f, 0.32f, amplitude, 0.002f, 0.09f);
        }

        public static void SoftHat(float[] buffer, float startSeconds, float amplitude, ref TidyRandom random)
        {
            FilteredNoise(buffer, startSeconds, 0.06f, amplitude, 0.001f, 0.014f, TidyBiquad.HighPass(7000f), ref random);
        }

        private static float[] PadEnvelope(int length, float attackSeconds, float sustainSeconds, float releaseSeconds)
        {
            var envelope = new float[length];
            int attack = Mathf.Max(1, TidyDsp.ToSamples(attackSeconds));
            int sustain = TidyDsp.ToSamples(sustainSeconds);
            int release = Mathf.Max(1, TidyDsp.ToSamples(releaseSeconds));
            for (int i = 0; i < length; i++)
            {
                float rise = TidyMath.SmoothStep(0f, 1f, i / (float)attack);
                float fall = i < sustain ? 1f : Mathf.Pow(1f - Mathf.Clamp01((i - sustain) / (float)release), 2f);
                envelope[i] = rise * fall;
            }

            return envelope;
        }

        private readonly struct NoteSpan
        {
            private readonly int releaseSamples;

            private NoteSpan(int start, int length)
            {
                Start = start;
                Length = length;
                releaseSamples = Mathf.Max(1, Mathf.Min(TidyDsp.ToSamples(ReleaseFadeSeconds), length));
            }

            public int Start { get; }

            public int Length { get; }

            public static NoteSpan Create(float[] buffer, float startSeconds, float durationSeconds)
            {
                int start = Mathf.Clamp(TidyDsp.ToSamples(startSeconds), 0, buffer.Length);
                int length = Mathf.Min(TidyDsp.ToSamples(durationSeconds), buffer.Length - start);
                return new NoteSpan(start, Mathf.Max(0, length));
            }

            public float ReleaseGain(int index)
            {
                int remaining = Length - index;
                return remaining >= releaseSamples ? 1f : remaining / (float)releaseSamples;
            }
        }
    }
}
