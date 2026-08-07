using System;

namespace HondaTuner.Calibration.VtecBoost
{
    public class VtecBoostTables
    {
        // 1. VTEC Parametreleri
        public double VtecMinRpm { get; set; } = 4800.0;
        public double VtecMinSpeed { get; set; } = 20.0; // km/h
        public bool[] VtecGearRestrictions { get; set; } = { true, false, false, false, false, false }; // Hangi viteslerde VTEC engelli (varsayılan: 1. viteste kapalı/true)

        // 2. Boost Target Table (RPM Bins: 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000; Gear Bins: 1, 2, 3, 4, 5)
        public double[] BoostRpmBins { get; set; } = { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000 };
        public double[,] BoostTargets { get; set; } = new double[8, 5]
        {
            // Vites 1, Vites 2, Vites 3, Vites 4, Vites 5 (kPa cinsinden hedefler)
            { 100.0, 100.0, 100.0, 100.0, 100.0 }, // 1000 RPM
            { 110.0, 120.0, 120.0, 120.0, 120.0 }, // 2000 RPM
            { 120.0, 140.0, 150.0, 150.0, 150.0 }, // 3000 RPM
            { 130.0, 160.0, 180.0, 180.0, 180.0 }, // 4000 RPM
            { 140.0, 170.0, 200.0, 200.0, 200.0 }, // 5000 RPM
            { 145.0, 180.0, 210.0, 210.0, 210.0 }, // 6000 RPM
            { 145.0, 180.0, 210.0, 210.0, 210.0 }, // 7000 RPM
            { 140.0, 170.0, 200.0, 200.0, 200.0 }  // 8000 RPM
        };

        // 3. Base Wastegate Duty Solenoid (RPM Bins: 1000-8000; Boost Bins: 100, 130, 160, 200, 220 kPa)
        public double[] WgBoostBins { get; set; } = { 100, 130, 160, 200, 220 };
        public double[,] BaseWgDuty { get; set; } = new double[8, 5]
        {
            // 100kPa, 130kPa, 160kPa, 200kPa, 220kPa (Duty % cinsinden)
            { 10.0, 15.0, 20.0, 30.0, 35.0 }, // 1000 RPM
            { 15.0, 25.0, 35.0, 45.0, 50.0 }, // 2000 RPM
            { 20.0, 35.0, 48.0, 58.0, 62.0 }, // 3000 RPM
            { 22.0, 40.0, 54.0, 68.0, 72.0 }, // 4000 RPM
            { 25.0, 45.0, 60.0, 75.0, 80.0 }, // 5000 RPM
            { 25.0, 45.0, 62.0, 78.0, 84.0 }, // 6000 RPM
            { 22.0, 42.0, 60.0, 75.0, 82.0 }, // 7000 RPM
            { 20.0, 38.0, 55.0, 70.0, 78.0 }  // 8000 RPM
        };

        // 4. Scramble Boost Ayarları
        public double ScrambleBoostAdd { get; set; } = 30.0; // +30 kPa (yaklaşık +4.3 psi)
        public double ScrambleDuration { get; set; } = 5.0; // 5 saniye
    }
}
