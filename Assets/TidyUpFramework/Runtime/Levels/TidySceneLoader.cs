using System;
using System.Collections;
using TidyUpFramework.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TidyUpFramework.Levels
{
    [AddComponentMenu("")]
    public sealed class TidySceneLoader : MonoBehaviour
    {
        const int OverlaySortingOrder = 32000;

        CanvasGroup overlay;
        float fadeSeconds = 0.4f;
        bool isLoading;

        public event Action<string> LoadStarted;
        public event Action<string> LoadCompleted;

        public bool IsLoading => isLoading;
        public TidyLevelDefinition PendingLevel { get; private set; }

        public void Configure(float fadeDuration, Color fadeColor)
        {
            fadeSeconds = fadeDuration;
            EnsureOverlay(fadeColor);
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("Tidy Up Framework: scene '" + sceneName + "' is not in the build scene list.");
                return;
            }

            StartCoroutine(LoadRoutine(sceneName));
        }

        public void LoadLevel(TidyLevelDefinition level)
        {
            if (level == null)
            {
                return;
            }

            PendingLevel = level;
            LoadScene(level.SceneName);
        }

        public void ReloadActiveScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            isLoading = true;
            LoadStarted?.Invoke(sceneName);
            Time.timeScale = 1f;
            yield return Fade(0f, 1f);
            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            yield return null;
            LoadCompleted?.Invoke(sceneName);
            yield return Fade(1f, 0f);
            isLoading = false;
        }

        IEnumerator Fade(float from, float to)
        {
            if (overlay == null)
            {
                yield break;
            }

            overlay.blocksRaycasts = true;
            var elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Lerp(from, to, TidyEase.Evaluate(TidyEaseType.InOutSine, elapsed / fadeSeconds));
                yield return null;
            }

            overlay.alpha = to;
            overlay.blocksRaycasts = to > 0.5f;
        }

        void EnsureOverlay(Color fadeColor)
        {
            if (overlay != null)
            {
                overlay.GetComponentInChildren<Image>().color = fadeColor;
                return;
            }

            var canvasObject = new GameObject("Tidy Fade Overlay", typeof(Canvas), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;
            overlay = canvasObject.GetComponent<CanvasGroup>();
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;
            var imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = imageObject.GetComponent<Image>();
            image.color = fadeColor;
            image.raycastTarget = true;
        }

        void Awake()
        {
            TidyServices.Register(this);
        }

        void OnDestroy()
        {
            TidyServices.Unregister(this);
        }
    }
}
