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
        public string PatchId { get; set; }
        public string EcuCompat { get; set; }
        public int TargetOffset { get; set; }
        public byte[] ExpectedSignature { get; set; }
        public byte[] PatchBytes { get; set; }
        public byte[] OriginalBytesBackup { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
    }

    public class PatchPreview
    {
        public string PatchId { get; set; }
        public int AffectedOffset { get; set; }
        public int ByteCount { get; set; }
        public string Summary { get; set; }
        public bool IsValid { get; set; }
    }

    public class PatchAuditEntry
    {
        public string PatchId { get; set; }
        public string Operation { get; set; }
        public System.DateTime Timestamp { get; set; }
        public string Details { get; set; }
    }
}
