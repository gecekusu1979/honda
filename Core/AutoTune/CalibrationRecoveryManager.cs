using System;
using System.IO;
using System.Text.Json;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationRecoveryManager : ICalibrationRecoveryManager
    {
        private readonly string _recoveryPath;

        public CalibrationRecoveryManager()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HondaTuner");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _recoveryPath = Path.Combine(appData, "pending_tune_transaction.json");
        }

        public void RegisterPendingTransaction(RecoveryMetaData meta)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            try
            {
                string json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_recoveryPath, json);
                ApplicationLogger.Info("CalibrationRecoveryManager", $"Bekleyen işlem kaydedildi: {meta.TransactionId}");
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("CalibrationRecoveryManager", $"İşlem kaydetme hatası: {ex.Message}");
            }
        }

        public void ClearPendingTransaction()
        {
            try
            {
                if (File.Exists(_recoveryPath))
                {
                    File.Delete(_recoveryPath);
                    ApplicationLogger.Info("CalibrationRecoveryManager", "Bekleyen işlem temizlendi (başarılı commit).");
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("CalibrationRecoveryManager", $"İşlem temizleme hatası: {ex.Message}");
            }
        }

        public bool DetectPendingTransaction(out RecoveryMetaData meta)
        {
            meta = null;
            try
            {
                if (File.Exists(_recoveryPath))
                {
                    string json = File.ReadAllText(_recoveryPath);
                    meta = JsonSerializer.Deserialize<RecoveryMetaData>(json);
                    return meta != null;
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("CalibrationRecoveryManager", $"Bekleyen işlem algılama hatası: {ex.Message}");
            }
            return false;
        }

        public bool PerformRecoveryRollback(ICalibrationSnapshotManager snapshotManager, out string resultMessage)
        {
            resultMessage = "";
            if (snapshotManager == null) throw new ArgumentNullException(nameof(snapshotManager));

            if (!DetectPendingTransaction(out var meta))
            {
                resultMessage = "Algılanan yarım kalmış işlem bulunamadı.";
                return false;
            }

            try
            {
                ApplicationLogger.Info("CalibrationRecoveryManager", $"Yarım kalmış işlem geri yükleniyor: {meta.TransactionId}");

                // Pack into a calibration snapshot to restore
                var mockSnapshot = new CalibrationSnapshot
                {
                    SnapshotId = meta.SnapshotId,
                    EcuIdentifier = meta.EcuProfile,
                    ActiveProfile = meta.EcuProfile,
                    UserRole = meta.ActiveUser,
                    RomChecksum = meta.PreviousChecksum,
                    TelemetryConditions = meta.Environment,
                    CellSnapshots = meta.PreviousCellValues,
                    IsRestored = false
                };

                snapshotManager.RestoreSnapshot(mockSnapshot);

                // Clean up registration
                ClearPendingTransaction();

                resultMessage = $"Zaman damgalı '{meta.Timestamp}' işlem başarıyla rolled back edildi.";
                return true;
            }
            catch (Exception ex)
            {
                resultMessage = $"Geri yükleme hatası: {ex.Message}";
                ApplicationLogger.Error("CalibrationRecoveryManager", resultMessage);
                return false;
            }
        }
    }
}
