using System;

namespace HondaTuner.Calibration.EngineProtection
{
    public class EngineProtectionTables
    {
        // 1. Yağ Sıcaklığı Limitleri
        public double MaxOilTemp { get; set; } = 125.0; // °C

        // 2. Minimum Yağ Basıncı Haritası (RPM Bins vs Min Pressure Bar)
        public double[] OilPressRpmBins { get; set; } = { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000 };
        public double[] MinOilPressureCurve { get; set; } = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.2, 3.5, 3.5 }; // Bar cinsinden yağ basıncı

        // 3. Yakıt Basıncı Limiti
        public double MinFuelPressure { get; set; } = 2.8; // Bar (Stock manifold 40 psi yaklaşık 2.8 bar)

        // 4. Fan Çalışma Derecesi
        public double FanTargetTemp { get; set; } = 92.0; // °C

        // 5. IAT Heat Soak Düzeltmeleri
        public double IatHeatSoakRetardThreshold { get; set; } = 55.0; // °C
        public double IatHeatSoakRetard { get; set; } = 4.0; // ° (Avans kısma derecesi)
        public double IatBoostLimitReduction { get; set; } = 20.0; // kPa

        // 6. EGT Termal Isı Limitleri
        public double MaxEgtLimit { get; set; } = 900.0; // °C
        public double EgtTimingPull { get; set; } = 3.0; // ° (Avans kısma derecesi)
        public double EgtFuelEnrichment { get; set; } = 15.0; // %15 ekstra enjeksiyon artışı

        // 7. Limp Mod Limitleri
        public double ThermalLimpModeRpm { get; set; } = 3000.0; // RPM

        // ── YENİ: Lean Cut Eşikleri ──────────────────────────────
        /// <summary>RPM eşiği: Bu değerin üzerinde ve yüksek yükte lean cut devreye girer.</summary>
        public double LeanCutRpmThreshold { get; set; } = 4000.0;
        /// <summary>MAP eşiği (kPa): Bu değerin üzerinde lean AFR tehlikeli kabul edilir.</summary>
        public double LeanCutMapThreshold { get; set; } = 120.0;
        /// <summary>AFR eşiği: Bu değerin üzerinde fakir yanma koruması devreye girer.</summary>
        public double LeanCutAfrThreshold { get; set; } = 12.8;

        // ── YENİ: Overboost Cut Eşikleri ─────────────────────────
        /// <summary>Hedef boost basıncı (kPa). Buna OverboostMarginKpa eklenerek kesme limiti hesaplanır.</summary>
        public double TargetBoostKpa { get; set; } = 150.0;
        /// <summary>Overboost marjı (kPa): Hedef boost üzerinde bu kadar tolerans tanınır.</summary>
        public double OverboostMarginKpa { get; set; } = 25.0;

        // ── YENİ: ECT Dinamik Timing Retard Eşikleri ─────────────
        /// <summary>ECT bu sıcaklığı aşarsa dinamik avans geri çekmesi başlar (°C).</summary>
        public double EctCriticalRetardThreshold { get; set; } = 102.0;
        /// <summary>ECT retardının minimum değeri (102°C'de -2.0°).</summary>
        public double EctTimingRetardMin { get; set; } = 2.0;
        /// <summary>ECT retardının maksimum değeri (110°C'de -4.0°).</summary>
        public double EctTimingRetardMax { get; set; } = 4.0;

        // ── YENİ: Knock Timing Retard Parametreleri ──────────────
        /// <summary>Vuruntu algılandığında anlık avans geri çekme derecesi.</summary>
        public double KnockTimingRetard { get; set; } = 3.0;
        /// <summary>Vuruntu kesildikten sonra her saniyede kaç derece toparlama yapılır.</summary>
        public double KnockRecoveryRate { get; set; } = 0.5; // °/saniye
    }
}
