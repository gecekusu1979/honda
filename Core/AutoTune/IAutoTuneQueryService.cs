using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public interface IAutoTuneQueryService
    {
        string GetSessionStatus(string sessionId);
        List<TuneDecision> GetActiveRecommendations(string sessionId);
        List<JournalEntry> GetCalibrationHistory(string sessionId);
        SafetyResult GetSafetyStatus(string sessionId);
    }
}
