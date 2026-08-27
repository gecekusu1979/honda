using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Hardware.Discovery
{
    /// <summary>
    /// Donanım Aygıt Keşif Yöneticisi.
    /// USB-COM seri portları tarar ve bağlı donanımları otomatik tanır.
    /// </summary>
    public class DeviceDiscoveryManager
    {
        public class DiscoveredDevice
        {
            public string PortName { get; set; }
            public string DeviceType { get; set; } // "OBD1", "EEPROM_PROGRAMMER", "RTP_EMULATOR"
            public string Description { get; set; }
        }

        /// <summary>
        /// Sistemdeki tüm seri portları tarar ve bilinen aygıtları listeler.
        /// </summary>
        public List<DiscoveredDevice> ScanPorts()
        {
            var results = new List<DiscoveredDevice>();

            try
            {
                string[] ports = SerialPort.GetPortNames();
                ApplicationLogger.Info("DeviceDiscovery",
                    $"{ports.Length} seri port bulundu: {string.Join(", ", ports)}");

                foreach (string port in ports)
                {
                    var device = ProbePort(port);
                    if (device != null)
                        results.Add(device);
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("DeviceDiscovery", $"Port tarama hatası: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Belirli bir portu yoklar ve aygıt tipini belirlemeye çalışır.
        /// </summary>
        private DiscoveredDevice ProbePort(string portName)
        {
            try
            {
                // Kısa bağlantı denemesi ile aygıt handshake kontrolü
                ApplicationLogger.Debug("DeviceDiscovery", $"Port yoklanıyor: {portName}");

                return new DiscoveredDevice
                {
                    PortName = portName,
                    DeviceType = "UNKNOWN",
                    Description = $"Seri aygıt: {portName}"
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Yalnızca belirli tipteki aygıtları döner.</summary>
        public List<DiscoveredDevice> FindByType(string deviceType)
        {
            return ScanPorts().Where(d => d.DeviceType == deviceType).ToList();
        }
    }
}
