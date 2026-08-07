using System;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri kanal örnekleme biçimi.
    /// </summary>
    public enum SamplingMode
    {
        FixedRate,
        OnChange,
        Manual,
        Burst,
        Adaptive
    }

    /// <summary>
    /// Kanal veri güvenilirlik kalitesi.
    /// </summary>
    public enum TelemetryQuality
    {
        Good,
        Poor,
        Bad
    }

    /// <summary>
    /// Kanal durum tanıları.
    /// </summary>
    public enum ChannelStatus
    {
        Valid,
        Estimated,
        Calculated,
        Timeout,
        Disconnected,
        Invalid,
        Suppressed
    }

    /// <summary>
    /// Tek bir telemetri kanalının yapılandırma ve meta veri modelidir.
    /// </summary>
    public class TelemetryChannel
    {
        public string ChannelId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public string Group { get; set; } // Engine, Fuel, Ignition, Sensors, Temperature, Pressure, Electrical, Transmission, Diagnostics, Calculated
        public string Category { get; set; }
        public string Priority { get; set; } // Critical, High, Normal, Low
        public int DisplayOrder { get; set; }

        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public int Precision { get; set; }

        public double SampleRate { get; set; } // Hz cinsinden
        public SamplingMode SamplingMode { get; set; }
        public int MaximumLatency { get; set; } // ms cinsinden beklenen maksimum gecikme

        public bool Visible { get; set; }
        public bool Loggable { get; set; }

        // Ölçeklendirme ve Ham Veri Tipleri
        public string RawType { get; set; } // Byte, Int16, Int32 vb.
        public double Scale { get; set; } = 1.0;
        public double Offset { get; set; } = 0.0;
        public string Formula { get; set; } // Hesaplanan kanallar için string formül (örn: "[AFR] / 14.7")
    }
}
