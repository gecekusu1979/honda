using System.Collections.Generic;

namespace HondaTuner.Core.Interfaces
{
    /// <summary>
    /// ROM yama yöneticisi — modüler patch operasyonları.
    /// </summary>
    public interface IRomPatchManager
    {
        bool ValidatePatch(byte[] romData, PatchBlueprint patch);
        PatchPreview PreviewPatch(byte[] romData, PatchBlueprint patch);
        byte[] ApplyPatch(byte[] romData, PatchBlueprint patch);
        byte[] RollbackPatch(byte[] romData, PatchBlueprint patch);
        void RemovePatch(string patchId);
        IReadOnlyList<PatchAuditEntry> GetAuditLog();
    }

    public class PatchBlueprint
    {
        public string PatchId { get; set; } = null!;
        public string EcuCompat { get; set; } = null!;
        public int TargetOffset { get; set; }
        public byte[] ExpectedSignature { get; set; } = null!;
        public byte[] PatchBytes { get; set; } = null!;
        public byte[] OriginalBytesBackup { get; set; } = null!;
        public string Version { get; set; } = null!;
        public string Author { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    public class PatchPreview
    {
        public string PatchId { get; set; } = null!;
        public int AffectedOffset { get; set; }
        public int ByteCount { get; set; }
        public string Summary { get; set; } = null!;
        public bool IsValid { get; set; }
    }

    public class PatchAuditEntry
    {
        public string PatchId { get; set; } = null!;
        public string Operation { get; set; } = null!;
        public System.DateTime Timestamp { get; set; }
        public string Details { get; set; } = null!;
    }
}