using UnityEngine;

namespace TidyUpFramework.Transformation
{
    public abstract class TidyProgressReactor : MonoBehaviour
    {
        [SerializeField, Tooltip("Progress range mapped to this reactor. X is where it starts reacting, Y is where it is fully transformed.")]
        Vector2 progressRange = new Vector2(0f, 1f);

        [SerializeField, Tooltip("Remaps the normalised progress, for example to ease in late.")]
        AnimationCurve response = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField, Min(0f), Tooltip("Seconds used to glide towards a new value. Zero applies instantly.")]
        float smoothingSeconds = 0.6f;

        float currentValue;
        float targetValue;
        bool hasValue;

        public Vector2 ProgressRange => progressRange;

        public void ConfigureRange(Vector2 range)
        {
            progressRange = range;
        }

        public void SetLevelProgress(float levelProgress, bool instant)
        {
            var span = Mathf.Max(0.0001f, progressRange.y - progressRange.x);
            var local = Mathf.Clamp01((levelProgress - progressRange.x) / span);
            targetValue = response != null && response.length > 0 ? response.Evaluate(local) : local;
            if (instant || !hasValue || smoothingSeconds <= 0f)
            {
                currentValue = targetValue;
                hasValue = true;
                Apply(currentValue);
            }
        }

        protected abstract void Apply(float organisedAmount);

        protected virtual void Update()
        {
            if (!hasValue || Mathf.Approximately(currentValue, targetValue))
            {
                return;
            }

            currentValue = Mathf.MoveTowards(currentValue, targetValue, Time.deltaTime / smoothingSeconds);
            Apply(currentValue);
        }
    }
}
