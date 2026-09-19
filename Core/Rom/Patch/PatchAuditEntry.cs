using System;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Denetim günlüğünde yama hareketlerini takip etmek için kullanılan model.
    /// </summary>
    public class PatchAuditEntry
    {
        /// <summary>İşlem zamanı</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>İşlemi gerçekleştiren kullanıcı</summary>
        public string User { get; set; } = null!;

        /// <summary>Yama ID'si</summary>
        public string PatchId { get; set; } = null!;

        /// <summary>Yama öncesindeki baytlar</summary>
        public byte[] OldBytes { get; set; } = null!;

        /// <summary>Yama sonrasındaki baytlar</summary>
        public byte[] NewBytes { get; set; } = null!;

        /// <summary>Uygulanan offset adresi</summary>
        public int Offset { get; set; }

        /// <summary>İşlem sonucu (Örn: SUCCESS, ROLLBACK, FAILED)</summary>
        public string Result { get; set; } = null!;

        /// <summary>Checksum değerinin güncellenip güncellenmediği</summary>
        public bool ChecksumUpdated { get; set; }

        /// <summary>Mevcut ise ilişkili CalibrationTransaction kimliği</summary>
        public string TransactionId { get; set; } = null!;
    }
}