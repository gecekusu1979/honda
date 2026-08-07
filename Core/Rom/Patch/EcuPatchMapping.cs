namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Bir ECU profili ile uyumlu olan bir yamanın offset eşleşme bilgisini taşır.
    /// </summary>
    public class EcuPatchMapping
    {
        /// <summary>Yama kimliği (Örn: LaunchControl)</summary>
        public string PatchId { get; set; }

        /// <summary>ECU ROM dosyasındaki yama adresi (offset)</summary>
        public int Offset { get; set; }

        /// <summary>Yama için gerekli olan özellik adı</summary>
        public string RequiredFeature { get; set; }
    }
}
