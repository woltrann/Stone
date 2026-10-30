namespace TidyUpFramework.Save
{
    // Optional gameplay payload. Restored before the object lookup so spawned objects can register.
    public interface ITidyObjectSnapshotParticipant
    {
        string CaptureObjectState();
        void RestoreObjectState(string state);
    }

    // A participant that cannot reconstruct a consumed object's result can ask to retain its source.
    public interface ITidyObjectSnapshotRecovery
    {
        bool RestoreFailed { get; }
    }
}
