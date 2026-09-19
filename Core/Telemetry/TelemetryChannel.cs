#pragma warning disable CS8618, CS8600, CS8601, CS8602, CS8603, CS8604, CS8765, CS8629, CS8622, CS0168
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
        public string ChannelId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public string Group { get; set; } // Engine, Fuel, Ignition, Sensors, Temperature, Pressure, Electrical, Transmission, Diagnostics, Calculated = null!;
        public string Category { get; set; } = null!;
        public string Priority { get; set; } // Critical, High, Normal, Low = null!;
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
        public string RawType { get; set; } // Byte, Int16, Int32 vb. = null!;
        public double Scale { get; set; } = 1.0;
        public double Offset { get; set; } = 0.0;
        public string Formula { get; set; } // Hesaplanan kanallar için string formül (örn: "[AFR] / 14.7") = null!;
    }
}