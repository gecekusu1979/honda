using System;
using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public class RecoveryMetaData
    {
        public string TransactionId { get; set; }
        public string SnapshotId { get; set; }
        public double PreviousChecksum { get; set; }
        public double ExpectedChecksum { get; set; }
        public string RollbackStatus { get; set; } // Pending, Completed, Failed
        public string EcuProfile { get; set; }
        public string ActiveUser { get; set; }
        public DateTime Timestamp { get; set; }
        public EnvironmentalContext Environment { get; set; }
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
