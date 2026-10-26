using System.Collections.Generic;
using TidyUpFramework.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace TidyUpFramework.Audio
{
    [AddComponentMenu("")]
    public sealed class TidyAudioService : MonoBehaviour, ITidyAudioService
    {
        const int EffectSourceCount = 16;
        const float MinimumDecibels = -80f;

        readonly List<AudioSource> effectSources = new List<AudioSource>(EffectSourceCount);
        readonly float[] channelVolumes = { 1f, 0.7f, 0.8f, 0.6f };
        AudioSource[] musicSources;
        AudioSource ambienceSource;
        int activeMusicIndex;
        int nextEffectIndex;
        float masterVolume = 1f;
        AudioMixer mixer;
        AudioMixerGroup[] groups = new AudioMixerGroup[4];
        string[] exposedParameters = new string[4];
        string masterParameter;
        TidyAudioCue currentMusicCue;
        TidyAudioCue currentAmbienceCue;
        int musicFadeHandle;
        int ambienceFadeHandle;

        public float MasterVolume => masterVolume;

        public void ConfigureMixer(AudioMixer audioMixer, AudioMixerGroup sfx, AudioMixerGroup music, AudioMixerGroup ui, AudioMixerGroup ambience, string masterParam, string sfxParam, string musicParam, string uiParam, string ambienceParam)
        {
            mixer = audioMixer;
            groups = new[] { sfx, music, ui, ambience };
            masterParameter = masterParam;
            exposedParameters = new[] { sfxParam, musicParam, uiParam, ambienceParam };
            foreach (var source in effectSources)
            {
                source.outputAudioMixerGroup = groups[(int)TidyAudioChannel.Sfx];
            }

            if (musicSources != null)
            {
                foreach (var source in musicSources)
                {
                    source.outputAudioMixerGroup = groups[(int)TidyAudioChannel.Music];
                }
            }

            if (ambienceSource != null)
            {
                ambienceSource.outputAudioMixerGroup = groups[(int)TidyAudioChannel.Ambience];
            }

            ApplyVolumes();
        }

        public void Play(TidyAudioCue cue, float volumeScale = 1f, float pitchOffset = 0f)
        {
            PlayInternal(cue, Vector3.zero, false, volumeScale, pitchOffset);
        }

        public void PlayAt(TidyAudioCue cue, Vector3 position, float volumeScale = 1f, float pitchOffset = 0f)
        {
            PlayInternal(cue, position, true, volumeScale, pitchOffset);
        }

        public void PlayMusic(TidyAudioCue cue, float fadeSeconds = 1.5f)
        {
            if (cue == currentMusicCue)
            {
                return;
            }

            currentMusicCue = cue;
            var previous = musicSources[activeMusicIndex];
            activeMusicIndex = 1 - activeMusicIndex;
            var next = musicSources[activeMusicIndex];
            next.clip = cue != null ? cue.PickClip() : null;
            next.loop = true;
            next.volume = 0f;
            var targetVolume = cue != null ? cue.Volume : 0f;
            if (next.clip != null)
            {
                next.Play();
            }

            TidyTweenRunner.Kill(musicFadeHandle);
            var previousStart = previous.volume;
            musicFadeHandle = TidyTweenRunner.Value(blend =>
            {
                previous.volume = Mathf.Lerp(previousStart, 0f, blend);
                next.volume = Mathf.Lerp(0f, targetVolume, blend) * ChannelGain(TidyAudioChannel.Music);
            }, fadeSeconds, TidyEaseType.InOutSine, () => previous.Stop(), true);
        }

        public void PlayAmbience(TidyAudioCue cue, float fadeSeconds = 2f)
        {
            if (cue == currentAmbienceCue)
            {
                return;
            }

            currentAmbienceCue = cue;
            TidyTweenRunner.Kill(ambienceFadeHandle);
            if (cue == null || !cue.HasClips)
            {
                var start = ambienceSource.volume;
                ambienceFadeHandle = TidyTweenRunner.Value(blend => ambienceSource.volume = Mathf.Lerp(start, 0f, blend), fadeSeconds, TidyEaseType.Linear, ambienceSource.Stop, true);
                return;
            }

            ambienceSource.clip = cue.PickClip();
            ambienceSource.loop = true;
            ambienceSource.volume = 0f;
            ambienceSource.Play();
            ambienceFadeHandle = TidyTweenRunner.Value(blend => ambienceSource.volume = Mathf.Lerp(0f, cue.Volume, blend) * ChannelGain(TidyAudioChannel.Ambience), fadeSeconds, TidyEaseType.Linear, null, true);
        }

        public void StopMusic(float fadeSeconds = 1f)
        {
            if (musicSources == null)
            {
                return;
            }

            currentMusicCue = null;
            var source = musicSources[activeMusicIndex];
            var start = source.volume;
            TidyTweenRunner.Kill(musicFadeHandle);
            musicFadeHandle = TidyTweenRunner.Value(blend => source.volume = Mathf.Lerp(start, 0f, blend), fadeSeconds, TidyEaseType.Linear, source.Stop, true);
        }

        public void SetChannelVolume(TidyAudioChannel channel, float volume)
        {
            channelVolumes[(int)channel] = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public float GetChannelVolume(TidyAudioChannel channel)
        {
            return channelVolumes[(int)channel];
        }

        void Awake()
        {
            for (var index = 0; index < EffectSourceCount; index++)
            {
                effectSources.Add(CreateSource("Sfx " + index));
            }

            musicSources = new[] { CreateSource("Music A"), CreateSource("Music B") };
            ambienceSource = CreateSource("Ambience");
            TidyServices.Register<ITidyAudioService>(this);
        }

        void OnDestroy()
        {
            TidyServices.Unregister<ITidyAudioService>(this);
        }

        AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 25f;
            source.dopplerLevel = 0f;
            return source;
        }

        void PlayInternal(TidyAudioCue cue, Vector3 position, bool positional, float volumeScale, float pitchOffset)
        {
            if (cue == null || !cue.HasClips || !cue.TryConsumeCooldown())
            {
                return;
            }

            var source = NextEffectSource();
            source.transform.position = position;
            source.clip = cue.PickClip();
            source.spatialBlend = positional ? cue.SpatialBlend : 0f;
            source.pitch = cue.RandomPitch + pitchOffset;
            source.loop = false;
            source.outputAudioMixerGroup = groups[(int)cue.Channel];
            source.volume = cue.Volume * volumeScale * ChannelGain(cue.Channel);
            source.Play();
        }

        AudioSource NextEffectSource()
        {
            for (var attempt = 0; attempt < effectSources.Count; attempt++)
            {
                var candidate = effectSources[(nextEffectIndex + attempt) % effectSources.Count];
                if (!candidate.isPlaying)
                {
                    nextEffectIndex = (nextEffectIndex + attempt + 1) % effectSources.Count;
                    return candidate;
                }
            }

            var oldest = effectSources[nextEffectIndex];
            nextEffectIndex = (nextEffectIndex + 1) % effectSources.Count;
            return oldest;
        }

        float ChannelGain(TidyAudioChannel channel)
        {
            if (mixer != null && !string.IsNullOrEmpty(exposedParameters[(int)channel]))
            {
                return 1f;
            }

            return channelVolumes[(int)channel] * (mixer != null && !string.IsNullOrEmpty(masterParameter) ? 1f : masterVolume);
        }

        void ApplyVolumes()
        {
            if (mixer != null)
            {
                SetMixerVolume(masterParameter, masterVolume);
                for (var index = 0; index < exposedParameters.Length; index++)
                {
                    SetMixerVolume(exposedParameters[index], channelVolumes[index]);
                }
            }

            if (musicSources != null && currentMusicCue != null)
            {
                musicSources[activeMusicIndex].volume = currentMusicCue.Volume * ChannelGain(TidyAudioChannel.Music);
            }

            if (ambienceSource != null && currentAmbienceCue != null)
            {
                ambienceSource.volume = currentAmbienceCue.Volume * ChannelGain(TidyAudioChannel.Ambience);
            }
        }

        void SetMixerVolume(string parameter, float linear)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                return;
            }

            var decibels = linear <= 0.0001f ? MinimumDecibels : Mathf.Log10(linear) * 20f;
            mixer.SetFloat(parameter, decibels);
        }
    }
}
