using System;
using System.Collections.Generic;
using HondaTuner.Core.Container;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Calibration
{
    /// <summary>
    /// Kalibrasyon değişiklik yöneticisi — tüm kalibrasyon düzenlemelerini
    /// işlem (transaction) bazlı olarak yönetir, yetkilendirir ve geri alma desteği sağlar.
    /// </summary>
    public class CalibrationManager : ICalibrationService
    {
        private readonly CalibrationHistory _history = new CalibrationHistory();
        private readonly CalibrationUndoManager _undoMgr = new CalibrationUndoManager();
        private readonly CalibrationValidator _validator = new CalibrationValidator();
        private CalibrationSession _session;

        private CalibrationTransaction _activeTx;
        private readonly object _lockObj = new object();

        public bool CanUndo => _undoMgr.CanUndo;
        public bool CanRedo => _undoMgr.CanRedo;
        public bool HasActiveTransaction => _activeTx != null;

        public event Action<CalibrationChange> OnCalibrationChanged;

        public CalibrationManager()
        {
            _session = new CalibrationSession("P28"); // Varsayılan oturum
            _history.AddAuditLog("Oturum başlatıldı (Varsayılan: P28).");
        }

        private IRomService GetRomService()
        {
            return ServiceContainer.Resolve<IRomService>();
        }

        public void BeginTransaction()
        {
            lock (_lockObj)
            {
                if (_activeTx != null)
                {
                    throw new InvalidOperationException("Zaten aktif bir kalibrasyon işlemi (transaction) devam ediyor.");
                }

                _activeTx = new CalibrationTransaction();
                _history.AddAuditLog($"İşlem başladı. ID: {_activeTx.TransactionId}");
            }
        }

        public void CommitTransaction()
        {
            lock (_lockObj)
            {
                if (_activeTx == null)
                {
                    throw new InvalidOperationException("Aktif bir işlem bulunamadı.");
                }

                if (_activeTx.Changes.Count == 0)
                {
                    _activeTx = null;
                    return;
                }

                // Tüm değişiklikleri doğrula
                foreach (var change in _activeTx.Changes)
                {
                    _validator.Validate(change);
                }

                _activeTx.Commit();
                _history.AddTransaction(_activeTx);
                _session.AddTransaction(_activeTx);
                _undoMgr.PushTransaction(_activeTx);

                _activeTx = null;
            }
        }

        public void RollbackTransaction()
        {
            lock (_lockObj)
            {
                if (_activeTx == null)
                {
                    throw new InvalidOperationException("Geri alınacak aktif bir işlem bulunamadı.");
                }

                // Değişiklikleri ters sırayla geri uygula
                var romService = GetRomService();
                if (romService != null && romService.IsLoaded)
                {
                    byte[] buffer = romService.GetBuffer();
                    var rollbackedChanges = new List<CalibrationChange>();
                    for (int i = _activeTx.Changes.Count - 1; i >= 0; i--)
                    {
                        var change = _activeTx.Changes[i];
                        RevertChangeInBuffer(buffer, change);
                        rollbackedChanges.Add(new CalibrationChange
                        {
                            Parameter = change.Parameter,
                            OldValue = change.NewValue,
                            NewValue = change.OldValue,
                            Offset = change.Offset,
                            MapName = change.MapName,
                            Source = "Rollback",
                            Timestamp = DateTime.Now
                        });
                    }
                    romService.SetBuffer(buffer);

                    foreach (var rollChange in rollbackedChanges)
                    {
                        OnCalibrationChanged?.Invoke(rollChange);
                    }
                }

                _activeTx.Rollback();
                _history.AddAuditLog($"İşlem geri alındı. ID: {_activeTx.TransactionId}");
                _activeTx = null;
            }
        }

        public void RecordChange(CalibrationChange change)
        {
            lock (_lockObj)
            {
                if (change == null) return;

                // İşlem öncesi girdi doğrulamaları (Ön doğrulama)
                _validator.Validate(change);

                bool autoCommit = false;
                if (_activeTx == null)
                {
                    BeginTransaction();
                    autoCommit = true;
                }

                change.Timestamp = DateTime.Now;

                // Değişikliği RAM'deki ROM tamponuna uygula
                var romService = GetRomService();
                if (romService != null && romService.IsLoaded)
                {
                    byte[] buffer = romService.GetBuffer();
                    ApplyChangeToBuffer(buffer, change);
                    romService.SetBuffer(buffer);
                }
                else
                {
                    throw new InvalidOperationException("ROM yüklenmeden kalibrasyon değişikliği yapılamaz.");
                }

                _activeTx.AddChange(change);

                if (autoCommit)
                {
                    CommitTransaction();
                }

                OnCalibrationChanged?.Invoke(change);

                string logStr = $"Değişiklik kaydedildi: {change.Parameter} [{change.OldValue} -> {change.NewValue}]";
                ApplicationLogger.Info("CalibrationManager", logStr);
            }
        }

        public void Undo()
        {
            lock (_lockObj)
            {
                if (!CanUndo) return;

                var tx = _undoMgr.PopUndo();
                if (tx != null)
                {
                    var romService = GetRomService();
                    if (romService != null && romService.IsLoaded)
                    {
                        byte[] buffer = romService.GetBuffer();
                        var reversedChanges = new List<CalibrationChange>();

                        // Değişiklikleri tersten geri al
                        for (int i = tx.Changes.Count - 1; i >= 0; i--)
                        {
                            var change = tx.Changes[i];
                            RevertChangeInBuffer(buffer, change);
                            reversedChanges.Add(new CalibrationChange
                            {
                                Parameter = change.Parameter,
                                OldValue = change.NewValue,
                                NewValue = change.OldValue,
                                Offset = change.Offset,
                                MapName = change.MapName,
                                Source = "Undo",
                                Timestamp = DateTime.Now
                            });
                        }
                        romService.SetBuffer(buffer);

                        foreach (var undoChange in reversedChanges)
                        {
                            OnCalibrationChanged?.Invoke(undoChange);
                        }
                    }
                    _history.AddAuditLog($"[Undo] Geri alındı: ID: {tx.TransactionId}");
                }
            }
        }

        public void Redo()
        {
            lock (_lockObj)
            {
                if (!CanRedo) return;

                var tx = _undoMgr.PopRedo();
                if (tx != null)
                {
                    var romService = GetRomService();
                    if (romService != null && romService.IsLoaded)
                    {
                        byte[] buffer = romService.GetBuffer();
                        // Değişiklikleri sırayla re-apply et
                        foreach (var change in tx.Changes)
                        {
                            ApplyChangeToBuffer(buffer, change);
                        }
                        romService.SetBuffer(buffer);

                        foreach (var change in tx.Changes)
                        {
                            OnCalibrationChanged?.Invoke(change);
                        }
                    }
                    _history.AddAuditLog($"[Redo] İleri alındı: ID: {tx.TransactionId}");
                }
            }
        }

        public IReadOnlyList<CalibrationChange> GetChangeHistory()
        {
            lock (_lockObj)
            {
                var list = new List<CalibrationChange>();
                foreach (var tx in _history.CommittedTransactions)
                {
                    list.AddRange(tx.Changes);
                }
                return list.AsReadOnly();
            }
        }

        public void ClearHistory()
        {
            lock (_lockObj)
            {
                _history.Clear();
                _undoMgr.Clear();
                _session = new CalibrationSession("P28");
                ApplicationLogger.Info("CalibrationManager", "Kalibrasyon geçmişi tamamen temizlendi.");
            }
        }

        public IReadOnlyList<string> GetAuditLogs() => _history.AuditLogs;

        // ── RAM Veri Manipülasyon Yardımcıları ───────────────────────

        private void ApplyChangeToBuffer(byte[] buffer, CalibrationChange change)
        {
            if (change.Offset < 0 || change.Offset >= buffer.Length) return;

            if (change.MapName != null && change.MapName.Contains("Map"))
            {
                // Çoklu harita hücresi değişimi
                if (byte.TryParse(change.NewValue, out byte val))
                {
                    buffer[change.Offset] = val;
                }
            }
            else
            {
                // Değer türüne göre RAM offset yazımı
                if (double.TryParse(change.NewValue, out double numericVal))
                {
                    if (change.Parameter != null && (change.Parameter.Contains("REV") || change.Parameter.Contains("VTEC") || change.Parameter.Contains("SPEED")))
                    {
                        // 2 byte Big-Endian olarak yaz
                        int value = (int)numericVal;
                        buffer[change.Offset] = (byte)(value >> 8);
                        buffer[change.Offset + 1] = (byte)(value & 0xFF);
                    }
                    else
                    {
                        // Tek byte yaz
                        buffer[change.Offset] = (byte)numericVal;
                    }
                }
            }
        }

        private void RevertChangeInBuffer(byte[] buffer, CalibrationChange change)
        {
            if (change.Offset < 0 || change.Offset >= buffer.Length) return;

            if (double.TryParse(change.OldValue, out double numericVal))
            {
                if (change.Parameter != null && (change.Parameter.Contains("REV") || change.Parameter.Contains("VTEC") || change.Parameter.Contains("SPEED")))
                {
                    int value = (int)numericVal;
                    buffer[change.Offset] = (byte)(value >> 8);
                    buffer[change.Offset + 1] = (byte)(value & 0xFF);
                }
                else
                {
                    buffer[change.Offset] = (byte)numericVal;
                }
            }
            else if (byte.TryParse(change.OldValue, out byte val))
            {
                buffer[change.Offset] = val;
            }
        }
    }
}
