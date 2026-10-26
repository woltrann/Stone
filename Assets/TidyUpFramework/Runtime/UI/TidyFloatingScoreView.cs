using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Feedback;
using TidyUpFramework.Interaction;
using TidyUpFramework.Levels;
using TidyUpFramework.Scoring;
using TMPro;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Floating Score View")]
    public sealed class TidyFloatingScoreView : TidyLevelBoundView
    {
        [SerializeField] TMP_Text labelPrefab;
        [SerializeField] RectTransform container;
        [SerializeField, Min(0.1f)] float lifetime = 0.9f;
        [SerializeField] float risePixels = 70f;
        [SerializeField] Color positiveColor = new Color(1f, 0.93f, 0.55f);
        [SerializeField] Color negativeColor = new Color(1f, 0.45f, 0.4f);

        sealed class FloatingLabel
        {
            public TMP_Text Label;
            public Vector3 WorldPosition;
            public float Age;
        }

        readonly List<FloatingLabel> active = new List<FloatingLabel>();
        readonly Stack<TMP_Text> pool = new Stack<TMP_Text>();
        Camera worldCamera;
        RectTransform parent;

        protected override void Bind(TidyLevelManager manager)
        {
            manager.Score.ScoreAwarded += HandleAward;
            parent = container != null ? container : (RectTransform)transform;
            if (TidyServices.TryGet<TidyInteractor>(out var interactor))
            {
                worldCamera = interactor.AimCamera;
            }

            if (TidyServices.TryGet<TidyFeedbackPlayer>(out var feedback) && feedback.Definition != null)
            {
                positiveColor = feedback.Definition.PositiveScoreColor;
                negativeColor = feedback.Definition.NegativeScoreColor;
            }

            if (labelPrefab != null)
            {
                labelPrefab.gameObject.SetActive(false);
            }
        }

        protected override void Unbind(TidyLevelManager manager)
        {
            manager.Score.ScoreAwarded -= HandleAward;
        }

        void HandleAward(TidyScoreAward award)
        {
            if (!award.HasWorldPosition || labelPrefab == null)
            {
                return;
            }

            var label = pool.Count > 0 ? pool.Pop() : Instantiate(labelPrefab, parent);
            label.gameObject.SetActive(true);
            label.text = award.Amount > 0 ? "+" + award.Amount : award.Amount.ToString();
            label.color = award.Amount > 0 ? positiveColor : negativeColor;
            active.Add(new FloatingLabel { Label = label, WorldPosition = award.WorldPosition });
            label.transform.localScale = Vector3.zero;
            TidyTweenRunner.ScaleTo(label.transform, Vector3.one, 0.25f, TidyEaseType.OutBack);
            Position(active[active.Count - 1]);
        }

        void LateUpdate()
        {
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var floating = active[index];
                floating.Age += Time.unscaledDeltaTime;
                if (floating.Age >= lifetime)
                {
                    floating.Label.gameObject.SetActive(false);
                    pool.Push(floating.Label);
                    active.RemoveAt(index);
                    continue;
                }

                Position(floating);
            }
        }

        void Position(FloatingLabel floating)
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            var screenPoint = worldCamera.WorldToScreenPoint(floating.WorldPosition);
            var visible = screenPoint.z > 0f;
            floating.Label.enabled = visible;
            if (!visible)
            {
                return;
            }

            var normalized = floating.Age / lifetime;
            screenPoint.y += normalized * risePixels;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out var local);
            floating.Label.rectTransform.anchoredPosition = local;
            var color = floating.Label.color;
            color.a = 1f - Mathf.Clamp01((normalized - 0.6f) / 0.4f);
            floating.Label.color = color;
        }
    }
}
