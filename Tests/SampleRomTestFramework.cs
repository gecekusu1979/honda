using System;
using HondaTuner.Core;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;
using HondaTuner.Core.Rom;

namespace HondaTuner.Tests
{
    /// <summary>
    /// Örnek ROM Test Çerçevesi.
    /// Hedef demo byte dizileri üzerinde tam doğrulama yapar.
    /// </summary>
    public static class SampleRomTestFramework
    {
        public static string RunAllTests()
        {
            var results = new System.Collections.Generic.List<string>();
            int passed = 0, failed = 0;

            results.Add("── Örnek ROM Test Çerçevesi ───────────────────");

            // Test 1: Checksum Offset Geçerliliği
            {
                byte[] rom = new byte[0x8000];
                rom[0x7FFF] = 0x42; // Checksum byte
                bool valid = rom.Length == EcuProfiles.P28.RomSize &&
                             EcuProfiles.P28.ChecksumOffset < rom.Length;
                results.Add(valid ? "  ✅ Checksum offset geçerli" : "  ❌ Checksum offset geçersiz");
                if (valid) passed++; else failed++;
            }

            // Test 2: Bozuk ROM (çok kısa)
            {
                byte[] rom = new byte[100];
                var identifier = new RomIdentifier();
                var result = identifier.IdentifyRom(rom, EcuProfiles.All);
                bool rejected = result.IsMismatch;
                results.Add(rejected ? "  ✅ Bozuk ROM reddedildi" : "  ❌ Bozuk ROM kabul edildi");
                if (rejected) passed++; else failed++;
            }

            // Test 3: Fuel Map Sınır Kontrolü
            {
                byte[] rom = new byte[0x8000];
                int endOffset = EcuProfiles.P28.FuelMapOffset +
                    (EcuProfiles.P28.FuelMapRows * EcuProfiles.P28.FuelMapCols);
                bool inBounds = endOffset <= rom.Length;
                results.Add(inBounds ? "  ✅ Fuel map sınırları geçerli" : "  ❌ Fuel map taşması");
                if (inBounds) passed++; else failed++;
            }

            // Test 4: Ignition Map Sınır Kontrolü
            {
                byte[] rom = new byte[0x8000];
                int endOffset = EcuProfiles.P28.IgnMapOffset +
                    (EcuProfiles.P28.IgnMapRows * EcuProfiles.P28.IgnMapCols);
                bool inBounds = endOffset <= rom.Length;
                results.Add(inBounds ? "  ✅ Ignition map sınırları geçerli" : "  ❌ Ignition map taşması");
                if (inBounds) passed++; else failed++;
            }

            // Test 5: Tüm profillerde ROM boyutu tutarlılığı
            {
                bool allValid = true;
                foreach (var p in EcuProfiles.All)
                {
                    if (p.RomSize != 0x8000) { allValid = false; break; }
                }
                results.Add(allValid
                    ? "  ✅ Tüm profiller 32KB ROM boyutu"
                    : "  ❌ Profillerde boyut tutarsızlığı");
                if (allValid) passed++; else failed++;
            }

            results.Add($"\n  Sonuç: {passed} geçti, {failed} kaldı");
            return string.Join(Environment.NewLine, results);
        }
    }
}
