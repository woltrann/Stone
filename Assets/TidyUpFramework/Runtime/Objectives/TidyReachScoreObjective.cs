using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Reach Score", fileName = "Objective_Score", order = 205)]
    public sealed class TidyReachScoreObjective : TidyObjectiveDefinition
    {
        [SerializeField, Min(1)]
        int targetScore = 1000;

        public void Configure(int score)
        {
            targetScore = Mathf.Max(1, score);
        }

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            return new TidyObjectiveProgress(Mathf.Min(context.Score.Score, targetScore), targetScore);
        }
    }
}
