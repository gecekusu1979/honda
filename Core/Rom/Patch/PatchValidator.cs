using System;
using System.Linq;
using HondaTuner.Core.Container;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// ROM Yamaları için gelişmiş doğrulama (validation) işlemlerini gerçekleştirir.
    /// ROM boyutunu, offset sınırlarını, expected byte doğrulamasını ve ECU uyumluluğunu denetler.
    /// </summary>
    public static class PatchValidator
    {
        /// <summary>
        /// Belirtilen yamayı verilen ROM tamponu üzerinde doğrular.
        /// </summary>
        /// <param name="romData">Ham ROM byte dizisi</param>
        /// <param name="patch">Yama tanımı</param>
        /// <param name="profile">Aktif ECU profil bilgileri</param>
        /// <param name="offset">Yamalanacak hedef offset adresi</param>
        /// <param name="error">Doğrulama başarısız ise hata açıklaması</param>
        /// <returns>Geçerli ise true</returns>
        public static bool Validate(byte[] romData, PatchDefinition patch, EcuProfile profile, int offset, out string error)
        {
            error = null;

            if (romData == null)
            {
                error = "ROM verisi null.";
                return false;
            }

            if (patch == null)
            {
                error = "Yama tanımı null.";
                return false;
            }

            if (profile == null)
            {
                error = "ECU profili null.";
                return false;
            }

            // 1. ROM Size Kontrolü
            if (patch.ValidationRules.Contains(ValidationRule.RequireRomSize))
            {
                if (romData.Length < patch.MinimumRomSize || romData.Length > patch.MaximumRomSize)
                {
                    error = $"ROM boyutu ({romData.Length} byte) yama kuralları dışındadır (Min: {patch.MinimumRomSize}, Max: {patch.MaximumRomSize}).";
                    return false;
                }
            }

            // 2. Compatible ECU Kontrolü
            if (patch.ValidationRules.Contains(ValidationRule.RequireCompatibleEcu))
            {
                if (patch.CompatibleEcus != null && patch.CompatibleEcus.Count > 0)
                {
                    bool isCompatible = false;
                    foreach (var code in patch.CompatibleEcus)
                    {
                        if (string.Equals(code, profile.EcuCode, StringComparison.OrdinalIgnoreCase))
                        {
                            isCompatible = true;
                            break;
                        }
                    }

                    if (!isCompatible)
                    {
                        error = $"Yama '{patch.PatchId}', ECU profili '{profile.EcuCode}' ile uyumsuz.";
                        return false;
                    }
                }
            }

            // 3. Offset Kontrolü
            if (offset < 0 || offset + patch.PatchBytes.Length > romData.Length)
            {
                error = $"Yama hedef alanı ROM sınırları dışında (Offset: {offset}, PatchSize: {patch.PatchBytes.Length}, RomSize: {romData.Length}).";
                return false;
            }

            // 4. Expected Bytes (İmza) Kontrolü
            if (patch.ValidationRules.Contains(ValidationRule.RequireExpectedBytes))
            {
                if (patch.ExpectedBytes != null && patch.ExpectedBytes.Length > 0)
                {
                    for (int i = 0; i < patch.ExpectedBytes.Length; i++)
                    {
                        if (romData[offset + i] != patch.ExpectedBytes[i])
                        {
                            error = $"ROM imza doğrulaması başarısız. Offset 0x{(offset + i):X4} konumunda beklenmeyen byte (Beklenen: 0x{patch.ExpectedBytes[i]:X2}, Mevcut: 0x{romData[offset + i]:X2}).";
                            return false;
                        }
                    }
                }
            }

            // 5. CalibrationTransaction Kontrolü
            if (patch.ValidationRules.Contains(ValidationRule.RequireTransaction))
            {
                try
                {
                    var calService = ServiceContainer.Resolve<ICalibrationService>();
                    if (calService == null || !calService.HasActiveTransaction)
                    {
                        error = "Yamalar sadece aktif bir CalibrationTransaction (işlem) içerisinden uygulanabilir.";
                        return false;
                    }
                }
                catch
                {
                    error = "Kalibrasyon servisine erişilemedi veya aktif işlem doğrulanamadı.";
                    return false;
                }
            }

            return true;
        }
    }
}
