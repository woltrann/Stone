using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Pause Menu")]
    public sealed class TidyPauseMenu : MonoBehaviour
    {
        [SerializeField] TidyUIScreen pauseScreen;
        [SerializeField] TidyScreenStack screenStack;
        [SerializeField] TidyUIScreen settingsScreen;
        [SerializeField] TidyUIScreen controlsScreen;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button menuButton;
        [SerializeField, Tooltip("Optional button that saves mid-level progress.")] Button saveButton;
        [SerializeField] string savedLabel = "Progress Saved";
        [SerializeField, Tooltip("Saves progress every time the game is paused.")] bool saveOnPause = true;

        ITidyPlayerInput input;
        TidyLevelManager levelManager;
        TidyInteractor interactor;
        float previousTimeScale = 1f;
        TMP_Text saveButtonLabel;
        string saveButtonText;
        int saveLabelHandle;

        public bool IsPaused { get; private set; }

        public void Pause()
        {
            if (IsPaused || (levelManager != null && !levelManager.IsPlaying))
            {
                return;
            }

            IsPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = false;
            if (saveOnPause && levelManager != null)
            {
                levelManager.SaveProgress();
            }

            if (screenStack != null)
            {
                screenStack.Push(pauseScreen);
            }
            else
            {
                pauseScreen?.Show();
            }
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            if (screenStack != null)
            {
                screenStack.Clear();
            }
            else
            {
                pauseScreen?.Hide();
            }
        }

        void Start()
        {
            TidyServices.TryGet(out levelManager);
            TidyServices.TryGet(out interactor);
            Bind(resumeButton, Resume);
            Bind(restartButton, () =>
            {
                Resume();
                TidyFramework.RestartLevel();
            });
            Bind(saveButton, SaveNow);
            if (saveButton != null)
            {
                saveButtonLabel = saveButton.GetComponentInChildren<TMP_Text>(true);
                saveButtonText = saveButtonLabel != null ? saveButtonLabel.text : null;
                saveButton.gameObject.SetActive(levelManager == null || levelManager.SavesProgress);
            }

            Bind(settingsButton, () => screenStack?.Push(settingsScreen));
            Bind(controlsButton, () => screenStack?.Push(controlsScreen));
            Bind(menuButton, () =>
            {
                Resume();
                TidyFramework.LoadMainMenu();
            });
            if (screenStack != null)
            {
                screenStack.StackEmptied += HandleStackEmptied;
            }
        }

        void OnDestroy()
        {
            if (screenStack != null)
            {
                screenStack.StackEmptied -= HandleStackEmptied;
            }

            if (IsPaused)
            {
                Time.timeScale = 1f;
            }
        }

        public void SaveNow()
        {
            if (levelManager == null || !levelManager.SaveProgress() || saveButtonLabel == null)
            {
                return;
            }

            TidyTweenRunner.Kill(saveLabelHandle);
            saveButtonLabel.text = savedLabel;
            TidyTweenRunner.Punch(saveButton.transform, Vector3.one, 0.06f, 0.25f);
            saveLabelHandle = TidyTweenRunner.Value(_ => { }, 1.4f, TidyEaseType.Linear, () => saveButtonLabel.text = saveButtonText, true);
        }

        void HandleStackEmptied()
        {
            if (IsPaused)
            {
                IsPaused = false;
                Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            }
        }

        void Update()
        {
            if (input == null)
            {
                TidyServices.TryGet(out input);
            }

            if (IsPaused || input == null || !input.PausePressed)
            {
                return;
            }

            if (interactor != null && interactor.IsInspecting)
            {
                return;
            }

            Pause();
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
