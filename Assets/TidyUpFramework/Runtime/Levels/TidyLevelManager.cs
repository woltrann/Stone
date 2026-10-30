using System;
using System.Collections.Generic;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Core;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objectives;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Player;
using TidyUpFramework.Save;
using TidyUpFramework.Scoring;
using UnityEngine;

namespace TidyUpFramework.Levels
{
    public enum TidyLevelState
    {
        NotStarted,
        Playing,
        Completing,
        Completed,
        Failed
    }

    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Levels/Level Manager")]
    public sealed class TidyLevelManager : MonoBehaviour
    {
        const float PeriodicRefreshInterval = 0.5f;

        [SerializeField, Tooltip("Level data. Without one the level completes when every counting target is complete.")]
        TidyLevelDefinition level;

        [SerializeField]
        bool startAutomatically = true;

        [SerializeField, Min(0f), Tooltip("Seconds between the final placement and the results screen, leaving time for the satisfying moment.")]
        float completionDelay = 1.6f;

        [Header("Saving")]
        [SerializeField, Tooltip("Saves mid-level progress so the player can leave and continue later.")]
        bool saveProgress = true;

        [SerializeField, Min(0f), Tooltip("Seconds between automatic saves while playing. 0 only saves on pause, quit and scene changes.")]
        float autosaveInterval = 30f;

        readonly List<TidyObjectiveTracker> trackers = new List<TidyObjectiveTracker>();
        readonly HashSet<TidyObject> scoredObjects = new HashSet<TidyObject>();
        TidyScoreDefinition scoreDefinition;
        bool isDirty;
        float periodicTimer;
        float completionTimer;
        TidyLevelSnapshot pendingRestore;
        float autosaveTimer;
        bool hasUnsavedChanges;
        bool progressDiscarded;
        TidyInteractor interactor;
        TidyFirstPersonController player;

        public event Action<TidyLevelManager> LevelStarted;
        public event Action<TidyLevelState> StateChanged;
        public event Action<float> ProgressChanged;
        public event Action<TidyObjectiveTracker> ObjectiveChanged;
        public event Action<TidyObjectiveTracker> ObjectiveCompleted;
        public event Action<TidyPlacementEvent> PlacementEvaluated;
        public event Action<TidyLevelResult> LevelCompleted;
        public event Action<TidyLevelResult> LevelFailed;
        public event Action AllRequiredObjectivesComplete;
        public event Action<TidyLevelSnapshot> ProgressSaved;
        public event Action<TidyLevelSnapshot> ProgressRestored;

        public TidyLevelDefinition Level => level;
        public TidyLevelContext Context { get; private set; }
        public TidyLevelState State { get; private set; } = TidyLevelState.NotStarted;
        public IReadOnlyList<TidyObjectiveTracker> Trackers => trackers;
        public float Progress { get; private set; }
        public TidyLevelResult LastResult { get; private set; }
        public TidyScoreSystem Score => Context?.Score;
        public TidyComboSystem Combo => Context?.Combo;
        public bool IsPlaying => State == TidyLevelState.Playing;
        public string LevelId => level != null ? level.LevelId : gameObject.scene.name;
        public bool SavesProgress => saveProgress;
        public bool IsRestoring { get; private set; }
        public bool HasPendingRestore => pendingRestore != null;
        public bool WasRestored { get; private set; }

        public float TimeRemaining
        {
            get
            {
                if (level == null || !level.HasTimeLimit || Context == null)
                {
                    return -1f;
                }

                return Mathf.Max(0f, level.TimeLimitSeconds - Context.ElapsedSeconds);
            }
        }

        public void AssignLevel(TidyLevelDefinition newLevel)
        {
            if (State == TidyLevelState.NotStarted)
            {
                level = newLevel;
            }
        }

        public void StartLevel()
        {
            if (State != TidyLevelState.NotStarted)
            {
                return;
            }

            scoreDefinition = level != null && level.ScoreDefinition != null ? level.ScoreDefinition : CreateRuntimeDefault<TidyScoreDefinition>();
            var comboDefinition = level != null && level.ComboDefinition != null ? level.ComboDefinition : CreateRuntimeDefault<TidyComboDefinition>();
            Context = new TidyLevelContext(level, new TidyScoreSystem(), new TidyComboSystem(comboDefinition));
            BuildTrackers();
            Subscribe();
            SetState(TidyLevelState.Playing);
            RefreshNow();
            PlayLevelAudio();
            autosaveTimer = autosaveInterval;
            if (saveProgress && TidyServices.TryGet<TidySaveService>(out var save) && save.TryLoadLevelSnapshot(LevelId, out var snapshot))
            {
                pendingRestore = snapshot;
            }

            LevelStarted?.Invoke(this);
        }

