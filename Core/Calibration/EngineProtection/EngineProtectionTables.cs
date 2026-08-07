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
    }
}
