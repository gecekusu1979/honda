using System;
using System.Collections.Generic;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Core.Algorithms
{
    /// <summary>
    /// Dyno Analiz Modülü — RPM sweep verilerinden HP ve Tork tahmini.
    /// Telemetri datalog'undan araç hızı ve RPM verisini kullanarak
    /// motor performans eğrilerini hesaplar.
    /// </summary>
    public class DynoAnalyzer
    {
        // ── Fiziksel Sabitler ──────────────────────────────────────
        private const double GravityMs2 = 9.81;
        private const double KmhToMs = 1.0 / 3.6;
        // HP = (Force * Velocity) / 745.7
        private const double WattToHp = 745.7;

        /// <summary>
        /// Telemetri çerçevelerinden HP ve Tork eğrisi hesaplar.
        /// </summary>
        /// <param name="frames">Kronolojik sıralı telemetri verileri</param>
        /// <param name="vehicleWeightKg">Araç ağırlığı (kg)</param>
        /// <param name="drivetrainLossPercent">Aktarma organı kayıp yüzdesi (varsayılan %15)</param>
        public List<DynoPoint> CalculateCurves(
            List<TelemetryFrameData> frames,
            double vehicleWeightKg,
            double drivetrainLossPercent = 15.0)
        {
            var results = new List<DynoPoint>();
            if (frames == null || frames.Count < 2) return results;

            double drivetrainFactor = 1.0 + (drivetrainLossPercent / 100.0);

            for (int i = 1; i < frames.Count; i++)
            {
                var prev = frames[i - 1];
                var curr = frames[i];

                // Sadece WOT (full throttle) sweep analizine izin ver
                if (curr.Tps < 85) continue;

                // Hız farkından ivme hesapla (m/s²)
                double v1 = prev.Map * KmhToMs; // MAP yerine speed kullanılmalı
                double v2 = curr.Map * KmhToMs; // Telemetri speed alanı yoksa MAP proxy

                // Gerçekte araç hızı kullanılmalı — burada simülasyon için
                // RPM'den hız tahmini (basitleştirilmiş)
                double speedKmh1 = EstimateSpeedFromRpm(prev.Rpm);
                double speedKmh2 = EstimateSpeedFromRpm(curr.Rpm);

                double s1 = speedKmh1 * KmhToMs;
                double s2 = speedKmh2 * KmhToMs;

                // dt varsayımı: 100ms (10 Hz datalog)
                double dt = 0.1;
                double acceleration = (s2 - s1) / dt;

                if (acceleration <= 0) continue;

                // Force = m * a (Newton)
                double force = vehicleWeightKg * acceleration;

                // Power (Watt) = F * v
                double powerWatt = force * s2;

                // Wheel HP → Flywheel HP (drivetrain loss eklenir)
                double hp = (powerWatt / WattToHp) * drivetrainFactor;

                // Torque (Nm) = (HP * 5252) / RPM  (converted from ft-lb)
                // Veya: Torque = Power / angular_velocity
                double torqueNm = (hp * 7120.0) / curr.Rpm; // 7120 = Nm dönüşüm sabiti

                results.Add(new DynoPoint
                {
                    Rpm = curr.Rpm,
                    SpeedKmh = speedKmh2,
                    Horsepower = Math.Round(hp, 1),
                    TorqueNm = Math.Round(torqueNm, 1)
                });
            }

            return results;
        }

        /// <summary>RPM'den basitleştirilmiş hız tahmini (Civic 5. vites).</summary>
        private static double EstimateSpeedFromRpm(double rpm)
        {
            // Yaklaşık final ratio * tire size faktörü
            return rpm * 0.0167; // ~120 km/h @ 7200 RPM
        }
    }

    public class DynoPoint
    {
        public double Rpm { get; set; }
        public double SpeedKmh { get; set; }
        public double Horsepower { get; set; }
        public double TorqueNm { get; set; }
    }
}
