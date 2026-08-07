namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Bir yama işleminin (Apply, Rollback, Preview vb.) sonucunu bildirir.
    /// </summary>
    public class PatchResult
    {
        /// <summary>Yama ID'si</summary>
        public string PatchId { get; set; }

        /// <summary>Yama uygulanan offset adresi</summary>
        public int AffectedOffset { get; set; }

        /// <summary>Yamalanan bayt adeti</summary>
        public int ByteCount { get; set; }

        /// <summary>Başarı durumu</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Hata durumunda hata mesajı</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Geri alma için kaydedilen ROM anlık görüntüsü veya anlık yedek</summary>
        public byte[] Snapshot { get; set; }
    }
}
