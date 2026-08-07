using System;

namespace HondaTuner.Calibration.Diagnostics
{
    public class DiagnosticsTables
    {
        // 1. İletişim Protokolü Seçimi (OBD1, ISO9141, CAN_BUS, J2534)
        public string SelectedProtocol { get; set; } = "OBD1";

        // 2. Seri Port Hızı
        public int DataloggingBaudRate { get; set; } = 38400; // bps

        // 3. Kablosuz Ağ Ayarları
        public string WifiIpAddress { get; set; } = "192.168.1.10";
        public int WifiPort { get; set; } = 8080;

        // 4. Teşhis Özellikleri
        public bool EnableSelfTest { get; set; } = true;
        public bool AutoResetAlarms { get; set; } = false;
    }
}
