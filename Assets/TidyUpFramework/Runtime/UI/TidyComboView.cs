using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using TidyUpFramework.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Combo View")]
    public sealed class TidyComboView : TidyLevelBoundView
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text countLabel;
        [SerializeField] TMP_Text multiplierLabel;
        [SerializeField] TMP_Text tierLabel;
        [SerializeField] Image windowFill;
        [SerializeField, Min(2)] int minimumVisibleCount = 2;

        TidyComboSystem combo;
        int fadeHandle;
        bool visible;

        protected override void Bind(TidyLevelManager manager)
        {
            combo = manager.Combo;
            combo.ComboChanged += HandleComboChanged;
            combo.TierReached += HandleTierReached;
            SetVisible(false, true);
            if (tierLabel != null)
            {
                tierLabel.gameObject.SetActive(false);
            }
        }

        protected override void Unbind(TidyLevelManager manager)
        {
            combo.ComboChanged -= HandleComboChanged;
            combo.TierReached -= HandleTierReached;
        }

        void HandleComboChanged(TidyComboSystem system)
        {
            var show = system.IsEnabled && system.Count >= minimumVisibleCount;
            SetVisible(show, false);
            if (!show)
            {
                return;
            }

            if (countLabel != null)
            {
                countLabel.text = "x" + system.Count;
                TidyTweenRunner.Punch(countLabel.transform, Vector3.one, 0.25f, 0.3f);
            }

            if (multiplierLabel != null)
            {
                multiplierLabel.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}x score", system.Multiplier);
            }

            if (countLabel != null && system.CurrentTier != null)
            {
                countLabel.color = system.CurrentTier.Color;
            }
        }

        void HandleTierReached(TidyComboTier tier)
        {
            if (tierLabel == null)
            {
                return;
            }

            tierLabel.text = tier.Label;
            tierLabel.color = tier.Color;
            tierLabel.gameObject.SetActive(true);
            tierLabel.transform.localScale = Vector3.zero;
            TidyTweenRunner.ScaleTo(tierLabel.transform, Vector3.one, 0.35f, TidyEaseType.OutBack, () =>
            {
                if (tierLabel != null)
                {
                    TidyTweenRunner.ScaleTo(tierLabel.transform, Vector3.zero, 0.25f, TidyEaseType.InQuad, HideTierLabel, 1.1f);
                }
            });
        }

        void HideTierLabel()
        {
            if (tierLabel != null)
            {
                tierLabel.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (visible && combo != null && windowFill != null)
            {
                windowFill.fillAmount = combo.WindowRemaining01;
            }
        }

        void SetVisible(bool show, bool instant)
        {
            if (group == null || (visible == show && !instant))
            {
                visible = show;
                return;
            }

            visible = show;
            TidyTweenRunner.Kill(fadeHandle);
            if (instant)
            {
                group.alpha = show ? 1f : 0f;
                return;
            }

            var start = group.alpha;
            fadeHandle = TidyTweenRunner.Value(blend => group.alpha = Mathf.Lerp(start, show ? 1f : 0f, blend), 0.2f, TidyEaseType.OutQuad, null, true);
        }
    }
}
