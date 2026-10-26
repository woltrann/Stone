using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal static class TidyMusicComposer
    {
        private const float TailSeconds = 4f;
        private const int BeatsPerBar = 4;
        private const int StepsPerBar = 8;
        private const int PhraseVariants = 2;
        private const int PhraseSeedStep = 7919;
        private const int PercussionSeedOffset = 4513;
        private const int MelodyOctaves = 2;
        private const int MelodyStartDegree = 4;
        private const int PadOctaveCeiling = 60;
        private const int BassCeiling = 50;
        private const int Octave = 12;
        private const float PeakDecibels = -3f;
        private const float RoomSize = 0.82f;
        private const float RoomDamping = 0.45f;

        private const float PadLevel = 0.11f;
        private const float PadAttack = 0.6f;
        private const float PadRelease = 1.6f;
        private const int PadHarmonics = 5;
        private const float BassLevel = 0.28f;
        private const int BassHarmonics = 3;
        private const float LeadLevel = 0.32f;
        private const float KeysLevel = 0.09f;
        private const float KeysStrum = 0.012f;
        private const float KickLevel = 0.35f;
        private const float HatLevel = 0.05f;

        public static float[] Render(TidyMusicStyle style, float seconds, int seed)
        {
            float beat = 60f / style.Tempo;
            float bar = beat * BeatsPerBar;
            int chordCount = Mathf.Max(1, style.Progression.Length);
            int cycles = Mathf.Max(1, Mathf.RoundToInt(seconds / (bar * chordCount)));
            int bars = cycles * chordCount;
            int loopSamples = TidyDsp.ToSamples(bars * bar);
            var buffer = new float[loopSamples + TidyDsp.ToSamples(TailSeconds)];
            var percussionRandom = new TidyRandom(seed + PercussionSeedOffset);
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                var melodyRandom = new TidyRandom(seed + (cycle % PhraseVariants) * PhraseSeedStep);
                int degree = MelodyStartDegree;
                for (int chordIndex = 0; chordIndex < chordCount; chordIndex++)
                {
                    float barStart = (cycle * chordCount + chordIndex) * bar;
                    TidyChord chord = style.Progression[chordIndex];
                    RenderHarmony(buffer, style, chord, barStart, beat, bar);
                    degree = RenderMelody(buffer, style, chord, barStart, beat, degree, ref melodyRandom);
                    if (style.UseDrums)
                    {
                        RenderDrums(buffer, style, barStart, beat, ref percussionRandom);
                    }
                }
            }

            TidyDsp.LowPass(buffer, style.MasterLowPass);
            TidyReverb.Apply(buffer, style.ReverbWet, RoomSize, RoomDamping);
            float[] loop = TidyDsp.WrapTail(buffer, loopSamples);
            TidyDsp.RemoveDc(loop);
            TidyDsp.Normalize(loop, PeakDecibels);
            return loop;
        }

        private static void RenderHarmony(float[] buffer, TidyMusicStyle style, TidyChord chord, float barStart, float beat, float bar)
        {
            int padRoot = style.RootMidi >= PadOctaveCeiling ? style.RootMidi - Octave : style.RootMidi;
            foreach (int tone in chord.Tones)
            {
                float frequency = TidyDsp.MidiToFrequency(padRoot + tone);
                TidyVoices.Pad(buffer, barStart, frequency, bar, PadRelease, PadAttack, PadLevel, style.PadBrightness, PadHarmonics);
            }

            int bass = style.RootMidi + chord.Bass - Octave;
            while (bass > BassCeiling)
            {
                bass -= Octave;
            }

            float bassFrequency = TidyDsp.MidiToFrequency(bass);
            TidyVoices.Pad(buffer, barStart, bassFrequency, beat * 2f - 0.05f, 0.3f, 0.02f, BassLevel, 0.3f, BassHarmonics);
            TidyVoices.Pad(buffer, barStart + beat * 2f, bassFrequency, beat * 1.5f, 0.3f, 0.02f, BassLevel * 0.75f, 0.3f, BassHarmonics);
            if (!style.UseKeysComping)
            {
                return;
            }

            for (int k = 0; k < chord.Tones.Length; k++)
            {
                float frequency = TidyDsp.MidiToFrequency(padRoot + Octave + chord.Tones[k]);
                TidyVoices.Fm(buffer, barStart + beat * 1.5f + k * KeysStrum, frequency, bar * 0.6f, KeysLevel, TidyFmPatch.ElectricPiano);
            }
        }

        private static int RenderMelody(float[] buffer, TidyMusicStyle style, TidyChord chord, float barStart, float beat, int degree, ref TidyRandom random)
        {
            int[] scale = style.MelodyScale;
            int maxDegree = scale.Length * MelodyOctaves;
            for (int step = 0; step < StepsPerBar; step++)
            {
                bool strong = step % 2 == 0;
                float probability = style.MelodyDensity * (step == 0 ? 1.4f : (strong ? 1f : 0.6f));
                if (!random.Chance(probability))
                {
                    continue;
                }

                degree = Mathf.Clamp(degree + random.Range(-2, 3), 0, maxDegree);
                if (strong)
                {
                    degree = NearestChordDegree(scale, chord, degree, maxDegree);
                }

                float swing = strong ? 0f : style.Swing * beat;
                float time = barStart + step * beat * 0.5f + swing;
                float velocity = random.Range(0.65f, 1f) * (strong ? 1f : 0.8f);
                int midi = style.RootMidi + Octave + DegreeToSemitone(scale, degree);
                PlayLead(buffer, style.Lead, time, TidyDsp.MidiToFrequency(midi), velocity * LeadLevel);
            }

            return degree;
        }

        private static void RenderDrums(float[] buffer, TidyMusicStyle style, float barStart, float beat, ref TidyRandom random)
        {
            TidyVoices.SoftKick(buffer, barStart, KickLevel);
            TidyVoices.SoftKick(buffer, barStart + beat * 2.5f, KickLevel * 0.8f);
            for (int step = 0; step < StepsPerBar; step++)
            {
                bool offbeat = step % 2 == 1;
                float time = barStart + step * beat * 0.5f + (offbeat ? style.Swing * beat : 0f);
                TidyVoices.SoftHat(buffer, time, HatLevel * (offbeat ? 0.6f : 1f) * random.Range(0.8f, 1f), ref random);
            }
        }

        private static void PlayLead(float[] buffer, TidyLeadInstrument lead, float time, float frequency, float amplitude)
        {
            switch (lead)
            {
                case TidyLeadInstrument.ElectricPiano:
                    TidyVoices.Fm(buffer, time, frequency, 1.6f, amplitude, TidyFmPatch.ElectricPiano);
                    break;
                case TidyLeadInstrument.MusicBox:
                    TidyVoices.Bell(buffer, time, frequency, 1.8f, amplitude * 0.8f);
                    break;
                default:
                    TidyVoices.Fm(buffer, time, frequency, 1.4f, amplitude, TidyFmPatch.Celesta);
                    TidyVoices.Bell(buffer, time, frequency * 2f, 0.8f, amplitude * 0.12f);
                    break;
            }
        }

        private static int NearestChordDegree(int[] scale, TidyChord chord, int degree, int maxDegree)
        {
            for (int offset = 0; offset <= 2; offset++)
            {
                if (IsChordTone(scale, chord, degree - offset, maxDegree))
                {
                    return degree - offset;
                }

                if (IsChordTone(scale, chord, degree + offset, maxDegree))
                {
                    return degree + offset;
                }
            }

            return degree;
        }

        private static bool IsChordTone(int[] scale, TidyChord chord, int degree, int maxDegree)
        {
            if (degree < 0 || degree > maxDegree)
            {
                return false;
            }

            int pitchClass = TidyMath.Wrap(DegreeToSemitone(scale, degree), Octave);
            foreach (int tone in chord.Tones)
            {
                if (TidyMath.Wrap(tone, Octave) == pitchClass)
                {
                    return true;
                }
            }

            return false;
        }

        private static int DegreeToSemitone(int[] scale, int degree)
        {
            return (degree / scale.Length) * Octave + scale[degree % scale.Length];
        }
    }
}
