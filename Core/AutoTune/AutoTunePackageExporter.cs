using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTunePackageExporter
    {
        public string ExportSessionToJson(AutoTuneSession session, CalibrationJournal journal, List<CalibrationSnapshot> snapshots)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var package = new
            {
                Session = session,
                Journal = journal != null ? journal.AllEntries : new List<JournalEntry>(),
                Snapshots = snapshots ?? new List<CalibrationSnapshot>(),
                ExportTimestamp = DateTime.Now,
                FormatVersion = "MDF-Compatible-1.0"
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(package, options);
        }

        public string ExportSessionToCsv(CalibrationJournal journal)
        {
            if (journal == null) throw new ArgumentNullException(nameof(journal));

            var sb = new StringBuilder();
            sb.AppendLine("JournalId,Timestamp,User,Profile,Parameter,RPM,Load,BeforeValue,AfterValue,Confidence,SafetyStatus,ApprovalStatus,Result");

            foreach (var e in journal.AllEntries)
            {
                sb.AppendLine($"{e.JournalId}," +
                              $"{e.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                              $"\"{e.User}\"," +
                              $"\"{e.Profile}\"," +
                              $"\"{e.Parameter}\"," +
                              $"{e.RPM}," +
                              $"{e.Load}," +
                              $"{e.BeforeValue:F2}," +
                              $"{e.AfterValue:F2}," +
                              $"{e.Confidence:F1}," +
                              $"\"{e.SafetyStatus}\"," +
                              $"\"{e.ApprovalStatus}\"," +
                              $"\"{e.Result}\"");
            }

            return sb.ToString();
        }
    }
}
