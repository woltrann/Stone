using TidyUpFramework.Levels;
using TidyUpFramework.Rules;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Clear Clutter", fileName = "Objective_ClearClutter", order = 202)]
    public sealed class TidyClearClutterObjective : TidyObjectiveDefinition
    {
        [SerializeField, Tooltip("Optional filter narrowing which clutter objects count.")]
        TidyMatchFilter clutterFilter = new TidyMatchFilter();

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            var total = 0;
            var resolved = 0;
            foreach (var tidyObject in context.Objects)
            {
                if (tidyObject == null || !tidyObject.CountsAsClutter || !clutterFilter.Matches(tidyObject))
                {
                    continue;
                }

                total++;
                if (tidyObject.IsResolvedClutter)
                {
                    resolved++;
                }
            }

            return new TidyObjectiveProgress(resolved, total);
        }
    }
}
