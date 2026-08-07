using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Report
{
    /// <summary>
    /// HTML Tune Raporu Oluşturucu.
    /// Kalibrasyon değişikliklerini görsel bir HTML belgesine dönüştürür.
    /// </summary>
    public class RawHtmlReportGenerator : IReportGenerator
    {
        public string GenerateReport(TuningSessionInfo session)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='tr'><head><meta charset='utf-8'>");
            sb.AppendLine("<title>HondaTuner — Tune Raporu</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', sans-serif; background: #0d1117; color: #e6edf3; padding: 24px; }");
            sb.AppendLine("h1 { color: #58a6ff; } h2 { color: #3fb950; margin-top: 24px; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 12px; }");
            sb.AppendLine("th, td { border: 1px solid #30363d; padding: 8px 12px; text-align: left; }");
            sb.AppendLine("th { background: #161b22; } tr:nth-child(even) { background: #161b22; }");
            sb.AppendLine(".positive { color: #3fb950; } .negative { color: #f85149; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<h1>🏁 HondaTuner Tune Raporu</h1>");
            sb.AppendLine($"<p><strong>Tarih:</strong> {session.Date:dd.MM.yyyy HH:mm}</p>");
            sb.AppendLine($"<p><strong>Araç:</strong> {session.Vehicle ?? "—"}</p>");
            sb.AppendLine($"<p><strong>Motor:</strong> {session.Engine ?? "—"}</p>");
            sb.AppendLine($"<p><strong>ECU:</strong> {session.EcuCode ?? "—"}</p>");
            sb.AppendLine($"<p><strong>Tuner:</strong> {session.TunerName ?? "—"}</p>");

            // Değişiklik tablosu
            if (session.Changes != null && session.Changes.Count > 0)
            {
                sb.AppendLine("<h2>📋 Kalibrasyon Değişiklikleri</h2>");
                sb.AppendLine("<table><tr><th>Parametre</th><th>Eski</th><th>Yeni</th><th>Değişim</th><th>Kaynak</th></tr>");

                foreach (var c in session.Changes)
                {
                    string pctClass = c.ChangePercent >= 0 ? "positive" : "negative";
                    string pctText = $"{c.ChangePercent:+0.0;-0.0}%";
                    sb.AppendLine($"<tr><td>{c.Parameter}</td><td>{c.OldValue}</td>" +
                        $"<td>{c.NewValue}</td><td class='{pctClass}'>{pctText}</td>" +
                        $"<td>{c.Source ?? "—"}</td></tr>");
                }
                sb.AppendLine("</table>");
            }

            // ROM boyutu
            if (session.OriginalRom != null && session.ModifiedRom != null)
            {
                int diffCount = 0;
                int minLen = Math.Min(session.OriginalRom.Length, session.ModifiedRom.Length);
                for (int i = 0; i < minLen; i++)
                    if (session.OriginalRom[i] != session.ModifiedRom[i]) diffCount++;

                sb.AppendLine("<h2>💾 ROM Özeti</h2>");
                sb.AppendLine($"<p>ROM boyutu: {session.OriginalRom.Length} byte</p>");
                sb.AppendLine($"<p>Değişen byte sayısı: <strong>{diffCount}</strong></p>");
            }

            if (!string.IsNullOrEmpty(session.Notes))
            {
                sb.AppendLine("<h2>📝 Notlar</h2>");
                sb.AppendLine($"<p>{session.Notes}</p>");
            }

            sb.AppendLine("<hr><p style='color:#8b949e;font-size:12px;'>HondaTuner V2 Advanced Calibration Platform</p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        public void SaveToFile(string filePath, TuningSessionInfo session)
        {
            string html = GenerateReport(session);
            System.IO.File.WriteAllText(filePath, html, Encoding.UTF8);
        }
    }
}
