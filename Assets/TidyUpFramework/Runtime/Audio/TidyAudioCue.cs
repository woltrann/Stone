using System;
using UnityEngine;

namespace TidyUpFramework.Audio
{
    public enum TidyAudioChannel
    {
        Sfx,
        Music,
        UI,
        Ambience
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Audio Cue", fileName = "Cue_New", order = 50)]
    public sealed class TidyAudioCue : ScriptableObject
    {
        [SerializeField, Tooltip("A random clip is chosen each time the cue plays.")]
        AudioClip[] clips = Array.Empty<AudioClip>();

        [SerializeField]
        TidyAudioChannel channel = TidyAudioChannel.Sfx;

        [SerializeField, Range(0f, 1f)]
        float volume = 0.8f;

        [SerializeField, Tooltip("Random pitch range.")]
        Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        [SerializeField, Range(0f, 1f), Tooltip("0 is 2D, 1 is fully positional.")]
        float spatialBlend;

        [SerializeField]
        bool loop;

        [SerializeField, Min(0f), Tooltip("Minimum seconds between two plays of this cue. Prevents stacking when many objects land at once.")]
        float cooldownSeconds = 0.03f;

        [NonSerialized] float lastPlayTime = -999f;
        [NonSerialized] int lastClipIndex = -1;

        public TidyAudioChannel Channel => channel;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public bool Loop => loop;
        public bool HasClips => clips != null && clips.Length > 0;

        public float RandomPitch => UnityEngine.Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));

        public void Configure(AudioClip[] newClips, TidyAudioChannel newChannel, float newVolume, Vector2 newPitchRange, float newSpatialBlend, bool shouldLoop)
        {
            clips = newClips ?? Array.Empty<AudioClip>();
            channel = newChannel;
            volume = newVolume;
            pitchRange = newPitchRange;
            spatialBlend = newSpatialBlend;
            loop = shouldLoop;
        }

        public bool TryConsumeCooldown()
        {
            var now = Time.unscaledTime;
            if (now - lastPlayTime < cooldownSeconds)
            {
                return false;
            }

            lastPlayTime = now;
            return true;
        }

        public AudioClip PickClip()
        {
            if (!HasClips)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                return clips[0];
            }

            var index = UnityEngine.Random.Range(0, clips.Length - 1);
            if (index >= lastClipIndex)
            {
                index++;
            }

            lastClipIndex = index;
            return clips[index];
        }
    }
}
