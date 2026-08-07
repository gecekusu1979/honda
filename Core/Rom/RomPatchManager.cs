using System;
using System.Collections.Generic;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.Rom
{
    /// <summary>
    /// ROM Yama Yöneticisi — Modüler byte-level patch operasyonları.
    /// Her işlem öncesi doğrulama, önizleme ve otomatik yedekleme yapar.
    /// Tüm operasyonlar denetim günlüğüne kaydedilir.
    /// </summary>
    public class RomPatchManager : IRomPatchManager
    {
        private readonly List<PatchAuditEntry> _auditLog = new List<PatchAuditEntry>();
        private readonly Dictionary<string, PatchBlueprint> _appliedPatches = new Dictionary<string, PatchBlueprint>();

        // ── Doğrulama ──────────────────────────────────────────────
        public bool ValidatePatch(byte[] romData, PatchBlueprint patch)
        {
            if (romData == null || patch == null) return false;
            if (string.IsNullOrEmpty(patch.PatchId)) return false;

            // Offset sınır kontrolü
            if (patch.TargetOffset < 0 || patch.TargetOffset + patch.PatchBytes.Length > romData.Length)
            {
                AddAuditEntry(patch.PatchId, "VALIDATE_FAIL", "Hedef offset ROM sınırları dışında.");
                return false;
            }

            // İmza doğrulaması — hedef alanda beklenen baytlar var mı
            if (patch.ExpectedSignature != null && patch.ExpectedSignature.Length > 0)
            {
                for (int i = 0; i < patch.ExpectedSignature.Length; i++)
                {
                    if (romData[patch.TargetOffset + i] != patch.ExpectedSignature[i])
                    {
                        AddAuditEntry(patch.PatchId, "VALIDATE_FAIL",
                            $"İmza uyumsuzluğu: offset 0x{(patch.TargetOffset + i):X4}");
                        return false;
                    }
                }
            }

            // Aynı patch zaten uygulanmış mı kontrol et
            if (_appliedPatches.ContainsKey(patch.PatchId))
            {
                AddAuditEntry(patch.PatchId, "VALIDATE_WARN", "Bu yama zaten uygulandı.");
                return false;
            }

            AddAuditEntry(patch.PatchId, "VALIDATE_OK", "Doğrulama başarılı.");
            return true;
        }

        // ── Önizleme ──────────────────────────────────────────────
        public PatchPreview PreviewPatch(byte[] romData, PatchBlueprint patch)
        {
            bool valid = ValidatePatch(romData, patch);
            return new PatchPreview
            {
                PatchId = patch.PatchId,
                AffectedOffset = patch.TargetOffset,
                ByteCount = patch.PatchBytes?.Length ?? 0,
                IsValid = valid,
                Summary = valid
                    ? $"Yama uygulanabilir: {patch.Description ?? patch.PatchId} — {patch.PatchBytes.Length} byte"
                    : "Yama uygulanamaz — doğrulama başarısız."
            };
        }

        // ── Uygula ────────────────────────────────────────────────
        public byte[] ApplyPatch(byte[] romData, PatchBlueprint patch)
        {
            if (!ValidatePatch(romData, patch))
                throw new InvalidOperationException($"Yama doğrulaması başarısız: {patch.PatchId}");

            // Orijinal baytları yedekle
            patch.OriginalBytesBackup = new byte[patch.PatchBytes.Length];
            Array.Copy(romData, patch.TargetOffset, patch.OriginalBytesBackup, 0, patch.PatchBytes.Length);

            // Yama uygula
            byte[] result = (byte[])romData.Clone();
            Array.Copy(patch.PatchBytes, 0, result, patch.TargetOffset, patch.PatchBytes.Length);

            _appliedPatches[patch.PatchId] = patch;
            AddAuditEntry(patch.PatchId, "APPLY",
                $"Yama uygulandı: offset 0x{patch.TargetOffset:X4}, {patch.PatchBytes.Length} byte");

            ApplicationLogger.Info("RomPatchManager",
                $"Yama uygulandı: {patch.PatchId} @ 0x{patch.TargetOffset:X4}");

            return result;
        }

        // ── Geri Al ───────────────────────────────────────────────
        public byte[] RollbackPatch(byte[] romData, PatchBlueprint patch)
        {
            if (patch.OriginalBytesBackup == null || patch.OriginalBytesBackup.Length == 0)
                throw new InvalidOperationException($"Yama geri alınamaz — yedek yok: {patch.PatchId}");

            byte[] result = (byte[])romData.Clone();
            Array.Copy(patch.OriginalBytesBackup, 0, result, patch.TargetOffset, patch.OriginalBytesBackup.Length);

            _appliedPatches.Remove(patch.PatchId);
            AddAuditEntry(patch.PatchId, "ROLLBACK",
                $"Yama geri alındı: offset 0x{patch.TargetOffset:X4}");

            ApplicationLogger.Info("RomPatchManager",
                $"Yama geri alındı: {patch.PatchId}");

            return result;
        }

        // ── Kaldır ────────────────────────────────────────────────
        public void RemovePatch(string patchId)
        {
            if (_appliedPatches.Remove(patchId))
                AddAuditEntry(patchId, "REMOVE", "Yama kayıtlardan çıkarıldı.");
        }

        // ── Denetim Günlüğü ───────────────────────────────────────
        public IReadOnlyList<PatchAuditEntry> GetAuditLog() => _auditLog.AsReadOnly();

        private void AddAuditEntry(string patchId, string operation, string details)
        {
            _auditLog.Add(new PatchAuditEntry
            {
                PatchId = patchId,
                Operation = operation,
                Timestamp = DateTime.Now,
                Details = details
            });
        }
    }
}
