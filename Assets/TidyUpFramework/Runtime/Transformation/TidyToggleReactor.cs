using TidyUpFramework.Core;
using UnityEngine;

namespace TidyUpFramework.Transformation
{
    [AddComponentMenu("Tidy Up Framework/Before After/Toggle Reactor")]
    public sealed class TidyToggleReactor : TidyProgressReactor
    {
        [SerializeField, Tooltip("Shown while the area is messy and hidden once organised, for example floor clutter, cobwebs or dust.")]
        GameObject[] messyObjects = new GameObject[0];

        [SerializeField, Tooltip("Hidden while messy and revealed once organised, for example sparkles, flowers or lit candles.")]
        GameObject[] organisedObjects = new GameObject[0];

        [SerializeField, Range(0f, 1f), Tooltip("Normalised amount at which the swap happens.")]
        float threshold = 0.99f;

        [SerializeField, Tooltip("Scale objects in and out instead of popping.")]
        bool animateScale = true;

        bool? organisedState;

        public void Configure(GameObject[] messy, GameObject[] organised, float swapThreshold)
        {
            messyObjects = messy ?? new GameObject[0];
            organisedObjects = organised ?? new GameObject[0];
            threshold = swapThreshold;
        }

        protected override void Apply(float organisedAmount)
        {
            var isOrganised = organisedAmount >= threshold;
            if (organisedState == isOrganised)
            {
                return;
            }

            var firstApply = organisedState == null;
            organisedState = isOrganised;
            foreach (var messy in messyObjects)
            {
                SetVisible(messy, !isOrganised, firstApply);
            }

            foreach (var organised in organisedObjects)
            {
                SetVisible(organised, isOrganised, firstApply);
            }
        }

        void SetVisible(GameObject target, bool visible, bool instant)
        {
            if (target == null)
            {
                return;
            }

            if (instant || !animateScale)
            {
                target.SetActive(visible);
                return;
            }

            var restScale = target.transform.localScale == Vector3.zero ? Vector3.one : target.transform.localScale;
            if (visible)
            {
                target.transform.localScale = Vector3.zero;
                target.SetActive(true);
                TidyTweenRunner.ScaleTo(target.transform, restScale, 0.45f, TidyEaseType.OutBack);
            }
            else
            {
                TidyTweenRunner.ScaleTo(target.transform, Vector3.zero, 0.3f, TidyEaseType.InQuad, () =>
                {
                    target.SetActive(false);
                    target.transform.localScale = restScale;
                });
            }
        }
    }
}
