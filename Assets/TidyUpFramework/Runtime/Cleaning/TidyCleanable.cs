using System;
using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Rules;
using UnityEngine;

namespace TidyUpFramework.Cleaning
{
    public enum TidyCleanVisualMode
    {
        FadeAlpha,
        ShrinkScale,
        FadeAndShrink
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Cleaning/Cleanable Surface")]
    public sealed class TidyCleanable : MonoBehaviour
    {
        static readonly List<TidyCleanable> RegisteredCleanables = new List<TidyCleanable>(64);
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Unique id used by objectives that target a specific group of stains.")]
        string cleanableId;

        [SerializeField, Tooltip("Optional group id, for example 'Kitchen'. Objectives can count a single group.")]
        string groupId;

        [SerializeField, Min(0.05f), Tooltip("Seconds of scrubbing with bare hands needed to clean this surface.")]
        float secondsToClean = 1.5f;

        [SerializeField, Tooltip("Cleaning methods that work on this surface, for example Scrubbing for stains and Vacuum for dust.")]
        TidyCleaningMethod acceptedMethods = TidyCleaningMethod.BareHands | TidyCleaningMethod.Scrubbing | TidyCleaningMethod.Vacuum;

        [SerializeField, Tooltip("Held tool required to clean. Empty accepts any tool whose method is allowed above.")]
        TidyMatchFilter requiredTool = new TidyMatchFilter();

        [SerializeField]
        TidyCleanVisualMode visualMode = TidyCleanVisualMode.FadeAndShrink;

        [SerializeField, Tooltip("Renderers faded while cleaning. Defaults to child renderers.")]
        Renderer[] dirtRenderers = Array.Empty<Renderer>();

        [SerializeField, Tooltip("Optional particle system played while scrubbing.")]
        ParticleSystem scrubParticles;

        [SerializeField, Tooltip("Disable the GameObject once clean.")]
        bool disableWhenClean = true;

        MaterialPropertyBlock propertyBlock;
        Color[] originalColors = Array.Empty<Color>();
        Vector3 originalScale;
        float lastScrubTime = -1f;

        public static event Action<TidyCleanable> AnyCleaned;
        public static event Action<TidyCleanable> AnyProgressChanged;
        public static IReadOnlyList<TidyCleanable> All => RegisteredCleanables;

        public event Action<TidyCleanable> Cleaned;

        public string CleanableId => string.IsNullOrEmpty(cleanableId) ? name : cleanableId;
        public string GroupId => groupId;
        public float Dirt { get; private set; } = 1f;
        public bool IsClean => Dirt <= 0f;
        public bool IsBeingScrubbed => Time.time - lastScrubTime < 0.1f;
        public TidyMatchFilter RequiredTool => requiredTool;
        public TidyCleaningMethod AcceptedMethods => acceptedMethods;

        public void Configure(string newId, string newGroupId, float newSecondsToClean)
        {
            cleanableId = newId;
            groupId = newGroupId;
            secondsToClean = Mathf.Max(0.05f, newSecondsToClean);
        }

        public void ConfigureMethods(TidyCleaningMethod methods)
        {
            acceptedMethods = methods;
        }

        public bool AcceptsMethod(TidyCleaningMethod method)
        {
            return (acceptedMethods & method) != 0;
        }

        public bool AcceptsTool(Objects.TidyObject tool)
        {
            var toolComponent = tool != null ? tool.GetComponent<TidyCleaningTool>() : null;
            var method = tool == null ? TidyCleaningMethod.BareHands : toolComponent != null ? toolComponent.Method : TidyCleaningMethod.Scrubbing;
            if (!AcceptsMethod(method))
            {
                return false;
            }

            if (requiredTool.IsEmpty)
            {
                return true;
            }

            return tool != null && requiredTool.Matches(tool);
        }

        public void Scrub(float seconds)
        {
            if (IsClean || seconds <= 0f)
            {
                return;
            }

            lastScrubTime = Time.time;
            if (scrubParticles != null && !scrubParticles.isPlaying)
            {
                scrubParticles.Play();
            }

            Dirt = Mathf.Max(0f, Dirt - seconds / secondsToClean);
            ApplyVisual();
            AnyProgressChanged?.Invoke(this);
            if (IsClean)
            {
                CompleteCleaning();
            }
        }

        public void RestoreDirt(float dirt)
        {
            Dirt = Mathf.Clamp01(dirt);
            ApplyVisual();
            if (IsClean)
            {
                if (scrubParticles != null)
                {
                    scrubParticles.Stop();
                }

                if (disableWhenClean)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        void CompleteCleaning()
        {
            if (scrubParticles != null)
            {
                scrubParticles.Stop();
            }

            Cleaned?.Invoke(this);
            AnyCleaned?.Invoke(this);
            if (disableWhenClean)
            {
                TidyTweenRunner.ScaleTo(transform, Vector3.zero, 0.2f, TidyEaseType.InQuad, () => gameObject.SetActive(false));
            }
        }

        void ApplyVisual()
        {
            var fade = visualMode != TidyCleanVisualMode.ShrinkScale;
            var shrink = visualMode != TidyCleanVisualMode.FadeAlpha;
            if (shrink)
            {
                transform.localScale = originalScale * Mathf.Lerp(0.35f, 1f, Dirt);
            }

            if (!fade)
            {
                return;
            }

            for (var index = 0; index < dirtRenderers.Length; index++)
            {
                var renderer = dirtRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                var color = originalColors[index];
                color.a *= Dirt;
                propertyBlock.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            originalScale = transform.localScale;
            if (dirtRenderers == null || dirtRenderers.Length == 0)
            {
                dirtRenderers = GetComponentsInChildren<Renderer>(true);
            }

            originalColors = new Color[dirtRenderers.Length];
            for (var index = 0; index < dirtRenderers.Length; index++)
            {
                var material = dirtRenderers[index] != null ? dirtRenderers[index].sharedMaterial : null;
                originalColors[index] = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            }

            RegisteredCleanables.Add(this);
        }

        void OnDestroy()
        {
            RegisteredCleanables.Remove(this);
        }

        void Update()
        {
            if (scrubParticles != null && scrubParticles.isPlaying && !IsBeingScrubbed)
            {
                scrubParticles.Stop();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnDomainReload()
        {
            RegisteredCleanables.Clear();
            AnyCleaned = null;
            AnyProgressChanged = null;
        }
    }
}
