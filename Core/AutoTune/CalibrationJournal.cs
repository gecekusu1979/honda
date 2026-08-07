using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace HondaTuner.Core.AutoTune
{
    public class JournalEntry
    {
        public string JournalId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string User { get; set; }
        public string Profile { get; set; }
        public string Parameter { get; set; }
        public int RPM { get; set; }
        public int Load { get; set; }
        public double BeforeValue { get; set; }
        public double AfterValue { get; set; }
        public double Confidence { get; set; }
        public string SafetyStatus { get; set; }
        public string ApprovalStatus { get; set; }
        public string Result { get; set; } // Accepted, Rejected, RolledBack
    }

    public class CalibrationJournal
    {
        private readonly List<JournalEntry> _entries = new List<JournalEntry>();
        private readonly object _lockObj = new object();

        public void Log(JournalEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            lock (_lockObj)
            {
                _entries.Add(entry);
            }
        }

        public IReadOnlyList<JournalEntry> AllEntries
        {
            get
            {
                lock (_lockObj)
                {
                    return new List<JournalEntry>(_entries).AsReadOnly();
                }
            }
        }

        public List<JournalEntry> Search(string username = null, string parameter = null, string result = null)
        {
            lock (_lockObj)
            {
                var query = _entries.AsEnumerable();
                if (!string.IsNullOrEmpty(username))
                {
                    query = query.Where(e => string.Equals(e.User, username, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(parameter))
                {
                    query = query.Where(e => string.Equals(e.Parameter, parameter, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(result))
                {
                    query = query.Where(e => string.Equals(e.Result, result, StringComparison.OrdinalIgnoreCase));
                }
                return query.ToList();
            }
        }

        public string ExportJson()
        {
            lock (_lockObj)
            {
                return JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        public void Replay(Action<JournalEntry> replayAction)
        {
            if (replayAction == null) throw new ArgumentNullException(nameof(replayAction));
            List<JournalEntry> snapshot;
            lock (_lockObj)
            {
                snapshot = new List<JournalEntry>(_entries);
            }

            foreach (var e in snapshot)
            {
                replayAction(e);
            }
        }
    }
}
