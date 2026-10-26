using System;
using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.Save
{
    public sealed class TidySaveService : ITidyProgressService
    {
        const string ProgressKey = "progress";
        const string SettingsKey = "settings";
        const string LevelSnapshotPrefix = "level_";

        readonly ITidySaveStorage storage;
        readonly TidySettingsData defaultSettings;

        public TidySaveService(ITidySaveStorage storage, TidySettingsData defaultSettings)
        {
            this.storage = storage;
            this.defaultSettings = defaultSettings ?? new TidySettingsData();
            Progress = Load<TidyProgressData>(ProgressKey) ?? new TidyProgressData();
            Settings = Load<TidySettingsData>(SettingsKey) ?? this.defaultSettings.Clone();
        }

        public event Action<TidyProgressData> ProgressChanged;
        public event Action<TidySettingsData> SettingsSaved;

        public TidyProgressData Progress { get; private set; }
        public TidySettingsData Settings { get; private set; }
        public ITidySaveStorage Storage => storage;

        public int TotalStars
        {
            get
            {
                var total = 0;
                foreach (var record in Progress.Levels)
                {
                    total += record.Stars;
                }

                return total;
            }
        }

        public bool IsUnlocked(TidyLevelDefinition level)
        {
            if (level == null)
            {
                return false;
            }

            var requirement = level.UnlockRequirement;
            if (requirement == null || requirement.UnlockedByDefault)
            {
                return true;
            }

            if (requirement.RequiredLevel != null && !IsCompleted(requirement.RequiredLevel.LevelId))
            {
                return false;
            }

            return TotalStars >= requirement.RequiredTotalStars;
        }

        public bool IsCompleted(string levelId)
        {
            var record = Progress.Find(levelId);
            return record != null && record.Completed;
        }

        public int GetStars(string levelId)
        {
            return Progress.Find(levelId)?.Stars ?? 0;
        }

        public int GetBestScore(string levelId)
        {
            return Progress.Find(levelId)?.BestScore ?? 0;
        }

        public float GetBestTime(string levelId)
        {
            return Progress.Find(levelId)?.BestTime ?? 0f;
        }

        public bool IsTutorialCompleted(string tutorialId)
        {
            return Progress.CompletedTutorials.Contains(tutorialId);
        }

        public void MarkTutorialCompleted(string tutorialId)
        {
            if (string.IsNullOrEmpty(tutorialId) || Progress.CompletedTutorials.Contains(tutorialId))
            {
                return;
            }

            Progress.CompletedTutorials.Add(tutorialId);
            SaveProgress();
        }

        public void RecordAttempt(string levelId)
        {
            Progress.FindOrCreate(levelId).Attempts++;
            SaveProgress();
        }

        public void RecordResult(TidyLevelResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.LevelId))
            {
                return;
            }

            var record = Progress.FindOrCreate(result.LevelId);
            if (result.IsCompleted)
            {
                record.Completed = true;
                record.Completions++;
                record.Stars = Mathf.Max(record.Stars, result.Stars);
                record.BestScore = Mathf.Max(record.BestScore, result.Score);
                record.BestTime = record.BestTime <= 0f ? result.TimeSeconds : Mathf.Min(record.BestTime, result.TimeSeconds);
            }

            SaveProgress();
        }

        public void AddReward(int amount)
        {
            Progress.RewardBalance += amount;
            SaveProgress();
        }

        public bool HasLevelSnapshot(string levelId)
        {
            var record = string.IsNullOrEmpty(levelId) ? null : Progress.Find(levelId);
            return record != null && record.HasSavedProgress;
        }

        public float GetSavedProgress(string levelId)
        {
            var record = string.IsNullOrEmpty(levelId) ? null : Progress.Find(levelId);
            return record != null && record.HasSavedProgress ? record.SavedProgress : 0f;
        }

        public bool TryLoadLevelSnapshot(string levelId, out TidyLevelSnapshot snapshot)
        {
            snapshot = string.IsNullOrEmpty(levelId) ? null : Load<TidyLevelSnapshot>(LevelSnapshotPrefix + levelId);
            if (snapshot == null || snapshot.Version > TidyLevelSnapshot.CurrentVersion)
            {
                snapshot = null;
                return false;
            }

            return true;
        }

        public void SaveLevelSnapshot(TidyLevelSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.LevelId))
            {
                return;
            }

            storage.Write(LevelSnapshotPrefix + snapshot.LevelId, JsonUtility.ToJson(snapshot));
            var record = Progress.FindOrCreate(snapshot.LevelId);
            record.HasSavedProgress = true;
            record.SavedProgress = snapshot.Progress;
            record.SavedAtUtc = snapshot.SavedAtUtc;
            SaveProgress();
        }

        public void DeleteLevelSnapshot(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return;
            }

            storage.Delete(LevelSnapshotPrefix + levelId);
            var record = Progress.Find(levelId);
            if (record != null && record.HasSavedProgress)
            {
                record.HasSavedProgress = false;
                record.SavedProgress = 0f;
                record.SavedAtUtc = null;
                SaveProgress();
            }
        }

        public string FindMostRecentSnapshotLevel()
        {
            string best = null;
            var bestTime = DateTime.MinValue;
            foreach (var record in Progress.Levels)
            {
                if (record == null || !record.HasSavedProgress)
                {
                    continue;
                }

                var time = DateTime.TryParse(record.SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed) ? parsed : DateTime.MinValue;
                if (best == null || time > bestTime)
                {
                    best = record.LevelId;
                    bestTime = time;
                }
            }

            return best;
        }

        public void SaveSettings(TidySettingsData settings)
        {
            Settings = settings ?? defaultSettings.Clone();
            Write(SettingsKey, Settings);
            SettingsSaved?.Invoke(Settings);
        }

        public void ResetProgress()
        {
            foreach (var record in Progress.Levels)
            {
                if (record != null && record.HasSavedProgress)
                {
                    storage.Delete(LevelSnapshotPrefix + record.LevelId);
                }
            }

            Progress = new TidyProgressData();
            storage.Delete(ProgressKey);
            ProgressChanged?.Invoke(Progress);
        }

        public void SaveProgress()
        {
            Write(ProgressKey, Progress);
            ProgressChanged?.Invoke(Progress);
        }

        T Load<T>(string key) where T : class
        {
            if (!storage.TryRead(key, out var json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Tidy Up Framework: save data '" + key + "' is corrupt and was reset. " + exception.Message);
                return null;
            }
        }

        void Write<T>(string key, T data)
        {
            storage.Write(key, JsonUtility.ToJson(data, true));
        }
    }
}
