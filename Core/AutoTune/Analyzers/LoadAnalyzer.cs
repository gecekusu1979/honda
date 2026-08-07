using HondaTuner.Core.Telemetry;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Core.AutoTune.Analyzers
{
    public class LoadAnalyzer : ITuneAnalyzer
    {
        public string Name => "LoadAnalyzer";

        public TuneDecision Analyze(TelemetrySnapshot telemetry, TargetMapProvider targetMapProvider)
        {
            if (telemetry == null || targetMapProvider == null) return null;

            double targetVE = targetMapProvider.GetTargetValue(targetMapProvider.VeTargets, telemetry.RPM, telemetry.MAP);
            if (targetVE <= 0.0) targetVE = 60.0;

            int row = targetMapProvider.FindClosestRpmBin(telemetry.RPM);
            int col = targetMapProvider.FindClosestLoadBin(telemetry.MAP);

            return new TuneDecision
            {
                Parameter = ParameterType.VE,
                ParameterName = "VE Map Target Cell",
                MapName = "FuelVETable",
                CellRow = row,
                CellCol = col,
                OldValue = targetVE,
                NewValue = targetVE,
                ChangePercent = 0.0,
                ConfidenceScore = 85.0,
                ConfidenceReason = $"Hedef yük eşleşmesi: {telemetry.MAP:F1} kPa",
                RequiredSamples = 10,
                EnvironmentalStability = 0.97
            };
        }
    }
}
