using System;
using UnityEngine;

namespace TidyUpFramework.Scoring
{
    public sealed class TidyComboSystem
    {
        readonly TidyComboDefinition definition;
        float timeRemaining;

        public event Action<TidyComboSystem> ComboChanged;
        public event Action<TidyComboTier> TierReached;
        public event Action<int> ComboBroken;

        public TidyComboSystem(TidyComboDefinition definition)
        {
            this.definition = definition;
        }

        public bool IsEnabled => definition != null && definition.ComboEnabled;
        public int Count { get; private set; }
        public int BestCount { get; private set; }
        public TidyComboTier CurrentTier { get; private set; }

        public float Multiplier
        {
            get
            {
                if (!IsEnabled || Count <= 1)
                {
                    return 1f;
                }

                return Mathf.Min(definition.MaximumMultiplier, 1f + (Count - 1) * definition.MultiplierPerStep);
            }
        }

        public float WindowRemaining01 => IsEnabled && Count > 0 ? Mathf.Clamp01(timeRemaining / definition.ComboWindowSeconds) : 0f;

        public void RegisterSuccess()
        {
            if (!IsEnabled)
            {
                return;
            }

            Count++;
            BestCount = Mathf.Max(BestCount, Count);
            timeRemaining = definition.ComboWindowSeconds;
            var reached = FindTierFor(Count);
            if (reached != null && reached != CurrentTier && reached.Threshold == Count)
            {
                CurrentTier = reached;
                TierReached?.Invoke(reached);
            }

            ComboChanged?.Invoke(this);
        }

        public void RestoreBest(int bestCount)
        {
            BestCount = Mathf.Max(BestCount, bestCount);
        }

        public void RegisterMistake()
        {
            if (IsEnabled && definition.BreakOnMistake)
            {
                Break();
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || Count == 0)
            {
                return;
            }

            timeRemaining -= deltaTime;
            if (timeRemaining <= 0f)
            {
                Break();
            }
        }

        public void Break()
        {
            if (Count == 0)
            {
                return;
            }

            var finalCount = Count;
            Count = 0;
            CurrentTier = null;
            timeRemaining = 0f;
            ComboBroken?.Invoke(finalCount);
            ComboChanged?.Invoke(this);
        }

        TidyComboTier FindTierFor(int count)
        {
            TidyComboTier best = null;
            foreach (var tier in definition.Tiers)
            {
                if (tier != null && count >= tier.Threshold && (best == null || tier.Threshold > best.Threshold))
                {
                    best = tier;
                }
            }

            return best;
        }
    }
}
