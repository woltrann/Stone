using System;
using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Level Card View")]
    public sealed class TidyLevelCardView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] TMP_Text difficultyLabel;
        [SerializeField] TMP_Text bestLabel;
        [SerializeField] TMP_Text lockLabel;
        [SerializeField, Tooltip("Shown on levels with saved progress. {0} is the completion percentage.")] string inProgressTemplate = "<color=#F2C46D>In progress {0}%</color>";
        [SerializeField] Image thumbnail;
        [SerializeField] Image accent;
        [SerializeField] Image[] stars = new Image[3];
        [SerializeField] GameObject lockOverlay;
        [SerializeField] GameObject completedBadge;
        [SerializeField] Color earnedStarColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField] Color emptyStarColor = new Color(1f, 1f, 1f, 0.18f);

        TidyLevelDefinition level;

        public event Action<TidyLevelDefinition> Selected;

        public Button Button => button;

        public void Bind(TidyLevelDefinition levelDefinition, ITidyProgressService progress, int index)
        {
            level = levelDefinition;
            var unlocked = progress == null || progress.IsUnlocked(level);
            var completed = progress != null && progress.IsCompleted(level.LevelId);
            var earned = progress != null ? progress.GetStars(level.LevelId) : 0;
            SetText(nameLabel, (index + 1) + ". " + level.DisplayName);
            SetText(descriptionLabel, level.Description);
            SetText(difficultyLabel, level.Difficulty.ToString());
            if (bestLabel != null)
            {
                var bestScore = progress != null ? progress.GetBestScore(level.LevelId) : 0;
                var bestTime = progress != null ? progress.GetBestTime(level.LevelId) : 0f;
                var whole = Mathf.FloorToInt(bestTime);
                bestLabel.text = completed ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "Best {0:N0}  ·  {1}:{2:00}", bestScore, whole / 60, whole % 60) : "Not completed";
                if (progress is Save.TidySaveService save && save.HasLevelSnapshot(level.LevelId))
                {
                    var percent = Mathf.RoundToInt(save.GetSavedProgress(level.LevelId) * 100f);
                    bestLabel.text = (completed ? bestLabel.text + "  ·  " : string.Empty) + string.Format(inProgressTemplate, percent);
                }
            }

            if (thumbnail != null)
            {
                thumbnail.sprite = level.Thumbnail;
                thumbnail.enabled = level.Thumbnail != null;
                thumbnail.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            if (accent != null)
            {
                accent.color = level.AccentColor;
            }

            for (var star = 0; star < stars.Length; star++)
            {
                if (stars[star] != null)
                {
                    stars[star].color = star < earned ? earnedStarColor : emptyStarColor;
                }
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!unlocked);
            }

            if (completedBadge != null)
            {
                completedBadge.SetActive(completed);
            }

            SetText(lockLabel, unlocked ? string.Empty : DescribeRequirement(level));
            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Selected?.Invoke(level));
            }
        }

        static string DescribeRequirement(TidyLevelDefinition level)
        {
            var requirement = level.UnlockRequirement;
            if (requirement == null)
            {
                return "Locked";
            }

            if (requirement.RequiredLevel != null)
            {
                return "Complete " + requirement.RequiredLevel.DisplayName;
            }

            return requirement.RequiredTotalStars > 0 ? "Collect " + requirement.RequiredTotalStars + " stars" : "Locked";
        }

        static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
