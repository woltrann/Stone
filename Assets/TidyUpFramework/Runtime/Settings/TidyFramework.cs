using System;
using TidyUpFramework.Audio;
using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using TidyUpFramework.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TidyUpFramework.Settings
{
    public static class TidyFramework
    {
        static TidyFrameworkSettings settings;

        public static bool IsInitialized { get; private set; }
        public static TidyFrameworkSettings Settings => settings;
        public static TidySaveService Save { get; private set; }
        public static TidySettingsService SettingsService { get; private set; }
        public static TidyAudioService Audio { get; private set; }
        public static TidySceneLoader SceneLoader { get; private set; }
        public static TidyLevelCatalog Catalog => settings != null ? settings.LevelCatalog : null;
        // Persistent services reset before the old scene can be discarded. A failed save cancels restart.
        public static event Func<bool> LevelRestarting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;
            settings = Resources.Load<TidyFrameworkSettings>(TidyFrameworkSettings.ResourcesPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TidyFrameworkSettings>();
                settings.hideFlags = HideFlags.DontSave;
            }

            Input.TidyInputPrompts.Definition = settings.InputPrompts;
            Save = new TidySaveService(CreateStorage(settings), settings.DefaultSettings);
            TidyServices.Register<ITidyProgressService>(Save);
            TidyServices.Register(Save);

            Audio = TidyRuntimeHost.GetOrAddComponent<TidyAudioService>();
            if (settings.AudioMixer != null)
            {
                Audio.ConfigureMixer(settings.AudioMixer, settings.SfxGroup, settings.MusicGroup, settings.UiGroup, settings.AmbienceGroup,
                    settings.MasterVolumeParameter, settings.SfxVolumeParameter, settings.MusicVolumeParameter, settings.UiVolumeParameter, settings.AmbienceVolumeParameter);
            }

            SceneLoader = TidyRuntimeHost.GetOrAddComponent<TidySceneLoader>();
            SceneLoader.Configure(settings.FadeSeconds, settings.FadeColor);

            SettingsService = TidyRuntimeHost.GetOrAddComponent<TidySettingsService>();
            SettingsService.Initialize(Save);
        }

        public static void LoadMainMenu()
        {
            var menuScene = Catalog != null ? Catalog.MainMenuSceneName : string.Empty;
            if (!string.IsNullOrEmpty(menuScene))
            {
                SceneLoader.LoadScene(menuScene);
            }
        }

        public static void LoadLevel(TidyLevelDefinition level)
        {
            if (level == null)
            {
                return;
            }

            Save.RecordAttempt(level.LevelId);
            SceneLoader.LoadLevel(level);
        }

        public static void RestartLevel()
        {
            if (LevelRestarting != null)
            {
                foreach (Func<bool> prepare in LevelRestarting.GetInvocationList())
                {
                    try { if (!prepare()) return; }
                    catch (Exception exception) { Debug.LogException(exception); return; }
                }
            }

            if (TidyServices.TryGet<TidyLevelManager>(out var levelManager))
            {
                levelManager.DiscardSavedProgress();
            }

            SceneLoader.ReloadActiveScene();
        }

        public static TidyLevelDefinition FindLevelForActiveScene()
        {
            return Catalog != null ? Catalog.FindByScene(SceneManager.GetActiveScene().name) : null;
        }

        static ITidySaveStorage CreateStorage(TidyFrameworkSettings frameworkSettings)
        {
            if (frameworkSettings.SaveStorage == TidySaveStorageKind.PlayerPrefs)
            {
                return new TidyPlayerPrefsStorage(frameworkSettings.SaveFolderName + ".");
            }

            return new TidyJsonFileStorage(frameworkSettings.SaveFolderName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnDomainReload()
        {
            IsInitialized = false;
            LevelRestarting = null;
            settings = null;
            Save = null;
            SettingsService = null;
            Audio = null;
            SceneLoader = null;
        }
    }
}
