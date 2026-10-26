using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Clean Surfaces", fileName = "Objective_Clean", order = 203)]
    public sealed class TidyCleanSurfacesObjective : TidyObjectiveDefinition
    {
        [SerializeField, Tooltip("Only cleanables with this group id count. Empty counts all.")]
        string groupId;

        [SerializeField, Min(0), Tooltip("Zero requires every matching surface.")]
        int requiredCount;

        public void Configure(string newGroupId, int count)
        {
            groupId = newGroupId;
            requiredCount = Mathf.Max(0, count);
        }

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            var total = 0;
            var clean = 0;
            foreach (var cleanable in context.Cleanables)
            {
                if (cleanable == null || (!string.IsNullOrEmpty(groupId) && cleanable.GroupId != groupId))
                {
                    continue;
                }

                total++;
                if (cleanable.IsClean)
                {
                    clean++;
                }
            }

            var target = requiredCount > 0 ? Mathf.Min(requiredCount, total) : total;
            return new TidyObjectiveProgress(Mathf.Min(clean, target), target);
        }
    }
}
