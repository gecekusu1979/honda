using System;

namespace HondaTuner.Core.AutoTune
{
    // A secure token that only PhysicalWriter/PatchManager can produce.
    public struct PhysicalWriterAck
    {
        public string Source { get; }
        public string TransactionId { get; }
        public bool Verified { get; }

        public PhysicalWriterAck(string source, string transactionId, bool verified)
        {
            if (string.IsNullOrEmpty(source)) throw new ArgumentException("Ack source cannot be empty");
            if (!verified) throw new ArgumentException("Ack must be from a verified physical write");
            Source = source;
            TransactionId = transactionId;
            Verified = verified;
        }
    }

    public static class TuneDecisionTransitionValidator
    {
        public static bool CanTransition(TuneDecisionStatus current, TuneDecisionStatus target, out string errorMessage, PhysicalWriterAck? ack = null)
        {
            errorMessage = string.Empty;

            if (current == target) return true;

            if (target == TuneDecisionStatus.Applied)
            {
                if (!ack.HasValue || !ack.Value.Verified)
                {
                    errorMessage = "Transitioning to 'Applied' requires a valid, verified PhysicalWriterAck from the PatchManager.";
                    return false;
                }

                if (current != TuneDecisionStatus.Approved)
                {
                    errorMessage = $"Decision must be 'Approved' before transitioning to 'Applied'. Current: {current}";
                    return false;
                }

                return true;
            }

            switch (current)
            {
                case TuneDecisionStatus.Suggested:
                case TuneDecisionStatus.PendingApproval:
                    if (target == TuneDecisionStatus.Approved || target == TuneDecisionStatus.Rejected)
                        return true;
                    break;
                case TuneDecisionStatus.Approved:
                    if (target == TuneDecisionStatus.Rejected)
                        return true;
                    break;
                case TuneDecisionStatus.Applied:
                case TuneDecisionStatus.Rejected:
                    errorMessage = $"Cannot transition from terminal state: {current}";
                    return false;
            }

            errorMessage = $"Invalid transition: {current} -> {target}";
            return false;
        }

        public static TuneDecisionStatus RequireTransition(TuneDecisionStatus current, TuneDecisionStatus target, PhysicalWriterAck? ack = null)
        {
            if (!CanTransition(current, target, out string err, ack))
                throw new InvalidOperationException($"TuneDecisionStatus transition BLOCKED: {err}");

            return target;
        }
    }
}
