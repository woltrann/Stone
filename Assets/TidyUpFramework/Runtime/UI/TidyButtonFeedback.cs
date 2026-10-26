using TidyUpFramework.Audio;
using TidyUpFramework.Core;
using TidyUpFramework.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Button Feedback")]
    public sealed class TidyButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField, Min(1f)] float hoverScale = 1.04f;
        [SerializeField, Min(0.5f)] float pressedScale = 0.96f;
        [SerializeField, Min(0f)] float duration = 0.12f;
        [SerializeField, Tooltip("Plays the back sound instead of the click sound.")] bool isBackButton;
        [SerializeField, Tooltip("Optional object enabled while hovered or selected, for example a glow.")] GameObject highlightVisual;

        Selectable selectable;
        Vector3 restScale = Vector3.one;
        bool isHovered;
        bool isSelected;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            restScale = transform.localScale;
            if (highlightVisual != null)
            {
                highlightVisual.SetActive(false);
            }
        }

        void OnDisable()
        {
            isHovered = false;
            isSelected = false;
            transform.localScale = restScale;
            if (highlightVisual != null)
            {
                highlightVisual.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            Refresh(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            Refresh(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            Refresh(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            Refresh(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsInteractable)
            {
                TidyTweenRunner.ScaleTo(transform, restScale * pressedScale, duration * 0.5f, TidyEaseType.OutQuad);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Refresh(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayClick();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            PlayClick();
            TidyTweenRunner.Punch(transform, restScale, 0.06f, 0.2f);
        }

        bool IsInteractable => selectable == null || selectable.IsInteractable();

        void Refresh(bool playHoverSound)
        {
            var active = (isHovered || isSelected) && IsInteractable;
            if (highlightVisual != null)
            {
                highlightVisual.SetActive(active);
            }

            TidyTweenRunner.ScaleTo(transform, active ? restScale * hoverScale : restScale, duration, TidyEaseType.OutCubic);
            if (active && playHoverSound && TidyFramework.Settings != null)
            {
                TidyAudio.Play(TidyFramework.Settings.UiHoverCue);
            }
        }

        void PlayClick()
        {
            if (!IsInteractable || TidyFramework.Settings == null)
            {
                return;
            }

            TidyAudio.Play(isBackButton ? TidyFramework.Settings.UiBackCue : TidyFramework.Settings.UiClickCue);
        }
    }
}
