using System.Collections.Generic;
using TidyUpFramework.Levels;
using TidyUpFramework.Objects;
using TidyUpFramework.Rules;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Place Objects", fileName = "Objective_Place", order = 200)]
    public sealed class TidyPlaceObjectsObjective : TidyObjectiveDefinition
    {
        [SerializeField, Tooltip("Which objects count. Empty counts every sortable object.")]
        TidyMatchFilter objectFilter = new TidyMatchFilter();

        [SerializeField, Min(0), Tooltip("How many objects must be placed. Zero means every matching object in the scene.")]
        int requiredCount;

        [SerializeField, Tooltip("Only objects placed correctly count.")]
        bool requireCorrectPlacement = true;

        [SerializeField, Tooltip("Optional target ids. When set, only objects placed in these targets count.")]
        List<string> restrictToTargetIds = new List<string>();

        public void Configure(TidyMatchFilter filter, int count, bool requireCorrect, IEnumerable<string> targetIds)
        {
            objectFilter = filter ?? new TidyMatchFilter();
            requiredCount = Mathf.Max(0, count);
            requireCorrectPlacement = requireCorrect;
            restrictToTargetIds = targetIds != null ? new List<string>(targetIds) : new List<string>();
        }

        public bool Counts(TidyObject tidyObject)
        {
            return tidyObject != null && tidyObject.IsSortable && objectFilter.Matches(tidyObject);
        }

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            var matching = 0;
            var placed = 0;
            foreach (var tidyObject in context.Objects)
            {
                if (!Counts(tidyObject))
                {
                    continue;
                }

                matching++;
                if (IsSatisfied(tidyObject))
                {
                    placed++;
                }
            }

            var target = requiredCount > 0 ? requiredCount : matching;
            return new TidyObjectiveProgress(Mathf.Min(placed, target), target);
        }

        bool IsSatisfied(TidyObject tidyObject)
        {
            if (tidyObject.State != TidyObjectState.Placed || tidyObject.CurrentTarget == null)
            {
                return false;
            }

            if (requireCorrectPlacement && !tidyObject.IsCorrectlyPlaced)
            {
                return false;
            }

            return restrictToTargetIds.Count == 0 || restrictToTargetIds.Contains(tidyObject.CurrentTarget.TargetId);
        }
    }
}
