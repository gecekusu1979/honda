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
        public string User { get; set; }

        /// <summary>Yama ID'si</summary>
        public string PatchId { get; set; }

        /// <summary>Yama öncesindeki baytlar</summary>
        public byte[] OldBytes { get; set; }

        /// <summary>Yama sonrasındaki baytlar</summary>
        public byte[] NewBytes { get; set; }

        /// <summary>Uygulanan offset adresi</summary>
        public int Offset { get; set; }

        /// <summary>İşlem sonucu (Örn: SUCCESS, ROLLBACK, FAILED)</summary>
        public string Result { get; set; }

        /// <summary>Checksum değerinin güncellenip güncellenmediği</summary>
        public bool ChecksumUpdated { get; set; }

        /// <summary>Mevcut ise ilişkili CalibrationTransaction kimliği</summary>
        public string TransactionId { get; set; }
    }
}
