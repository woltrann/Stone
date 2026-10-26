using System.Collections.Generic;
using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Complete Targets", fileName = "Objective_CompleteTargets", order = 201)]
    public sealed class TidyCompleteTargetsObjective : TidyObjectiveDefinition
    {
        [SerializeField, Tooltip("Target ids that must be complete, for example a full and ordered shelf. Empty means every target that counts towards progress.")]
        List<string> targetIds = new List<string>();

        [SerializeField, Tooltip("Target id prefix, for example 'Shelf_'. Combined with the list above.")]
        string targetIdPrefix;

        public void Configure(IEnumerable<string> ids, string prefix)
        {
            targetIds = ids != null ? new List<string>(ids) : new List<string>();
            targetIdPrefix = prefix;
        }

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            var total = 0;
            var complete = 0;
            foreach (var target in context.Targets)
            {
                if (target == null || !Includes(target))
                {
                    continue;
                }

                total++;
                if (target.IsComplete)
                {
                    complete++;
                }
            }

            return new TidyObjectiveProgress(complete, total);
        }

        bool Includes(Placement.TidyPlacementTarget target)
        {
            var hasFilters = targetIds.Count > 0 || !string.IsNullOrEmpty(targetIdPrefix);
            if (!hasFilters)
            {
                return target.CountsTowardsProgress && target.CompletionMode != Placement.TidyCompletionMode.Never;
            }

            return targetIds.Contains(target.TargetId) || (!string.IsNullOrEmpty(targetIdPrefix) && target.TargetId.StartsWith(targetIdPrefix));
        }
    }
}
