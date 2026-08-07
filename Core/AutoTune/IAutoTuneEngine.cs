using System;
using System.Collections.Generic;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune
{
    public interface IAutoTuneEngine
    {
        bool IsRunning { get; }
        AutoTuneSession ActiveSession { get; }
        AdaptiveMemory Memory { get; }
        CalibrationJournal Journal { get; }
        IReadOnlyList<CalibrationSnapshot> Snapshots { get; }
        TargetMapProvider TargetMapProvider { get; }
        ICalibrationRecoveryManager RecoveryManager { get; }
        ICalibrationSnapshotManager SnapshotManager { get; }

        bool StartSession(string ecuid, string userId, AutoTuneOperatingMode mode, string profile);
        void StopSession();
        void PauseSession();
        void ResumeSession();

        void ProcessTelemetry(TelemetrySnapshot telemetry);
        bool ApproveDecision(string decisionId);
        void RejectDecision(string decisionId);
        bool RollbackLastChange(out string resultMessage);

        event Action<IAutoTuneDomainEvent> OnDomainEvent;
        event Action<CalibrationStreamPayload> OnCalibrationStream;
    }
}
