using TidyUpFramework.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Progress View")]
    public sealed class TidyProgressView : TidyLevelBoundView
    {
        [SerializeField] Image fillImage;
        [SerializeField] TMP_Text percentLabel;
        [SerializeField] string format = "{0:0}% tidy";
        [SerializeField, Min(0.1f)] float fillSpeed = 1.5f;
        [SerializeField] bool useGradient;
        [SerializeField] Gradient fillColor;

        float target;
        float shown;

        protected override void Bind(TidyLevelManager manager)
        {
            manager.ProgressChanged += HandleProgress;
            target = manager.Progress;
            shown = target;
            Apply();
        }

        protected override void Unbind(TidyLevelManager manager)
        {
            manager.ProgressChanged -= HandleProgress;
        }

        void HandleProgress(float progress)
        {
            target = progress;
        }

        void Update()
        {
            if (Mathf.Approximately(shown, target))
            {
                return;
            }

            shown = Mathf.MoveTowards(shown, target, fillSpeed * Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = shown;
                if (useGradient && fillColor != null)
                {
                    fillImage.color = fillColor.Evaluate(shown);
                }
            }

            if (percentLabel != null)
            {
                percentLabel.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, shown * 100f);
            }
        }
    }
}
