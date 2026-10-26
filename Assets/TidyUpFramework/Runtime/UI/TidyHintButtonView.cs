using TidyUpFramework.Core;
using TidyUpFramework.Hints;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Hint Button View")]
    public sealed class TidyHintButtonView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image cooldownFill;
        [SerializeField] TMP_Text countLabel;
        [SerializeField] TMP_Text costLabel;
        [SerializeField] GameObject root;

        TidyHintSystem hints;

        void Start()
        {
            if (!TidyServices.TryGet(out hints))
            {
                SetRootVisible(false);
                return;
            }

            hints.StateChanged += Refresh;
            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }

            Refresh(hints);
        }

        void OnDestroy()
        {
            if (hints != null)
            {
                hints.StateChanged -= Refresh;
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        void HandleClicked()
        {
            hints?.RequestHint();
        }

        void Refresh(TidyHintSystem system)
        {
            SetRootVisible(system.IsHintsFeatureEnabled);
            if (button != null)
            {
                button.interactable = system.IsAvailable;
            }

            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = system.Cooldown01;
            }

            if (countLabel != null)
            {
                countLabel.text = system.HintsLeft < 0 ? string.Empty : system.HintsLeft.ToString();
            }

            if (costLabel != null)
            {
                costLabel.text = system.FreeHintsLeft > 0 ? "Free" : string.Empty;
            }
        }

        void SetRootVisible(bool visible)
        {
            var target = root != null ? root : gameObject;
            if (target.activeSelf != visible && target != gameObject)
            {
                target.SetActive(visible);
            }
        }
    }
}
