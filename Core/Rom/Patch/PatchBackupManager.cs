using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// ROM Yamaları için yedekleme ve geri yükleme işlemlerini (Backup, Undo, Redo) yönetir.
    /// Her yama öncesinde orijinal byte dizisini hafızada saklar.
    /// </summary>
    public class PatchBackupManager
    {
        private readonly List<PatchBackup> _backups = new List<PatchBackup>();
        private readonly List<PatchBackup> _redoStack = new List<PatchBackup>();
        private readonly object _lock = new object();

        /// <summary>
        /// Belirtilen yama için yedek kaydı oluşturur.
        /// </summary>
        public void CreateBackup(string patchId, int offset, byte[] romData, byte[] patchBytes, string user, string romSignature)
        {
            if (romData == null || patchBytes == null) return;

            var originalBytes = new byte[patchBytes.Length];
            if (offset >= 0 && offset + patchBytes.Length <= romData.Length)
            {
                Array.Copy(romData, offset, originalBytes, 0, patchBytes.Length);
            }

            var backup = new PatchBackup
            {
                PatchId = patchId,
                Offset = offset,
                OriginalBytes = originalBytes,
                PatchedBytes = (byte[])patchBytes.Clone(),
                ChecksumBefore = 0,
                ChecksumAfter = 0,
                Timestamp = DateTime.Now,
                User = user,
                RomSignature = romSignature
            };

            lock (_lock)
            {
                // Varsa eski yedeği kaldır
                RemoveBackupInternal(patchId);
                _backups.Add(backup);
                _redoStack.Clear(); // Yeni yama işleminde redo geçmişi temizlenir
            }
        }

        /// <summary>
        /// Belirtilen yama kimliğine sahip yedeği bulur.
        /// </summary>
        public PatchBackup GetBackup(string patchId)
        {
            lock (_lock)
            {
                for (int i = 0; i < _backups.Count; i++)
                {
                    if (string.Equals(_backups[i].PatchId, patchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return _backups[i];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Yama yedeğini doğrudan listeye ekler.
        /// </summary>
        public void AddBackupDirectly(PatchBackup backup)
        {
            if (backup == null) return;
            lock (_lock)
            {
                RemoveBackupInternal(backup.PatchId);
                _backups.Add(backup);
            }
        }

        /// <summary>
        /// Belirtilen yama kimliğine sahip yedeği siler.
        /// </summary>
        public bool RemoveBackup(string patchId)
        {
            lock (_lock)
            {
                return RemoveBackupInternal(patchId);
            }
        }

        private bool RemoveBackupInternal(string patchId)
        {
            for (int i = 0; i < _backups.Count; i++)
            {
                if (string.Equals(_backups[i].PatchId, patchId, StringComparison.OrdinalIgnoreCase))
                {
                    _backups.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Redo yığınına yedeği taşır.
        /// </summary>
        public void PushToRedo(PatchBackup backup)
        {
            if (backup == null) return;
            lock (_lock)
            {
                _redoStack.Add(backup);
            }
        }

        /// <summary>
        /// Son geri alınan yedeği tekrar uygulamak için redo yığınından çeker.
        /// </summary>
        public PatchBackup PopRedo()
        {
            lock (_lock)
            {
                if (_redoStack.Count == 0) return null;
                int idx = _redoStack.Count - 1;
                var backup = _redoStack[idx];
                _redoStack.RemoveAt(idx);
                return backup;
            }
        }

        /// <summary>
        /// Mevcut tüm yedekleri listeler.
        /// </summary>
        public IReadOnlyList<PatchBackup> GetBackups()
        {
            lock (_lock)
            {
                return new List<PatchBackup>(_backups).AsReadOnly();
            }
        }

        /// <summary>
        /// Tüm yedek ve ileri/geri al geçmişini temizler.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _backups.Clear();
                _redoStack.Clear();
            }
        }
    }
}
