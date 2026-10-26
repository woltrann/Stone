using UnityEngine;

namespace TidyUpFramework.Scoring
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Score Definition", fileName = "Score_Default", order = 40)]
    public sealed class TidyScoreDefinition : ScriptableObject
    {
        [Header("Placement")]
        [SerializeField] bool scoreCorrectPlacements = true;
        [SerializeField, Min(0f), Tooltip("Multiplies the base score of each object definition.")] float placementMultiplier = 1f;
        [SerializeField] bool penalizeIncorrectPlacements = true;
        [SerializeField, Min(0)] int incorrectPlacementPenalty = 5;
        [SerializeField] bool applyComboMultiplier = true;

        [Header("Cleaning")]
        [SerializeField] bool scoreCleaning = true;
        [SerializeField, Min(0)] int pointsPerCleanedSurface = 15;
        [SerializeField, Min(0)] int pointsPerDisposedClutter = 5;

        [Header("Completion Bonuses")]
        [SerializeField] bool awardObjectiveBonuses = true;
        [SerializeField] bool awardLevelCompleteBonus = true;
        [SerializeField, Min(0)] int levelCompleteBonus = 250;
        [SerializeField] bool awardSpeedBonus = true;
        [SerializeField, Min(0f), Tooltip("Speed bonus is awarded for every second below this time.")] float parTimeSeconds = 300f;
        [SerializeField, Min(0)] int speedBonusPerSecond = 2;
        [SerializeField] bool awardNoMistakeBonus = true;
        [SerializeField, Min(0)] int noMistakeBonus = 150;
        [SerializeField] bool awardAccuracyBonus = true;
        [SerializeField, Min(0), Tooltip("Awarded in full at 100% accuracy and scaled down linearly.")] int accuracyBonus = 100;
        [SerializeField, Min(0)] int hintPenalty;

        public bool ScoreCorrectPlacements => scoreCorrectPlacements;
        public float PlacementMultiplier => placementMultiplier;
        public bool PenalizeIncorrectPlacements => penalizeIncorrectPlacements;
        public int IncorrectPlacementPenalty => incorrectPlacementPenalty;
        public bool ApplyComboMultiplier => applyComboMultiplier;
        public bool ScoreCleaning => scoreCleaning;
        public int PointsPerCleanedSurface => pointsPerCleanedSurface;
        public int PointsPerDisposedClutter => pointsPerDisposedClutter;
        public bool AwardObjectiveBonuses => awardObjectiveBonuses;
        public bool AwardLevelCompleteBonus => awardLevelCompleteBonus;
        public int LevelCompleteBonus => levelCompleteBonus;
        public bool AwardSpeedBonus => awardSpeedBonus;
        public float ParTimeSeconds => parTimeSeconds;
        public int SpeedBonusPerSecond => speedBonusPerSecond;
        public bool AwardNoMistakeBonus => awardNoMistakeBonus;
        public int NoMistakeBonus => noMistakeBonus;
        public bool AwardAccuracyBonus => awardAccuracyBonus;
        public int AccuracyBonus => accuracyBonus;
        public int HintPenalty => hintPenalty;

        public void ConfigureCompletion(int completeBonus, float parTime, int perSecond)
        {
            levelCompleteBonus = completeBonus;
            parTimeSeconds = parTime;
            speedBonusPerSecond = perSecond;
        }
    }
}
