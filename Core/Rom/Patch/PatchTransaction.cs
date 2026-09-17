using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Immutable Patch Transaction class for safe rollbacks and audit.
    /// Phase 7 Requirement: Cannot be mutated after creation. Rollback relies ONLY on OriginalBytes.
    /// </summary>
    public sealed class PatchTransaction
    {
        public string TransactionId { get; }
        public string ProfileId { get; }
        public string OriginalSha256 { get; }
        public string PatchedSha256 { get; }
        public byte[] OriginalBytes { get; }
        public byte[] PatchedBytes { get; }
        public List<int> ChangedOffsets { get; }

        public string BeforeChecksum { get; }
        public string AfterChecksum { get; }

        public DateTime Timestamp { get; }
        public string AuditInfo { get; }

        // Patch C: Physical Write Authorization Boundary
        public string AuthorizationToken { get; private set; }
        public bool IsAuthorized => !string.IsNullOrEmpty(AuthorizationToken) && AuthorizationToken.StartsWith("AUTH-");

        public void Authorize(string token)
        {
            if (!string.IsNullOrEmpty(AuthorizationToken))
                throw new InvalidOperationException("Transaction is already authorized.");
            AuthorizationToken = token;
        }

        public PatchTransaction(
            string profileId,
            byte[] originalRom,
            byte[] patchedRom,
            List<int> changedOffsets,
            string beforeChecksum = "UNKNOWN",
            string afterChecksum = "UNKNOWN",
            string auditInfo = "System Patch")
        {
            if (originalRom == null || patchedRom == null)
                throw new ArgumentNullException("Rom buffers cannot be null.");

            TransactionId = Guid.NewGuid().ToString();
            ProfileId = profileId ?? "UNKNOWN";

            OriginalSha256 = ComputeHash(originalRom);
            PatchedSha256 = ComputeHash(patchedRom);

            // Strictly duplicate bytes to prevent by-ref mutation
            OriginalBytes = new byte[originalRom.Length];
            Array.Copy(originalRom, OriginalBytes, originalRom.Length);

            PatchedBytes = new byte[patchedRom.Length];
            Array.Copy(patchedRom, PatchedBytes, patchedRom.Length);

            ChangedOffsets = changedOffsets?.ToList() ?? new List<int>();

            BeforeChecksum = beforeChecksum ?? "UNKNOWN";
            AfterChecksum = afterChecksum ?? "UNKNOWN";

            Timestamp = DateTime.UtcNow;
            AuditInfo = auditInfo;
        }

        private static string ComputeHash(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
