using System;

namespace HondaTuner.Calibration.Ignition
{
    public class AdvancedIgnitionTables
    {
        // 1. Cranking Timing Table: Motor Harareti (ECT °C) vs Avans Derekesi
        public double[] CrankingTimingEctBins { get; set; } = { -40, -20, 0, 20, 40, 60, 80 };
        public double[] CrankingTimingAdvances { get; set; } = { 15.0, 12.0, 10.0, 8.5, 7.0, 6.0, 5.0 }; // Derece avans

        // 2. Silindir Bazlı Ateşleme Düzeltmeleri (Cylinder Offset 1-4)
        public double[] CylinderOffsets { get; set; } = { 0.0, 0.0, 0.0, 0.0 }; // Varsayılan 0 offset

        // 3. Krank/Eksantrik Sensor Desen Profili (Crank and Cam trigger)
        public string CrankTriggerType { get; set; } = "Honda OEM 24+4+1";
        public double CrankTriggerOffset { get; set; } = 0.0; // Derece bazlı trigger offset
        public int CrankToothCount { get; set; } = 24;
        public int CamToothCount { get; set; } = 1;
    }
}
