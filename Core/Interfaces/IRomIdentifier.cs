using System.Collections.Generic;

namespace HondaTuner.Core.Interfaces
{
    /// <summary>
    /// ROM imza tarayıcısı — yüklenen binary'yi otomatik olarak tanır.
    /// </summary>
    public interface IRomIdentifier
    {
        RomAnalysisResult IdentifyRom(byte[] romData, IEnumerable<EcuProfile> knownProfiles);
    }

    public class RomAnalysisResult
    {
        public string EcuCode { get; set; }
        public string EngineCode { get; set; }
        public string ObdType { get; set; }
        public int RomSize { get; set; }
        public double CompatibilityScore { get; set; }
        public bool IsMismatch { get; set; }
        public string Feedback { get; set; }
        public EcuProfile MatchedProfile { get; set; }

        // Extra V2 rules-based diagnostic properties
        public double Confidence { get; set; }
        public List<string> MatchedRules { get; set; } = new List<string>();
        public List<string> UnsupportedRules { get; set; } = new List<string>();
    }
}
