using System;
using HondaTuner.Core.Logging;

namespace HondaTuner.Calibration.ColdStart
{
    /// <summary>
    /// Soğuk Çalıştırma Kalibrasyon Modülü.
    /// ECT (motor soğutma suyu sıcaklığı) bazlı zenginleştirme tablolarını yönetir.
    /// </summary>
    public class ColdStartCalibration
    {
        // ECT sıcaklık noktaları (°C) ve karşılık gelen zenginleştirme çarpanları
        // -40°C'den 80°C'ye kadar 10 nokta
        private static readonly int[] EctTempPoints =
            { -40, -20, -10, 0, 10, 20, 30, 40, 60, 80 };

        // Varsayılan zenginleştirme çarpanları (% olarak ek yakıt)
        private double[] _ectEnrichmentPercent =
            { 85, 65, 50, 40, 28, 18, 10, 5, 2, 0 };

        // İlk çalışma sonrası ek yakıt süresi (ms)
        public double AfterStartEnrichmentMs { get; set; } = 1200;

        // Isınma süresi yakıt faktörü (normal 1.0, soğuğa göre artar)
        public double WarmupFuelFactor { get; set; } = 1.35;

        /// <summary>
        /// Verilen ECT sıcaklığı için % zenginleştirme miktarını döner.
        /// Ara değerler lineer interpolasyonla hesaplanır.
        /// </summary>
        public double GetEnrichmentAtTemp(double ectCelsius)
        {
            if (ectCelsius <= EctTempPoints[0])
                return _ectEnrichmentPercent[0];
            if (ectCelsius >= EctTempPoints[EctTempPoints.Length - 1])
                return _ectEnrichmentPercent[_ectEnrichmentPercent.Length - 1];

            for (int i = 0; i < EctTempPoints.Length - 1; i++)
            {
                if (ectCelsius >= EctTempPoints[i] && ectCelsius <= EctTempPoints[i + 1])
                {
                    double t = (ectCelsius - EctTempPoints[i]) /
                               (double)(EctTempPoints[i + 1] - EctTempPoints[i]);
                    return _ectEnrichmentPercent[i] +
                           t * (_ectEnrichmentPercent[i + 1] - _ectEnrichmentPercent[i]);
                }
            }
            return 0;
        }

        /// <summary>
        /// ECT zenginleştirme tablosunu kullanıcı tanımlı değerlerle günceller.
        /// </summary>
        public void SetEnrichmentTable(double[] newValues)
        {
            if (newValues == null || newValues.Length != EctTempPoints.Length)
                throw new ArgumentException($"Tablo {EctTempPoints.Length} eleman içermelidir.");

            _ectEnrichmentPercent = (double[])newValues.Clone();
            ApplicationLogger.Info("ColdStartCalibration", "ECT zenginleştirme tablosu güncellendi.");
        }

        public int[] GetTempPoints() => (int[])EctTempPoints.Clone();
        public double[] GetEnrichmentTable() => (double[])_ectEnrichmentPercent.Clone();
    }
}
