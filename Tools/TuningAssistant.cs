using System;
using System.Text;
using HondaTuner.Core;

namespace HondaTuner.Tools
{
    // ── Veri Tipleri ─────────────────────────────────────────────

    public enum TuningGoal
    {
        IesVtecStreet,      // Tipik VTEC sokak araci
        NaturallyAspirated, // Dogal emişli performans
        TurboSafeBase,      // Turbo güvenli başlangıç haritası
        Economy,            // Düşük yakıt tüketimi
        StockStreet,        // Stock benzeri sehir kullanımı
    }

    public class TuningSetup
    {
        public TuningGoal Goal { get; set; } = TuningGoal.IesVtecStreet;
        public int InjectorCc { get; set; } = 240;
        public int MapSensorBar { get; set; } = 1;
        public double TargetAfrPower { get; set; } = 12.8;
        public double TargetAfrCruise { get; set; } = 14.2;
        public int VtecRpm { get; set; } = 4800;
        public int RevLimitRpm { get; set; } = 7200;
        public int SpeedLimitKmh { get; set; } = 220;
        public double InjectorDeadTimeMs { get; set; } = 0.80;
    }

    public class BasemapResult
    {
        public byte[,] FuelMap { get; set; }
        public byte[,] IgnitionMap { get; set; }
        public string Summary { get; set; }
    }

    /// <summary>
    /// Tuning asistanı: basemap üretimi ve wideband AFR düzeltmesi.
    /// Telifli ROM kullanmaz; matematiksel model üretir.
    /// </summary>
    public static class TuningAssistant
    {
        // ── Hedef açıklamaları ────────────────────────────────────

        public static string DescribeGoal(TuningGoal goal) => goal switch
        {
            TuningGoal.IesVtecStreet => "VTEC Sokak (Dengeli)",
            TuningGoal.NaturallyAspirated => "NA Performans",
            TuningGoal.TurboSafeBase => "Turbo Güvenli Başlangıç",
            TuningGoal.Economy => "Yakıt Ekonomisi",
            TuningGoal.StockStreet => "Stock Benzeri Sehir",
            _ => "Bilinmiyor",
        };

        // ── Profil için varsayılan değerler ───────────────────────

        public static TuningSetup DefaultsFor(EcuProfile profile, VehicleEntry vehicle = null)
        {
            bool isB = profile.EngineCode.StartsWith("B") || profile.EngineCode.StartsWith("H");
            return new TuningSetup
            {
                Goal = TuningGoal.IesVtecStreet,
                InjectorCc = isB ? 310 : 240,
                MapSensorBar = 1,
                TargetAfrPower = isB ? 12.5 : 12.8,
                TargetAfrCruise = 14.2,
                VtecRpm = profile.HasVtec ? profile.VtecRpmDefault : 0,
                RevLimitRpm = profile.RevLimitDefault,
                SpeedLimitKmh = 220,
                InjectorDeadTimeMs = isB ? 0.90 : 0.80,
            };
        }

        // ── Basemap Üretimi ───────────────────────────────────────

        /// <summary>
        /// Seçilen hedefe göre yakıt ve ateşleme haritası oluşturur.
        /// Mevcut haritayı referans alarak üstüne yazar.
        /// </summary>
        public static BasemapResult CreateBaseMap(
            EcuProfile profile,
            byte[,] existingFuel,
            byte[,] existingIgn,
            TuningSetup setup)
        {
            int rows = profile.FuelMapRows;
            int cols = profile.FuelMapCols;

            var fuel = new byte[rows, cols];
            var ign = new byte[rows, cols];

            // Injector ölçeklendirme faktörü (240cc baz)
            double injScale = 240.0 / Math.Max(1, setup.InjectorCc);

            for (int r = 0; r < rows; r++)
            {
                int rpm = profile.RpmAxis[r];
                for (int c = 0; c < cols; c++)
                {
                    int load = profile.LoadAxis[c];

                    // ── Yakıt Hesabı ─────────────────────────────
                    double afrTarget = TargetAfr(setup, rpm, load, profile);
                    // VE = volumetric efficiency model (basit sinüsoidal)
                    double ve = VolumEfficiency(rpm, load, profile);
                    // Ham yakıt değeri (0-255 skala)
                    double rawFuel = ve * (14.7 / afrTarget) * injScale * 128.0;
                    rawFuel = ApplyGoalFuelMod(rawFuel, setup.Goal, rpm, load, profile);
                    fuel[r, c] = Clamp(rawFuel);

                    // ── Ateşleme Hesabı ──────────────────────────
                    double timing = BaseTiming(rpm, load, profile);
                    timing = ApplyGoalTimingMod(timing, setup.Goal, rpm, load, profile);
                    ign[r, c] = TimingToByte(timing);
                }
            }

            string summary = BuildSummary(setup, profile);
            return new BasemapResult { FuelMap = fuel, IgnitionMap = ign, Summary = summary };
        }

        // ── Wideband AFR Düzeltmesi ───────────────────────────────

