using UnityEngine;

namespace TidyUpFramework.Audio
{
    public interface ITidyAudioService
    {
        void Play(TidyAudioCue cue, float volumeScale = 1f, float pitchOffset = 0f);
        void PlayAt(TidyAudioCue cue, Vector3 position, float volumeScale = 1f, float pitchOffset = 0f);
        void PlayMusic(TidyAudioCue cue, float fadeSeconds = 1.5f);
        void PlayAmbience(TidyAudioCue cue, float fadeSeconds = 2f);
        void StopMusic(float fadeSeconds = 1f);
        void SetChannelVolume(TidyAudioChannel channel, float volume);
        void SetMasterVolume(float volume);
        float GetChannelVolume(TidyAudioChannel channel);
        float MasterVolume { get; }
    }
}
