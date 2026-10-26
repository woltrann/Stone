using UnityEngine;

namespace TidyUpFramework.UI
{
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Tidy Up Framework/UI/Safe Area")]
    public sealed class TidySafeArea : MonoBehaviour
    {
        RectTransform rectTransform;
        Rect appliedArea;
        Vector2Int appliedScreen;

        void Awake()
        {
            rectTransform = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != appliedArea || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y)
            {
                Apply();
            }
        }

        void Apply()
        {
            appliedArea = Screen.safeArea;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var min = appliedArea.position;
            var max = appliedArea.position + appliedArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
