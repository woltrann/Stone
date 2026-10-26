using UnityEngine;

namespace TidyUpFramework.Hints
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Hint Definition", fileName = "Hints_Default", order = 42)]
    public sealed class TidyHintDefinition : ScriptableObject
    {
        [SerializeField]
        bool hintsEnabled = true;

        [SerializeField, Tooltip("Negative values allow unlimited hints.")]
        int maximumHints = -1;

        [SerializeField, Min(0), Tooltip("Hints that cost nothing.")]
        int freeHints = 3;

        [SerializeField, Min(0), Tooltip("Score deducted for each hint after the free ones.")]
        int scoreCost = 25;

        [SerializeField, Min(0f)]
        float cooldownSeconds = 8f;

        [SerializeField, Min(0.5f), Tooltip("How long a hint stays visible.")]
        float durationSeconds = 5f;

        [Header("Presentation")]
        [SerializeField] bool highlightObject = true;
        [SerializeField] bool highlightDestination = true;
        [SerializeField] bool showArrow = true;

        [SerializeField, Min(0f), Tooltip("Shows a free hint automatically after this many idle seconds. Zero disables.")]
        float autoHintAfterIdleSeconds;

        public bool HintsEnabled => hintsEnabled;
        public int MaximumHints => maximumHints;
        public int FreeHints => freeHints;
        public int ScoreCost => scoreCost;
        public float CooldownSeconds => cooldownSeconds;
        public float DurationSeconds => durationSeconds;
        public bool HighlightObject => highlightObject;
        public bool HighlightDestination => highlightDestination;
        public bool ShowArrow => showArrow;
        public float AutoHintAfterIdleSeconds => autoHintAfterIdleSeconds;

        public void Configure(bool enabledState, int maximum, int free, int cost, float cooldown)
        {
            hintsEnabled = enabledState;
            maximumHints = maximum;
            freeHints = free;
            scoreCost = cost;
            cooldownSeconds = cooldown;
        }
    }
}
