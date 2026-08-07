using System;
using System.Text;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Report
{
    /// <summary>
    /// Kalibrasyon Rapor Derleyicisi — ROM byte delta'larını
    /// insanlar tarafından okunabilir açıklamalara dönüştürür.
    /// </summary>
    public static class CalibrationReportCompiler
    {
        /// <summary>
        /// İki ROM arasındaki farkları kalibrasyon terimleriyle açıklar.
        /// </summary>
        public static string CompileHumanReadableDiff(
            byte[] originalRom, byte[] modifiedRom, Core.EcuProfile profile)
        {
            if (originalRom == null || modifiedRom == null || profile == null)
                return "Karşılaştırma verisi yetersiz.";

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Kalibrasyon Karşılaştırma Raporu — {profile.EcuCode} / {profile.EngineCode}");
            sb.AppendLine(new string('─', 60));

            // Fuel Map değişiklikleri
            var fuelChanges = CompareMapRegion(originalRom, modifiedRom,
                profile.FuelMapOffset, profile.FuelMapRows, profile.FuelMapCols);
            if (fuelChanges.Count > 0)
            {
                sb.AppendLine($"\n🔥 Yakıt Haritası ({fuelChanges.Count} hücre değişti):");
                double avgChange = 0;
                foreach (var c in fuelChanges)
                {
                    avgChange += c.pct;
                }
                avgChange /= fuelChanges.Count;
                sb.AppendLine($"   Ortalama değişim: {avgChange:+0.0;-0.0}%");
            }
            else
            {
                sb.AppendLine("\n🔥 Yakıt Haritası: Değişiklik yok");
            }

            // Ignition Map değişiklikleri
            var ignChanges = CompareMapRegion(originalRom, modifiedRom,
                profile.IgnMapOffset, profile.IgnMapRows, profile.IgnMapCols);
            if (ignChanges.Count > 0)
            {
                sb.AppendLine($"\n⚡ Ateşleme Haritası ({ignChanges.Count} hücre değişti):");
                double avgChange = 0;
                foreach (var c in ignChanges)
                    avgChange += c.pct;
                avgChange /= ignChanges.Count;
                sb.AppendLine($"   Ortalama değişim: {avgChange:+0.0;-0.0}°");
            }
            else
            {
                sb.AppendLine("\n⚡ Ateşleme Haritası: Değişiklik yok");
            }

            // VTEC
            if (profile.VtecRpmOffset > 0 && profile.VtecRpmOffset < originalRom.Length)
            {
                int origVtec = originalRom[profile.VtecRpmOffset] * 50;
                int modVtec = modifiedRom[profile.VtecRpmOffset] * 50;
                if (origVtec != modVtec)
                    sb.AppendLine($"\n🏎️ VTEC Geçiş: {origVtec} RPM → {modVtec} RPM ({modVtec - origVtec:+0;-0} RPM)");
            }

            // Rev Limit
            if (profile.RevLimitOffset > 0 && profile.RevLimitOffset < originalRom.Length)
            {
                int origRev = originalRom[profile.RevLimitOffset] * 50;
                int modRev = modifiedRom[profile.RevLimitOffset] * 50;
                if (origRev != modRev)
                    sb.AppendLine($"\n🔴 Rev Limit: {origRev} RPM → {modRev} RPM ({modRev - origRev:+0;-0} RPM)");
            }

            return sb.ToString();
        }

        private static System.Collections.Generic.List<(int row, int col, double pct)> CompareMapRegion(
            byte[] orig, byte[] mod, int offset, int rows, int cols)
        {
            var changes = new System.Collections.Generic.List<(int, int, double)>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int idx = offset + (r * cols) + c;
                    if (idx >= orig.Length || idx >= mod.Length) continue;
                    if (orig[idx] != mod[idx])
                    {
                        double pct = orig[idx] == 0 ? 0 :
                            ((mod[idx] - orig[idx]) / (double)orig[idx]) * 100.0;
                        changes.Add((r, c, pct));
                    }
                }
            }
            return changes;
        }
    }
}
