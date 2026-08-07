using System;
using System.Collections.Generic;
using System.Linq;

namespace HondaTuner.Core.Rom.Checksum
{
    public class ChecksumEngine : IChecksumEngine
    {
        private readonly Dictionary<ChecksumAlgorithm, IChecksumAlgorithm> _algorithms;

        public ChecksumEngine(IEnumerable<IChecksumAlgorithm> algorithms)
        {
            if (algorithms == null) throw new ArgumentNullException(nameof(algorithms));
            _algorithms = algorithms.ToDictionary(a => a.Type);
        }

        public int Calculate(byte[] buffer, ChecksumDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (!_algorithms.TryGetValue(def.Algorithm, out var algo))
                throw new NotSupportedException($"Algoritma desteklenmiyor: {def.Algorithm}");

            return algo.Calculate(buffer, def);
        }

        public ChecksumResult Validate(byte[] buffer, ChecksumDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            int calculated = Calculate(buffer, def);
            int expected = ReadExpectedValue(buffer, def);

            bool isValid = calculated == expected;
            string msg = isValid
                ? $"Checksum geçerli: calculated=0x{calculated:X4}, expected=0x{expected:X4}"
                : $"Checksum geçersiz: calculated=0x{calculated:X4}, expected=0x{expected:X4}";

            return new ChecksumResult(isValid, calculated, expected, def.ChecksumAddress, msg);
        }

        public void Update(byte[] buffer, ChecksumDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            int calculated = Calculate(buffer, def);
            WriteValue(buffer, def.ChecksumAddress, def.ChecksumSize, calculated, def.ByteOrder);
        }

        public bool VerifyBeforeSave(byte[] buffer, List<ChecksumDefinition> definitions, out List<ChecksumResult> results)
        {
            results = new List<ChecksumResult>();
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (definitions == null || definitions.Count == 0)
            {
                return true;
            }

            bool allValid = true;
            foreach (var def in definitions)
            {
                var res = Validate(buffer, def);
                results.Add(res);
                if (!res.IsValid)
                {
                    allValid = false;
                }
            }

            return allValid;
        }

        private int ReadExpectedValue(byte[] buffer, ChecksumDefinition def)
        {
            int addr = def.ChecksumAddress;
            if (addr < 0 || addr + def.ChecksumSize > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(def.ChecksumAddress), "Checksum adresi ROM boyutunu aşıyor.");

            if (def.ChecksumSize == 1)
            {
                return buffer[addr];
            }
            else if (def.ChecksumSize == 2)
            {
                if (def.ByteOrder == "BigEndian")
                {
                    return (buffer[addr] << 8) | buffer[addr + 1];
                }
                else
                {
                    return buffer[addr] | (buffer[addr + 1] << 8);
                }
            }
            throw new NotSupportedException($"Geçersiz checksum boyutu: {def.ChecksumSize}");
        }

        private void WriteValue(byte[] buffer, int address, int size, int value, string byteOrder)
        {
            if (address < 0 || address + size > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(address), "Checksum yazma adresi ROM boyutunu aşıyor.");

            if (size == 1)
            {
                buffer[address] = (byte)(value & 0xFF);
            }
            else if (size == 2)
            {
                if (byteOrder == "BigEndian")
                {
                    buffer[address] = (byte)((value >> 8) & 0xFF);
                    buffer[address + 1] = (byte)(value & 0xFF);
                }
                else
                {
                    buffer[address] = (byte)(value & 0xFF);
                    buffer[address + 1] = (byte)((value >> 8) & 0xFF);
                }
            }
            else
            {
                throw new NotSupportedException($"Geçersiz checksum yazma boyutu: {size}");
            }
        }
    }
}
