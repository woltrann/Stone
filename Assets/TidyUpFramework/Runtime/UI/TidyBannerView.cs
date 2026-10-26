using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using TidyUpFramework.Placement;
using TMPro;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Banner View")]
    public sealed class TidyBannerView : TidyLevelBoundView
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text subtitleLabel;
        [SerializeField, Min(0.2f)] float displaySeconds = 2.2f;
        [SerializeField] bool announceTargets = true;
        [SerializeField, Min(1), Tooltip("Small targets such as single slots are not announced.")] int minimumCapacityToAnnounce = 3;
        [SerializeField] string objectiveTitle = "Objective complete";
        [SerializeField] string targetSubtitleFormat = "{0} is organised";
        [SerializeField] string restoredTitle = "Welcome back";
        [SerializeField, Tooltip("{0} is replaced with the saved completion percentage.")] string restoredSubtitleFormat = "Progress restored  ·  {0}% tidy";

        readonly Queue<(string title, string subtitle)> queue = new Queue<(string, string)>();
        float remaining;
        bool showing;

        public void Announce(string title, string subtitle)
        {
            queue.Enqueue((title, subtitle));
        }

        protected override void Start()
        {
            base.Start();
            if (group != null)
            {
                group.alpha = 0f;
            }
        }

        protected override void Bind(TidyLevelManager manager)
        {
            manager.ObjectiveCompleted += HandleObjectiveCompleted;
            manager.ProgressRestored += HandleProgressRestored;
            TidyTargetRegistry.AnyCompletionChanged += HandleTargetCompletion;
            if (manager.Level != null)
            {
                Announce(manager.Level.DisplayName, manager.Level.Description);
            }
        }

        protected override void Unbind(TidyLevelManager manager)
        {
            manager.ObjectiveCompleted -= HandleObjectiveCompleted;
            manager.ProgressRestored -= HandleProgressRestored;
            TidyTargetRegistry.AnyCompletionChanged -= HandleTargetCompletion;
        }

        void HandleProgressRestored(Save.TidyLevelSnapshot snapshot)
        {
            Announce(restoredTitle, string.Format(restoredSubtitleFormat, Mathf.RoundToInt(snapshot.Progress * 100f)));
        }

        void HandleObjectiveCompleted(TidyObjectiveTracker tracker)
        {
            Announce(objectiveTitle, tracker.Title);
        }

        void HandleTargetCompletion(TidyPlacementTarget target, bool complete)
        {
            if (announceTargets && complete && target.Capacity >= minimumCapacityToAnnounce && LevelManager != null && LevelManager.IsPlaying && !LevelManager.IsRestoring)
            {
                Announce(target.DisplayName, string.Format(targetSubtitleFormat, target.DisplayName));
            }
        }

        void Update()
        {
            if (showing)
            {
                remaining -= Time.unscaledDeltaTime;
                if (remaining <= 0f)
                {
                    showing = false;
                    Fade(0f);
                }

                return;
            }

            if (queue.Count == 0 || (group != null && group.alpha > 0.01f))
            {
                return;
            }

            var next = queue.Dequeue();
            if (titleLabel != null)
            {
                titleLabel.text = next.title;
            }

            if (subtitleLabel != null)
            {
                subtitleLabel.text = next.subtitle;
                subtitleLabel.gameObject.SetActive(!string.IsNullOrEmpty(next.subtitle));
            }

            showing = true;
            remaining = displaySeconds;
            Fade(1f);
            TidyTweenRunner.Punch(transform, Vector3.one, 0.06f, 0.35f);
        }

        void Fade(float target)
        {
            if (group == null)
            {
                return;
            }

            var start = group.alpha;
            TidyTweenRunner.Value(blend => group.alpha = Mathf.Lerp(start, target, blend), 0.25f, TidyEaseType.OutQuad, null, true);
        }
    }
}
