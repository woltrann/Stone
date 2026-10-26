using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyAudioSynth
    {
        private const float ChimeRootMidi = 72f;
        private const float SparkleRootMidi = 79f;
        private const int MaxComboTier = 5;
        private const float SparkleStep = 0.045f;
        private const float RoomSize = 0.8f;
        private const float RoomDamping = 0.4f;

        private static readonly int[] PentatonicSteps = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21 };

        public static float[] CorrectChime(float pitchSemitones)
        {
            float[] buffer = TidyDsp.Allocate(1.1f);
            float root = ChimeRootMidi + pitchSemitones;
            TidyVoices.Bell(buffer, 0f, Note(root), 0.8f, 0.7f);
            TidyVoices.Fm(buffer, 0f, Note(root), 0.6f, 0.25f, TidyFmPatch.Celesta);
            TidyVoices.Bell(buffer, 0.07f, Note(root + 7f), 0.9f, 0.8f);
            TidyVoices.Fm(buffer, 0.07f, Note(root + 7f), 0.7f, 0.25f, TidyFmPatch.Celesta);
            TidyVoices.Bell(buffer, 0.07f, Note(root + 19f), 0.6f, 0.12f);
            return FinishWithRoom(buffer, 0.15f);
        }

        public static float[] GentleError()
        {
            float[] buffer = TidyDsp.Allocate(0.55f);
            AddSoftBoop(buffer, 0f, Note(64f));
            AddSoftBoop(buffer, 0.16f, Note(60f));
            TidyDsp.LowPass(buffer, 2500f);
            return Finish(buffer);
        }

        public static float[] ComboSparkle(int tier)
        {
            int level = Mathf.Clamp(tier, 0, MaxComboTier);
            int count = 3 + level;
            float[] buffer = TidyDsp.Allocate(count * SparkleStep + 0.9f);
            var random = new TidyRandom(83 + level);
            float root = SparkleRootMidi + level * 2f;
            for (int k = 0; k < count; k++)
            {
                float midi = root + PentatonicSteps[k % PentatonicSteps.Length];
                TidyVoices.Bell(buffer, k * SparkleStep, Note(midi), 0.55f, 0.8f - k * 0.05f);
            }

            TidyVoices.FilteredNoise(buffer, 0f, count * SparkleStep + 0.3f, 0.08f, 0.05f, 0.2f, TidyBiquad.HighPass(6000f), ref random);
            return FinishWithRoom(buffer, 0.2f);
        }

        public static float[] ObjectiveFanfare()
        {
            float[] buffer = TidyDsp.Allocate(1.9f);
            float[] arpeggio = { 72f, 76f, 79f };
            for (int k = 0; k < arpeggio.Length; k++)
            {
                TidyVoices.Fm(buffer, k * 0.09f, Note(arpeggio[k]), 0.8f, 0.5f, TidyFmPatch.SoftPluck);
            }

            TidyVoices.Bell(buffer, 0.27f, Note(84f), 1.2f, 0.6f);
            foreach (float midi in new[] { 60f, 64f, 67f })
            {
                TidyVoices.Pad(buffer, 0.1f, Note(midi), 0.6f, 0.8f, 0.15f, 0.12f, 0.45f, 5);
            }

            TidyVoices.Pad(buffer, 0.1f, Note(48f), 0.5f, 0.6f, 0.02f, 0.3f, 0.3f, 3);
            return FinishWithRoom(buffer, 0.25f);
        }

        public static float[] LevelCompleteFanfare()
        {
            float[] buffer = TidyDsp.Allocate(3.4f);
            float[] arpeggio = { 60f, 64f, 67f, 72f, 76f, 79f, 84f };
            for (int k = 0; k < arpeggio.Length; k++)
            {
                float start = k * 0.17f;
                TidyVoices.Fm(buffer, start, Note(arpeggio[k]), 1f, 0.45f, TidyFmPatch.SoftPluck);
                TidyVoices.Bell(buffer, start, Note(arpeggio[k] + 12f), 0.9f, 0.18f);
            }

            float[] finalChord = { 84f, 88f, 91f };
            for (int k = 0; k < finalChord.Length; k++)
            {
                TidyVoices.Bell(buffer, 1.25f + k * 0.02f, Note(finalChord[k]), 2f, 0.4f);
            }

            foreach (float midi in new[] { 48f, 55f, 60f, 64f, 67f })
            {
                TidyVoices.Pad(buffer, 1.2f, Note(midi), 1.2f, 1f, 0.3f, 0.1f, 0.5f, 5);
            }

            TidyVoices.Pad(buffer, 1.2f, Note(36f), 1.1f, 0.9f, 0.05f, 0.35f, 0.3f, 3);
            return FinishWithRoom(buffer, 0.3f);
        }

        public static float[] StarPing()
        {
            float[] buffer = TidyDsp.Allocate(1f);
            var random = new TidyRandom(97);
            TidyVoices.Bell(buffer, 0f, Note(91f), 0.9f, 1f);
            TidyVoices.Bell(buffer, 0.01f, Note(98f), 0.6f, 0.3f);
            TidyVoices.FilteredNoise(buffer, 0f, 0.4f, 0.05f, 0.01f, 0.12f, TidyBiquad.HighPass(7000f), ref random);
            return FinishWithRoom(buffer, 0.15f);
        }

        public static float[] UiClick()
        {
            float[] buffer = TidyDsp.Allocate(0.07f);
            TidyVoices.Tone(buffer, 0f, 1400f, 1100f, 0.05f, 0.6f, 0.0005f, 0.008f);
            TidyVoices.Tone(buffer, 0f, 420f, 380f, 0.05f, 0.4f, 0.001f, 0.012f);
            return Finish(buffer);
        }

        public static float[] UiHover()
        {
            float[] buffer = TidyDsp.Allocate(0.09f);
            TidyVoices.Tone(buffer, 0f, 880f, 960f, 0.08f, 1f, 0.006f, 0.025f);
            return Finish(buffer);
        }

        public static float[] UiBack()
        {
            float[] buffer = TidyDsp.Allocate(0.2f);
            TidyVoices.Tone(buffer, 0f, 760f, 700f, 0.08f, 0.8f, 0.002f, 0.025f);
            TidyVoices.Tone(buffer, 0.06f, 560f, 520f, 0.12f, 0.8f, 0.002f, 0.035f);
            return Finish(buffer);
        }

        private static void AddSoftBoop(float[] buffer, float start, float frequency)
        {
            TidyVoices.Partial(buffer, start, frequency, 0.3f, 0.8f, 0.01f, 0.12f);
            TidyVoices.Partial(buffer, start, frequency * 2f, 0.25f, 0.15f, 0.01f, 0.06f);
            TidyVoices.Partial(buffer, start, frequency * 3f, 0.2f, 0.05f, 0.01f, 0.04f);
        }

        private static float[] FinishWithRoom(float[] buffer, float wet)
        {
            TidyReverb.Apply(buffer, wet, RoomSize, RoomDamping);
            TidyDsp.LowPass(buffer, SoftTopCutoff);
            return Finish(buffer);
        }
    }
}
