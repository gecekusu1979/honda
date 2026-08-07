using System;
using System.Collections.Generic;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune
{
    public class ReplayResult
    {
        public int TotalReplayed { get; set; }
        public int MatchCount { get; set; }
        public double DeviationPercentage { get; set; }
        public double MaxCellDifference { get; set; }
        public bool IsDeterministic { get; set; }
        public List<string> Details { get; set; } = new List<string>();
    }

    public interface IReplayDeterministicValidator
    {
        ReplayResult ValidateReplayDeterminism(
            List<TelemetrySnapshot> telemetryHistory,
            List<TuneDecision> originalDecisions,
            Func<TelemetrySnapshot, TuneDecision> processSnapshotFunc);
    }
}
