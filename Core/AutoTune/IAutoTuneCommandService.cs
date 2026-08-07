namespace HondaTuner.Core.AutoTune
{
    public interface IAutoTuneCommandService
    {
        bool StartSession(string ecuid, string userId, AutoTuneOperatingMode mode, string profile);
        void StopSession(string sessionId);
        void PauseSession(string sessionId);
        void ResumeSession(string sessionId);
        bool ApproveDecision(string sessionId, string decisionId);
        void RejectDecision(string sessionId, string decisionId);
        bool RollbackLastChange(string sessionId, out string resultMessage);
    }
}
