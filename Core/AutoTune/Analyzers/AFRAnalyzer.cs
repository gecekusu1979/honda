using HondaTuner.Core.Telemetry;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Core.AutoTune.Analyzers
{
    public class AFRAnalyzer : ITuneAnalyzer
    {
        public string Name => "AFRAnalyzer";

        public TuneDecision Analyze(TelemetrySnapshot telemetry, TargetMapProvider targetMapProvider)
        {
            if (telemetry == null || targetMapProvider == null) return null;

            double targetAfr = targetMapProvider.GetTargetValue(targetMapProvider.AfrTargets, telemetry.RPM, telemetry.MAP);
            if (targetAfr <= 0.1) targetAfr = 14.7;

            double errorPct = ((telemetry.AFR - targetAfr) / targetAfr) * 100.0;

            int row = targetMapProvider.FindClosestRpmBin(telemetry.RPM);
            int col = targetMapProvider.FindClosestLoadBin(telemetry.MAP);

            return new TuneDecision
            {
                Parameter = ParameterType.Lambda,
                ParameterName = "Target AFR Value",
                MapName = "AFRTargetTable",
                CellRow = row,
                CellCol = col,
                OldValue = targetAfr,
                NewValue = targetAfr, // Targets are read-only representation in decision
                ChangePercent = errorPct,
                ConfidenceScore = 90.0,
                ConfidenceReason = $"AFR sapması: {(telemetry.AFR - targetAfr):+0.00;-0.00;0.00}",
                RequiredSamples = 10,
                EnvironmentalStability = 0.98
            };
        }
    }
}
