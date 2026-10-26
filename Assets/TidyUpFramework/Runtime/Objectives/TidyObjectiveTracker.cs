using System;
using TidyUpFramework.Levels;

namespace TidyUpFramework.Objectives
{
    public sealed class TidyObjectiveTracker
    {
        public TidyObjectiveTracker(TidyObjectiveDefinition definition, bool isOptional)
        {
            Definition = definition;
            IsOptional = isOptional;
        }

        public event Action<TidyObjectiveTracker> Changed;
        public event Action<TidyObjectiveTracker> CompletedFirstTime;

        public TidyObjectiveDefinition Definition { get; }
        public bool IsOptional { get; }
        public TidyObjectiveProgress Progress { get; private set; }
        public bool HasEverCompleted { get; private set; }
        public bool IsComplete => Progress.IsComplete;
        public bool IsFailed => Progress.IsFailed;
        public string Title => Definition != null ? Definition.FormatTitle(Progress) : string.Empty;
        public string Name => Definition != null ? Definition.FormatName(Progress) : string.Empty;
        public bool ShowsAsComplete => IsComplete && (Definition == null || !Definition.IsResolvedAtLevelEnd || HasEverCompleted);

        public void Refresh(TidyLevelContext context)
        {
            if (Definition == null)
            {
                return;
            }

            var next = Definition.Evaluate(context);
            var changed = next.Current != Progress.Current || next.Target != Progress.Target || next.IsFailed != Progress.IsFailed;
            Progress = next;
            if (!changed)
            {
                return;
            }

            Changed?.Invoke(this);
            if (!Definition.IsResolvedAtLevelEnd)
            {
                RaiseFirstCompletionIfNeeded();
            }
        }

        public void ResolveAtLevelEnd(TidyLevelContext context)
        {
            Refresh(context);
            RaiseFirstCompletionIfNeeded();
        }

        void RaiseFirstCompletionIfNeeded()
        {
            if (Progress.IsComplete && !HasEverCompleted)
            {
                HasEverCompleted = true;
                CompletedFirstTime?.Invoke(this);
            }
        }
    }
}
