using System;
using System.Collections.Generic;
using System.Threading;
using HondaTuner.Core;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

/*
 * Honda OBD1 DTC Manager
 * Read/Clear Diagnostic Trouble Codes via serial OBD1 protocol.
 * Also provides ROM-level DTC bypass for offline use.
 *
 * REQUIRES TESTING WITH REAL OBD1 HARDWARE.
 */

namespace HondaTuner.Hardware.OBD
{
    /// <summary>Single Honda OBD1 Diagnostic Trouble Code.</summary>
    public class HondaDtc
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public override string ToString() => $"P{Code:D4} — {Description}";
    }

    public class DtcManager
    {
        private const byte CMD_READ_DTC = 0x43;
        private const byte CMD_CLEAR_DTC = 0x44;
        private const int DTC_TIMEOUT = 2000; // ms

        // ── Honda OBD1 DTC code descriptions ───────────────────────────
        // Source: Honda FSM (Factory Service Manual) + community database
        private static readonly Dictionary<int, string> DtcDescriptions = new Dictionary<int, string>
        {
            { 1,  "Manifold Absolute Pressure (MAP) sensörü arızası" },
            { 3,  "Manifold Absolute Pressure (MAP) sensörü devresi" },
            { 4,  "Crank Position (CKP) sensörü yok — düşük devir" },
            { 5,  "Manifold Absolute Pressure (MAP) sensörü — aralık dışı" },
            { 6,  "Engine Coolant Temp (ECT) sensörü aralık dışı" },
            { 7,  "Throttle Position (TPS) sensörü aralık dışı" },
            { 8,  "Crank Position (CKP) sensörü — TDC/CYL" },
            { 9,  "Crank Position (CKP) sensörü yok — No.1 CYL" },
            { 10, "Intake Air Temp (IAT) sensörü aralık dışı" },
            { 12, "Egzost Gas Recirculation (EGR) sistemi hatası" },
            { 13, "BARO (atmosfer basıncı) sensör arızası" },
            { 14, "Elektronik Yük Dedektörü (ELD) devresi" },
            { 15, "Ignition Output Signal — eksik ya da zayıf" },
            { 16, "Fuel Injector devresi hatası" },
            { 17, "Vehicle Speed Sensor (VSS) devresi hatası" },
            { 20, "Elektrikli Yük Dedektörü (ELD) — aralık dışı" },
            { 21, "VTEC Solenoid Valf devresi hatası" },
            { 22, "VTEC Basınç Anahtarı — açık devre / yanlış basınç" },
            { 23, "Knock Sensor devresi hatası" },
            { 30, "A/T FI sinyal hatası (manuel için geçersiz)" },
            { 41, "Ön Lambda (O2) sensörü ısıtıcı devresi" },
            { 43, "Yakıt Sistemi çok zengin (uzun dönem)" },
            { 45, "Yakıt Sistemi çok zayıf (uzun dönem)" },
            { 54, "Crank Position (CKP) sensörü — CYL sinyali eksik" },
            { 61, "Ön Lambda (O2) sensörü yavaş yanıt" },
            { 63, "Arka Lambda (O2) sensörü devresi (uygulanabilirse)" },
            { 65, "Arka Lambda (O2) sensörü ısıtıcı" },
            { 67, "Lambda sensörü ısıtıcı akımı" },
            { 70, "Otomatik Vites Kilidi Kilidi (yalnızca A/T)" },
            { 71, "Rastgele/çoklu ateşleme arızası — tüm silindirler" },
            { 72, "Rastgele/çoklu ateşleme — silindir 1" },
            { 73, "Rastgele/çoklu ateşleme — silindir 2" },
            { 74, "Rastgele/çoklu ateşleme — silindir 3" },
            { 75, "Rastgele/çoklu ateşleme — silindir 4" },
            { 80, "Exhaust Gas Recirculation (EGR) yetersiz akışı" },
            { 86, "Soğutma Sistemi — termostat arızası" },
            { 92, "Evaporatif Salınım Sistemi — sızıntı tespiti" },
        };

        // ── Live DTC operations ─────────────────────────────────────────

        /// <summary>
        /// Reads active DTCs from ECU via OBD1 serial command 0x43.
        /// Returns list of decoded DTC objects.
        /// </summary>
        public List<HondaDtc> ReadDtcsLive(IObdConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (connection.State != ConnectionState.Connected)
                throw new InvalidOperationException("OBD1 bağlantısı kurulu değil.");

            var dtcs = new List<HondaDtc>();

            try
            {
                ApplicationLogger.Info("DtcManager", "Hata kodları okunuyor (0x43)...");

                // Send DTC read request
                var port = GetSerialPort(connection);
                port.Write(new byte[] { CMD_READ_DTC }, 0, 1);
                Thread.Sleep(200);

                // Read response until 0xFF terminator or timeout
                var deadline = DateTime.UtcNow.AddMilliseconds(DTC_TIMEOUT);
                while (DateTime.UtcNow < deadline && port.BytesToRead > 0)
                {
                    int b = port.ReadByte();
                    if (b < 0 || b == 0xFF) break; // 0xFF = end of DTC list

                    // Each code byte maps directly to a Honda DTC number
                    if (b != 0x00)
                    {
                        dtcs.Add(new HondaDtc
                        {
                            Code = b,
                            Description = DtcDescriptions.TryGetValue(b, out var desc) ? desc : $"Bilinmeyen DTC kodu: {b}",
                            IsActive = true
                        });
                    }
                }

                ApplicationLogger.Info("DtcManager", $"{dtcs.Count} hata kodu okundu.");
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("DtcManager", $"DTC okuma hatası: {ex.Message}");
                throw;
            }

            return dtcs;
        }

        /// <summary>
        /// Clears active DTCs from ECU via OBD1 serial command 0x44.
        /// </summary>
        public void ClearDtcsLive(IObdConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (connection.State != ConnectionState.Connected)
                throw new InvalidOperationException("OBD1 bağlantısı kurulu değil.");

            try
            {
                ApplicationLogger.Info("DtcManager", "Hata kodları temizleniyor (0x44)...");

                var port = GetSerialPort(connection);
                port.Write(new byte[] { CMD_CLEAR_DTC }, 0, 1);
                Thread.Sleep(500); // ECU needs time to process clear command
                // ECU echoes 0x44 on success — drain the echo
                while (port.BytesToRead > 0) port.ReadByte();

                ApplicationLogger.Info("DtcManager", "Hata kodları başarıyla temizlendi.");
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("DtcManager", $"DTC temizleme hatası: {ex.Message}");
                throw;
            }
        }

        // ── ROM-level DTC bypass ────────────────────────────────────────

        /// <summary>
        /// Sets DTC bypass flags in the loaded ROM for offline use.
        /// Safe to call without a live OBD1 connection.
        /// </summary>
        public void ClearDtcsFromRom(RomParser parser, bool bypassKnock = true, bool bypassVtec = true,
            bool bypassO2 = true, bool bypassEld = true)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            ApplicationLogger.Info("DtcManager", "ROM DTC bypass bayrakları yazılıyor...");

            // ROM offsets from prompts: 0x1FB6=Knock, 0x1FB7=VTEC, 0x1FB8=O2, 0x1FB9=ELD
            if (bypassKnock) parser.WriteDtcBypass(0x1FB6, true);
            if (bypassVtec) parser.WriteDtcBypass(0x1FB7, true);
            if (bypassO2) parser.WriteDtcBypass(0x1FB8, true);
            if (bypassEld) parser.WriteDtcBypass(0x1FB9, true);

            ApplicationLogger.Info("DtcManager", "ROM DTC bypass tamamlandı.");
        }

        /// <summary>Reads current ROM DTC bypass states.</summary>
        public (bool Knock, bool Vtec, bool O2, bool Eld) ReadDtcBypassState(RomParser parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            return (
                parser.ReadDtcBypass(0x1FB6),
                parser.ReadDtcBypass(0x1FB7),
                parser.ReadDtcBypass(0x1FB8),
                parser.ReadDtcBypass(0x1FB9)
            );
        }

        /// <summary>Returns description for a given DTC code number.</summary>
        public static string GetDescription(int code)
        {
            return DtcDescriptions.TryGetValue(code, out var d) ? d : $"Bilinmeyen kod: {code}";
        }

        // Reflection to get the serial port from the concrete implementation
        private static System.IO.Ports.SerialPort GetSerialPort(IObdConnection connection)
        {
            var field = connection.GetType().GetField("_port",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sp = field?.GetValue(connection) as System.IO.Ports.SerialPort;
            if (sp == null || !sp.IsOpen)
                throw new InvalidOperationException("Seri port açık değil ya da erişilemiyor.");
            return sp;
        }
    }
}
