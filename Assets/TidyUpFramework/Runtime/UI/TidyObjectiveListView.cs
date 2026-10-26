using System.Collections.Generic;
using TidyUpFramework.Levels;
using TidyUpFramework.Objectives;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/HUD/Objective List View")]
    public sealed class TidyObjectiveListView : TidyLevelBoundView
    {
        [SerializeField] TidyObjectiveItemView itemPrefab;
        [SerializeField] RectTransform container;
        [SerializeField] bool showOptionalObjectives = true;

        readonly Dictionary<TidyObjectiveTracker, TidyObjectiveItemView> views = new Dictionary<TidyObjectiveTracker, TidyObjectiveItemView>();

        protected override void Bind(TidyLevelManager manager)
        {
            Rebuild(manager);
            manager.ObjectiveChanged += HandleObjectiveChanged;
        }

        protected override void Unbind(TidyLevelManager manager)
        {
            manager.ObjectiveChanged -= HandleObjectiveChanged;
        }

        void Rebuild(TidyLevelManager manager)
        {
            var parent = container != null ? container : (RectTransform)transform;
            foreach (var view in views.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            views.Clear();
            if (itemPrefab == null)
            {
                return;
            }

            foreach (var tracker in manager.Trackers)
            {
                if (tracker.IsOptional && !showOptionalObjectives)
                {
                    continue;
                }

                var view = Instantiate(itemPrefab, parent);
                view.gameObject.SetActive(true);
                view.Bind(tracker);
                views[tracker] = view;
            }
        }

        void HandleObjectiveChanged(TidyObjectiveTracker tracker)
        {
            if (views.TryGetValue(tracker, out var view) && view != null)
            {
                view.Refresh(true);
            }
        }
    }
}
