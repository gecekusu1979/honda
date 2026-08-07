using System;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneCommandService : IAutoTuneCommandService
    {
        private readonly IAutoTuneEngine _engine;

        public AutoTuneCommandService(IAutoTuneEngine engine)
        {
            _engine = engine;
        }

        public bool StartSession(string ecuid, string userId, AutoTuneOperatingMode mode, string profile)
        {
            return _engine.StartSession(ecuid, userId, mode, profile);
        }

        public void StopSession(string sessionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                _engine.StopSession();
            }
        }

        public void PauseSession(string sessionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                _engine.PauseSession();
            }
        }

        public void ResumeSession(string sessionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                _engine.ResumeSession();
            }
        }

        public bool ApproveDecision(string sessionId, string decisionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                return _engine.ApproveDecision(decisionId);
            }
            return false;
        }

        public void RejectDecision(string sessionId, string decisionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                _engine.RejectDecision(decisionId);
            }
        }

        public bool RollbackLastChange(string sessionId, out string resultMessage)
        {
            resultMessage = "";
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                return _engine.RollbackLastChange(out resultMessage);
            }
            resultMessage = "Aktif oturum bulunamadı.";
            return false;
        }
    }
}
