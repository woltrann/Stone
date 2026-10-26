using System;
using TidyUpFramework.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TidyUpFramework.UI
{
    public enum TidyScreenTransition
    {
        Fade,
        FadeScale,
        SlideUp,
        SlideRight
    }

    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Tidy Up Framework/UI/UI Screen")]
    public class TidyUIScreen : MonoBehaviour
    {
        [SerializeField]
        TidyScreenTransition transition = TidyScreenTransition.FadeScale;

        [SerializeField, Min(0f)]
        float duration = 0.28f;

        [SerializeField, Min(0f)]
        float slideDistance = 60f;

        [SerializeField, Tooltip("Selected when the screen opens so gamepads can navigate immediately.")]
        GameObject firstSelected;

        [SerializeField, Tooltip("Gameplay channels locked while this screen is visible.")]
        TidyControlChannel lockedChannels = TidyControlChannel.None;

        [SerializeField]
        bool startVisible;

        CanvasGroup canvasGroup;
        RectTransform rectTransform;
        Vector2 restPosition;
        int tweenHandle;
        bool initialised;

        public event Action<TidyUIScreen> Shown;
        public event Action<TidyUIScreen> Hidden;

        public bool IsVisible { get; private set; }
        public GameObject FirstSelected
        {
            get => firstSelected;
            set => firstSelected = value;
        }

        public void Configure(TidyScreenTransition newTransition, TidyControlChannel channels, bool visibleAtStart)
        {
            transition = newTransition;
            lockedChannels = channels;
            startVisible = visibleAtStart;
        }

        public void Show()
        {
            Initialise();
            if (IsVisible)
            {
                return;
            }

            IsVisible = true;
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            if (lockedChannels != TidyControlChannel.None)
            {
                TidyControlLock.Acquire(this, lockedChannels);
            }

            Animate(true);
            SelectFirst();
            OnShown();
            Shown?.Invoke(this);
        }

        public void Hide()
        {
            Initialise();
            if (!IsVisible)
            {
                return;
            }

            IsVisible = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            TidyControlLock.Release(this);
            Animate(false);
            OnHidden();
            Hidden?.Invoke(this);
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void HideImmediately()
        {
            Initialise();
            TidyTweenRunner.Kill(tweenHandle);
            IsVisible = false;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            TidyControlLock.Release(this);
            gameObject.SetActive(false);
        }

        public void SelectFirst()
        {
            if (firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected);
            }
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        protected virtual void Awake()
        {
            Initialise();
            if (IsVisible)
            {
                return;
            }

            if (startVisible)
            {
                Show();
            }
            else
            {
                HideImmediately();
            }
        }

        protected virtual void OnDestroy()
        {
            TidyTweenRunner.Kill(tweenHandle);
            TidyControlLock.Release(this);
        }

        void Initialise()
        {
            if (initialised)
            {
                return;
            }

            initialised = true;
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = transform as RectTransform;
            restPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        }

        void Animate(bool showing)
        {
            TidyTweenRunner.Kill(tweenHandle);
            var startAlpha = canvasGroup.alpha;
            var targetAlpha = showing ? 1f : 0f;
            var ease = showing ? TidyEaseType.OutCubic : TidyEaseType.InQuad;
            tweenHandle = TidyTweenRunner.Value(blend =>
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, blend);
                ApplyMotion(showing ? blend : 1f - blend);
            }, duration, ease, () =>
            {
                if (!showing)
                {
                    gameObject.SetActive(false);
                }
            }, true);
        }

        void ApplyMotion(float visibleAmount)
        {
            if (rectTransform == null)
            {
                return;
            }

            switch (transition)
            {
                case TidyScreenTransition.FadeScale:
                    rectTransform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, visibleAmount);
                    break;
                case TidyScreenTransition.SlideUp:
                    rectTransform.anchoredPosition = restPosition + Vector2.down * slideDistance * (1f - visibleAmount);
                    break;
                case TidyScreenTransition.SlideRight:
                    rectTransform.anchoredPosition = restPosition + Vector2.left * slideDistance * (1f - visibleAmount);
                    break;
            }
        }
    }
}
