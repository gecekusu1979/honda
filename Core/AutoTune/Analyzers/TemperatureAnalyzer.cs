using HondaTuner.Core.Telemetry;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Core.AutoTune.Analyzers
{
    public class TemperatureAnalyzer : ITuneAnalyzer
    {
        public string Name => "TemperatureAnalyzer";

        public TuneDecision Analyze(TelemetrySnapshot telemetry, TargetMapProvider targetMapProvider)
        {
            if (telemetry == null || targetMapProvider == null) return null;

            // ECT based correction compensation
            if (telemetry.ECT < 95.0) return null; // Only correct under high heat loads

            double currentVE = targetMapProvider.GetTargetValue(targetMapProvider.VeTargets, telemetry.RPM, telemetry.MAP);

            // Add extra 2% fuel correction to cool down engine cylinders
            double pct = 2.0;
            double proposedVE = currentVE * (1.0 + (pct / 100.0));

            int row = targetMapProvider.FindClosestRpmBin(telemetry.RPM);
            int col = targetMapProvider.FindClosestLoadBin(telemetry.MAP);

            return new TuneDecision
            {
                Parameter = ParameterType.Compensation,
                ParameterName = "ECT Compensation Factor",
                MapName = "ECTTable",
                CellRow = row,
                CellCol = col,
                OldValue = currentVE,
                NewValue = proposedVE,
                ChangePercent = pct,
                ConfidenceScore = 80.0,
                ConfidenceReason = $"ECT Yüksek ({telemetry.ECT}°C). Silindirleri korumak için zenginleştirme.",
                RequiredSamples = 5,
                EnvironmentalStability = 0.90
            };
        }
    }
}
