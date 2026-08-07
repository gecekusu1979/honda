using System.Collections.Generic;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Uygulanacak yamanın önizleme bilgilerini içerir.
    /// </summary>
    public class PatchPreview
    {
        /// <summary>Yama ID'si</summary>
        public string PatchId { get; set; }

        /// <summary>Yama uygulanacak offset adresi</summary>
        public int Offset { get; set; }

        /// <summary>Orijinal baytlar</summary>
        public byte[] OriginalBytes { get; set; }

        /// <summary>Yama sonrasında yazılacak baytlar</summary>
        public byte[] NewBytes { get; set; }

        /// <summary>Yama sebebiyle değişecek bayt adeti</summary>
        public int ByteDifference { get; set; }

        /// <summary>Checksum değerinin değişip değişmeyeceği</summary>
        public bool ChecksumWillChange { get; set; }

        /// <summary>Güvenlik seviyesi</summary>
        public PatchSafetyLevel SafetyLevel { get; set; }

        /// <summary>Önizleme esnasında saptanan uyarı mesajları</summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>Yamanın geçerli/uygulanabilir olup olmadığı</summary>
        public bool IsValid { get; set; }
    }
}
