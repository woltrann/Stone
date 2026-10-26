using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyAudioSynth
    {
        private const float MinimumMusicSeconds = 8f;
        private const int MenuSeed = 7;

        public static float[] CalmMusicLoop(float seconds, int seed, int mood)
        {
            return TidyMusicComposer.Render(TidyMusicStyles.ForMood(mood), Mathf.Max(MinimumMusicSeconds, seconds), seed);
        }

        public static float[] MenuMusicLoop(float seconds)
        {
            return TidyMusicComposer.Render(TidyMusicStyles.Menu(), Mathf.Max(MinimumMusicSeconds, seconds), MenuSeed);
        }
    }
}
