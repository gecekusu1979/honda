using System.Collections.Generic;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune.Safety
{
    public interface ISafetyRule
    {
        string Name { get; }
        SafetyResult Evaluate(TelemetrySnapshot telemetry, TuneDecision decision);
    }

    public interface ISafetyRuleProvider
    {
        List<ISafetyRule> LoadRules(string safetyLimitsFilePath);
    }
}
