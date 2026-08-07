using System;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Calibration.AutoTune
{
    /// <summary>
    /// AutoTune Güvenlik Doğrulayıcı.
    /// Telemetri verisinin düzeltme hesaplaması için güvenli olup olmadığını kontrol eder.
    /// AutoTuneEngine, validator onayı olmadan hiçbir düzeltme öneremez.
    /// </summary>
    public class AutoTuneValidator
    {
        // ── Güvenlik Limitleri ─────────────────────────────────────
        public double MinEctCelsius { get; set; } = 72.0;     // Motor ısınmış olmalı
        public double MinBatteryVolts { get; set; } = 12.0;   // Kararlı elektrik
        public double MinRpm { get; set; } = 1500;            // Rölanti koruma
        public double MaxRpm { get; set; } = 7000;            // Over-rev koruma
        public double MaxTpsRatePerSec { get; set; } = 5.0;   // Gaz pedalı kararlılığı (% / saniye)
        public double MinTps { get; set; } = 15.0;            // Yakıt kesme koruma
        public double MaxCorrectionPercent { get; set; } = 12.0; // Tek iterasyonda max düzeltme

        // Son kontrol durumu
        private double _lastTps = -1;
        private DateTime _lastTpsTime = DateTime.MinValue;

        /// <summary>
        /// Telemetri çerçevesinin güvenlik koşullarını karşılayıp karşılamadığını kontrol eder.
        /// </summary>
        public ValidationResult Validate(TelemetryFrameData frame)
        {
            // 1) Motor sıcaklığı kontrolü
            if (frame.Ect < MinEctCelsius)
                return Reject("ECT_LOW", $"Motor sıcaklığı düşük: {frame.Ect:F0}°C < {MinEctCelsius}°C");

            // 2) Batarya voltajı
            if (frame.BatteryVolts < MinBatteryVolts)
                return Reject("BATT_LOW", $"Batarya düşük: {frame.BatteryVolts:F1}V < {MinBatteryVolts}V");

            // 3) RPM aralığı
            if (frame.Rpm < MinRpm)
                return Reject("RPM_LOW", $"RPM çok düşük: {frame.Rpm:F0} < {MinRpm}");
            if (frame.Rpm > MaxRpm)
                return Reject("RPM_HIGH", $"RPM çok yüksek: {frame.Rpm:F0} > {MaxRpm}");

            // 4) TPS minimum (yakıt kesme bölgesi)
            if (frame.Tps < MinTps)
                return Reject("TPS_LOW", $"TPS düşük (dec-fuel): {frame.Tps:F1}% < {MinTps}%");

            // 5) TPS kararlılığı (dTPS/dt kontrolü)
            if (_lastTps >= 0 && _lastTpsTime != DateTime.MinValue)
            {
                double elapsed = (DateTime.UtcNow - _lastTpsTime).TotalSeconds;
                if (elapsed > 0.01)
                {
                    double rate = Math.Abs(frame.Tps - _lastTps) / elapsed;
                    if (rate > MaxTpsRatePerSec)
                    {
                        _lastTps = frame.Tps;
                        _lastTpsTime = DateTime.UtcNow;
                        return Reject("TPS_UNSTABLE",
                            $"TPS değişim hızı yüksek: {rate:F1}%/s > {MaxTpsRatePerSec}%/s");
                    }
                }
            }
            _lastTps = frame.Tps;
            _lastTpsTime = DateTime.UtcNow;

            return new ValidationResult { IsValid = true, Code = "OK" };
        }

        /// <summary>
        /// Hesaplanan düzeltme yüzdesini güvenlik limitine sığdırır.
        /// </summary>
        public double ClampCorrection(double correctionPercent)
        {
            return Math.Max(-MaxCorrectionPercent,
                   Math.Min(MaxCorrectionPercent, correctionPercent));
        }

        private static ValidationResult Reject(string code, string reason)
        {
            ApplicationLogger.Debug("AutoTuneValidator", $"Reddedildi [{code}]: {reason}");
            return new ValidationResult { IsValid = false, Code = code, Reason = reason };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Code { get; set; }
        public string Reason { get; set; }
    }
}
