using System;
using System.Collections.Generic;
using System.Linq;
using HondaTuner.Calibration;
using HondaTuner.Core.Container;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Rom.Checksum;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// ROM Yama Motorunun (Patch Engine) standart implementasyonu.
    /// JSON tabanlı yamaları okur, doğrular, yedekleme yönetimiyle beraber uygular.
    /// Checksum güncellemesini tetikler ve CalibrationTransaction ile entegre çalışır.
    /// </summary>
    public class PatchEngine : IPatchEngine
    {
        private readonly IChecksumEngine _checksumEngine;
        private readonly ICalibrationService _calibrationService;
        private readonly PatchBackupManager _backupManager;
        private readonly List<PatchAuditEntry> _auditLog = new List<PatchAuditEntry>();
        private List<PatchDefinition> _patchDefinitions;
        private readonly object _lock = new object();

        /// <summary>
        /// Sınıf yapıcısı (Constructor Injection).
        /// </summary>
        public PatchEngine(IChecksumEngine checksumEngine, ICalibrationService calibrationService)
        {
            _checksumEngine = checksumEngine ?? throw new ArgumentNullException(nameof(checksumEngine));
            _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
            _backupManager = new PatchBackupManager();
            _patchDefinitions = new List<PatchDefinition>();
            LoadDefinitions();
        }

        private void LoadDefinitions()
        {
            _patchDefinitions = new List<PatchDefinition>();
            try
            {
                string dbDir = AppDomain.CurrentDomain.BaseDirectory;
                string subDir = System.IO.Path.Combine(dbDir, "Database");
                if (!System.IO.Directory.Exists(subDir) || !System.IO.File.Exists(System.IO.Path.Combine(subDir, "patch_definitions.json")))
                {
                    subDir = System.IO.Path.Combine(dbDir, "..", "..", "..", "Database");
                }
                string path = System.IO.Path.Combine(subDir, "patch_definitions.json");
                if (System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path);
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var dtos = System.Text.Json.JsonSerializer.Deserialize<List<PatchDefinitionJsonDto>>(content, options);
                    if (dtos != null)
                    {
                        foreach (var dto in dtos)
                        {
                            var def = new PatchDefinition
                            {
                                PatchId = dto.PatchId,
                                Name = dto.Name,
                                Description = dto.Description,
                                Category = dto.Category,
                                CompatibleEcus = dto.CompatibleEcus ?? new List<string>(),
                                RequiredFeatures = dto.RequiredFeatures ?? new List<string>(),
                                ExpectedBytes = dto.ExpectedBytes?.Select(x => (byte)x).ToArray() ?? Array.Empty<byte>(),
                                PatchBytes = dto.PatchBytes?.Select(x => (byte)x).ToArray() ?? Array.Empty<byte>(),
                                RollbackBytes = dto.RollbackBytes?.Select(x => (byte)x).ToArray() ?? Array.Empty<byte>(),
                                ChecksumRequired = dto.ChecksumRequired,
                                MinimumRomSize = dto.MinimumRomSize > 0 ? dto.MinimumRomSize : EcuConstants.DefaultRomSize,
                                MaximumRomSize = dto.MaximumRomSize > 0 ? dto.MaximumRomSize : EcuConstants.DefaultRomSize,
                                CreatedVersion = dto.CreatedVersion,
                                LastUpdated = dto.LastUpdated
                            };

                            if (Enum.TryParse<AddressType>(dto.AddressType, true, out var addrType))
                                def.AddressType = addrType;
                            if (Enum.TryParse<PatchSafetyLevel>(dto.SafetyLevel, true, out var safety))
                                def.SafetyLevel = safety;

                            if (dto.ValidationRules != null)
                            {
                                foreach (var r in dto.ValidationRules)
                                {
                                    if (Enum.TryParse<ValidationRule>(r, true, out var ruleVal))
                                        def.ValidationRules.Add(ruleVal);
                                }
                            }

                            _patchDefinitions.Add(def);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.ApplicationLogger.Error("PatchEngine", $"Yama tanımları yüklenemedi: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public PatchResult ApplyPatch(byte[] romData, string patchId, EcuProfile profile, string username)
        {
            var result = new PatchResult { PatchId = patchId, IsSuccess = false };

            // 1. Validate
            if (!ValidatePatch(romData, patchId, profile, out string err))
            {
                result.ErrorMessage = err;
                AddAudit(username, patchId, null, null, 0, $"FAILED: {err}", false, null);
                return result;
            }

            var patch = _patchDefinitions.First(p => string.Equals(p.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
            var mapping = profile.SupportedPatches.First(m => string.Equals(m.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
            int offset = mapping.Offset;

            // Orijinal baytları oku
            var originalBytes = new byte[patch.PatchBytes.Length];
            if (offset >= 0 && offset + patch.PatchBytes.Length <= romData.Length)
            {
                Array.Copy(romData, offset, originalBytes, 0, patch.PatchBytes.Length);
            }

            // Geri alma (rollback) yedek kaydı (romData henüz değiştirilmemişken)
            _backupManager.CreateBackup(patchId, offset, romData, patch.PatchBytes, username, GetRomSignature(romData));

            bool runLocalTx = !_calibrationService.HasActiveTransaction;
            if (runLocalTx)
            {
                _calibrationService.BeginTransaction();
            }

            try
            {
                // Baytları CalibrationService üzerinden tek tek yaz (Böylelikle Undo/Redo/Transaction otomatik çalışır)
                for (int i = 0; i < patch.PatchBytes.Length; i++)
                {
                    var change = new CalibrationChange
                    {
                        Parameter = $"Patch.{patchId}.Byte{i}",
                        OldValue = originalBytes[i].ToString(),
                        NewValue = patch.PatchBytes[i].ToString(),
                        Offset = offset + i,
                        Source = "PatchEngine",
                        UserAction = username,
                        MapName = "Patch"
                    };
                    _calibrationService.RecordChange(change);
                    // Local tamponu da senkronize et
                    romData[offset + i] = patch.PatchBytes[i];
                }

                // Otomatik Checksum Güncelleme
                bool checksumUpdated = false;
                if (patch.ChecksumRequired)
                {
                    if (profile.ChecksumDefinitions != null && profile.ChecksumDefinitions.Count > 0)
                    {
                        foreach (var checksumDef in profile.ChecksumDefinitions)
                        {
                            _checksumEngine.Update(romData, checksumDef);
                        }
                        checksumUpdated = true;

                        // Checksum Doğrulama
                        if (!_checksumEngine.VerifyBeforeSave(romData, profile.ChecksumDefinitions, out var results))
                        {
                            throw new InvalidOperationException("Yama sonrası Checksum doğrulaması başarısız oldu.");
                        }

                        // RomService de güncellenmeli
                        var romService = ServiceContainer.Resolve<IRomService>();
                        if (romService != null && romService.IsLoaded)
                        {
                            romService.SetBuffer(romData);
                        }
                    }
                }

                if (runLocalTx)
                {
                    _calibrationService.CommitTransaction();
                }

                result.IsSuccess = true;
                result.AffectedOffset = offset;
                result.ByteCount = patch.PatchBytes.Length;
                result.Snapshot = (byte[])romData.Clone();

                AddAudit(username, patchId, originalBytes, patch.PatchBytes, offset, "SUCCESS", checksumUpdated, null);
                Logging.ApplicationLogger.Info("PatchEngine", $"Yama başarıyla uygulandı: {patchId} @ 0x{offset:X4}");
            }
            catch (Exception ex)
            {
                if (runLocalTx)
                {
                    try { _calibrationService.RollbackTransaction(); } catch (Exception rollbackEx) { Logging.ApplicationLogger.Error("PatchEngine", $"Rollback hatası: {rollbackEx.Message}"); }
                    for (int i = 0; i < patch.PatchBytes.Length; i++)
                    {
                        romData[offset + i] = originalBytes[i];
                    }
                }

                result.ErrorMessage = ex.Message;
                AddAudit(username, patchId, originalBytes, patch.PatchBytes, offset, $"FAILED_ROLLBACK: {ex.Message}", false, null);
                Logging.ApplicationLogger.Error("PatchEngine", $"Yama uygulanırken hata olustu ve rollback yapıldı: {ex.Message}");
            }

            return result;
        }

        /// <inheritdoc />
        public bool RemovePatch(string patchId)
        {
            return _backupManager.RemoveBackup(patchId);
        }

        /// <inheritdoc />
        public PatchResult RollbackPatch(byte[] romData, string patchId, EcuProfile profile, string username)
        {
            var result = new PatchResult { PatchId = patchId, IsSuccess = false };
            var backup = _backupManager.GetBackup(patchId);
            if (backup == null)
            {
                result.ErrorMessage = $"Yama '{patchId}' için geri yükleme yedeği bulunamadı.";
                return result;
            }

            bool runLocalTx = !_calibrationService.HasActiveTransaction;
            if (runLocalTx)
            {
                _calibrationService.BeginTransaction();
            }

            try
            {
                // Orijinal baytları geri yaz
                for (int i = 0; i < backup.OriginalBytes.Length; i++)
                {
                    var change = new CalibrationChange
                    {
                        Parameter = $"Patch.{patchId}.Byte{i}",
                        OldValue = backup.PatchedBytes[i].ToString(),
                        NewValue = backup.OriginalBytes[i].ToString(),
                        Offset = backup.Offset + i,
                        Source = "PatchEngine",
                        UserAction = username,
                        MapName = "Patch"
                    };
                    _calibrationService.RecordChange(change);
                    // Local tampon
                    romData[backup.Offset + i] = backup.OriginalBytes[i];
                }

                // Checksum Güncelleme
                bool checksumUpdated = false;
                if (profile.ChecksumDefinitions != null && profile.ChecksumDefinitions.Count > 0)
                {
                    foreach (var checksumDef in profile.ChecksumDefinitions)
                    {
                        _checksumEngine.Update(romData, checksumDef);
                    }
                    checksumUpdated = true;

                    var romService = ServiceContainer.Resolve<IRomService>();
                    if (romService != null && romService.IsLoaded)
                    {
                        romService.SetBuffer(romData);
                    }
                }

                if (runLocalTx)
                {
                    _calibrationService.CommitTransaction();
                }

                _backupManager.RemoveBackup(patchId);
                result.IsSuccess = true;
                result.AffectedOffset = backup.Offset;
                result.ByteCount = backup.OriginalBytes.Length;
                result.Snapshot = (byte[])romData.Clone();

                AddAudit(username, patchId, backup.PatchedBytes, backup.OriginalBytes, backup.Offset, "ROLLBACK", checksumUpdated, null);
                Logging.ApplicationLogger.Info("PatchEngine", $"Yama başarıyla geri alındı (Rollback): {patchId}");
            }
            catch (Exception ex)
            {
                if (runLocalTx)
                {
                    try { _calibrationService.RollbackTransaction(); } catch (Exception rollbackEx) { Logging.ApplicationLogger.Error("PatchEngine", $"Rollback (ikincil) hatası: {rollbackEx.Message}"); }
                }
                result.ErrorMessage = ex.Message;
                AddAudit(username, patchId, backup.PatchedBytes, backup.OriginalBytes, backup.Offset, $"ROLLBACK_FAILED: {ex.Message}", false, null);
            }

            return result;
        }

        /// <inheritdoc />
        public bool ValidatePatch(byte[] romData, string patchId, EcuProfile profile, out string errorMessage)
        {
            errorMessage = null;
            var patch = _patchDefinitions.FirstOrDefault(p => string.Equals(p.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
            if (patch == null)
            {
                errorMessage = $"Yama tanımları arasında '{patchId}' bulunamadı.";
                return false;
            }

            var mapping = profile?.SupportedPatches?.FirstOrDefault(m => string.Equals(m.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
            if (profile != null && mapping == null)
            {
                errorMessage = $"Seçilen ECU profili ({profile.EcuCode}) '{patchId}' yamasını desteklemiyor.";
                return false;
            }

            int offset = mapping != null ? mapping.Offset : patch.Offset;
            return PatchValidator.Validate(romData, patch, profile, offset, out errorMessage);
        }

        /// <inheritdoc />
        public PatchPreview PreviewPatch(byte[] romData, string patchId, EcuProfile profile)
        {
            var preview = new PatchPreview { PatchId = patchId, IsValid = false };
            var patch = _patchDefinitions.FirstOrDefault(p => string.Equals(p.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
            if (patch == null)
            {
                preview.Warnings.Add("Yama tanımı bulunamadı.");
                return preview;
            }

            var mapping = profile?.SupportedPatches?.FirstOrDefault(m => string.Equals(m.PatchId, patchId, StringComparison.OrdinalIgnoreCase));
            int offset = mapping != null ? mapping.Offset : patch.Offset;

            preview.Offset = offset;
            preview.SafetyLevel = patch.SafetyLevel;
            preview.ChecksumWillChange = patch.ChecksumRequired;

            bool valid = ValidatePatch(romData, patchId, profile, out string err);
            preview.IsValid = valid;
            if (!valid)
            {
                preview.Warnings.Add(err);
            }

            if (offset >= 0 && offset + patch.PatchBytes.Length <= romData.Length)
            {
                preview.OriginalBytes = new byte[patch.PatchBytes.Length];
                Array.Copy(romData, offset, preview.OriginalBytes, 0, patch.PatchBytes.Length);
                preview.NewBytes = (byte[])patch.PatchBytes.Clone();

                int diffCount = 0;
                for (int i = 0; i < patch.PatchBytes.Length; i++)
                {
                    if (preview.OriginalBytes[i] != preview.NewBytes[i])
                        diffCount++;
                }
                preview.ByteDifference = diffCount;
            }

            return preview;
        }

        /// <inheritdoc />
        public List<PatchDefinition> GetAvailablePatches(EcuProfile profile)
        {
            if (profile?.SupportedPatches == null)
                return new List<PatchDefinition>();

            return _patchDefinitions.Where(p => profile.SupportedPatches.Any(
                m => string.Equals(m.PatchId, p.PatchId, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        /// <inheritdoc />
        public bool IsPatchApplied(string patchId)
        {
            return _backupManager.GetBackup(patchId) != null;
        }

        /// <inheritdoc />
        public IReadOnlyList<PatchAuditEntry> GetPatchAudit()
        {
            lock (_lock)
            {
                return new List<PatchAuditEntry>(_auditLog).AsReadOnly();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<PatchBackup> GetBackups() => _backupManager.GetBackups();

        private void AddAudit(string user, string patchId, byte[] oldBytes, byte[] newBytes, int offset, string result, bool csUpdated, string txId)
        {
            lock (_lock)
            {
                _auditLog.Add(new PatchAuditEntry
                {
                    Timestamp = DateTime.Now,
                    User = user,
                    PatchId = patchId,
                    OldBytes = oldBytes,
                    NewBytes = newBytes,
                    Offset = offset,
                    Result = result,
                    ChecksumUpdated = csUpdated,
                    TransactionId = txId
                });
            }
        }

        private string GetRomSignature(byte[] romData)
        {
            if (romData == null || romData.Length < 16) return "UNKNOWN";
            int sum = 0;
            for (int i = 0; i < Math.Min(romData.Length, 128); i++) sum += romData[i];
            return $"SIG-{sum:X4}-{romData.Length}";
        }
    }

    internal class PatchDefinitionJsonDto
    {
        public string PatchId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public List<string> CompatibleEcus { get; set; }
        public List<string> RequiredFeatures { get; set; }
        public string AddressType { get; set; }
        public int Offset { get; set; }
        public List<int> ExpectedBytes { get; set; }
        public List<int> PatchBytes { get; set; }
        public List<int> RollbackBytes { get; set; }
        public bool ChecksumRequired { get; set; }
        public string SafetyLevel { get; set; }
        public List<string> ValidationRules { get; set; }
        public int MinimumRomSize { get; set; }
        public int MaximumRomSize { get; set; }
        public string CreatedVersion { get; set; }
        public string LastUpdated { get; set; }
    }
}
