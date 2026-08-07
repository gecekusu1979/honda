using System;

namespace HondaTuner.Core
{
    /// <summary>
    /// Bir ECU profilinin tüm ROM yapısını tanımlar.
    /// </summary>
    public class EcuProfile
    {
        public string Name { get; }
        public string EcuCode { get; }
        public string EngineCode { get; }
        public string CasaTag { get; }
        public bool HasVtec { get; }
        public bool HasIab { get; }   // Integra GSR P72 — IAB solenoidi
        public int RomSize { get; }

        public int FuelMapOffset { get; }
        public int FuelMapRows { get; }
        public int FuelMapCols { get; }

        public int IgnMapOffset { get; }
        public int IgnMapRows { get; }
        public int IgnMapCols { get; }

        public int[] RpmAxis { get; }
        public int[] LoadAxis { get; }

        public int VtecRpmOffset { get; }
        public int VtecRpmMin { get; }
        public int VtecRpmMax { get; }
        public int VtecRpmDefault { get; }

        public int RevLimitOffset { get; }
        public int RevLimitMin { get; }
        public int RevLimitMax { get; }
        public int RevLimitDefault { get; }

        public int ChecksumOffset { get; }

        // V2 Dynamic properties (Data-driven verification)
        public byte[] SignatureBytes { get; set; }
        public string ChecksumAlgorithm { get; set; }
        public int SpeedLimiterOffset { get; set; }
        public int KnockOffset { get; set; }
        public int InjectorOffset { get; set; }
        public int IdleOffset { get; set; }
        public string HeaderPattern { get; set; }
        public int FuelAxisOffset { get; set; }
        public int IgnitionAxisOffset { get; set; }
        public System.Collections.Generic.List<Calibration.Maps.MapDefinition> Maps { get; set; } = new System.Collections.Generic.List<Calibration.Maps.MapDefinition>();
        public System.Collections.Generic.List<Rom.Checksum.ChecksumDefinition> ChecksumDefinitions { get; set; } = new System.Collections.Generic.List<Rom.Checksum.ChecksumDefinition>();
        public System.Collections.Generic.List<Rom.Patch.EcuPatchMapping> SupportedPatches { get; set; } = new System.Collections.Generic.List<Rom.Patch.EcuPatchMapping>();

        public EcuProfile(
            string name, string ecuCode, string engineCode, string casaTag,
            bool hasVtec, bool hasIab, int romSize,
            int fuelMapOffset, int fuelMapRows, int fuelMapCols,
            int ignMapOffset, int ignMapRows, int ignMapCols,
            int[] rpmAxis, int[] loadAxis,
            int vtecRpmOffset, int vtecRpmMin, int vtecRpmMax, int vtecRpmDefault,
            int revLimitOffset, int revLimitMin, int revLimitMax, int revLimitDefault,
            int checksumOffset)
        {
            Name = name;
            EcuCode = ecuCode;
            EngineCode = engineCode;
            CasaTag = casaTag;
            HasVtec = hasVtec;
            HasIab = hasIab;
            RomSize = romSize;
            FuelMapOffset = fuelMapOffset;
            FuelMapRows = fuelMapRows;
            FuelMapCols = fuelMapCols;
            IgnMapOffset = ignMapOffset;
            IgnMapRows = ignMapRows;
            IgnMapCols = ignMapCols;
            RpmAxis = rpmAxis;
            LoadAxis = loadAxis;
            VtecRpmOffset = vtecRpmOffset;
            VtecRpmMin = vtecRpmMin;
            VtecRpmMax = vtecRpmMax;
            VtecRpmDefault = vtecRpmDefault;
            RevLimitOffset = revLimitOffset;
            RevLimitMin = revLimitMin;
            RevLimitMax = revLimitMax;
            RevLimitDefault = revLimitDefault;
            ChecksumOffset = checksumOffset;

            // Varsayılan V2 özellikleri
            SignatureBytes = Array.Empty<byte>();
            ChecksumAlgorithm = "Xor8";
            SpeedLimiterOffset = 0x1FAC;
            KnockOffset = 0x1FB6;
            InjectorOffset = 0x1D80;
            IdleOffset = 0x1E80;
            HeaderPattern = string.Empty;
            FuelAxisOffset = fuelMapOffset - 64;
            IgnitionAxisOffset = ignMapOffset - 64;
        }
    }

    public static class EcuProfiles
    {
        // ── Ortak Eksen Sabitleri ─────────────────────────────────
        private static readonly int[] StdRpmAxis =
        {
            500, 750, 1000, 1250, 1500, 2000, 2500, 3000,
            3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000
        };
        private static readonly int[] StdLoadAxis =
        {
            20, 30, 40, 50, 60, 70, 80, 90,
            100, 110, 120, 130, 140, 150, 160, 170
        };
        // B-serisi motorda daha yüksek RPM bandı
        private static readonly int[] BSeries_RpmAxis =
        {
            500, 750, 1000, 1500, 2000, 2500, 3000, 3500,
            4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500
        };

        // ── P05 / D15Z1 — Civic CX HF ────────────────────────────
        public static readonly EcuProfile P05 = new EcuProfile(
            name: "P05 / D15Z1 — Civic CX HF",
            ecuCode: "P05", engineCode: "D15Z1",
            casaTag: "EG kasa 1.5L VTEC-E (Yüksek Verimli)",
            hasVtec: true, hasIab: false, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: StdRpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x1F40, vtecRpmMin: 1000, vtecRpmMax: 5500, vtecRpmDefault: 2500,
            revLimitOffset: 0x1FAA, revLimitMin: 4000, revLimitMax: 8000, revLimitDefault: 6500,
            checksumOffset: 0x7FFF
        );

