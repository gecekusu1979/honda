using HondaTuner.Core.Telemetry;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Core.AutoTune.Analyzers
{
    public interface ITuneAnalyzer
    {
        string Name { get; }
        TuneDecision Analyze(TelemetrySnapshot telemetry, TargetMapProvider targetMapProvider);
    }
}
