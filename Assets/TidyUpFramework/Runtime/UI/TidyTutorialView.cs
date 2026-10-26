using TidyUpFramework.Core;
using TidyUpFramework.Input;
using TidyUpFramework.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Tutorial View")]
    public sealed class TidyTutorialView : MonoBehaviour
    {
        [SerializeField] TidyTutorialRunner runner;
        [SerializeField] TidyUIScreen screen;
        [SerializeField] TMP_Text messageLabel;
        [SerializeField] TMP_Text promptLabel;
        [SerializeField] TMP_Text stepLabel;
        [SerializeField] Button continueButton;
        [SerializeField] Button skipButton;

        [Header("Shortcuts")]
        [SerializeField, Tooltip("Keyboard key that skips the whole tutorial.")] Key skipKey = Key.Backspace;
        [SerializeField, Tooltip("Gamepad button that skips the whole tutorial.")] GamepadButton skipGamepadButton = GamepadButton.Select;
        [SerializeField, Tooltip("Keyboard key that confirms steps waiting for Continue.")] Key continueKey = Key.Enter;
        [SerializeField, Tooltip("Label added to the skip button. {key} is replaced with the shortcut for the active scheme.")] string skipShortcutTemplate = "  <size=75%><color=#C9C2B8>[{key}]</color></size>";

        ITidyPlayerInput input;
        TidyTutorialStep currentStep;
        TMP_Text skipLabel;
        string skipBaseText;
        TMP_Text continueLabel;
        string continueBaseText;

        void Start()
        {
            if (runner == null)
            {
                runner = FindAnyObjectByType<TidyTutorialRunner>();
            }

            if (runner == null)
            {
                return;
            }

            runner.StepStarted += HandleStepStarted;
            runner.TutorialFinished += HandleFinished;
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(runner.Continue);
                continueLabel = continueButton.GetComponentInChildren<TMP_Text>(true);
                continueBaseText = continueLabel != null ? continueLabel.text : null;
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(runner.Skip);
                skipLabel = skipButton.GetComponentInChildren<TMP_Text>(true);
                skipBaseText = skipLabel != null ? skipLabel.text : null;
            }

            if (TidyServices.TryGet(out input))
            {
                input.SchemeChanged += HandleSchemeChanged;
            }
        }

        void Update()
        {
            if (runner == null || !runner.IsRunning || currentStep == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if ((keyboard != null && skipKey != Key.None && keyboard[skipKey].wasPressedThisFrame) || (gamepad != null && gamepad[skipGamepadButton].wasPressedThisFrame))
            {
                runner.Skip();
                return;
            }

            var waitsForContinue = currentStep.Completion == TidyTutorialCompletion.PressContinue;
            if (waitsForContinue && keyboard != null && ((continueKey != Key.None && keyboard[continueKey].wasPressedThisFrame) || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                runner.Continue();
            }
        }

        void OnDestroy()
        {
            if (runner != null)
            {
                runner.StepStarted -= HandleStepStarted;
                runner.TutorialFinished -= HandleFinished;
            }

            if (input != null)
            {
                input.SchemeChanged -= HandleSchemeChanged;
            }
        }

        void HandleStepStarted(TidyTutorialStep step, int index, int count)
        {
            currentStep = step;
            Render();
            if (stepLabel != null)
            {
                stepLabel.text = (index + 1) + " / " + count;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(step.Completion == TidyTutorialCompletion.PressContinue);
            }

            screen?.Show();
            TidyTweenRunner.Punch(transform, Vector3.one, 0.05f, 0.3f);
        }

        void HandleSchemeChanged(TidyInputScheme scheme)
        {
            Render();
        }

        void RenderSkipLabel()
        {
            if (skipLabel == null || skipBaseText == null)
            {
                return;
            }

            var scheme = TidyInputPrompts.CurrentScheme;
            var key = scheme == TidyInputScheme.Gamepad ? "View" : scheme == TidyInputScheme.Touch ? string.Empty : KeyName(skipKey);
            skipLabel.text = string.IsNullOrEmpty(key) ? skipBaseText : skipBaseText + skipShortcutTemplate.Replace("{key}", key);
            FitButton(skipButton, skipLabel);
            if (continueLabel != null && continueBaseText != null)
            {
                var continueKeyName = scheme == TidyInputScheme.KeyboardMouse ? KeyName(continueKey) : string.Empty;
                continueLabel.text = string.IsNullOrEmpty(continueKeyName) ? continueBaseText : continueBaseText + skipShortcutTemplate.Replace("{key}", continueKeyName);
                FitButton(continueButton, continueLabel);
            }
        }

        static void FitButton(Button button, TMP_Text label)
        {
            if (button == null || label == null)
            {
                return;
            }

            var rect = (RectTransform)button.transform;
            var needed = label.GetPreferredValues(label.text).x + 44f;
            if (needed <= rect.rect.width)
            {
                return;
            }

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, needed);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = Mathf.Max(layout.preferredWidth, needed);
            }
        }

        static string KeyName(Key key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || key == Key.None)
            {
                return string.Empty;
            }

            var control = keyboard[key];
            return string.IsNullOrEmpty(control.displayName) ? key.ToString() : control.displayName;
        }

        void Render()
        {
            if (currentStep == null)
            {
                return;
            }

            RenderSkipLabel();

            if (messageLabel != null)
            {
                messageLabel.text = TidyInputPrompts.Format(currentStep.Message);
            }

            if (promptLabel != null)
            {
                promptLabel.gameObject.SetActive(currentStep.ShowPrompt && currentStep.Message.IndexOf('{') < 0);
                promptLabel.text = TidyInputPrompts.Key(currentStep.PromptAction);
            }
        }

        void HandleFinished()
        {
            currentStep = null;
            screen?.Hide();
        }
    }
}
