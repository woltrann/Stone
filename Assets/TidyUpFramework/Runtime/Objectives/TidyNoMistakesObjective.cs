using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Limit Mistakes", fileName = "Objective_NoMistakes", order = 207)]
    public sealed class TidyNoMistakesObjective : TidyObjectiveDefinition
    {
        [SerializeField, Min(0)]
        int allowedMistakes;

        public override bool IsResolvedAtLevelEnd => true;

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            var failed = context.Mistakes > allowedMistakes;
            return new TidyObjectiveProgress(failed ? 0 : 1, 1, failed);
        }
    }
}