        /// <summary>
        /// Ölçülen AFR ile hedef AFR arasındaki farkı yakıt haritasına uygular.
        /// Etki alanı (radius) ile komşu hücreleri de orantılı biçimde düzeltir.
        /// </summary>
        public static byte[,] ApplyWidebandCorrection(
            byte[,] fuelMap,
            EcuProfile profile,
            double measuredAfr,
            double targetAfr,
            int rpm, int load,
            int radius)
        {
            if (Math.Abs(measuredAfr - targetAfr) < 0.05)
                return fuelMap; // Düzeltme gereksiz

            var result = (byte[,])fuelMap.Clone();
            int rows = profile.FuelMapRows;
            int cols = profile.FuelMapCols;

            // Merkez hücre
            int centerRow = FindNearest(profile.RpmAxis, rpm);
            int centerCol = FindNearest(profile.LoadAxis, load);

            // Düzeltme oranı: measuredAfr > target → yakıt ekle (map ++)
            double corrRatio = measuredAfr / targetAfr;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int dist = Math.Max(Math.Abs(r - centerRow), Math.Abs(c - centerCol));
                    if (dist > radius) continue;

                    // Mesafeye göre azalan etki
                    double weight = radius == 0 ? 1.0 : 1.0 - (double)dist / (radius + 1);
                    double blended = 1.0 + (corrRatio - 1.0) * weight;
                    int newVal = (int)Math.Round(result[r, c] * blended);
                    result[r, c] = (byte)Math.Max(0, Math.Min(255, newVal));
                }
            }
            return result;
        }

        // ── Yardımcı Hesap Fonksiyonları ─────────────────────────

        private static double TargetAfr(TuningSetup setup, int rpm, int load, EcuProfile profile)
        {
            bool isVtecRange = profile.HasVtec && rpm >= profile.VtecRpmDefault;
            bool isHighLoad = load >= 120;

            if (isHighLoad || isVtecRange)
                return setup.TargetAfrPower;      // Zengin güç karışımı
            if (load <= 50 && rpm <= 3000)
                return 15.0;                       // Rölanti/çok hafif - biraz fakir
            return setup.TargetAfrCruise;          // Seyir - stoich yakını
        }

        private static double VolumEfficiency(int rpm, int load, EcuProfile profile)
        {
            // Basit VE modeli: düşük-orta rpm etkin, yüksek rpm/düşük load düşük
            double rpmNorm = rpm / (double)(profile.RpmAxis[profile.RpmAxis.Length - 1]);
            double ldNorm = load / 170.0;
            // Peak VE ~ 75% rpm
            double ve = 0.65 + 0.35 * Math.Sin(rpmNorm * Math.PI) * ldNorm;
            return Math.Max(0.3, Math.Min(1.0, ve));
        }

        private static double BaseTiming(int rpm, int load, EcuProfile profile)
        {
            // Ateşleme avansı: düşük yükte fazla avans, yüksek yükte daha az
            double rpmNorm = rpm / (double)(profile.RpmAxis[profile.RpmAxis.Length - 1]);
            double ldNorm = load / 170.0;
            // 5° (rölanti) → 30° (orta) → 20° (tam yük) arası
            double timing = 8.0 + 22.0 * rpmNorm * (1.0 - ldNorm * 0.4);
            return Math.Max(5.0, Math.Min(40.0, timing));
        }

        private static double ApplyGoalFuelMod(double raw, TuningGoal goal, int rpm, int load, EcuProfile profile)
        {
            switch (goal)
            {
                case TuningGoal.TurboSafeBase:
                    return raw * 1.15; // turbo için zenginleştir
                case TuningGoal.Economy:
                    return raw * (load > 100 ? 0.95 : 0.92);
                case TuningGoal.NaturallyAspirated:
                    return raw * (rpm > profile.VtecRpmDefault ? 1.08 : 1.0);
                case TuningGoal.StockStreet:
                    return raw * 0.98;
                default: // IesVtecStreet
                    return raw;
            }
        }

        private static double ApplyGoalTimingMod(double t, TuningGoal goal, int rpm, int load, EcuProfile profile)
        {
            switch (goal)
            {
                case TuningGoal.TurboSafeBase:
                    return t * 0.85;
                case TuningGoal.Economy:
                    return t * 1.05;
                case TuningGoal.NaturallyAspirated:
                    return t * (rpm > profile.VtecRpmDefault ? 1.06 : 1.02);
                default:
                    return t;
            }
        }

        private static byte TimingToByte(double deg)
        {
            // 0-255 → yaklaşık 0-60° avans
            return Clamp(deg / 60.0 * 255.0);
        }

        private static byte Clamp(double v) =>
            (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));

        private static int FindNearest(int[] axis, int target)
        {
            int best = 0, bestDist = int.MaxValue;
            for (int i = 0; i < axis.Length; i++)
            {
                int d = Math.Abs(axis[i] - target);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private static string BuildSummary(TuningSetup setup, EcuProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Tuning Asistanı Basemap ===");
            sb.AppendLine($"Profil   : {profile.Name}");
            sb.AppendLine($"Hedef    : {DescribeGoal(setup.Goal)}");
            sb.AppendLine($"Enjektör : {setup.InjectorCc} cc");
            sb.AppendLine($"MAP Sns. : {setup.MapSensorBar} bar");
            sb.AppendLine($"Güç AFR  : {setup.TargetAfrPower:F1}");
            sb.AppendLine($"Seyir AFR: {setup.TargetAfrCruise:F1}");
            if (profile.HasVtec)
                sb.AppendLine($"VTEC RPM : {setup.VtecRpm}");
            sb.AppendLine($"Rev Limit: {setup.RevLimitRpm} rpm");
            sb.AppendLine($"Hız Sınır: {setup.SpeedLimitKmh} km/h");
            sb.AppendLine($"Inj.Dead : {setup.InjectorDeadTimeMs:F2} ms");
            sb.AppendLine();
            sb.AppendLine("NOT: Bu basemap matematiksel model üretir.");
            sb.AppendLine("Dinamometrede ince ayar ZORUNLUDUR.");
            sb.AppendLine("Telifli ROM kullanılmaz/dağıtılmaz.");
            return sb.ToString();
        }
    }
}
