using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Scoring
{
    [Serializable]
    public sealed class TidyComboTier
    {
        [Min(1)] public int Threshold = 3;
        public string Label = "Nice!";
        public Color Color = Color.white;
        [Range(0f, 1f), Tooltip("Strength passed to feedback players for particles, sound pitch and camera punch.")]
        public float FeedbackIntensity = 0.5f;
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Combo Definition", fileName = "Combo_Default", order = 41)]
    public sealed class TidyComboDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Disable to remove the combo mechanic entirely.")]
        bool comboEnabled = true;

        [SerializeField, Min(0.1f), Tooltip("Seconds allowed between correct placements before the combo resets.")]
        float comboWindowSeconds = 4f;

        [SerializeField, Min(0f)]
        float multiplierPerStep = 0.1f;

        [SerializeField, Min(1f)]
        float maximumMultiplier = 3f;

        [SerializeField]
        bool breakOnMistake = true;

        [SerializeField]
        List<TidyComboTier> tiers = new List<TidyComboTier>
        {
            new TidyComboTier { Threshold = 3, Label = "Tidy!", Color = new Color(0.6f, 0.95f, 0.7f), FeedbackIntensity = 0.3f },
            new TidyComboTier { Threshold = 6, Label = "Neat!", Color = new Color(0.55f, 0.85f, 1f), FeedbackIntensity = 0.55f },
            new TidyComboTier { Threshold = 10, Label = "Spotless!", Color = new Color(1f, 0.85f, 0.45f), FeedbackIntensity = 0.8f },
            new TidyComboTier { Threshold = 15, Label = "Immaculate!", Color = new Color(1f, 0.6f, 0.9f), FeedbackIntensity = 1f }
        };

        public bool ComboEnabled => comboEnabled;
        public float ComboWindowSeconds => comboWindowSeconds;
        public float MultiplierPerStep => multiplierPerStep;
        public float MaximumMultiplier => maximumMultiplier;
        public bool BreakOnMistake => breakOnMistake;
        public IReadOnlyList<TidyComboTier> Tiers => tiers;
    }
}
