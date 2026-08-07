using System;

namespace HondaTuner.Core.AutoTune
{
    public class SafetyResult
    {
        public string Status { get; set; } // Allow, Warning, Reject
        public string RuleName { get; set; }
        public double CurrentValue { get; set; }
        public double LimitValue { get; set; }
        public string Severity { get; set; } // Info, Warning, Critical
        public string Reason { get; set; }
    }

    public class TuneDecision
    {
        public string DecisionId { get; } = Guid.NewGuid().ToString();
        public ParameterType Parameter { get; set; }
        public string ParameterName { get; set; }
        public string MapName { get; set; }
        public int CellRow { get; set; }
        public int CellCol { get; set; }
        public int Offset { get; set; }

        public double OldValue { get; set; }
        public double NewValue { get; set; }
        public double ChangePercent { get; set; }

        public double ConfidenceScore { get; set; } // 0 - 100
        public string ConfidenceReason { get; set; }
        public int RequiredSamples { get; set; }
        public double EnvironmentalStability { get; set; }

        public SafetyResult Safety { get; set; }
        public string ApprovalStatus { get; set; } // RecommendationGenerated, PendingApproval, Approved, Rejected, Applied
        public string Explanation { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
