namespace HondaTuner.Core.AutoTune
{
    public interface IAutoTuneSessionManager
    {
        bool AcquireSessionLock(string ecuid, string userId, AutoTuneOperatingMode mode, string state, out string existingSessionOwner);
        void ReleaseSessionLock(string ecuid);
        bool IsECULocked(string ecuid, out string userId);
        void UpdateSessionState(string ecuid, string state);
    }
}