        public bool SaveProgress()
        {
            if (!saveProgress || progressDiscarded || IsRestoring || pendingRestore != null || State != TidyLevelState.Playing || Context == null)
            {
                return false;
            }

            if (!TidyServices.TryGet<TidySaveService>(out var save))
            {
                return false;
            }

            ResolveRuntimeReferences();
            var snapshot = new TidyLevelSnapshot
            {
                LevelId = LevelId,
                SceneName = gameObject.scene.name,
                SavedAtUtc = TidyLevelSnapshotter.UtcNow(),
                Progress = Progress,
                ElapsedSeconds = Context.ElapsedSeconds,
                Score = Context.Score.Score,
                CorrectPlacements = Context.CorrectPlacements,
                Mistakes = Context.Mistakes,
                HintsUsed = Context.HintsUsed,
                CleanedSurfaces = Context.CleanedSurfaces,
                DisposedClutter = Context.DisposedClutter,
                BestCombo = Context.Combo.BestCount
            };

            TidyLevelSnapshotter.CaptureWorld(snapshot, interactor, player);
            var ids = TidyLevelSnapshotter.BuildIds();
            foreach (var scored in scoredObjects)
            {
                if (scored != null && ids.TryGetValue(scored, out var id))
                {
                    snapshot.ScoredObjects.Add(id);
                }
            }

            save.SaveLevelSnapshot(snapshot);
            hasUnsavedChanges = false;
            autosaveTimer = autosaveInterval;
            ProgressSaved?.Invoke(snapshot);
            return true;
        }

        public void MarkCustomStateDirty()
        {
            if(!IsRestoring && State==TidyLevelState.Playing)hasUnsavedChanges=true;
        }

        public void DiscardSavedProgress()
        {
            progressDiscarded = true;
            pendingRestore = null;
            if (TidyServices.TryGet<TidySaveService>(out var save))
            {
                save.DeleteLevelSnapshot(LevelId);
            }
        }

        public void RegisterHintUsed(int scoreCost)
        {
            if (Context == null)
            {
                return;
            }

            Context.HintsUsed++;
            if (scoreCost > 0)
            {
                Context.Score.Add(-scoreCost, TidyScoreReason.Hint);
            }
        }

        public void FailLevel()
        {
            if (State != TidyLevelState.Playing)
            {
                return;
            }

            LastResult = BuildResult(TidyLevelOutcome.Failed);
            DiscardSavedProgress();
            SetState(TidyLevelState.Failed);
            LevelFailed?.Invoke(LastResult);
        }

        public void ForceComplete()
        {
            if (State == TidyLevelState.Playing)
            {
                BeginCompletion();
            }
        }

        void Awake()
        {
            TidyServices.Register(this);
        }

        void Start()
        {
            if (startAutomatically)
            {
                StartLevel();
            }
        }

        void OnDestroy()
        {
            Unsubscribe();
            TidyServices.Unregister(this);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveProgress();
            }
        }

        void OnApplicationQuit()
        {
            SaveProgress();
        }

        void Update()
        {
            if (pendingRestore != null && State == TidyLevelState.Playing)
            {
                ApplyRestore();
            }

            if (State == TidyLevelState.Playing && saveProgress && autosaveInterval > 0f && hasUnsavedChanges)
            {
                autosaveTimer -= Time.unscaledDeltaTime;
                if (autosaveTimer <= 0f)
                {
                    SaveProgress();
                }
            }

            if (State == TidyLevelState.Completing)
            {
                completionTimer -= Time.unscaledDeltaTime;
                if (completionTimer <= 0f)
                {
                    FinishCompletion();
                }

                return;
            }

            if (State != TidyLevelState.Playing)
            {
                return;
            }

            Context.ElapsedSeconds += Time.deltaTime;
            Context.Combo.Tick(Time.deltaTime);
            periodicTimer -= Time.deltaTime;
            if (periodicTimer <= 0f)
            {
                periodicTimer = PeriodicRefreshInterval;
                isDirty = true;
            }

            if (level != null && level.HasTimeLimit && level.FailWhenTimeRunsOut && TimeRemaining <= 0f)
            {
                FailLevel();
            }
        }

        void LateUpdate()
        {
            if (isDirty && State == TidyLevelState.Playing)
            {
                RefreshNow();
            }
        }

