using System;
using System.Collections.Generic;
using TidyUpFramework.Audio;
using TidyUpFramework.Hints;
using TidyUpFramework.Objectives;
using TidyUpFramework.Scoring;
using TidyUpFramework.Tutorial;
using UnityEngine;

namespace TidyUpFramework.Levels
{
    public enum TidyDifficulty
    {
        Relaxed,
        Easy,
        Normal,
        Hard,
        Expert
    }

    public enum TidyStarMetric
    {
        Score,
        TimeSeconds,
        Mistakes
    }

    [Serializable]
    public sealed class TidyObjectiveEntry
    {
        public TidyObjectiveDefinition Objective;
        [Tooltip("Optional objectives award bonus score but are not required to finish the level.")]
        public bool Optional;
    }

    [Serializable]
    public sealed class TidyUnlockRequirement
    {
        [Tooltip("Unlocked from the start, ignoring the other fields.")]
        public bool UnlockedByDefault;
        [Tooltip("Level that must be completed first.")]
        public TidyLevelDefinition RequiredLevel;
        [Min(0), Tooltip("Total stars collected across all levels.")]
        public int RequiredTotalStars;
    }

    [CreateAssetMenu(menuName = "Tidy Up Framework/Level Definition", fileName = "Level_New", order = 30)]
    public sealed class TidyLevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string levelId;
        [SerializeField] string displayName;
        [SerializeField, TextArea(2, 5)] string description;
        [SerializeField, Tooltip("Scene name as listed in the Build Profiles scene list.")] string sceneName;
        [SerializeField] Sprite thumbnail;
        [SerializeField] TidyDifficulty difficulty = TidyDifficulty.Normal;
        [SerializeField] Color accentColor = new Color(0.95f, 0.75f, 0.45f);

        [Header("Objectives")]
        [SerializeField] List<TidyObjectiveEntry> objectives = new List<TidyObjectiveEntry>();

        [Header("Time")]
        [SerializeField, Min(0f), Tooltip("Zero disables the time limit.")] float timeLimitSeconds;
        [SerializeField, Tooltip("Fail the level when the time limit runs out. Otherwise the timer is informative only.")] bool failWhenTimeRunsOut = true;

        [Header("Stars")]
        [SerializeField] TidyStarMetric starMetric = TidyStarMetric.Score;
        [SerializeField, Tooltip("Thresholds for two and three stars. Completion always awards one star. For time and mistakes lower is better.")] Vector2 starThresholds = new Vector2(500f, 1000f);

        [Header("Systems")]
        [SerializeField] TidyScoreDefinition scoreDefinition;
        [SerializeField] TidyComboDefinition comboDefinition;
        [SerializeField] TidyHintDefinition hintDefinition;
        [SerializeField] TidyTutorialDefinition tutorial;

        [Header("Audio")]
        [SerializeField] TidyAudioCue music;
        [SerializeField] TidyAudioCue ambience;

        [Header("Progression")]
        [SerializeField] TidyUnlockRequirement unlockRequirement = new TidyUnlockRequirement();
        [SerializeField, Min(0), Tooltip("Generic reward amount stored in the save data, for example coins.")] int rewardAmount;
        [SerializeField] string rewardDescription;

        public string LevelId => string.IsNullOrEmpty(levelId) ? name : levelId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public string SceneName => sceneName;
        public Sprite Thumbnail => thumbnail;
        public TidyDifficulty Difficulty => difficulty;
        public Color AccentColor => accentColor;
        public IReadOnlyList<TidyObjectiveEntry> Objectives => objectives;
        public float TimeLimitSeconds => timeLimitSeconds;
        public bool HasTimeLimit => timeLimitSeconds > 0f;
        public bool FailWhenTimeRunsOut => failWhenTimeRunsOut;
        public TidyStarMetric StarMetric => starMetric;
        public Vector2 StarThresholds => starThresholds;
        public TidyScoreDefinition ScoreDefinition => scoreDefinition;
        public TidyComboDefinition ComboDefinition => comboDefinition;
        public TidyHintDefinition HintDefinition => hintDefinition;
        public TidyTutorialDefinition Tutorial => tutorial;
        public TidyAudioCue Music => music;
        public TidyAudioCue Ambience => ambience;
        public TidyUnlockRequirement UnlockRequirement => unlockRequirement;
        public int RewardAmount => rewardAmount;
        public string RewardDescription => rewardDescription;

        public int CalculateStars(int score, float timeSeconds, int mistakes)
        {
            switch (starMetric)
            {
                case TidyStarMetric.TimeSeconds:
                    return 1 + (timeSeconds <= starThresholds.x ? 1 : 0) + (timeSeconds <= starThresholds.y ? 1 : 0);
                case TidyStarMetric.Mistakes:
                    return 1 + (mistakes <= starThresholds.x ? 1 : 0) + (mistakes <= starThresholds.y ? 1 : 0);
                default:
                    return 1 + (score >= starThresholds.x ? 1 : 0) + (score >= starThresholds.y ? 1 : 0);
            }
        }

        public void ConfigureIdentity(string newId, string newName, string newDescription, string newScene, TidyDifficulty newDifficulty, Color accent)
        {
            levelId = newId;
            displayName = newName;
            description = newDescription;
            sceneName = newScene;
            difficulty = newDifficulty;
            accentColor = accent;
        }

        public void ConfigureObjectives(IEnumerable<TidyObjectiveEntry> entries)
        {
            objectives = new List<TidyObjectiveEntry>(entries);
        }

        public void ConfigureTiming(float limit, bool failOnTimeout, TidyStarMetric metric, Vector2 thresholds)
        {
            timeLimitSeconds = limit;
            failWhenTimeRunsOut = failOnTimeout;
            starMetric = metric;
            starThresholds = thresholds;
        }

        public void ConfigureSystems(TidyScoreDefinition score, TidyComboDefinition combo, TidyHintDefinition hints, TidyTutorialDefinition tutorialDefinition)
        {
            scoreDefinition = score;
            comboDefinition = combo;
            hintDefinition = hints;
            tutorial = tutorialDefinition;
        }

        public void ConfigureAudio(TidyAudioCue musicCue, TidyAudioCue ambienceCue)
        {
            music = musicCue;
            ambience = ambienceCue;
        }

        public void ConfigureProgression(TidyUnlockRequirement requirement, int reward, string rewardText)
        {
            unlockRequirement = requirement ?? new TidyUnlockRequirement();
            rewardAmount = reward;
            rewardDescription = rewardText;
        }

        public void AssignThumbnail(Sprite sprite)
        {
            thumbnail = sprite;
        }
    }
}
