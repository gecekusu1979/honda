using HondaTuner.Core.AutoTune.Safety;
using System.Collections.Generic;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune
{
    public interface IAutoTuneSafetyManager
    {
        SafetyResult EvaluateSafety(TelemetrySnapshot telemetry, TuneDecision decision);
        void ReloadSafetyLimits(string safetyLimitsFilePath);
        IReadOnlyList<ISafetyRule> ActiveRules { get; }
    }
}
