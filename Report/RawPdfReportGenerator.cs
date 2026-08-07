using System.Text;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Report
{
    /// <summary>
    /// PDF Tune Raporu Oluşturucu.
    /// HTML raporunu temel alarak basit metin tabanlı PDF benzeri çıktı üretir.
    /// Tam PDF desteği için harici kütüphane (iTextSharp, QuestPDF) entegrasyonu yapılabilir.
    /// </summary>
    public class RawPdfReportGenerator : IReportGenerator
    {
        private readonly RawHtmlReportGenerator _htmlGenerator = new RawHtmlReportGenerator();

        public string GenerateReport(TuningSessionInfo session)
        {
            // Metin tabanlı rapor — PDF kütüphanesi olmadan düz metin çıktı
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine("  HONDATUNER — TUNE RAPORU");
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"  Tarih  : {session.Date:dd.MM.yyyy HH:mm}");
            sb.AppendLine($"  Araç   : {session.Vehicle ?? "—"}");
            sb.AppendLine($"  Motor  : {session.Engine ?? "—"}");
            sb.AppendLine($"  ECU    : {session.EcuCode ?? "—"}");
            sb.AppendLine($"  Tuner  : {session.TunerName ?? "—"}");
            sb.AppendLine("───────────────────────────────────────────────────────────");

            if (session.Changes != null && session.Changes.Count > 0)
            {
                sb.AppendLine("\n  KALİBRASYON DEĞİŞİKLİKLERİ:");
                sb.AppendLine("  ─────────────────────────────────────────────────────");
                foreach (var c in session.Changes)
                {
                    sb.AppendLine($"  {c.Parameter,-25} {c.OldValue,6} → {c.NewValue,6}  ({c.ChangePercent:+0.0;-0.0}%)");
                }
            }

            if (session.OriginalRom != null && session.ModifiedRom != null)
            {
                int diffCount = 0;
                for (int i = 0; i < session.OriginalRom.Length && i < session.ModifiedRom.Length; i++)
                    if (session.OriginalRom[i] != session.ModifiedRom[i]) diffCount++;

                sb.AppendLine($"\n  ROM Boyutu: {session.OriginalRom.Length} byte");
                sb.AppendLine($"  Değişen:    {diffCount} byte");
            }

            if (!string.IsNullOrEmpty(session.Notes))
            {
                sb.AppendLine($"\n  Notlar: {session.Notes}");
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine("  HondaTuner V2 Advanced Calibration Platform");
            return sb.ToString();
        }

        public void SaveToFile(string filePath, TuningSessionInfo session)
        {
            string content = GenerateReport(session);
            System.IO.File.WriteAllText(filePath, content, Encoding.UTF8);
            ApplicationLogger.Info("PdfReportGenerator", $"Rapor kaydedildi: {filePath}");
        }
    }
}
