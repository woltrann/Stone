using System.Collections.Generic;
using TidyUpFramework.Audio;
using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Results Screen")]
    public sealed class TidyResultsScreen : TidyLevelBoundView
    {
        [SerializeField] TidyUIScreen screen;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text accuracyLabel;
        [SerializeField] TMP_Text comboLabel;
        [SerializeField] GameObject newBestBadge;
        [SerializeField] Image[] stars = new Image[3];
        [SerializeField] Color earnedStarColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField] Color emptyStarColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] RectTransform breakdownContainer;
        [SerializeField] TMP_Text breakdownLinePrefab;
        [SerializeField] Button nextButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button menuButton;
        [SerializeField] TidyAudioCue starCue;
        [SerializeField] string completedTitle = "Beautifully organised!";
        [SerializeField] string failedTitle = "Out of time";
        [SerializeField, Min(0f)] float starInterval = 0.35f;

        readonly List<TMP_Text> breakdownLines = new List<TMP_Text>();
        TidyLevelDefinition nextLevel;

        protected override void Start()
        {
            base.Start();
            Bind(nextButton, () =>
            {
                if (nextLevel != null)
                {
                    TidyFramework.LoadLevel(nextLevel);
                }
            });
            Bind(retryButton, TidyFramework.RestartLevel);
            Bind(menuButton, TidyFramework.LoadMainMenu);
            if (breakdownLinePrefab != null)
            {
                breakdownLinePrefab.gameObject.SetActive(false);
            }
        }

        protected override void Bind(TidyLevelManager manager)
        {
            manager.LevelCompleted += Present;
            manager.LevelFailed += Present;
        }

        protected override void Unbind(TidyLevelManager manager)
        {
            manager.LevelCompleted -= Present;
            manager.LevelFailed -= Present;
        }

        void Present(TidyLevelResult result)
        {
            if (titleLabel != null)
            {
                titleLabel.text = result.IsCompleted ? completedTitle : failedTitle;
            }

            if (timeLabel != null)
            {
                var whole = Mathf.FloorToInt(result.TimeSeconds);
                timeLabel.text = string.Format("{0}:{1:00}", whole / 60, whole % 60);
            }

            if (accuracyLabel != null)
            {
                accuracyLabel.text = Mathf.RoundToInt(result.Accuracy * 100f) + "%";
            }

            if (comboLabel != null)
            {
                comboLabel.text = "x" + result.BestCombo;
            }

            if (newBestBadge != null)
            {
                newBestBadge.SetActive(result.IsCompleted && result.IsNewBestScore);
            }

            BuildBreakdown(result);
            AnimateScore(result.Score);
            AnimateStars(result.Stars);
            ConfigureNext(result);
            screen?.Show();
        }

        void ConfigureNext(TidyLevelResult result)
        {
            var catalog = TidyFramework.Catalog;
            nextLevel = catalog != null && LevelManager != null && LevelManager.Level != null ? catalog.NextAfter(LevelManager.Level) : null;
            var unlocked = nextLevel != null && TidyFramework.Save != null && TidyFramework.Save.IsUnlocked(nextLevel);
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(result.IsCompleted && unlocked);
            }

            if (screen != null)
            {
                screen.FirstSelected = nextButton != null && nextButton.gameObject.activeSelf ? nextButton.gameObject : retryButton != null ? retryButton.gameObject : null;
                screen.SelectFirst();
            }
        }

        void BuildBreakdown(TidyLevelResult result)
        {
            foreach (var line in breakdownLines)
            {
                Destroy(line.gameObject);
            }

            breakdownLines.Clear();
            if (breakdownContainer == null || breakdownLinePrefab == null)
            {
                return;
            }

            foreach (var scoreLine in result.ScoreBreakdown)
            {
                var line = Instantiate(breakdownLinePrefab, breakdownContainer);
                line.gameObject.SetActive(true);
                line.text = scoreLine.Label + "<pos=75%>+" + scoreLine.Amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                breakdownLines.Add(line);
            }
        }

        void AnimateScore(int score)
        {
            if (scoreLabel == null)
            {
                return;
            }

            TidyTweenRunner.Value(blend => scoreLabel.text = Mathf.RoundToInt(score * blend).ToString("N0", System.Globalization.CultureInfo.InvariantCulture), 1.1f, TidyEaseType.OutCubic, null, true, 0.3f);
        }

        void AnimateStars(int earned)
        {
            for (var index = 0; index < stars.Length; index++)
            {
                var star = stars[index];
                if (star == null)
                {
                    continue;
                }

                var isEarned = index < earned;
                star.color = emptyStarColor;
                star.transform.localScale = Vector3.one;
                if (!isEarned)
                {
                    continue;
                }

                var delay = 0.6f + index * starInterval;
                var pitch = index * 0.12f;
                TidyTweenRunner.Value(blend =>
                {
                    if (blend <= 0f)
                    {
                        return;
                    }

                    star.color = earnedStarColor;
                    star.transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.8f, 1f, blend);
                }, 0.35f, TidyEaseType.OutBack, () => TidyAudio.Play(starCue, 1f, pitch), true, delay);
            }
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
