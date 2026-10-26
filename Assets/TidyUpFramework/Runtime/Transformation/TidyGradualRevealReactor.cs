using TidyUpFramework.Core;
using UnityEngine;

namespace TidyUpFramework.Transformation
{
    [AddComponentMenu("Tidy Up Framework/Before After/Gradual Reveal Reactor")]
    public sealed class TidyGradualRevealReactor : TidyProgressReactor
    {
        [SerializeField, Tooltip("Objects revealed one by one as the area becomes organised.")]
        Transform[] revealedInOrder = new Transform[0];

        [SerializeField, Tooltip("Objects hidden one by one as the area becomes organised.")]
        Transform[] hiddenInOrder = new Transform[0];

        Vector3[] revealedScales;
        Vector3[] hiddenScales;
        int revealedCount = -1;
        int hiddenCount = -1;

        public void Configure(Transform[] reveal, Transform[] hide)
        {
            revealedInOrder = reveal ?? new Transform[0];
            hiddenInOrder = hide ?? new Transform[0];
            revealedScales = null;
            hiddenScales = null;
        }

        protected override void Apply(float organisedAmount)
        {
            CacheScales();
            var nextRevealed = Mathf.RoundToInt(organisedAmount * revealedInOrder.Length);
            var nextHidden = Mathf.RoundToInt(organisedAmount * hiddenInOrder.Length);
            var instant = revealedCount < 0;
            UpdateSet(revealedInOrder, revealedScales, ref revealedCount, nextRevealed, true, instant);
            UpdateSet(hiddenInOrder, hiddenScales, ref hiddenCount, nextHidden, false, instant);
        }

        void CacheScales()
        {
            if (revealedInOrder == null)
                revealedInOrder = new Transform[0];

            if (hiddenInOrder == null)
                hiddenInOrder = new Transform[0];

            if (revealedScales == null ||
                revealedScales.Length != revealedInOrder.Length)
            {
                revealedScales = CaptureScales(revealedInOrder);
                revealedCount = -1;
            }

            if (hiddenScales == null ||
                hiddenScales.Length != hiddenInOrder.Length)
            {
                hiddenScales = CaptureScales(hiddenInOrder);
                hiddenCount = -1;
            }
        }

        static Vector3[] CaptureScales(Transform[] items)
        {
            var scales = new Vector3[items.Length];
            for (var index = 0; index < items.Length; index++)
            {
                scales[index] = items[index] != null ? items[index].localScale : Vector3.one;
            }

            return scales;
        }

        static void UpdateSet(Transform[] items, Vector3[] scales, ref int currentCount, int nextCount, bool revealing, bool instant)
        {
            if (currentCount == nextCount)
            {
                return;
            }

            for (var index = 0; index < items.Length; index++)
            {
                var item = items[index];
                if (item == null)
                {
                    continue;
                }

                var withinCount = index < nextCount;
                var shouldBeVisible = revealing ? withinCount : !withinCount;
                var wasWithin = index < currentCount;
                var wasVisible = currentCount >= 0 && (revealing ? wasWithin : !wasWithin);
                if (!instant && shouldBeVisible == wasVisible)
                {
                    continue;
                }

                Animate(item, scales[index], shouldBeVisible, instant);
            }

            currentCount = nextCount;
        }

        static void Animate(Transform item, Vector3 restScale, bool visible, bool instant)
        {
            if (instant)
            {
                item.gameObject.SetActive(visible);
                item.localScale = restScale;
                return;
            }

            if (visible)
            {
                item.localScale = Vector3.zero;
                item.gameObject.SetActive(true);
                TidyTweenRunner.ScaleTo(item, restScale, 0.4f, TidyEaseType.OutBack);
            }
            else
            {
                TidyTweenRunner.ScaleTo(item, Vector3.zero, 0.25f, TidyEaseType.InQuad, () =>
                {
                    item.gameObject.SetActive(false);
                    item.localScale = restScale;
                });
            }
        }
    }
}