        void ApplyRestore()
        {
            var snapshot = pendingRestore;
            pendingRestore = null;
            ResolveRuntimeReferences();
            IsRestoring = true;
            try
            {
                TidyLevelSnapshotter.RestoreWorld(snapshot, interactor, player);
                Context.ElapsedSeconds = snapshot.ElapsedSeconds;
                Context.CorrectPlacements = snapshot.CorrectPlacements;
                Context.Mistakes = snapshot.Mistakes;
                Context.HintsUsed = snapshot.HintsUsed;
                Context.CleanedSurfaces = snapshot.CleanedSurfaces;
                Context.DisposedClutter = snapshot.DisposedClutter;
                Context.Score.Restore(snapshot.Score);
                Context.Combo.RestoreBest(snapshot.BestCombo);
                scoredObjects.Clear();
                var scoredIds = new HashSet<string>(snapshot.ScoredObjects);
                foreach (var pair in TidyLevelSnapshotter.BuildIds())
                {
                    if (scoredIds.Contains(pair.Value))
                    {
                        scoredObjects.Add(pair.Key);
                    }
                }

                RefreshNow();
                WasRestored = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Tidy Up Framework: saved progress for '" + LevelId + "' could not be restored. " + exception.Message);
            }
            finally
            {
                IsRestoring = false;
            }

            hasUnsavedChanges = false;
            autosaveTimer = autosaveInterval;
            if (WasRestored)
            {
                ProgressRestored?.Invoke(snapshot);
            }
        }

        void ResolveRuntimeReferences()
        {
            if (interactor == null)
            {
                TidyServices.TryGet(out interactor);
            }

            if (player == null)
            {
                TidyServices.TryGet(out player);
            }
        }

        void BuildTrackers()
        {
            trackers.Clear();
            if (level == null)
            {
                return;
            }

            foreach (var entry in level.Objectives)
            {
                if (entry == null || entry.Objective == null)
                {
                    continue;
                }

                var tracker = new TidyObjectiveTracker(entry.Objective, entry.Optional);
                tracker.Changed += HandleTrackerChanged;
                tracker.CompletedFirstTime += HandleTrackerCompleted;
                trackers.Add(tracker);
            }
        }

        void Subscribe()
        {
            TidyTargetRegistry.AnyObjectPlaced += HandleObjectPlaced;
            TidyTargetRegistry.AnyObjectRemoved += HandleObjectRemoved;
            TidyTargetRegistry.AnyCompletionChanged += HandleTargetCompletionChanged;
            TidyObjectRegistry.AnyStateChanged += HandleObjectStateChanged;
            TidyCleanable.AnyCleaned += HandleCleaned;
            if (Settings.TidyFramework.SceneLoader != null)
            {
                Settings.TidyFramework.SceneLoader.LoadStarted += HandleSceneLoadStarted;
            }
        }

        void Unsubscribe()
        {
            TidyTargetRegistry.AnyObjectPlaced -= HandleObjectPlaced;
            TidyTargetRegistry.AnyObjectRemoved -= HandleObjectRemoved;
            TidyTargetRegistry.AnyCompletionChanged -= HandleTargetCompletionChanged;
            TidyObjectRegistry.AnyStateChanged -= HandleObjectStateChanged;
            TidyCleanable.AnyCleaned -= HandleCleaned;
            if (Settings.TidyFramework.SceneLoader != null)
            {
                Settings.TidyFramework.SceneLoader.LoadStarted -= HandleSceneLoadStarted;
            }
        }

        void HandleObjectPlaced(TidyPlacementEvent placement)
        {
            if (State != TidyLevelState.Playing || placement.Object == null || IsRestoring)
            {
                return;
            }

            hasUnsavedChanges = true;

            var position = placement.Object.transform.position + Vector3.up * placement.Object.ScaledSize.y;
            if (placement.IsCorrect)
            {
                Context.CorrectPlacements++;
                Context.Combo.RegisterSuccess();
                if (placement.Object.CountsAsClutter)
                {
                    Context.DisposedClutter++;
                }

                if (scoredObjects.Add(placement.Object) && scoreDefinition.ScoreCorrectPlacements)
                {
                    var multiplier = scoreDefinition.PlacementMultiplier * (scoreDefinition.ApplyComboMultiplier ? Context.Combo.Multiplier : 1f);
                    var reason = placement.Object.CountsAsClutter ? TidyScoreReason.Clutter : TidyScoreReason.CorrectPlacement;
                    Context.Score.Add(Mathf.RoundToInt(placement.Object.BaseScore * multiplier), reason, position);
                }
            }
            else
            {
                Context.Mistakes++;
                Context.Combo.RegisterMistake();
                if (scoreDefinition.PenalizeIncorrectPlacements)
                {
                    Context.Score.Add(-scoreDefinition.IncorrectPlacementPenalty, TidyScoreReason.IncorrectPlacement, position);
                }
            }

            PlacementEvaluated?.Invoke(placement);
            isDirty = true;
        }

