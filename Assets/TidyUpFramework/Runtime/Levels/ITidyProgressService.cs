namespace TidyUpFramework.Levels
{
    public interface ITidyProgressService
    {
        bool IsUnlocked(TidyLevelDefinition level);
        bool IsCompleted(string levelId);
        int GetStars(string levelId);
        int GetBestScore(string levelId);
        float GetBestTime(string levelId);
        int TotalStars { get; }
        void RecordResult(TidyLevelResult result);
    }
}
