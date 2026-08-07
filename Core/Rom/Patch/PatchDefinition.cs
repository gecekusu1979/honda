using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Tek bir yama (patch) tanımını temsil eder.
    /// Yama tanımları veritabanından JSON formatında yüklenir.
    /// </summary>
    public class PatchDefinition
    {
        /// <summary>Yamanın benzersiz kimliği (Örn: LaunchControl)</summary>
        public string PatchId { get; set; }

        /// <summary>Yama ismi</summary>
        public string Name { get; set; }

        /// <summary>Yama açıklaması</summary>
        public string Description { get; set; }

        /// <summary>Yamanın kategorisi (Örn: Safety, Control, DTC)</summary>
        public string Category { get; set; }

        /// <summary>Uyumlu ECU kodları listesi</summary>
        public List<string> CompatibleEcus { get; set; } = new List<string>();

        /// <summary>Uygulanması için gerekli özelliklerin listesi</summary>
        public List<string> RequiredFeatures { get; set; } = new List<string>();

        /// <summary>Yamanın hedef adresleme alanı (ROM, RAM vb.)</summary>
        public AddressType AddressType { get; set; } = AddressType.ROM;

        /// <summary>Varsayılan offset değeri</summary>
        public int Offset { get; set; }

        /// <summary>Yama yapılmadan önce bulunması beklenen orijinal baytlar</summary>
        public byte[] ExpectedBytes { get; set; } = Array.Empty<byte>();

        /// <summary>Yazılacak yeni yama baytları</summary>
        public byte[] PatchBytes { get; set; } = Array.Empty<byte>();

        /// <summary>Geri alma işleminde yazılacak orijinal baytlar (belirtilmemişse ExpectedBytes kullanılır)</summary>
        public byte[] RollbackBytes { get; set; } = Array.Empty<byte>();

        /// <summary>Yama sonrasında Checksum güncellemesi gerekip gerekmediği</summary>
        public bool ChecksumRequired { get; set; } = true;

        /// <summary>Yamanın güvenlik seviyesi (Safe, Caution, Dangerous)</summary>
        public PatchSafetyLevel SafetyLevel { get; set; } = PatchSafetyLevel.Safe;

        /// <summary>Doğrulama esnasında zorunlu tutulacak kurallar</summary>
        public List<ValidationRule> ValidationRules { get; set; } = new List<ValidationRule>();

        /// <summary>Minimum desteklenen ROM boyutu</summary>
        public int MinimumRomSize { get; set; } = 32768;

        /// <summary>Maximum desteklenen ROM boyutu</summary>
        public int MaximumRomSize { get; set; } = 32768;

        /// <summary>Oluşturulma sürümü</summary>
        public string CreatedVersion { get; set; }

        /// <summary>Son güncellenme tarihi</summary>
        public string LastUpdated { get; set; }
    }
}
