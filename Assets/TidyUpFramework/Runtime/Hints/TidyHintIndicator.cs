using TidyUpFramework.Core;
using UnityEngine;

namespace TidyUpFramework.Hints
{
    [AddComponentMenu("Tidy Up Framework/Hints/Hint Indicator")]
    public sealed class TidyHintIndicator : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        float bobHeight = 0.06f;

        [SerializeField, Min(0f)]
        float bobSpeed = 3f;

        [SerializeField]
        float spinDegreesPerSecond = 90f;

        [SerializeField, Min(0f)]
        float hoverOffset = 0.2f;

        Vector3 anchor;
        Vector3 restScale = Vector3.one;
        bool isShown;

        public void ShowAt(Vector3 worldPoint)
        {
            anchor = worldPoint + Vector3.up * hoverOffset;
            transform.position = anchor;
            if (!isShown)
            {
                gameObject.SetActive(true);
                transform.localScale = Vector3.zero;
                TidyTweenRunner.ScaleTo(transform, restScale, 0.3f, TidyEaseType.OutBack);
            }

            isShown = true;
        }

        public void Hide()
        {
            if (!isShown)
            {
                return;
            }

            isShown = false;
            TidyTweenRunner.ScaleTo(transform, Vector3.zero, 0.2f, TidyEaseType.InQuad, () => gameObject.SetActive(false));
        }

        void Awake()
        {
            restScale = transform.localScale;
        }

        void Update()
        {
            if (!isShown)
            {
                return;
            }

            transform.position = anchor + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }
}