        void HandleObjectRemoved(TidyPlacementEvent removal)
        {
            isDirty = true;
            hasUnsavedChanges = true;
        }

        void HandleSceneLoadStarted(string sceneName)
        {
            SaveProgress();
        }

        void HandleTargetCompletionChanged(TidyPlacementTarget target, bool complete)
        {
            isDirty = true;
        }

        void HandleObjectStateChanged(TidyObject tidyObject)
        {
            if (State != TidyLevelState.Playing || IsRestoring)
            {
                return;
            }

            hasUnsavedChanges = true;

            if (tidyObject.State == TidyObjectState.Consumed && tidyObject.CurrentTarget == null && tidyObject.CountsAsClutter)
            {
                Context.DisposedClutter++;
                if (scoreDefinition.ScoreCleaning && scoredObjects.Add(tidyObject))
                {
                    Context.Score.Add(scoreDefinition.PointsPerDisposedClutter, TidyScoreReason.Clutter, tidyObject.transform.position);
                }
            }

            if (tidyObject.State == TidyObjectState.Held || tidyObject.State == TidyObjectState.Consumed || tidyObject.State == TidyObjectState.Loose)
            {
                isDirty = true;
            }
        }

        void HandleCleaned(TidyCleanable cleanable)
        {
            if (State != TidyLevelState.Playing || IsRestoring)
            {
                return;
            }

            hasUnsavedChanges = true;

            Context.CleanedSurfaces++;
            Context.Combo.RegisterSuccess();
            if (scoreDefinition.ScoreCleaning)
            {
                var amount = Mathf.RoundToInt(scoreDefinition.PointsPerCleanedSurface * (scoreDefinition.ApplyComboMultiplier ? Context.Combo.Multiplier : 1f));
                Context.Score.Add(amount, TidyScoreReason.Cleaning, cleanable.transform.position);
            }

            isDirty = true;
        }

        void HandleTrackerChanged(TidyObjectiveTracker tracker)
        {
            ObjectiveChanged?.Invoke(tracker);
        }

        void HandleTrackerCompleted(TidyObjectiveTracker tracker)
        {
            if (IsRestoring)
            {
                return;
            }

            if (scoreDefinition.AwardObjectiveBonuses && tracker.Definition.BonusScore > 0 && State == TidyLevelState.Playing)
            {
                Context.Score.Add(tracker.Definition.BonusScore, TidyScoreReason.Objective);
            }

            ObjectiveCompleted?.Invoke(tracker);
        }

        void RefreshNow()
        {
            isDirty = false;
            foreach (var tracker in trackers)
            {
                tracker.Refresh(Context);
            }

            UpdateProgress();
            if (State == TidyLevelState.Playing && AreRequirementsMet())
            {
                AllRequiredObjectivesComplete?.Invoke();
                BeginCompletion();
            }
        }

        void UpdateProgress()
        {
            var next = CalculateProgress();
            if (Mathf.Approximately(next, Progress))
            {
                return;
            }

            Progress = next;
            ProgressChanged?.Invoke(Progress);
        }

        float CalculateProgress()
        {
            var requiredCount = 0;
            var ratioSum = 0f;
            foreach (var tracker in trackers)
            {
                if (tracker.IsOptional || tracker.Definition.IsResolvedAtLevelEnd)
                {
                    continue;
                }

                requiredCount++;
                ratioSum += tracker.Progress.Ratio;
            }

            if (requiredCount > 0)
            {
                return ratioSum / requiredCount;
            }

            return TargetCompletionRatio();
        }

        float TargetCompletionRatio()
        {
            var counted = 0;
            var ratioSum = 0f;
            foreach (var target in TidyTargetRegistry.All)
            {
                if (target == null || !target.CountsTowardsProgress || target.CompletionMode == TidyCompletionMode.Never)
                {
                    continue;
                }

                counted++;
                ratioSum += target.IsComplete ? 1f : target.CompletionRatio;
            }

            return counted == 0 ? 0f : ratioSum / counted;
        }

