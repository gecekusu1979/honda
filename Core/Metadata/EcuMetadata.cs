using System;
using System.IO;
using System.Text.Json;

namespace HondaTuner.Core.Metadata
{
    /// <summary>
    /// Projeye veya ROM dosyasına eşlik eden ECU ve motor meta veri modeli.
    /// </summary>
    public class EcuMetadata
    {
        public string SerialNumber { get; set; } = "HT-00000";
        public string HardwareRevision { get; set; } = "OBD1-A";
        public string Vin { get; set; } = "1HGEC000000000000";
        public string Chassis { get; set; } = "EG6";
        public double CompressionRatio { get; set; } = 9.2;
        public string CamshaftProfile { get; set; } = "OEM";
        public string GearboxType { get; set; } = "S40";
        public string InductionType { get; set; } = "N/A"; // N/A, Turbo, Supercharger

        /// <summary>
        /// Meta verileri JSON formatında serileştirir.
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// JSON verisini EcuMetadata nesnesine dönüştürür.
        /// </summary>
        public static EcuMetadata FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new EcuMetadata();
            try
            {
                return JsonSerializer.Deserialize<EcuMetadata>(json);
            }
            catch
            {
                return new EcuMetadata();
            }
        }
    }
}
