using System;

namespace HondaTuner.Core.AutoTune
{
    public class TuneApprovalWorkflow
    {
        public static string DetermineInitialStatus(string userRole, AutoTuneOperatingMode mode, out string explanation)
        {
            explanation = "";
            if (mode == AutoTuneOperatingMode.DryRun || mode == AutoTuneOperatingMode.Simulation || mode == AutoTuneOperatingMode.SafeMode)
            {
                explanation = "DryRun/Simulation/SafeMode modunda otomatik onay verildi.";
                return "Approved";
            }

            switch (userRole?.ToLowerInvariant())
            {
                case "professional":
                    explanation = "Professional rolü için doğrudan onay verildi.";
                    return "Approved";

                case "advanced":
                    explanation = "Advanced rolü için kullanıcı onayı bekleniyor.";
                    return "PendingApproval";

                case "beginner":
                default:
                    explanation = "Beginner yetkisiyle gerçek ROM yazma işlemi engellendi (Sadece okuma izinli).";
                    return "Rejected"; // Safe mode limits
            }
        }

        public static bool CanTransition(string currentStatus, string targetStatus, string userRole, out string errorMessage)
        {
            errorMessage = "";
            if (currentStatus == targetStatus) return true;

            if (currentStatus == "Applied")
            {
                errorMessage = "Zaten uygulanmış bir karar değiştirilemez.";
                return false;
            }

            if (targetStatus == "Approved" || targetStatus == "Rejected")
            {
                if (currentStatus == "PendingApproval")
                {
                    if (string.Equals(userRole, "beginner", StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage = "Beginner kullanıcıları önerileri onaylayamaz veya reddedemez.";
                        return false;
                    }
                    return true;
                }
                errorMessage = "Sadece beklemedeki (PendingApproval) kararlar onaylanabilir veya reddedilebilir.";
                return false;
            }

            if (targetStatus == "Applied")
            {
                if (currentStatus == "Approved")
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
