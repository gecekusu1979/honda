using System.Collections.Generic;

namespace HondaTuner.Core.Rom.Checksum
{
    public interface IChecksumEngine
    {
        int Calculate(byte[] buffer, ChecksumDefinition def);
        ChecksumResult Validate(byte[] buffer, ChecksumDefinition def);
        void Update(byte[] buffer, ChecksumDefinition def);
        bool VerifyBeforeSave(byte[] buffer, List<ChecksumDefinition> definitions, out List<ChecksumResult> results);
    }
}
