using System.Collections.Generic;
using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Transformation
{
    public enum TidyTransformationSource
    {
        LevelProgress,
        TargetCompletion,
        Manual
    }

    [AddComponentMenu("Tidy Up Framework/Before After/Transformation Controller")]
    public sealed class TidyTransformationController : MonoBehaviour
    {
        [SerializeField, Tooltip("Level Progress follows the objectives. Target Completion follows a single target. Manual is driven from code or events.")]
        TidyTransformationSource source = TidyTransformationSource.LevelProgress;

        [SerializeField, Tooltip("Target id used when the source is Target Completion.")]
        string targetId;

        [SerializeField, Tooltip("Reactors driven by this controller. Empty collects every reactor in the children.")]
        List<TidyProgressReactor> reactors = new List<TidyProgressReactor>();

        TidyLevelManager levelManager;
        Placement.TidyPlacementTarget trackedTarget;

        public float CurrentProgress { get; private set; }

        public void SetProgress(float progress, bool instant = false)
        {
            CurrentProgress = Mathf.Clamp01(progress);
            foreach (var reactor in reactors)
            {
                if (reactor != null)
                {
                    reactor.SetLevelProgress(CurrentProgress, instant);
                }
            }
        }

        void Awake()
        {
            if (reactors.Count == 0)
            {
                reactors.AddRange(GetComponentsInChildren<TidyProgressReactor>(true));
            }
        }

        void Start()
        {
            SetProgress(0f, true);
            switch (source)
            {
                case TidyTransformationSource.LevelProgress:
                    if (TidyServices.TryGet(out levelManager))
                    {
                        levelManager.ProgressChanged += HandleProgressChanged;
                        SetProgress(levelManager.Progress, true);
                    }

                    break;
                case TidyTransformationSource.TargetCompletion:
                    trackedTarget = Placement.TidyTargetRegistry.FindById(targetId);
                    if (trackedTarget != null)
                    {
                        trackedTarget.ObjectPlaced += HandleTargetChanged;
                        trackedTarget.ObjectRemoved += HandleTargetChanged;
                    }

                    break;
            }
        }

        void OnDestroy()
        {
            if (levelManager != null)
            {
                levelManager.ProgressChanged -= HandleProgressChanged;
            }

            if (trackedTarget != null)
            {
                trackedTarget.ObjectPlaced -= HandleTargetChanged;
                trackedTarget.ObjectRemoved -= HandleTargetChanged;
            }
        }

        void HandleProgressChanged(float progress)
        {
            SetProgress(progress);
        }

        void HandleTargetChanged(Placement.TidyPlacementEvent placementEvent)
        {
            SetProgress(trackedTarget.CompletionRatio);
        }
    }
}
