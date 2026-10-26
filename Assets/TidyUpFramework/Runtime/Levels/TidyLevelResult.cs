using System.Collections.Generic;

namespace TidyUpFramework.Levels
{
    public enum TidyLevelOutcome
    {
        Completed,
        Failed
    }

    public sealed class TidyLevelResult
    {
        public string LevelId;
        public TidyLevelOutcome Outcome;
        public int Score;
        public int Stars;
        public float TimeSeconds;
        public int Mistakes;
        public int HintsUsed;
        public int BestCombo;
        public float Accuracy;
        public int OptionalObjectivesCompleted;
        public int OptionalObjectivesTotal;
        public bool IsNewBestScore;
        public bool IsNewBestTime;
        public readonly List<TidyScoreLine> ScoreBreakdown = new List<TidyScoreLine>();

        public bool IsCompleted => Outcome == TidyLevelOutcome.Completed;
    }

    public readonly struct TidyScoreLine
    {
        public readonly string Label;
        public readonly int Amount;

        public TidyScoreLine(string label, int amount)
        {
            Label = label;
            Amount = amount;
        }
    }
}
