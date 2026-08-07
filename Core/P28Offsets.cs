namespace HondaTuner.Core
{
    /// <summary>
    /// Honda P28-A01 (D16Z6) ROM offset sabitleri.
    /// ROM boyutu: 32KB = 0x8000 byte
    /// Kaynak: pgmfi.org + hondabase community docs
    /// </summary>
    public static class P28Offsets
    {
        // ── ROM Boyutu ──────────────────────────────────────────
        public const int RomSize = 0x8000; // 32768 byte

        // ── Fuel Map (16x16) ────────────────────────────────────
        // Her hücre: enjektör açılma süresi (0-255, yaklaşık 0-20ms)
        // Satır = RPM ekseni (düşük → yüksek)
        // Sütun = MAP/Load ekseni (düşük vakum → yüksek yük)
        public const int FuelMap       = 0x1D40;
        public const int FuelMapRows   = 16;
        public const int FuelMapCols   = 16;

        // ── Ignition Map (16x16) ────────────────────────────────
        // Her hücre: ateşleme avansı (derece, 0-255 → 0-60°)
        public const int IgnitionMap     = 0x1E40;
        public const int IgnitionMapRows = 16;
        public const int IgnitionMapCols = 16;

        // ── RPM Ekseni Değerleri ─────────────────────────────────
        // 16 adet RPM noktası (devir/dak)
        public static readonly int[] RpmAxis =
        {
            500, 750, 1000, 1250, 1500, 2000, 2500, 3000,
            3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000
        };

        // ── MAP (Manifold Pressure) Ekseni ───────────────────────
        // 16 adet yük noktası (kPa)
        public static readonly int[] LoadAxis =
        {
            20, 30, 40, 50, 60, 70, 80, 90,
            100, 110, 120, 130, 140, 150, 160, 170
        };

        // ── VTEC ────────────────────────────────────────────────
        public const int VtecRpmThreshold  = 0x1F40; // VTEC devreye giriş RPM
        public const int VtecLoadThreshold = 0x1F42; // VTEC devreye giriş yük

        // ── Limitler ────────────────────────────────────────────
        public const int RevLimit      = 0x1FAA; // Devir sınırı
        public const int SpeedLimiter  = 0x1FAC; // Hız sınırı

        // ── Enjektör ────────────────────────────────────────────
        public const int InjectorDeadTime = 0x1D80; // Enjektör ölü süre

        // ── Checksum ────────────────────────────────────────────
        // P28: son byte = tüm byte'ların XOR'u
        public const int ChecksumByte  = 0x7FFF;
    }
}
