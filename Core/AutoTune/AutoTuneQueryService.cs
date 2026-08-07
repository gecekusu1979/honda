using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneQueryService : IAutoTuneQueryService
    {
        private readonly IAutoTuneEngine _engine;

        public AutoTuneQueryService(IAutoTuneEngine engine)
        {
            _engine = engine;
        }

        public string GetSessionStatus(string sessionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                return _engine.ActiveSession.State;
            }
            return "NotFound";
        }

        public List<TuneDecision> GetActiveRecommendations(string sessionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.SessionId == sessionId)
            {
                return new List<TuneDecision>(_engine.ActiveSession.Decisions);
            }
            return new List<TuneDecision>();
        }

        public List<JournalEntry> GetCalibrationHistory(string sessionId)
        {
            return new List<JournalEntry>(_engine.Journal.AllEntries);
        }

        public SafetyResult GetSafetyStatus(string sessionId)
        {
            if (_engine.ActiveSession != null && _engine.ActiveSession.Decisions.Count > 0)
            {
                var last = _engine.ActiveSession.Decisions[_engine.ActiveSession.Decisions.Count - 1];
                return last.Safety;
            }
            return new SafetyResult { Status = "Allow", Reason = "Aktif güvenlik ihlali yok." };
        }
    }
}
