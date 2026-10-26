using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Objectives
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Objectives/Finish Within Time", fileName = "Objective_Time", order = 206)]
    public sealed class TidyTimeLimitObjective : TidyObjectiveDefinition
    {
        [SerializeField, Min(1f)]
        float timeLimitSeconds = 180f;

        public float TimeLimitSeconds => timeLimitSeconds;
        public override bool IsResolvedAtLevelEnd => true;

        public void Configure(float seconds)
        {
            timeLimitSeconds = Mathf.Max(1f, seconds);
        }

        public override TidyObjectiveProgress Evaluate(TidyLevelContext context)
        {
            var failed = context.ElapsedSeconds > timeLimitSeconds;
            return new TidyObjectiveProgress(failed ? 0 : 1, 1, failed);
        }

        protected override string Format(string template, TidyObjectiveProgress progress)
        {
            var minutes = Mathf.FloorToInt(timeLimitSeconds / 60f);
            var seconds = Mathf.FloorToInt(timeLimitSeconds % 60f);
            return base.Format(template, progress).Replace("{limit}", string.Format("{0}:{1:00}", minutes, seconds));
        }
    }
}
