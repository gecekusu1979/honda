#pragma warning disable CS8618, CS8600, CS8601, CS8602, CS8603, CS8604, CS8765, CS8629, CS8622, CS0168
using System;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Event Bus üzerinde yayınlanabilecek telemetri ve sistem olay türleridir.
    /// </summary>
    public enum TelemetryEventType
    {
        // Bus Lifecycle Olayları
        BusStarted,
        BusStopped,

        // Veri ve Sağlık Durumu Olayları
        FrameReceived,
        DiagnosticMessage,
        ErrorOccurred,

        // Yapılandırma ve Kayıt Olayları
        ProfileChanged,
        ProviderStateChanged,
        SessionStarted,
        SessionStopped
    }

    /// <summary>
    /// Event Bus üzerinde taşınan olay nesnesidir.
    /// </summary>
    public class TelemetryEvent
    {
        public TelemetryEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public object Payload { get; set; }        // Olay verisi (örn. TelemetryFrame, Exception, string) = null!;
        public string Source { get; set; }         // Olayı üreten kaynak adı = null!;
        public string Message { get; set; }        // İsteğe bağlı açıklayıcı mesaj = null!;
        public MessagePriority Priority { get; set; }
    }
}