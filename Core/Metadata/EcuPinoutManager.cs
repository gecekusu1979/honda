using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.Metadata
{
    public class EcuPinoutPin
    {
        public string PinNumber { get; set; }
        public string Connector { get; set; }
        public string Symbol { get; set; }
        public string SignalType { get; set; }
        public string WiringColor { get; set; }
        public string Description { get; set; }
    }

    public class EcuPinoutManager
    {
        private static readonly object LockObj = new object();
        private static EcuPinoutManager _instance;
        private readonly List<EcuPinoutPin> _pins = new List<EcuPinoutPin>();

        public static EcuPinoutManager Instance
        {
            get
            {
                lock (LockObj)
                {
                    return _instance ??= new EcuPinoutManager();
                }
            }
        }

        private EcuPinoutManager()
        {
            LoadPinouts();
        }

        private void LoadPinouts()
        {
            // Veritabanı dizinini bul
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dbDir = Path.Combine(baseDir, "Database");
            string pinoutFile = Path.Combine(dbDir, "ecu_pinouts.json");

            if (!File.Exists(pinoutFile))
            {
                // Fallback 1: Test ve proje geliştirme ortamlarında
                dbDir = Path.Combine(baseDir, "..", "..", "..", "Database");
                pinoutFile = Path.Combine(dbDir, "ecu_pinouts.json");
            }

            if (!File.Exists(pinoutFile))
            {
                // Fallback 2: Working directory
                dbDir = Path.Combine(Directory.GetCurrentDirectory(), "Database");
                pinoutFile = Path.Combine(dbDir, "ecu_pinouts.json");
            }

            if (!File.Exists(pinoutFile))
            {
                ApplicationLogger.Warn("EcuPinoutManager", "ecu_pinouts.json dosyası bulunamadı, varsayılan boş başlatılıyor.");
                return;
            }

            try
            {
                string json = File.ReadAllText(pinoutFile);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<EcuPinoutPin>>(json, options);
                if (list != null)
                {
                    _pins.Clear();
                    _pins.AddRange(list);
                    ApplicationLogger.Info("EcuPinoutManager", $"{list.Count} OBD1 ECU pin tanımlaması başarıyla yüklendi.");
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("EcuPinoutManager", $"ecu_pinouts.json yükleme hatası: {ex.Message}");
            }
        }

        public IReadOnlyList<EcuPinoutPin> GetAllPins()
        {
            return _pins;
        }

        public List<EcuPinoutPin> GetPinsByConnector(string connector)
        {
            var result = new List<EcuPinoutPin>();
            foreach (var pin in _pins)
            {
                if (pin.Connector.Equals(connector, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(pin);
                }
            }
            return result;
        }
    }
}
