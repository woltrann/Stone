using TidyUpFramework.Core;
using TidyUpFramework.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Objective Item View")]
    public sealed class TidyObjectiveItemView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text counterLabel;
        [SerializeField] Image progressFill;
        [SerializeField] GameObject checkmark;
        [SerializeField] GameObject optionalBadge;
        [SerializeField] Image icon;
        [SerializeField] Color activeColor = Color.white;
        [SerializeField] Color completeColor = new Color(0.6f, 0.95f, 0.65f);
        [SerializeField] Color failedColor = new Color(1f, 0.5f, 0.45f);

        const string OptionalSuffix = "  <size=70%><i><color=#968880>optional</color></i></size>";

        TidyObjectiveTracker tracker;
        bool wasComplete;

        public void Bind(TidyObjectiveTracker objectiveTracker)
        {
            tracker = objectiveTracker;
            if (counterLabel != null && !counterLabel.enableAutoSizing)
            {
                counterLabel.textWrappingMode = TextWrappingModes.NoWrap;
                counterLabel.fontSizeMax = counterLabel.fontSize;
                counterLabel.fontSizeMin = Mathf.Max(10f, counterLabel.fontSize * 0.6f);
                counterLabel.enableAutoSizing = true;
            }

            if (icon != null)
            {
                icon.sprite = tracker.Definition.Icon;
                icon.enabled = icon.sprite != null;
            }

            if (optionalBadge != null)
            {
                optionalBadge.SetActive(tracker.IsOptional);
            }

            wasComplete = tracker.ShowsAsComplete;
            Refresh(false);
        }

        public void Refresh(bool animate)
        {
            if (tracker == null)
            {
                return;
            }

            var progress = tracker.Progress;
            var complete = tracker.ShowsAsComplete;
            var showCounter = counterLabel != null && !tracker.Definition.HideCounter;
            if (titleLabel != null)
            {
                var text = showCounter ? tracker.Name : tracker.Title;
                if (tracker.IsOptional && optionalBadge == null)
                {
                    text += OptionalSuffix;
                }

                titleLabel.text = text;
                titleLabel.color = progress.IsFailed ? failedColor : complete ? completeColor : activeColor;
                titleLabel.fontStyle = complete ? FontStyles.Strikethrough : FontStyles.Normal;
            }

            if (counterLabel != null)
            {
                counterLabel.gameObject.SetActive(showCounter);
                counterLabel.text = progress.Current + "/" + progress.Target;
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = tracker.Definition.IsResolvedAtLevelEnd ? (complete ? 1f : 0f) : progress.Ratio;
            }

            if (checkmark != null)
            {
                checkmark.SetActive(complete);
            }

            if (animate && complete && !wasComplete)
            {
                TidyTweenRunner.Punch(transform, Vector3.one, 0.12f, 0.35f);
                if (checkmark != null)
                {
                    checkmark.transform.localScale = Vector3.zero;
                    TidyTweenRunner.ScaleTo(checkmark.transform, Vector3.one, 0.35f, TidyEaseType.OutBack);
                }
            }
            else if (animate && counterLabel != null)
            {
                TidyTweenRunner.Punch(counterLabel.transform, Vector3.one, 0.15f, 0.25f);
            }

            wasComplete = complete;
        }
    }
}
