using System;
using System.Collections.Generic;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune
{
    public class ReplayDeterministicValidator : IReplayDeterministicValidator
    {
        public ReplayResult ValidateReplayDeterminism(
            List<TelemetrySnapshot> telemetryHistory,
            List<TuneDecision> originalDecisions,
            Func<TelemetrySnapshot, TuneDecision> processSnapshotFunc)
        {
            if (telemetryHistory == null) throw new ArgumentNullException(nameof(telemetryHistory));
            if (originalDecisions == null) throw new ArgumentNullException(nameof(originalDecisions));
            if (processSnapshotFunc == null) throw new ArgumentNullException(nameof(processSnapshotFunc));

            var result = new ReplayResult
            {
                TotalReplayed = telemetryHistory.Count,
                IsDeterministic = true
            };

            int matches = 0;
            double maxDiff = 0.0;

            for (int i = 0; i < Math.Min(telemetryHistory.Count, originalDecisions.Count); i++)
            {
                var snap = telemetryHistory[i];
                var orig = originalDecisions[i];

                // Regenerate decision
                var regen = processSnapshotFunc(snap);

                if (regen == null)
                {
                    result.Details.Add($"Index {i}: Yeniden analiz karar üretemedi.");
                    result.IsDeterministic = false;
                    continue;
                }

                double diff = Math.Abs(regen.NewValue - orig.NewValue);
                if (diff > maxDiff) maxDiff = diff;

                // We evaluate matching properties: cell destination, parameters, confidence indices
                bool cellMatch = regen.MapName == orig.MapName && regen.CellRow == orig.CellRow && regen.CellCol == orig.CellCol;
                bool valueMatch = diff < 0.001;
                bool confidenceMatch = Math.Abs(regen.ConfidenceScore - orig.ConfidenceScore) < 0.01;

                if (cellMatch && valueMatch && confidenceMatch)
                {
                    matches++;
                }
                else
                {
                    result.IsDeterministic = false;
                    result.Details.Add($"Index {i} Uyuşmazlığı: Orijinal Deg={orig.NewValue:F2}(Conf={orig.ConfidenceScore:F0}), " +
                                       $"Yeniden Deg={regen.NewValue:F2}(Conf={regen.ConfidenceScore:F0})");
                }
            }

            result.MatchCount = matches;
            result.MaxCellDifference = maxDiff;

            if (result.TotalReplayed > 0)
            {
                result.DeviationPercentage = 100.0 * (1.0 - ((double)matches / result.TotalReplayed));
            }

            return result;
        }
    }
}
