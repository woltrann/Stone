using System;
using System.Collections.Generic;
using TidyUpFramework.Audio;
using TidyUpFramework.Core;
using TidyUpFramework.Hints;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Save;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TidyUpFramework.Settings
{
    [AddComponentMenu("")]
    public sealed class TidySettingsService : MonoBehaviour
    {
        static readonly float[] ShadowDistances = { 0f, 12f, 25f, 45f };
        static readonly int[] ShadowCascades = { 1, 1, 2, 4 };

        TidySaveService saveService;
        readonly List<Resolution> resolutions = new List<Resolution>();

        public event Action<TidySettingsData> Changed;

        public TidySettingsData Current { get; private set; } = new TidySettingsData();
        public IReadOnlyList<Resolution> Resolutions => resolutions;

        public void Initialize(TidySaveService save)
        {
            saveService = save;
            Current = save.Settings.Clone();
            CollectResolutions();
            Apply(Current, false);
        }

        public void Apply(TidySettingsData settings, bool persist)
        {
            Current = settings.Clone();
            ApplyGraphics();
            ApplyAudio();
            ApplySceneComponents();
            if (persist)
            {
                saveService?.SaveSettings(Current);
            }

            Changed?.Invoke(Current);
        }

        public void ApplyAndSave(Action<TidySettingsData> modify)
        {
            var next = Current.Clone();
            modify?.Invoke(next);
            Apply(next, true);
        }

        public int FindResolutionIndex(int width, int height)
        {
            for (var index = 0; index < resolutions.Count; index++)
            {
                if (resolutions[index].width == width && resolutions[index].height == height)
                {
                    return index;
                }
            }

            return resolutions.Count - 1;
        }

        void Awake()
        {
            TidyServices.Register(this);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            TidyServices.Unregister(this);
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneComponents();
        }

        void CollectResolutions()
        {
            resolutions.Clear();
            var seen = new HashSet<long>();
            foreach (var resolution in Screen.resolutions)
            {
                var key = ((long)resolution.width << 20) | (uint)resolution.height;
                if (seen.Add(key))
                {
                    resolutions.Add(resolution);
                }
            }
        }

        void ApplyGraphics()
        {
            if (Current.QualityLevel >= 0 && Current.QualityLevel < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != Current.QualityLevel)
            {
                QualitySettings.SetQualityLevel(Current.QualityLevel, true);
            }

            QualitySettings.vSyncCount = Current.VSync ? 1 : 0;
            Application.targetFrameRate = Current.VSync ? -1 : Current.TargetFrameRate;
            QualitySettings.globalTextureMipmapLimit = Mathf.Clamp(Current.TextureQuality, 0, 3);
            ApplyRenderPipeline();
            if (!Application.isMobilePlatform)
            {
                ApplyDisplayMode();
            }
        }

        void ApplyRenderPipeline()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset))
            {
                return;
            }

            var shadowIndex = Mathf.Clamp(Current.ShadowQuality, 0, ShadowDistances.Length - 1);
            urpAsset.shadowDistance = ShadowDistances[shadowIndex];
            urpAsset.shadowCascadeCount = ShadowCascades[shadowIndex];
            urpAsset.renderScale = Mathf.Clamp(Current.RenderScale, 0.5f, 1.5f);
        }

        void ApplyDisplayMode()
        {
            var mode = (FullScreenMode)Mathf.Clamp(Current.FullScreenMode, 0, 3);
            if (Current.ResolutionWidth > 0 && Current.ResolutionHeight > 0)
            {
                if (Screen.width != Current.ResolutionWidth || Screen.height != Current.ResolutionHeight || Screen.fullScreenMode != mode)
                {
                    Screen.SetResolution(Current.ResolutionWidth, Current.ResolutionHeight, mode);
                }
            }
            else if (Screen.fullScreenMode != mode)
            {
                Screen.fullScreenMode = mode;
            }
        }

        void ApplyAudio()
        {
            if (!TidyServices.TryGet<ITidyAudioService>(out var audioService))
            {
                return;
            }

            audioService.SetMasterVolume(Current.MasterVolume);
            audioService.SetChannelVolume(TidyAudioChannel.Music, Current.MusicVolume);
            audioService.SetChannelVolume(TidyAudioChannel.Sfx, Current.SfxVolume);
            audioService.SetChannelVolume(TidyAudioChannel.UI, Current.UiVolume);
            audioService.SetChannelVolume(TidyAudioChannel.Ambience, Current.AmbienceVolume);
        }

        void ApplySceneComponents()
        {
            if (TidyServices.TryGet<TidyInputReader>(out var reader))
            {
                reader.LookSensitivity = Current.LookSensitivity;
                reader.InvertY = Current.InvertY;
                reader.ForceScheme(Current.HasPreferredScheme ? Current.Scheme : (TidyInputScheme?)null);
            }

            if (TidyServices.TryGet<Player.TidyFirstPersonController>(out var controller))
            {
                controller.HeadBobEnabled = Current.HeadBob;
            }

            if (TidyServices.TryGet<TidyHintSystem>(out var hints))
            {
                hints.SetGloballyEnabled(Current.HintsEnabled);
            }

            if (TidyServices.TryGet<TidyInteractor>(out var interactor))
            {
                interactor.AimAssistMultiplier = Current.InteractionSensitivity;
                var preview = interactor.GetComponent<TidyPlacementPreview>();
                if (preview != null)
                {
                    preview.enabled = Current.ShowPlacementPreview;
                }
            }
        }
    }
}
