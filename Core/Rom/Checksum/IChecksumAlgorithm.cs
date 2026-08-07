using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Rom.Checksum
{
    public interface IChecksumAlgorithm
    {
        ChecksumAlgorithm Type { get; }
        int Calculate(byte[] buffer, ChecksumDefinition def);
    }

    public class Xor8Algorithm : IChecksumAlgorithm
    {
        public ChecksumAlgorithm Type => ChecksumAlgorithm.Xor8;

        public int Calculate(byte[] buffer, ChecksumDefinition def)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            byte checksum = def.Seed;
            for (int i = def.RangeStart; i <= def.RangeEnd; i++)
            {
                if (i < 0 || i >= buffer.Length) continue;
                if (IsExcluded(i, def.ExcludeRanges)) continue;
                if (i >= def.ChecksumAddress && i < def.ChecksumAddress + def.ChecksumSize) continue;
                checksum ^= buffer[i];
            }
            return checksum;
        }

        private bool IsExcluded(int idx, List<ExcludeRange> excludes)
        {
            if (excludes == null) return false;
            foreach (var r in excludes)
            {
                if (idx >= r.Start && idx <= r.End) return true;
            }
            return false;
        }
    }

    public class Add8Algorithm : IChecksumAlgorithm
    {
        public ChecksumAlgorithm Type => ChecksumAlgorithm.Add8;

        public int Calculate(byte[] buffer, ChecksumDefinition def)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            byte checksum = def.Seed;
            for (int i = def.RangeStart; i <= def.RangeEnd; i++)
            {
                if (i < 0 || i >= buffer.Length) continue;
                if (IsExcluded(i, def.ExcludeRanges)) continue;
                if (i >= def.ChecksumAddress && i < def.ChecksumAddress + def.ChecksumSize) continue;
                checksum = (byte)((checksum + buffer[i]) & 0xFF);
            }
            return checksum;
        }

        private bool IsExcluded(int idx, List<ExcludeRange> excludes)
        {
            if (excludes == null) return false;
            foreach (var r in excludes)
            {
                if (idx >= r.Start && idx <= r.End) return true;
            }
            return false;
        }
    }

    public class Sum16Algorithm : IChecksumAlgorithm
    {
        public ChecksumAlgorithm Type => ChecksumAlgorithm.Sum16;

        public int Calculate(byte[] buffer, ChecksumDefinition def)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            int sum = def.Seed;
            for (int i = def.RangeStart; i <= def.RangeEnd; i++)
            {
                if (i < 0 || i >= buffer.Length) continue;
                if (IsExcluded(i, def.ExcludeRanges)) continue;
                if (i >= def.ChecksumAddress && i < def.ChecksumAddress + def.ChecksumSize) continue;
                sum = (sum + buffer[i]) & 0xFFFF;
            }
            return sum;
        }

        private bool IsExcluded(int idx, List<ExcludeRange> excludes)
        {
            if (excludes == null) return false;
            foreach (var r in excludes)
            {
                if (idx >= r.Start && idx <= r.End) return true;
            }
            return false;
        }
    }

    public class Xor16Algorithm : IChecksumAlgorithm
    {
        public ChecksumAlgorithm Type => ChecksumAlgorithm.Xor16;

        public int Calculate(byte[] buffer, ChecksumDefinition def)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            int checksum = def.Seed;
            for (int i = def.RangeStart; i <= def.RangeEnd; i += 2)
            {
                if (i < 0 || i + 1 >= buffer.Length) continue;
                if (IsExcluded(i, def.ExcludeRanges) || IsExcluded(i + 1, def.ExcludeRanges)) continue;
                if ((i >= def.ChecksumAddress && i < def.ChecksumAddress + def.ChecksumSize) ||
                    (i + 1 >= def.ChecksumAddress && i + 1 < def.ChecksumAddress + def.ChecksumSize))
                {
                    continue;
                }

                int val = 0;
                if (def.ByteOrder == "BigEndian")
                {
                    val = (buffer[i] << 8) | buffer[i + 1];
                }
                else
                {
                    val = buffer[i] | (buffer[i + 1] << 8);
                }
                checksum ^= val;
            }
            return checksum & 0xFFFF;
        }

        private bool IsExcluded(int idx, List<ExcludeRange> excludes)
        {
            if (excludes == null) return false;
            foreach (var r in excludes)
            {
                if (idx >= r.Start && idx <= r.End) return true;
            }
            return false;
        }
    }

    public class HondaCustomAlgorithm : IChecksumAlgorithm
    {
        public ChecksumAlgorithm Type => ChecksumAlgorithm.HondaCustom;

        public int Calculate(byte[] buffer, ChecksumDefinition def)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            int sum = def.Seed;
            for (int i = def.RangeStart; i <= def.RangeEnd; i++)
            {
                if (i < 0 || i >= buffer.Length) continue;
                if (IsExcluded(i, def.ExcludeRanges)) continue;
                if (i >= def.ChecksumAddress && i < def.ChecksumAddress + def.ChecksumSize) continue;
                sum = (sum + buffer[i]) & 0xFF;
            }
            return (byte)((0x100 - sum) & 0xFF);
        }

        private bool IsExcluded(int idx, List<ExcludeRange> excludes)
        {
            if (excludes == null) return false;
            foreach (var r in excludes)
            {
                if (idx >= r.Start && idx <= r.End) return true;
            }
            return false;
        }
    }
}
