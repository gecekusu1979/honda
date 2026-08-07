namespace HondaTuner.Core.AutoTune
{
    public interface ICalibrationSecurityManager
    {
        bool ValidatePermissions(string userRole, AutoTuneOperatingMode mode, string action, out string reason);
        bool ValidateEcuCompatibility(string ecuIdentifier, string targetEcuType, out string reason);
        bool ValidateProfilePermissions(string activeProfile, string requestedMode, out string reason);
        bool ValidateTransactionOwnership(string transactionOwnerId, string sessionOwnerId, out string reason);
    }
}
