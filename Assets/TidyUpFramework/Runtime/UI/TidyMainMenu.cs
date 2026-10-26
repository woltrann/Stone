using TidyUpFramework.Audio;
using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Main Menu")]
    public sealed class TidyMainMenu : MonoBehaviour
    {
        [SerializeField] TidyScreenStack screenStack;
        [SerializeField] TidyUIScreen mainScreen;
        [SerializeField] TidyUIScreen levelSelectScreen;
        [SerializeField] TidyUIScreen settingsScreen;
        [SerializeField] TidyUIScreen controlsScreen;
        [SerializeField] TidyUIScreen creditsScreen;
        [SerializeField] Button playButton;
        [SerializeField] TMP_Text playButtonLabel;
        [SerializeField] Button levelSelectButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button creditsButton;
        [SerializeField] Button quitButton;
        [SerializeField] string newGameLabel = "Play";
        [SerializeField] string continueLabel = "Continue";

        void Start()
        {
            TidyControlLock.Release(this);
            Time.timeScale = 1f;
            Bind(playButton, Play);
            Bind(levelSelectButton, () => screenStack.Push(levelSelectScreen));
            Bind(settingsButton, () => screenStack.Push(settingsScreen));
            Bind(controlsButton, () => screenStack.Push(controlsScreen));
            Bind(creditsButton, () => screenStack.Push(creditsScreen));
            Bind(quitButton, Quit);
            if (quitButton != null && (Application.platform == RuntimePlatform.WebGLPlayer || Application.isMobilePlatform))
            {
                quitButton.gameObject.SetActive(false);
            }

            if (playButtonLabel != null)
            {
                playButtonLabel.text = HasAnyProgress() ? continueLabel : newGameLabel;
            }

            if (TidyFramework.Settings != null && TidyFramework.Settings.MenuMusic != null && TidyServices.TryGet<ITidyAudioService>(out var audioService))
            {
                audioService.PlayMusic(TidyFramework.Settings.MenuMusic);
                audioService.PlayAmbience(null);
            }

            mainScreen?.Show();
        }

        public void Play()
        {
            var level = FindNextLevel();
            if (level != null)
            {
                TidyFramework.LoadLevel(level);
            }
            else
            {
                screenStack.Push(levelSelectScreen);
            }
        }

        static TidyLevelDefinition FindNextLevel()
        {
            var catalog = TidyFramework.Catalog;
            var save = TidyFramework.Save;
            if (catalog == null)
            {
                return null;
            }

            var savedLevelId = save != null ? save.FindMostRecentSnapshotLevel() : null;
            if (!string.IsNullOrEmpty(savedLevelId))
            {
                var savedLevel = catalog.FindById(savedLevelId);
                if (savedLevel != null && save.IsUnlocked(savedLevel))
                {
                    return savedLevel;
                }
            }

            TidyLevelDefinition firstUnlocked = null;
            foreach (var level in catalog.Levels)
            {
                if (level == null || save == null || !save.IsUnlocked(level))
                {
                    continue;
                }

                if (firstUnlocked == null)
                {
                    firstUnlocked = level;
                }

                if (!save.IsCompleted(level.LevelId))
                {
                    return level;
                }
            }

            return firstUnlocked;
        }

        static bool HasAnyProgress()
        {
            return TidyFramework.Save != null && TidyFramework.Save.Progress.Levels.Count > 0;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }
    }
}
