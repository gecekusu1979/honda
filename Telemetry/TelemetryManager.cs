using System;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Telemetry
{
    /// <summary>
    /// Merkezi telemetri olay yolu (Event Bus).
    /// Tüm modüller doğrudan bağımlı olmak yerine bu bus üzerinden haberleşir:
    ///   TelemetryManager → FrameReceived event
    ///     → VisualTraceEngine
    ///     → AutoTuneEngine
    ///     → DatalogManager (CSV)
    /// </summary>
    public class TelemetryManager
    {
        public event EventHandler<TelemetryFrameEventArgs> FrameReceived;

        /// <summary>Telemetri verisini tüm dinleyicilere yayınlar.</summary>
        public void Publish(TelemetryFrameData frame)
        {
            FrameReceived?.Invoke(this, new TelemetryFrameEventArgs(frame));
        }
    }

    public class TelemetryFrameEventArgs : EventArgs
    {
        public TelemetryFrameData Frame { get; }
        public DateTime ReceivedAt { get; }

        public TelemetryFrameEventArgs(TelemetryFrameData frame)
        {
            Frame = frame;
            ReceivedAt = DateTime.UtcNow;
        }
    }
}
