using System;
using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public class CellSnapshot
    {
        public string MapName { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public double Value { get; set; }
    }

    public class CalibrationSnapshot
    {
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString();
        public string EcuIdentifier { get; set; }
        public string ActiveProfile { get; set; }
        public string UserRole { get; set; }
        public double RomChecksum { get; set; }
        public SafetyResult Safety { get; set; }
        public double ConfidenceScore { get; set; }
        public EnvironmentalContext TelemetryConditions { get; set; }
        public List<CellSnapshot> CellSnapshots { get; set; } = new List<CellSnapshot>();
        public bool IsRestored { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public interface ICalibrationSnapshotManager
    {
        CalibrationSnapshot CaptureSnapshot(string ecuIdentifier, string userRole, string activeProfile, double currentChecksum, SafetyResult safety, double confidence, EnvironmentalContext env, List<CellSnapshot> cells);
        void RestoreSnapshot(CalibrationSnapshot snapshot);
    }
}
