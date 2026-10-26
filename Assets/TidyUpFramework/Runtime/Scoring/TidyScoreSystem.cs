using System;
using UnityEngine;

namespace TidyUpFramework.Scoring
{
    public enum TidyScoreReason
    {
        CorrectPlacement,
        IncorrectPlacement,
        Cleaning,
        Clutter,
        Objective,
        LevelComplete,
        Speed,
        NoMistakes,
        Accuracy,
        Hint,
        Custom
    }

    public readonly struct TidyScoreAward
    {
        public readonly int Amount;
        public readonly TidyScoreReason Reason;
        public readonly Vector3 WorldPosition;
        public readonly bool HasWorldPosition;

        public TidyScoreAward(int amount, TidyScoreReason reason, Vector3 worldPosition, bool hasWorldPosition)
        {
            Amount = amount;
            Reason = reason;
            WorldPosition = worldPosition;
            HasWorldPosition = hasWorldPosition;
        }
    }

    public sealed class TidyScoreSystem
    {
        public event Action<int> ScoreChanged;
        public event Action<TidyScoreAward> ScoreAwarded;

        public int Score { get; private set; }

        public void Add(int amount, TidyScoreReason reason)
        {
            Apply(new TidyScoreAward(amount, reason, Vector3.zero, false));
        }

        public void Add(int amount, TidyScoreReason reason, Vector3 worldPosition)
        {
            Apply(new TidyScoreAward(amount, reason, worldPosition, true));
        }

        public void Reset()
        {
            Score = 0;
            ScoreChanged?.Invoke(Score);
        }

        public void Restore(int score)
        {
            Score = Mathf.Max(0, score);
            ScoreChanged?.Invoke(Score);
        }

        void Apply(TidyScoreAward award)
        {
            if (award.Amount == 0)
            {
                return;
            }

            Score = Mathf.Max(0, Score + award.Amount);
            ScoreAwarded?.Invoke(award);
            ScoreChanged?.Invoke(Score);
        }
    }
}
