using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Interfaces
{
    /// <summary>
    /// Kalibrasyon değişiklik takibi ve genel kalibrasyon servisi.
    /// </summary>
    public interface ICalibrationService
    {
        void RecordChange(CalibrationChange change);
        IReadOnlyList<CalibrationChange> GetChangeHistory();
        void ClearHistory();

        // V2 Transaction support
        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
        void Undo();
        void Redo();
        bool CanUndo { get; }
        bool CanRedo { get; }
        bool HasActiveTransaction { get; }

        event Action<CalibrationChange> OnCalibrationChanged;
    }

    public class CalibrationChange
    {
        public string Parameter { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public int CellRow { get; set; }
        public int CellCol { get; set; }
        public double ChangePercent { get; set; }

        // V2 Properties
        public int Offset { get; set; }
        public string MapName { get; set; }
        public string UserAction { get; set; }
    }
}
