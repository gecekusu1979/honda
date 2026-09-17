using System;

namespace HondaTuner.Core.AutoTune
{
    public class TuneApprovalWorkflow
    {
        public static TuneDecisionStatus DetermineInitialStatus(string userRole, AutoTuneOperatingMode mode, out string explanation)
        {
            explanation = "";
            if (mode == AutoTuneOperatingMode.DryRun || mode == AutoTuneOperatingMode.Simulation || mode == AutoTuneOperatingMode.SafeMode)
            {
                explanation = "DryRun/Simulation/SafeMode modunda otomatik öneri üretildi.";
                return TuneDecisionStatus.Suggested;
            }

            switch (userRole?.ToLowerInvariant())
            {
                case "professional":
                    explanation = "Professional rolü için doğrudan öneri (Suggested) üretildi.";
                    return TuneDecisionStatus.Suggested;

                case "advanced":
                    explanation = "Advanced rolü için kullanıcı onayı bekleniyor.";
                    return TuneDecisionStatus.PendingApproval;

                case "beginner":
                default:
                    explanation = "Beginner yetkisiyle gerçek ROM yazma işlemi engellendi (Sadece okuma izinli).";
                    return TuneDecisionStatus.Rejected; // Safe mode limits
            }
        }

        public static bool CanTransition(TuneDecisionStatus currentStatus, TuneDecisionStatus targetStatus, string userRole, out string errorMessage)
        {
            errorMessage = "";
            if (currentStatus == targetStatus) return true;

            if (currentStatus == TuneDecisionStatus.Applied)
            {
                errorMessage = "Zaten uygulanmış bir karar değiştirilemez.";
                return false;
            }

            if (targetStatus == TuneDecisionStatus.Approved || targetStatus == TuneDecisionStatus.Rejected)
            {
                if (currentStatus == TuneDecisionStatus.PendingApproval || currentStatus == TuneDecisionStatus.Suggested)
                {
                    if (string.Equals(userRole, "beginner", StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage = "Beginner kullanıcıları önerileri onaylayamaz veya reddedemez.";
                        return false;
                    }
                    return true;
                }
                errorMessage = "Sadece beklemedeki (PendingApproval) veya önerilen (Suggested) kararlar onaylanabilir veya reddedilebilir.";
                return false;
            }

            if (targetStatus == TuneDecisionStatus.Applied)
            {
                if (currentStatus == TuneDecisionStatus.Approved)
                {
                    return true;
                }
                errorMessage = "Öncelikle kararın onaylanması (Approved) gerekmektedir.";
                return false;
            }

            errorMessage = $"Desteklenmeyen durum geçişi: {currentStatus} -> {targetStatus}";
            return false;
        }
    }
}
