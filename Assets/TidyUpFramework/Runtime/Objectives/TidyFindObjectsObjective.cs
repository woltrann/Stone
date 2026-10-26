using TidyUpFramework.Levels;
using TidyUpFramework.Rules;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Find Objects", fileName = "Objective_Find", order = 204)]
    public sealed class TidyFindObjectsObjective : TidyObjectiveDefinition
    {
        [SerializeField, Tooltip("Optional filter. Objects must also be flagged 'Requires Discovery'.")]
        TidyMatchFilter objectFilter = new TidyMatchFilter();

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            var total = 0;
            var found = 0;
            foreach (var tidyObject in context.Objects)
            {
                if (tidyObject == null || !tidyObject.RequiresDiscovery || !objectFilter.Matches(tidyObject))
                {
                    continue;
                }

                total++;
                if (tidyObject.HasBeenPickedUp)
                {
                    found++;
                }
            }

            return new TidyObjectiveProgress(found, total);
        }
    }
}
