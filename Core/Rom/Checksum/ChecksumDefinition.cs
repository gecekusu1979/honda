using System.Collections.Generic;

namespace HondaTuner.Core.Rom.Checksum
{
    public class ChecksumDefinition
    {
        public string ChecksumType { get; set; }
        public ChecksumAlgorithm Algorithm { get; set; }
        public int ChecksumAddress { get; set; }
        public int ChecksumSize { get; set; } = 1;
        public int RangeStart { get; set; }
        public int RangeEnd { get; set; }
        public List<ExcludeRange> ExcludeRanges { get; set; } = new List<ExcludeRange>();
        public byte Seed { get; set; } = 0;
        public string ByteOrder { get; set; } = "LittleEndian"; // "LittleEndian" veya "BigEndian"
        public int ExpectedValue { get; set; }
    }

    public class ExcludeRange
    {
        public int Start { get; set; }
        public int End { get; set; }
    }
}
