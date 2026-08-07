using HondaTuner.Core.Telemetry;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Core.AutoTune.Analyzers
{
    public class KnockAnalyzer : ITuneAnalyzer
    {
        public string Name => "KnockAnalyzer";

        public TuneDecision Analyze(TelemetrySnapshot telemetry, TargetMapProvider targetMapProvider)
        {
            if (telemetry == null || targetMapProvider == null) return null;

            // Retard spark if knock is registered
            if (telemetry.KnockCount <= 0.0) return null;

            double currentIgn = targetMapProvider.GetTargetValue(targetMapProvider.IgnitionTargets, telemetry.RPM, telemetry.MAP);

            // Retard calibration: deduct 2 degrees per knock count register
            double retardAmount = telemetry.KnockCount * 2.0;
            double proposedIgn = currentIgn - retardAmount;

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
                ChangePercent = -retardAmount,
                ConfidenceScore = 95.0,
                ConfidenceReason = $"Knock algılandı! Vuruntu sayısı: {telemetry.KnockCount}",
                RequiredSamples = 1, // Urgent action
                EnvironmentalStability = 0.5 // High priority safety override
            };
        }
    }
}
