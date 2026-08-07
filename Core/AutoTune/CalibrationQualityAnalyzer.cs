using System;
using System.Collections.Generic;
using System.Linq;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationQualityAnalyzer
    {
        public double AnalyzeQuality(List<TelemetrySnapshot> telemetryHistory, List<TuneDecision> appliedDecisions, out string summary)
        {
            summary = "Veri yetersiz.";
            if (telemetryHistory == null || telemetryHistory.Count == 0)
            {
                return 50.0; // Default base quality
            }

            double afrScore = 100.0;
            double knockScore = 100.0;
            double tempScore = 100.0;
            double efficiencyScore = 100.0;

            // 1. AFR stability: check how close AFR is to standard stoichiometric 14.7 or average
            double afrAvg = telemetryHistory.Average(f => f.AFR);
            double afrVar = telemetryHistory.Sum(f => Math.Pow(f.AFR - afrAvg, 2)) / telemetryHistory.Count;
            afrScore -= Math.Min(afrVar * 20.0, 50.0); // Var deduction

            // 2. Knock count deduction
            double totalKnocks = telemetryHistory.Max(f => f.KnockCount) - telemetryHistory.Min(f => f.KnockCount);
            if (totalKnocks < 0) totalKnocks = 0;
            knockScore -= Math.Min(totalKnocks * 20.0, 50.0);

            // 3. ECT stability
            double ectAvg = telemetryHistory.Average(f => f.ECT);
            if (ectAvg < 80.0 || ectAvg > 95.0)
            {
                tempScore -= 20.0;
            }

            // 4. Correction efficiency: review applied decisions
            if (appliedDecisions != null && appliedDecisions.Count > 0)
            {
                // Large fluctuations reduce efficiency (ideally tuning converges with smaller and smaller corrections)
                double avgCorrection = appliedDecisions.Average(d => Math.Abs(d.ChangePercent));
                efficiencyScore -= Math.Min(avgCorrection * 2.0, 40.0);
            }

            double finalScore = (afrScore + knockScore + tempScore + efficiencyScore) / 4.0;
            finalScore = Math.Clamp(finalScore, 0.0, 100.0);

            summary = $"AFR Dengesi: {afrScore:F0}/100, Knock Engelleme: {knockScore:F0}/100, Isı Verimliliği: {tempScore:F0}/100, " +
                      $"Düzeltme Verimliliği: {efficiencyScore:F0}/100. Genel Kalite Skoru: {finalScore:F0}/100";

            return finalScore;
        }

        public double CalculateQualityScore(AdaptiveMemory memory)
        {
            if (memory == null || memory.Entries.Count == 0)
                return 100.0;

            double successCount = memory.Entries.Count(e => e.Success);
            return (successCount / memory.Entries.Count) * 100.0;
        }
    }
}