        // ── P06 / D15B7 — Civic DX/LX ────────────────────────────
        public static readonly EcuProfile P06 = new EcuProfile(
            name: "P06 / D15B7 — Civic DX/LX",
            ecuCode: "P06", engineCode: "D15B7",
            casaTag: "EG kasa 1.5L Non-VTEC",
            hasVtec: false, hasIab: false, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: StdRpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x0000, vtecRpmMin: 0, vtecRpmMax: 0, vtecRpmDefault: 0,
            revLimitOffset: 0x1FAA, revLimitMin: 4000, revLimitMax: 8000, revLimitDefault: 6200,
            checksumOffset: 0x7FFF
        );

        // ── P28 / D16Z6 — Civic EX/Si, Del Sol Si ────────────────
        public static readonly EcuProfile P28 = new EcuProfile(
            name: "P28 / D16Z6 — Civic EX/Si",
            ecuCode: "P28", engineCode: "D16Z6",
            casaTag: "EG/EK kasa 1.6L SOHC VTEC",
            hasVtec: true, hasIab: false, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: StdRpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x1F40, vtecRpmMin: 1000, vtecRpmMax: 8000, vtecRpmDefault: 4800,
            revLimitOffset: 0x1FAA, revLimitMin: 4000, revLimitMax: 9500, revLimitDefault: 7200,
            checksumOffset: 0x7FFF
        );

        // ── P30 / D15B2 — EG kasa 1.5i Non-VTEC ─────────────────
        public static readonly EcuProfile P30 = new EcuProfile(
            name: "P30 / D15B2 — EG kasa 1.5i",
            ecuCode: "P30", engineCode: "D15B2",
            casaTag: "EG kasa 1.5L Non-VTEC",
            hasVtec: false, hasIab: false, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: StdRpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x0000, vtecRpmMin: 0, vtecRpmMax: 0, vtecRpmDefault: 0,
            revLimitOffset: 0x1FAA, revLimitMin: 4000, revLimitMax: 8500, revLimitDefault: 6800,
            checksumOffset: 0x7FFF
        );

        // ── P61 / B17A1 — Integra GS-R 1992-93 ──────────────────
        public static readonly EcuProfile P61 = new EcuProfile(
            name: "P61 / B17A1 — Integra GS-R",
            ecuCode: "P61", engineCode: "B17A1",
            casaTag: "DC2 Integra GS-R 1.7L DOHC VTEC (1992-93)",
            hasVtec: true, hasIab: false, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: BSeries_RpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x1F40, vtecRpmMin: 3000, vtecRpmMax: 9000, vtecRpmDefault: 5500,
            revLimitOffset: 0x1FAA, revLimitMin: 5000, revLimitMax: 9500, revLimitDefault: 8400,
            checksumOffset: 0x7FFF
        );

        // ── P72 / B18C1 — Integra GSR 1994-95 (IAB) ─────────────
        public static readonly EcuProfile P72 = new EcuProfile(
            name: "P72 / B18C1 — Integra GSR (IAB)",
            ecuCode: "P72", engineCode: "B18C1",
            casaTag: "DC2 Integra GSR 1.8L DOHC VTEC + IAB (1994-95)",
            hasVtec: true, hasIab: true, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: BSeries_RpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x1F40, vtecRpmMin: 3000, vtecRpmMax: 9500, vtecRpmDefault: 5800,
            revLimitOffset: 0x1FAA, revLimitMin: 5000, revLimitMax: 9800, revLimitDefault: 8600,
            checksumOffset: 0x7FFF
        );

        // ── P74 / B18B1 — Integra LS/GS ──────────────────────────
        public static readonly EcuProfile P74 = new EcuProfile(
            name: "P74 / B18B1 — Integra LS/GS",
            ecuCode: "P74", engineCode: "B18B1",
            casaTag: "DC2 Integra LS/GS 1.8L DOHC Non-VTEC",
            hasVtec: false, hasIab: false, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: BSeries_RpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x0000, vtecRpmMin: 0, vtecRpmMax: 0, vtecRpmDefault: 0,
            revLimitOffset: 0x1FAA, revLimitMin: 4000, revLimitMax: 9000, revLimitDefault: 7600,
            checksumOffset: 0x7FFF
        );

        // ── P13 / H22A — Prelude VTEC ────────────────────────────
        public static readonly EcuProfile P13 = new EcuProfile(
            name: "P13 / H22A — Prelude VTEC",
            ecuCode: "P13", engineCode: "H22A",
            casaTag: "BB Prelude 2.2L DOHC VTEC (1993-95)",
            hasVtec: true, hasIab: false, romSize: 0x8000,
            fuelMapOffset: 0x1D40, fuelMapRows: 16, fuelMapCols: 16,
            ignMapOffset: 0x1E40, ignMapRows: 16, ignMapCols: 16,
            rpmAxis: BSeries_RpmAxis, loadAxis: StdLoadAxis,
            vtecRpmOffset: 0x1F40, vtecRpmMin: 2500, vtecRpmMax: 8500, vtecRpmDefault: 5200,
            revLimitOffset: 0x1FAA, revLimitMin: 4000, revLimitMax: 9000, revLimitDefault: 7800,
            checksumOffset: 0x7FFF
        );

        /// <summary>Tüm profiller (menü ve dialog için).</summary>
        public static readonly EcuProfile[] All =
        {
            P05, P06, P28, P30, P61, P72, P74, P13
        };
    }
}
