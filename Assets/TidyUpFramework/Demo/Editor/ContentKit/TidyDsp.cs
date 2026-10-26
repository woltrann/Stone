using System;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal static class TidyDsp
    {
        public const int SampleRate = 44100;
        public const float SecondsPerSample = 1f / SampleRate;

        private const int SineTableSize = 4096;
        private const float SilenceThreshold = 1e-6f;
        private const float ReferenceMidi = 69f;
        private const float ReferenceFrequency = 440f;
        private const float SemitonesPerOctave = 12f;
        private const float DcCutoff = 20f;

        private static readonly float[] SineTable = CreateSineTable();

        public static float Sine(float phaseCycles)
        {
            return ReadTable(SineTable, phaseCycles);
        }

        public static float ReadTable(float[] table, float phaseCycles)
        {
            int size = table.Length - 1;
            float wrapped = phaseCycles >= 0f && phaseCycles < 1f ? phaseCycles : phaseCycles - Mathf.Floor(phaseCycles);
            float position = wrapped * size;
            int index = (int)position;
            if (index >= size)
            {
                index = size - 1;
            }

            float fraction = position - index;
            return table[index] + (table[index + 1] - table[index]) * fraction;
        }

        public static float Advance(float phase, float frequency)
        {
            phase += frequency * SecondsPerSample;
            return phase >= 1f ? phase - 1f : phase;
        }

        public static float[] CreateHarmonicTable(int harmonics, float brightness, float baseFrequency, float maxFrequency)
        {
            var table = new float[SineTableSize + 1];
            for (int harmonic = 1; harmonic <= harmonics && baseFrequency * harmonic <= maxFrequency; harmonic++)
            {
                float level = Mathf.Pow(brightness, harmonic - 1) / harmonic;
                for (int i = 0; i <= SineTableSize; i++)
                {
                    table[i] += level * SineTable[(i * harmonic) % SineTableSize];
                }
            }

            return table;
        }

        public static int ToSamples(float seconds)
        {
            return Mathf.Max(0, Mathf.RoundToInt(seconds * SampleRate));
        }

        public static float[] Allocate(float seconds)
        {
            return new float[Mathf.Max(1, ToSamples(seconds))];
        }

        public static float MidiToFrequency(float midi)
        {
            return ReferenceFrequency * Mathf.Pow(2f, (midi - ReferenceMidi) / SemitonesPerOctave);
        }

        public static float DecibelsToGain(float decibels)
        {
            return Mathf.Pow(10f, decibels / 20f);
        }

        public static float DecayFactor(float decaySeconds)
        {
            return decaySeconds > 0f ? Mathf.Exp(-SecondsPerSample / decaySeconds) : 0f;
        }

        public static void Normalize(float[] buffer, float peakDecibels)
        {
            float peak = Peak(buffer);
            if (peak < SilenceThreshold)
            {
                return;
            }

            float gain = DecibelsToGain(peakDecibels) / peak;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= gain;
            }
        }

        public static float Peak(float[] buffer)
        {
            float peak = 0f;
            foreach (float sample in buffer)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            return peak;
        }

        public static void FadeEdges(float[] buffer, float fadeInSeconds, float fadeOutSeconds)
        {
            int fadeIn = Mathf.Min(buffer.Length, ToSamples(fadeInSeconds));
            int fadeOut = Mathf.Min(buffer.Length, ToSamples(fadeOutSeconds));
            for (int i = 0; i < fadeIn; i++)
            {
                buffer[i] *= RaisedCosine(i / (float)fadeIn);
            }

            for (int i = 0; i < fadeOut; i++)
            {
                buffer[buffer.Length - 1 - i] *= RaisedCosine(i / (float)fadeOut);
            }
        }

        public static float[] CrossfadeLoop(float[] buffer, float crossfadeSeconds)
        {
            int crossfade = Mathf.Clamp(ToSamples(crossfadeSeconds), 1, buffer.Length / 2);
            int loopLength = buffer.Length - crossfade;
            var result = new float[loopLength];
            Array.Copy(buffer, result, loopLength);
            for (int i = 0; i < crossfade; i++)
            {
                float t = (i + 0.5f) / crossfade * Mathf.PI * 0.5f;
                result[i] = buffer[i] * Mathf.Sin(t) + buffer[loopLength + i] * Mathf.Cos(t);
            }

            return result;
        }

        public static float[] WrapTail(float[] buffer, int loopLength)
        {
            var result = new float[loopLength];
            Array.Copy(buffer, result, Mathf.Min(loopLength, buffer.Length));
            for (int i = loopLength; i < buffer.Length; i++)
            {
                result[(i - loopLength) % loopLength] += buffer[i];
            }

            return result;
        }

        public static void Mix(float[] destination, float[] source, int offset, float gain)
        {
            for (int i = 0; i < source.Length; i++)
            {
                int target = offset + i;
                if (target >= 0 && target < destination.Length)
                {
                    destination[target] += source[i] * gain;
                }
            }
        }

        public static void Scale(float[] buffer, float gain)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= gain;
            }
        }

        public static void LowPass(float[] buffer, float cutoff)
        {
            Filter(buffer, TidyBiquad.LowPass(cutoff));
        }

        public static void HighPass(float[] buffer, float cutoff)
        {
            Filter(buffer, TidyBiquad.HighPass(cutoff));
        }

        public static void RemoveDc(float[] buffer)
        {
            HighPass(buffer, DcCutoff);
        }

        public static void Filter(float[] buffer, TidyBiquad filter)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = filter.Process(buffer[i]);
            }
        }

        public static float Percussive(float time, float attack, float decay)
        {
            if (time < attack)
            {
                return time / attack;
            }

            return decay > 0f ? Mathf.Exp(-(time - attack) / decay) : 0f;
        }

        private static float RaisedCosine(float t)
        {
            return 0.5f - 0.5f * Mathf.Cos(Mathf.Clamp01(t) * Mathf.PI);
        }

        private static float[] CreateSineTable()
        {
            var table = new float[SineTableSize + 1];
            for (int i = 0; i <= SineTableSize; i++)
            {
                table[i] = Mathf.Sin(i * TidyMath.TwoPi / SineTableSize);
            }

            return table;
        }
    }
}
