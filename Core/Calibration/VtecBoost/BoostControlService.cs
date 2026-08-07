using System;

namespace HondaTuner.Calibration.VtecBoost
{
    public class BoostControlService
    {
        public VtecBoostTables Tables { get; } = new VtecBoostTables();

        // PID Ayarları
        public double Kp { get; set; } = 0.8;
        public double Ki { get; set; } = 0.15;
        public double Kd { get; set; } = 0.05;

        // PID Geçici Değişkenleri
        private double _integral = 0.0;
        private double _lastError = 0.0;

        // Diagnostik ve Scramble Durumu
        public double ScrambleTimeRemaining { get; private set; } = 0.0;
        public double WgHighDutyTimer { get; private set; } = 0.0;

        public event EventHandler<string> WgFailureAlarm;

        public void TriggerScramble()
        {
            ScrambleTimeRemaining = Tables.ScrambleDuration;
        }

        public void UpdateTimers(double dt)
        {
            if (ScrambleTimeRemaining > 0)
            {
                ScrambleTimeRemaining = Math.Max(0.0, ScrambleTimeRemaining - dt);
            }
        }

        public void ResetPid()
        {
            _integral = 0.0;
            _lastError = 0.0;
        }

        // 1. VTEC Koşul Denetimi
        public bool IsVtecActive(double rpm, double speed, int gear)
        {
            if (rpm < Tables.VtecMinRpm) return false;
            if (speed < Tables.VtecMinSpeed) return false;

            // Vites kısıtlamalarını kontrol et (1-6 arası). Indis 0-5
            int gearIdx = gear - 1;
            if (gearIdx >= 0 && gearIdx < Tables.VtecGearRestrictions.Length)
            {
                if (Tables.VtecGearRestrictions[gearIdx]) return false; // Bu viteste engelli
            }

            return true;
        }

        // 2. Hedef Boost Basıncı (Vites ve Devre göre interpolasyon + Scramble)
        public double GetTargetBoost(double rpm, int gear)
        {
            // Vites kısıtlaması (1 ile 5. vites arası haritalanır)
            int col = Math.Max(1, Math.Min(5, gear)) - 1;

            // RPM interpolasyonu (1D kısımlı veya 2D aramasından tek sütun çekme)
            double baseTarget = InterpolateTargetBoost(rpm, col);

            // Scramble aktif mi?
            if (ScrambleTimeRemaining > 0.0)
            {
                baseTarget += Tables.ScrambleBoostAdd;
            }

            return baseTarget;
        }

        // 3. Kapalı Çevrim PID Solenoid Doluluk Hesabı (Base WG Duty + PID)
        public double CalculateWgDuty(double targetBoost, double actualBoost, double rpm, double dt)
        {
            if (dt <= 0.001) dt = 0.1;

            // Hata hesabı
            double error = targetBoost - actualBoost;

            // Integral ve türev hesabı
            _integral += error * dt;
            _integral = Math.Max(-50.0, Math.Min(50.0, _integral)); // Windup koruması
            double derivative = (error - _lastError) / dt;
            _lastError = error;

            double pidContribution = (Kp * error) + (Ki * _integral) + (Kd * derivative);

            // Temel WG Duty interpolasyonu (RPM ve Hedef Boost tablosundan)
            double baseDuty = InterpolateBaseWgDuty(rpm, targetBoost);

            double finalDuty = baseDuty + pidContribution;

            // Teşhis ve alarm izleme fonksiyonu
            UpdateDiagnostics(finalDuty, targetBoost, actualBoost, dt);

            return Math.Max(0.0, Math.Min(100.0, finalDuty));
        }

        // 4. Teşhis Fonksiyonu: Mekanik Kaçak / Wastegate Sıkışması
        private void UpdateDiagnostics(double duty, double targetBoost, double actualBoost, double dt)
        {
            // Eğer enjektör WG solenoid doluluk oranı aşırı yüksekse (>90.0) ve turbo basıncı hedefin 20 kPa gerisinde kalıyorsa
            if (duty >= 90.0 && (targetBoost - actualBoost) >= 20.0)
            {
                WgHighDutyTimer += dt;
                if (WgHighDutyTimer >= 2.0)
                {
                    WgFailureAlarm?.Invoke(this, $"🚨 BOOST KAÇAĞI VEYA WASTEGATE ARIZASI: Solenoid Doluluk Oranı %{Math.Round(duty, 1)} fakat Basınç Yükselmiyor!");
                }
            }
            else
            {
                WgHighDutyTimer = 0.0;
            }
        }

        // Bilinear Eksen / Grid Arama Fonksiyonları
        private double InterpolateTargetBoost(double rpm, int gearCol)
        {
            var rpms = Tables.BoostRpmBins;
            int len = rpms.Length;

            if (rpm <= rpms[0]) return Tables.BoostTargets[0, gearCol];
            if (rpm >= rpms[len - 1]) return Tables.BoostTargets[len - 1, gearCol];

            for (int i = 0; i < len - 1; i++)
            {
                if (rpm >= rpms[i] && rpm <= rpms[i + 1])
                {
                    double pct = (rpm - rpms[i]) / (rpms[i + 1] - rpms[i]);
                    return Tables.BoostTargets[i, gearCol] + pct * (Tables.BoostTargets[i + 1, gearCol] - Tables.BoostTargets[i, gearCol]);
                }
            }
            return Tables.BoostTargets[0, gearCol];
        }

        private double InterpolateBaseWgDuty(double rpm, double targetBoost)
        {
            var rpms = Tables.BoostRpmBins;
            var boosts = Tables.WgBoostBins;
            var duties = Tables.BaseWgDuty;

            int rpmLen = rpms.Length;
            int boostLen = boosts.Length;

            // RPM & Boost sınırlarını daralt
            double clampedRpm = Math.Max(rpms[0], Math.Min(rpms[rpmLen - 1], rpm));
            double clampedBoost = Math.Max(boosts[0], Math.Min(boosts[boostLen - 1], targetBoost));

            // RPM Hücresi bul
            int r0 = 0, r1 = 0;
            for (int i = 0; i < rpmLen - 1; i++)
            {
                if (clampedRpm >= rpms[i] && clampedRpm <= rpms[i + 1])
                {
                    r0 = i; r1 = i + 1;
                    break;
                }
            }

            // Boost Hücresi bul
            int b0 = 0, b1 = 0;
            for (int j = 0; j < boostLen - 1; j++)
            {
                if (clampedBoost >= boosts[j] && clampedBoost <= boosts[j + 1])
                {
                    b0 = j; b1 = j + 1;
                    break;
                }
            }

            // Ağırlık katsayıları
            double xFrac = (clampedRpm - rpms[r0]) / (rpms[r1] - rpms[r0]);
            double yFrac = (clampedBoost - boosts[b0]) / (boosts[b1] - boosts[b0]);

            // Bilinear Interpolation
            double q00 = duties[r0, b0];
            double q01 = duties[r0, b1];
            double q10 = duties[r1, b0];
            double q11 = duties[r1, b1];

            double r1Val = (1.0 - xFrac) * q00 + xFrac * q10;
            double r2Val = (1.0 - xFrac) * q01 + xFrac * q11;

            return (1.0 - yFrac) * r1Val + yFrac * r2Val;
        }
    }
}
