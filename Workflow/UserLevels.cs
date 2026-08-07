using System;

namespace HondaTuner.Workflow
{
    /// <summary>
    /// Kullanıcı yetki seviyeleri — özellik erişimi kısıtlaması.
    /// Tehlikeli operasyonlar PRO seviyesinde bile onay gerektirir.
    /// </summary>
    public enum UserLevel
    {
        Beginner,   // Sadece sihirbazlar
        Advanced,   // Haritalar + kalibrasyon
        Professional // Hex editör + Patch Manager + donanım kontrolü
    }

    public static class UserLevels
    {
        public static UserLevel CurrentLevel { get; set; } = UserLevel.Advanced;

        public static bool CanAccessMaps() =>
            CurrentLevel >= UserLevel.Advanced;

        public static bool CanAccessCalibration() =>
            CurrentLevel >= UserLevel.Advanced;

        public static bool CanAccessHexEditor() =>
            CurrentLevel >= UserLevel.Professional;

        public static bool CanAccessPatchManager() =>
            CurrentLevel >= UserLevel.Professional;

        public static bool CanAccessHardware() =>
            CurrentLevel >= UserLevel.Professional;

        /// <summary>
        /// Tehlikeli operasyon onayı — tüm seviyelerde gereklidir.
        /// </summary>
        public static bool RequiresConfirmation(string operationType)
        {
            switch (operationType)
            {
                case "ROM_WRITE":
                case "EEPROM_WRITE":
                case "EEPROM_ERASE":
                case "RTP_WRITE":
                case "PATCH_APPLY":
                    return true;
                default:
                    return false;
            }
        }
    }
}