        bool AreRequirementsMet()
        {
            var hasRequired = false;
            foreach (var tracker in trackers)
            {
                if (tracker.IsOptional)
                {
                    continue;
                }

                hasRequired = true;
                if (!tracker.IsComplete)
                {
                    return false;
                }
            }

            if (hasRequired)
            {
                return true;
            }

            return TidyTargetRegistry.All.Count > 0 && TargetCompletionRatio() >= 1f;
        }

        void BeginCompletion()
        {
            completionTimer = completionDelay;
            Context.Combo.Break();
            SetState(TidyLevelState.Completing);
        }

        void FinishCompletion()
        {
            foreach (var tracker in trackers)
            {
                tracker.ResolveAtLevelEnd(Context);
            }

            LastResult = BuildResult(TidyLevelOutcome.Completed);
            if (TidyServices.TryGet<ITidyProgressService>(out var progressService))
            {
                LastResult.IsNewBestScore = LastResult.Score > progressService.GetBestScore(LastResult.LevelId);
                var bestTime = progressService.GetBestTime(LastResult.LevelId);
                LastResult.IsNewBestTime = bestTime <= 0f || LastResult.TimeSeconds < bestTime;
                progressService.RecordResult(LastResult);
            }

            DiscardSavedProgress();
            SetState(TidyLevelState.Completed);
            LevelCompleted?.Invoke(LastResult);
        }

        TidyLevelResult BuildResult(TidyLevelOutcome outcome)
        {
            var result = new TidyLevelResult
            {
                LevelId = level != null ? level.LevelId : gameObject.scene.name,
                Outcome = outcome,
                TimeSeconds = Context.ElapsedSeconds,
                Mistakes = Context.Mistakes,
                HintsUsed = Context.HintsUsed,
                BestCombo = Context.Combo.BestCount,
                Accuracy = Context.Accuracy
            };

            result.ScoreBreakdown.Add(new TidyScoreLine("Organising", Context.Score.Score));
            if (outcome == TidyLevelOutcome.Completed)
            {
                AwardCompletionBonuses(result);
            }

            foreach (var tracker in trackers)
            {
                if (!tracker.IsOptional)
                {
                    continue;
                }

                result.OptionalObjectivesTotal++;
                if (tracker.IsComplete)
                {
                    result.OptionalObjectivesCompleted++;
                }
            }

            result.Score = Context.Score.Score;
            result.Stars = outcome == TidyLevelOutcome.Completed
                ? (level != null ? level.CalculateStars(result.Score, result.TimeSeconds, result.Mistakes) : 3)
                : 0;
            return result;
        }

        void AwardCompletionBonuses(TidyLevelResult result)
        {
            AddBonus(result, scoreDefinition.AwardLevelCompleteBonus, "Level complete", scoreDefinition.LevelCompleteBonus, TidyScoreReason.LevelComplete);
            var secondsUnderPar = Mathf.Max(0f, scoreDefinition.ParTimeSeconds - Context.ElapsedSeconds);
            AddBonus(result, scoreDefinition.AwardSpeedBonus, "Speed", Mathf.RoundToInt(secondsUnderPar * scoreDefinition.SpeedBonusPerSecond), TidyScoreReason.Speed);
            AddBonus(result, scoreDefinition.AwardNoMistakeBonus && Context.Mistakes == 0, "No mistakes", scoreDefinition.NoMistakeBonus, TidyScoreReason.NoMistakes);
            AddBonus(result, scoreDefinition.AwardAccuracyBonus, "Accuracy", Mathf.RoundToInt(scoreDefinition.AccuracyBonus * Context.Accuracy), TidyScoreReason.Accuracy);
        }

        void AddBonus(TidyLevelResult result, bool enabledBonus, string label, int amount, TidyScoreReason reason)
        {
            if (!enabledBonus || amount <= 0)
            {
                return;
            }

            Context.Score.Add(amount, reason);
            result.ScoreBreakdown.Add(new TidyScoreLine(label, amount));
        }

        void PlayLevelAudio()
        {
            if (level == null || !TidyServices.TryGet<Audio.ITidyAudioService>(out var audioService))
            {
                return;
            }

            if (level.Music != null)
            {
                audioService.PlayMusic(level.Music);
            }

            if (level.Ambience != null)
            {
                audioService.PlayAmbience(level.Ambience);
            }
        }

        void SetState(TidyLevelState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateChanged?.Invoke(next);
        }

        static T CreateRuntimeDefault<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            instance.hideFlags = HideFlags.DontSave;
            return instance;
        }
    }
}
