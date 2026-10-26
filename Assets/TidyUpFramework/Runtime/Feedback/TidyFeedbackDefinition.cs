using System;
using TidyUpFramework.Audio;
using UnityEngine;

namespace TidyUpFramework.Feedback
{
    [Serializable]
    public sealed class TidyFeedbackEffect
    {
        public TidyAudioCue Sound;
        [Tooltip("Particle prefab spawned from a pool at the event position.")]
        public ParticleSystem Particles;
        [Min(0f)] public float CameraPunch;
        [Tooltip("Raise pitch with combo intensity.")]
        public bool PitchFollowsCombo;
        [Min(0f)] public float ComboPitchRange = 0.35f;
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Feedback Definition", fileName = "Feedback_Default", order = 45)]
    public sealed class TidyFeedbackDefinition : ScriptableObject
    {
        [SerializeField] TidyFeedbackEffect pickUp = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect drop = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect placeCorrect = new TidyFeedbackEffect { PitchFollowsCombo = true };
        [SerializeField] TidyFeedbackEffect placeIncorrect = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect rejected = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect targetComplete = new TidyFeedbackEffect { CameraPunch = 0.3f };
        [SerializeField] TidyFeedbackEffect comboTier = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect objectiveComplete = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect levelComplete = new TidyFeedbackEffect { CameraPunch = 0.5f };
        [SerializeField] TidyFeedbackEffect cleaned = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect hint = new TidyFeedbackEffect();
        [SerializeField] TidyFeedbackEffect rotate = new TidyFeedbackEffect();

        [Header("Floating Score")]
        [SerializeField] bool showFloatingScore = true;
        [SerializeField] Color positiveScoreColor = new Color(1f, 0.93f, 0.55f);
        [SerializeField] Color negativeScoreColor = new Color(1f, 0.45f, 0.4f);

        public TidyFeedbackEffect PickUp => pickUp;
        public TidyFeedbackEffect Drop => drop;
        public TidyFeedbackEffect PlaceCorrect => placeCorrect;
        public TidyFeedbackEffect PlaceIncorrect => placeIncorrect;
        public TidyFeedbackEffect Rejected => rejected;
        public TidyFeedbackEffect TargetComplete => targetComplete;
        public TidyFeedbackEffect ComboTier => comboTier;
        public TidyFeedbackEffect ObjectiveComplete => objectiveComplete;
        public TidyFeedbackEffect LevelComplete => levelComplete;
        public TidyFeedbackEffect Cleaned => cleaned;
        public TidyFeedbackEffect Hint => hint;
        public TidyFeedbackEffect Rotate => rotate;
        public bool ShowFloatingScore => showFloatingScore;
        public Color PositiveScoreColor => positiveScoreColor;
        public Color NegativeScoreColor => negativeScoreColor;
    }
}
