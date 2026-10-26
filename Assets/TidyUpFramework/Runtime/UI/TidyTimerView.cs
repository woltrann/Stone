using TidyUpFramework.Levels;
using TMPro;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Timer View")]
    public sealed class TidyTimerView : TidyLevelBoundView
    {
        [SerializeField] TMP_Text timeLabel;
        [SerializeField, Tooltip("Shows remaining time when the level has a limit, otherwise elapsed time.")] bool preferCountdown = true;
        [SerializeField, Min(0f)] float warningThresholdSeconds = 20f;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color warningColor = new Color(1f, 0.45f, 0.4f);

        int lastShownSeconds = -1;

        protected override void Bind(TidyLevelManager manager)
        {
            lastShownSeconds = -1;
        }

        protected override void Unbind(TidyLevelManager manager)
        {
        }

        void Update()
        {
            if (LevelManager == null || LevelManager.Context == null || timeLabel == null)
            {
                return;
            }

            var remaining = LevelManager.TimeRemaining;
            var countdown = preferCountdown && remaining >= 0f;
            var seconds = countdown ? remaining : LevelManager.Context.ElapsedSeconds;
            var whole = Mathf.CeilToInt(seconds);
            if (whole == lastShownSeconds)
            {
                return;
            }

            lastShownSeconds = whole;
            timeLabel.text = string.Format("{0}:{1:00}", whole / 60, whole % 60);
            timeLabel.color = countdown && remaining <= warningThresholdSeconds ? warningColor : normalColor;
        }
    }
}
