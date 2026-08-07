using HondaTuner.Core.Telemetry;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Core.AutoTune.Analyzers
{
    public class FuelTrimAnalyzer : ITuneAnalyzer
    {
        public string Name => "FuelTrimAnalyzer";

        public TuneDecision Analyze(TelemetrySnapshot telemetry, TargetMapProvider targetMapProvider)
        {
            if (telemetry == null || targetMapProvider == null) return null;

            double targetAfr = targetMapProvider.GetTargetValue(targetMapProvider.AfrTargets, telemetry.RPM, telemetry.MAP);
            if (targetAfr <= 0.1) targetAfr = 14.7;

            double afrError = telemetry.AFR - targetAfr;
            double pct = (afrError / targetAfr) * 100.0; // Positive means lean, need more fuel

            int row = targetMapProvider.FindClosestRpmBin(telemetry.RPM);
            int col = targetMapProvider.FindClosestLoadBin(telemetry.MAP);

            // Propose fuel trim change
            double currentVE = targetMapProvider.GetTargetValue(targetMapProvider.VeTargets, telemetry.RPM, telemetry.MAP);
            double proposedVE = currentVE * (1.0 + (pct / 100.0));

            return new TuneDecision
            {
                Parameter = ParameterType.Fuel,
                ParameterName = "Fuel VE Cell",
                MapName = "FuelVETable",
                CellRow = row,
                CellCol = col,
                OldValue = currentVE,
                NewValue = proposedVE,
                ChangePercent = pct,
                ConfidenceScore = 85.0,
                ConfidenceReason = $"AFR Hatalı: %{pct:+0.0;-0.0;0.0}",
                RequiredSamples = 10,
                EnvironmentalStability = 0.95
            };
        }
    }
}
