using TidyUpFramework.Core;
using UnityEngine;

namespace TidyUpFramework.Audio
{
    public static class TidyAudio
    {
        public static void Play(TidyAudioCue cue, float volumeScale = 1f, float pitchOffset = 0f)
        {
            if (cue != null && TidyServices.TryGet<ITidyAudioService>(out var service))
            {
                service.Play(cue, volumeScale, pitchOffset);
            }
        }

        public static void PlayAt(TidyAudioCue cue, Vector3 position, float volumeScale = 1f, float pitchOffset = 0f)
        {
            if (cue != null && TidyServices.TryGet<ITidyAudioService>(out var service))
            {
                service.PlayAt(cue, position, volumeScale, pitchOffset);
            }
        }
    }
}
