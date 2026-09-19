using System;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Yamanın geri alınabilmesi için oluşturulan yedek sınıfı.
    /// </summary>
    public class PatchBackup
    {
        /// <summary>Yama kimliği</summary>
        public string PatchId { get; set; } = null!;

        /// <summary>Yama uygulanan offset adresi</summary>
        public int Offset { get; set; }

        /// <summary>Orijinal (yamadan önceki) baytlar</summary>
        public byte[] OriginalBytes { get; set; } = null!;

        /// <summary>Yamalanmış baytlar</summary>
        public byte[] PatchedBytes { get; set; } = null!;

        /// <summary>Yama öncesindeki checksum değeri</summary>
        public int ChecksumBefore { get; set; }

        /// <summary>Yama sonrasındaki checksum değeri</summary>
        public int ChecksumAfter { get; set; }

        /// <summary>Yedekleme/İşlem zamanı</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Yama işlemini gerçekleştiren kullanıcı adı</summary>
        public string User { get; set; } = null!;

        /// <summary>Yama esnasındaki ROM imzası</summary>
        public string RomSignature { get; set; } = null!;
    }
}