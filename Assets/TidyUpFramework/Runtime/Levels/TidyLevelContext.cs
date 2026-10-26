using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Scoring;

namespace TidyUpFramework.Levels
{
    public sealed class TidyLevelContext
    {
        public TidyLevelContext(TidyLevelDefinition level, TidyScoreSystem score, TidyComboSystem combo)
        {
            Level = level;
            Score = score;
            Combo = combo;
        }

        public TidyLevelDefinition Level { get; }
        public TidyScoreSystem Score { get; }
        public TidyComboSystem Combo { get; }
        public float ElapsedSeconds { get; internal set; }
        public int CorrectPlacements { get; internal set; }
        public int Mistakes { get; internal set; }
        public int HintsUsed { get; internal set; }
        public int CleanedSurfaces { get; internal set; }
        public int DisposedClutter { get; internal set; }

        public IReadOnlyList<TidyObject> Objects => TidyObjectRegistry.All;
        public IReadOnlyList<TidyPlacementTarget> Targets => TidyTargetRegistry.All;
        public IReadOnlyList<TidyCleanable> Cleanables => TidyCleanable.All;

        public float Accuracy
        {
            get
            {
                var attempts = CorrectPlacements + Mistakes;
                return attempts == 0 ? 1f : (float)CorrectPlacements / attempts;
            }
        }
    }
}
