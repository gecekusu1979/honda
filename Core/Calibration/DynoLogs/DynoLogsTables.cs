using System;
using System.Collections.Generic;

namespace HondaTuner.Calibration.DynoLogs
{
    public class DynoLogsTables
    {
        // 1. Virtual Dyno Girdileri
        public double VehicleWeightKg { get; set; } = 1100.0;
        public double DrivetrainLossPct { get; set; } = 15.0; // %15 kayıp
        public string CorrectionFactorType { get; set; } = "SAE"; // SAE, DIN, NONE

        // 2. Şanzıman Oranları (3. Vites Varsayılan)
        public double SelectedGearRatio { get; set; } = 1.52;
        public double FinalDriveRatio { get; set; } = 4.26;
        public double TyreDiameterInches { get; set; } = 23.0;

        // 3. İzleme Listesi MCU Değişkenleri
        public List<string> RamWatchlist { get; set; } = new List<string>
        {
            "VTEC_ACTIVE",
            "AFR_TARGET",
            "IGN_ADVANCE",
            "MANIFOLD_KPA",
            "ECT_CELSIUS"
        };
    }
}
