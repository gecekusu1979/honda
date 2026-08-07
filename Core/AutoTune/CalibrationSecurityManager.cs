using System;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationSecurityManager : ICalibrationSecurityManager
    {
        public bool ValidatePermissions(string userRole, AutoTuneOperatingMode mode, string action, out string reason)
        {
            reason = "";
            string role = userRole?.ToLowerInvariant() ?? "beginner";

            // SafeMode rules: block all writes (Apply actions)
            if (mode == AutoTuneOperatingMode.SafeMode && string.Equals(action, "Apply", StringComparison.OrdinalIgnoreCase))
            {
                reason = "SafeMode modunda ROM yazmaları engellenmiştir.";
                return false;
            }

            if (role == "beginner")
            {
                // Beginner can only view/read, absolutely no Write/Apply actions to physical ROM
                if (string.Equals(action, "Apply", StringComparison.OrdinalIgnoreCase) && mode == AutoTuneOperatingMode.Normal)
                {
                    reason = "Beginner yetkisiyle gerçek ROM yazma işlemi yetkisizdir.";
                    return false;
                }
            }

            return true;
        }

        public bool ValidateEcuCompatibility(string ecuIdentifier, string targetEcuType, out string reason)
        {
            reason = "";
            if (string.IsNullOrEmpty(ecuIdentifier) || string.IsNullOrEmpty(targetEcuType))
            {
                reason = "ECU tanımı veya hedef tipi boş olamaz.";
                return false;
            }

            // Assume compatibility check (mocked rules for ECU types like P28, P30, PR4)
            bool isCompatible = ecuIdentifier.ToLowerInvariant().Contains(targetEcuType.ToLowerInvariant()) ||
                                targetEcuType.ToLowerInvariant().Contains(ecuIdentifier.ToLowerInvariant()) ||
                                ecuIdentifier == "P28-Mock" ||
                                targetEcuType == "P28-Mock";

            if (!isCompatible)
            {
                reason = $"ECU tipi uyuşmazlığı detected: Donanım {ecuIdentifier}, Yazılım {targetEcuType}";
                return false;
            }

            return true;
        }

        public bool ValidateProfilePermissions(string activeProfile, string requestedMode, out string reason)
        {
            reason = "";
            // E.g., Street allows all modes. Race only allows Normal or DryRun.
            if (string.Equals(activeProfile, "Race", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(requestedMode, "Simulation", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(requestedMode, "SafeMode", StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Race profilinde Simulation veya SafeMode modları kullanılamaz.";
                    return false;
                }
            }

            return true;
        }

        public bool ValidateTransactionOwnership(string transactionOwnerId, string sessionOwnerId, out string reason)
        {
            reason = "";
            if (string.IsNullOrEmpty(transactionOwnerId) || string.IsNullOrEmpty(sessionOwnerId))
            {
                reason = "Sahiplik bilgisi eksik.";
                return false;
            }

            if (transactionOwnerId != sessionOwnerId)
            {
                reason = $"İşlem sahibi uyuşmazlığı: İşlem sahibi '{transactionOwnerId}', aktif oturum '{sessionOwnerId}'";
                return false;
            }

            return true;
        }
    }
}
