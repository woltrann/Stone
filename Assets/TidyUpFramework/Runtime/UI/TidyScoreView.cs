using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using TMPro;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Score View")]
    public sealed class TidyScoreView : TidyLevelBoundView
    {
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] string format = "{0:N0}";
        [SerializeField, Min(0f)] float countDuration = 0.4f;
        [SerializeField] Transform punchTarget;

        int displayedScore;
        int tweenHandle;

        protected override void Bind(TidyLevelManager manager)
        {
            manager.Score.ScoreChanged += HandleScoreChanged;
            SetDisplayed(manager.Score.Score);
        }

        protected override void Unbind(TidyLevelManager manager)
        {
            manager.Score.ScoreChanged -= HandleScoreChanged;
            TidyTweenRunner.Kill(tweenHandle);
        }

        void HandleScoreChanged(int score)
        {
            var from = displayedScore;
            TidyTweenRunner.Kill(tweenHandle);
            tweenHandle = TidyTweenRunner.Value(blend => SetDisplayed(Mathf.RoundToInt(Mathf.Lerp(from, score, blend))), countDuration, TidyEaseType.OutCubic, null, true);
            var target = punchTarget != null ? punchTarget : scoreLabel != null ? scoreLabel.transform : null;
            if (target != null && score > from)
            {
                TidyTweenRunner.Punch(target, Vector3.one, 0.12f, 0.3f);
            }
        }

        void SetDisplayed(int value)
        {
            displayedScore = value;
            if (scoreLabel != null)
            {
                scoreLabel.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, value);
            }
        }
    }
}
