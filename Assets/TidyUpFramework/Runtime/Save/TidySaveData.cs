using System;
using System.Collections.Generic;
using TidyUpFramework.Input;

namespace TidyUpFramework.Save
{
    [Serializable]
    public sealed class TidyLevelRecord
    {
        public string LevelId;
        public bool Completed;
        public int Stars;
        public int BestScore;
        public float BestTime;
        public int Attempts;
        public int Completions;
        public bool HasSavedProgress;
        public float SavedProgress;
        public string SavedAtUtc;
    }

    [Serializable]
    public sealed class TidyProgressData
    {
        public int Version = 1;
        public List<TidyLevelRecord> Levels = new List<TidyLevelRecord>();
        public List<string> CompletedTutorials = new List<string>();
        public int RewardBalance;

        public TidyLevelRecord Find(string levelId)
        {
            foreach (var record in Levels)
            {
                if (record.LevelId == levelId)
                {
                    return record;
                }
            }

            return null;
        }

        public TidyLevelRecord FindOrCreate(string levelId)
        {
            var record = Find(levelId);
            if (record != null)
            {
                return record;
            }

            record = new TidyLevelRecord { LevelId = levelId };
            Levels.Add(record);
            return record;
        }
    }

    [Serializable]
    public sealed class TidySettingsData
    {
        public int Version = 1;
        public int QualityLevel = -1;
        public int ResolutionWidth;
        public int ResolutionHeight;
        public int FullScreenMode = 1;
        public bool VSync = true;
        public int ShadowQuality = 2;
        public int TextureQuality;
        public int TargetFrameRate = 60;
        public float RenderScale = 1f;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.7f;
        public float SfxVolume = 0.85f;
        public float UiVolume = 0.8f;
        public float AmbienceVolume = 0.6f;
        public float LookSensitivity = 1f;
        public float InteractionSensitivity = 1f;
        public bool InvertY;
        public bool HintsEnabled = true;
        public bool TutorialEnabled = true;
        public bool HeadBob = true;
        public bool ShowPlacementPreview = true;
        public int PreferredScheme = -1;

        public TidySettingsData Clone()
        {
            return (TidySettingsData)MemberwiseClone();
        }

        public bool HasPreferredScheme => PreferredScheme >= 0;
        public TidyInputScheme Scheme => (TidyInputScheme)PreferredScheme;
    }
}
