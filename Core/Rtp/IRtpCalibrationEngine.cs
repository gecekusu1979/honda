using System;
using HondaTuner.Hardware.Emulator;

namespace HondaTuner.Core.Rtp
{
    public interface IRtpCalibrationEngine
    {
        RtpConnectionState ConnectionState { get; }
        bool IsSyncActive { get; }
        int QueueDepth { get; }
        long DroppedWritesCount { get; }
        double AvgSyncLatencyMs { get; }
        long RetryCount { get; }
        long FailureCount { get; }
        RtpConfig Configuration { get; }

        void ConnectEmulator();
        void DisconnectEmulator();
        void EnableSync();
        void DisableSync();
        void SyncFullCalibration();

        event Action<IRtpDomainEvent> OnRtpDomainEvent;
    }
}
