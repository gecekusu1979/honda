using System;
using System.IO;
using HondaTuner.Core;

namespace HondaTuner.Tools
{
    /// <summary>
    /// Her ECU profili için gerçekçi stock benzeri demo ROM üretir.
    /// 32KB binary, geçerli XOR checksum ile.
    /// </summary>
    public static class RomGenerator
    {
        // ── Gerçekçi Stok Yakıt Haritası (D-serisi benzeri) ──────
        // Düşük RPM/düşük yük → az yakıt; yüksek yük → daha fazla
        private static byte FuelValue(int row, int col, float multiplier)
        {
            // row=0 düşük RPM, col=0 düşük yük
            float load = col / 15f;          // 0..1
            float rpm = row / 15f;          // 0..1
            float lean = 0.3f + load * 0.5f; // lean at idle, rich at WOT
            float rpmCorr = 1f + rpm * 0.15f;
            float v = lean * rpmCorr * multiplier * 180f;
            return (byte)Clamp((int)v, 20, 250);
        }

        // ── Gerçekçi Ateşleme Haritası ────────────────────────────
        // Yüksek RPM/düşük yük → daha fazla avans
        private static byte IgnValue(int row, int col, float multiplier)
        {
            float load = col / 15f;
            float rpm = row / 15f;
            // Düşük yük = fazla avans, yük arttıkça knock ihtimali
            float advance = (0.4f + rpm * 0.4f) * (1f - load * 0.35f);
            float v = advance * multiplier * 160f;
            return (byte)Clamp((int)v, 5, 200);
        }

        /// <summary>Verilen profil için 32KB stock demo ROM oluşturur.</summary>
        public static byte[] Generate(EcuProfile profile, float fuelMult = 1.0f, float ignMult = 1.0f)
        {
            byte[] rom = new byte[profile.RomSize];

            // Fuel Map
            for (int r = 0; r < profile.FuelMapRows; r++)
                for (int c = 0; c < profile.FuelMapCols; c++)
                    rom[profile.FuelMapOffset + r * profile.FuelMapCols + c] =
                        FuelValue(r, c, fuelMult);

            // Ignition Map
            for (int r = 0; r < profile.IgnMapRows; r++)
                for (int c = 0; c < profile.IgnMapCols; c++)
                    rom[profile.IgnMapOffset + r * profile.IgnMapCols + c] =
                        IgnValue(r, c, ignMult);

            // VTEC RPM (2 byte big-endian)
            if (profile.HasVtec && profile.VtecRpmOffset != 0)
            {
                int vtec = profile.VtecRpmDefault;
                rom[profile.VtecRpmOffset] = (byte)(vtec >> 8);
                rom[profile.VtecRpmOffset + 1] = (byte)(vtec & 0xFF);
            }

            // Rev Limit (2 byte big-endian)
            if (profile.RevLimitOffset != 0)
            {
                int rev = profile.RevLimitDefault;
                rom[profile.RevLimitOffset] = (byte)(rev >> 8);
                rom[profile.RevLimitOffset + 1] = (byte)(rev & 0xFF);
            }

            // XOR Checksum (son byte hariç tümünün XOR'u)
            byte xor = 0;
            for (int i = 0; i < rom.Length - 1; i++)
                xor ^= rom[i];
            rom[profile.ChecksumOffset] = xor;

            return rom;
        }

        /// <summary>Dosyaya yaz (varsa üzerine yaz).</summary>
        public static void SaveToFile(EcuProfile profile, string path,
                                       float fuelMult = 1.0f, float ignMult = 1.0f)
        {
            byte[] rom = Generate(profile, fuelMult, ignMult);
            File.WriteAllBytes(path, rom);
        }

        private static int Clamp(int v, int min, int max) =>
            v < min ? min : v > max ? max : v;
    }
}
