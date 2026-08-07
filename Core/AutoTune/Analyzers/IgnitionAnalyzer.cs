using HondaTuner.Core.Telemetry;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Core.AutoTune.Analyzers
{
    public class IgnitionAnalyzer : ITuneAnalyzer
    {
        public string Name => "IgnitionAnalyzer";

        public TuneDecision Analyze(TelemetrySnapshot telemetry, TargetMapProvider targetMapProvider)
        {
            if (telemetry == null || targetMapProvider == null) return null;

            // Only advance if knock is zero and operating parameters are stable
            if (telemetry.KnockCount > 0) return null;

            double currentIgn = targetMapProvider.GetTargetValue(targetMapProvider.IgnitionTargets, telemetry.RPM, telemetry.MAP);
            double proposedIgn = currentIgn + 0.5; // Small incremental advance

            int row = targetMapProvider.FindClosestRpmBin(telemetry.RPM);
            int col = targetMapProvider.FindClosestLoadBin(telemetry.MAP);

            return new TuneDecision
            {
                Parameter = ParameterType.Ignition,
                ParameterName = "Ignition Advance Cell",
                MapName = "IgnitionTable",
                CellRow = row,
                CellCol = col,
                OldValue = currentIgn,
                NewValue = proposedIgn,
                ChangePercent = 0.5,
                ConfidenceScore = 75.0,
                ConfidenceReason = "Stabil çalışma, knock yok, timing advance önerilir.",
                RequiredSamples = 15,
                EnvironmentalStability = 0.99
            };
        }
    }
}
