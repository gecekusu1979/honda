#pragma warning disable CS8618, CS8600, CS8601, CS8602, CS8603, CS8604, CS8765, CS8629, CS8622, CS0168
using System;
using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public class RecoveryMetaData
    {
        public string TransactionId { get; set; } = null!;
        public string SnapshotId { get; set; } = null!;
        public double PreviousChecksum { get; set; }
        public double ExpectedChecksum { get; set; }
        public string RollbackStatus { get; set; } // Pending, Completed, Failed = null!;
        public string EcuProfile { get; set; } = null!;
        public string ActiveUser { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public EnvironmentalContext Environment { get; set; } = null!;
        public List<CellSnapshot> PreviousCellValues { get; set; } = new List<CellSnapshot>();
    }

    public interface ICalibrationRecoveryManager
    {
        void RegisterPendingTransaction(RecoveryMetaData meta);
        void ClearPendingTransaction();
        bool DetectPendingTransaction(out RecoveryMetaData meta);
        bool PerformRecoveryRollback(ICalibrationSnapshotManager snapshotManager, out string resultMessage);
    }
}