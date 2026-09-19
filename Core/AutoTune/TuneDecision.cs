#pragma warning disable CS8618, CS8600, CS8601, CS8602, CS8603, CS8604, CS8765, CS8629, CS8622, CS0168
using System;

namespace HondaTuner.Core.AutoTune
{
    public class SafetyResult
    {
        public string Status { get; set; } // Allow, Warning, Reject = null!;
        public string RuleName { get; set; } = null!;
        public double CurrentValue { get; set; }
        public double LimitValue { get; set; }
        public string Severity { get; set; } // Info, Warning, Critical = null!;
        public string Reason { get; set; } = null!;
    }

    public class TuneDecision
    {
        public string DecisionId { get; } = Guid.NewGuid().ToString();
        public ParameterType Parameter { get; set; }
        public string ParameterName { get; set; } = null!;
        public string MapName { get; set; } = null!;
        public int CellRow { get; set; }
        public int CellCol { get; set; }
        public int Offset { get; set; }

        public double OldValue { get; set; }
        public double NewValue { get; set; }
        public double ChangePercent { get; set; }

        public double ConfidenceScore { get; set; } // 0 - 100
        public string ConfidenceReason { get; set; } = null!;
        public int RequiredSamples { get; set; }
        public double EnvironmentalStability { get; set; }

        public SafetyResult Safety { get; set; } = null!;
        public TuneDecisionStatus ApprovalStatus { get; set; } = TuneDecisionStatus.Suggested;
        public string Explanation { get; set; } = null!;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}