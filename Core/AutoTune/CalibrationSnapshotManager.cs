using System;
using System.Collections.Generic;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationSnapshotManager : ICalibrationSnapshotManager
    {
        private readonly List<CalibrationSnapshot> _snapshots = new List<CalibrationSnapshot>();
        private readonly object _lockObj = new object();

        public CalibrationSnapshot CaptureSnapshot(
            string ecuIdentifier,
            string userRole,
            string activeProfile,
            double currentChecksum,
            SafetyResult safety,
            double confidence,
            EnvironmentalContext env,
            List<CellSnapshot> cells)
        {
            var snapshot = new CalibrationSnapshot
            {
                EcuIdentifier = ecuIdentifier,
                UserRole = userRole,
                ActiveProfile = activeProfile,
                RomChecksum = currentChecksum,
                Safety = safety,
                ConfidenceScore = confidence,
                TelemetryConditions = env != null ? new EnvironmentalContext
                {
                    Temperature = env.Temperature,
                    Altitude = env.Altitude,
                    Humidity = env.Humidity,
                    FuelType = env.FuelType,
                    OperatingConditions = env.OperatingConditions
                } : new EnvironmentalContext(),
                CellSnapshots = cells != null ? new List<CellSnapshot>(cells) : new List<CellSnapshot>(),
                IsRestored = false,
                Timestamp = DateTime.Now
            };

            lock (_lockObj)
            {
                _snapshots.Add(snapshot);
            }

            ApplicationLogger.Info("CalibrationSnapshotManager", $"Snapshot oluşturuldu: {snapshot.SnapshotId}, Hücre sayısı: {snapshot.CellSnapshots.Count}");
            return snapshot;
        }

        public void RestoreSnapshot(CalibrationSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.IsRestored)
            {
                ApplicationLogger.Warn("CalibrationSnapshotManager", $"Snapshot {snapshot.SnapshotId} zaten geri yüklendi!");
                return;
            }

            lock (_lockObj)
            {
                // Set restoration flag
                snapshot.IsRestored = true;
            }

            ApplicationLogger.Info("CalibrationSnapshotManager", $"Snapshot geri yükleniyor: {snapshot.SnapshotId}, Kurtarılacak hücre: {snapshot.CellSnapshots.Count}");
        }
    }
}
