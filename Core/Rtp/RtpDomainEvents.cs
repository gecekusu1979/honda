using System;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Core.Rtp
{
    public class ConnectionEstablishedEvent : IRtpDomainEvent
    {
        public string EventName => "ConnectionEstablished";
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Message => "RTP Emulator connected successfully.";
    }

    public class ConnectionLostEvent : IRtpDomainEvent
    {
        public string EventName => "ConnectionLost";
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Message { get; }
        public string Reason { get; }

        public ConnectionLostEvent(string reason)
        {
            Reason = reason;
            Message = $"RTP Emulator connection lost. Reason: {reason}";
        }
    }

    public class SyncStartedEvent : IRtpDomainEvent
    {
        public string EventName => "SyncStarted";
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Message => "Real-time synchronization enabled.";
    }

    public class SyncCompletedEvent : IRtpDomainEvent
    {
        public string EventName => "SyncCompleted";
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Message => "Full ROM synchronization completed successfully.";
    }

    public class SyncFailedEvent : IRtpDomainEvent
    {
        public string EventName => "SyncFailed";
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Message { get; }
        public string ErrorDetails { get; }

        public SyncFailedEvent(string errorDetails)
        {
            ErrorDetails = errorDetails;
            Message = $"Real-time synchronization failed. Error: {errorDetails}";
        }
    }

    public class CalibrationSentEvent : IRtpDomainEvent
    {
        public string EventName => "CalibrationSent";
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Message { get; }
        public CalibrationChange Change { get; }
        public double LatencyMs { get; }

        public CalibrationSentEvent(CalibrationChange change, double latencyMs)
        {
            Change = change;
            LatencyMs = latencyMs;
            Message = $"Calibration sent to offset 0x{change.Offset:X4} (New: {change.NewValue}) in {latencyMs:F2}ms.";
        }
    }
}
