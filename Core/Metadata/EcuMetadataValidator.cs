using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Metadata
{
    public enum ValidationLevel
    {
        Info,
        Warning,
        Error
    }

    public class MetadataValidationResult
    {
        public string RuleId { get; set; }
        public ValidationLevel Level { get; set; }
        public string Message { get; set; }
        public bool IsValid => Level != ValidationLevel.Error;
    }

    public static class EcuMetadataValidator
    {
        public static List<MetadataValidationResult> Validate(EcuMetadata metadata, EcuProfile profile, int revLimit, int[] loadAxis)
        {
            var results = new List<MetadataValidationResult>();

            // 1. VIN Kontrolü
            if (string.IsNullOrWhiteSpace(metadata.Vin) || metadata.Vin.Length != 17)
            {
                results.Add(new MetadataValidationResult
                {
                    RuleId = "VAL_VIN_LENGTH",
                    Level = ValidationLevel.Warning,
                    Message = "Şasi numarası (VIN) standart 17 karakter uzunluğunda olmalıdır."
                });
            }

            // 2. Seri Numarası Kontrolü
            if (string.IsNullOrWhiteSpace(metadata.SerialNumber) || !metadata.SerialNumber.StartsWith("HT-", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new MetadataValidationResult
                {
                    RuleId = "VAL_SERIAL_FORMAT",
                    Level = ValidationLevel.Info,
                    Message = "ECU seri numarası HondaTuner standardı ('HT-XXXXX') ile başlamalıdır."
                });
            }

            // 3. Aşırı Besleme ve MAP Sensörü Eşleşmesi
            bool isForcedInduction = metadata.InductionType.Equals("Turbo", StringComparison.OrdinalIgnoreCase) ||
                                     metadata.InductionType.Equals("Supercharger", StringComparison.OrdinalIgnoreCase);

            if (isForcedInduction)
            {
                // Max MAP değerini belirle
                int maxPressure = 105;
                if (loadAxis != null && loadAxis.Length > 0)
                {
                    maxPressure = loadAxis[loadAxis.Length - 1];
                }

                if (maxPressure <= 105)
                {
                    results.Add(new MetadataValidationResult
                    {
                        RuleId = "VAL_TURBO_MAP_LIMIT",
                        Level = ValidationLevel.Error,
                        Message = $"Aşırı besleme ({metadata.InductionType}) seçili fakat MAP sensörü stock 1-bar ({maxPressure} kPa) limitinde. Lütfen MAP sensörünü en az 2.5-bar veya üzeri kalibre edin."
                    });
                }
            }

            // 4. Sıkıştırma Oranı Limitleri
            if (metadata.CompressionRatio > 11.5)
            {
                results.Add(new MetadataValidationResult
                {
                    RuleId = "VAL_COMPRESSION_OCTANE",
                    Level = ValidationLevel.Warning,
                    Message = $"Yüksek sıkıştırma oranı (CR: {metadata.CompressionRatio:0.0}:1) tespit edildi. Avans haritalarını dikkatli düzenleyin ve yüksek oktanlı yakıt profilini kullanın."
                });
            }

            // 5. Eksantrik Profili & Devir Sınırı
            bool isAggressiveCam = metadata.CamshaftProfile.Equals("Stage 2", StringComparison.OrdinalIgnoreCase) ||
                                   metadata.CamshaftProfile.Equals("Stage 3", StringComparison.OrdinalIgnoreCase);

            if (isAggressiveCam && revLimit <= 7300)
            {
                results.Add(new MetadataValidationResult
                {
                    RuleId = "VAL_CAM_REV_LIMIT",
                    Level = ValidationLevel.Warning,
                    Message = $"{metadata.CamshaftProfile} eksantrik profili için devir kesici ({revLimit} RPM) çok düşük. Güç bandından yararlanmak için devir kesiciyi artırabilirsiniz."
                });
            }

            return results;
        }
    }
}
