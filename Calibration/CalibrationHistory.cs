using System;
using System.Collections.Generic;

namespace HondaTuner.Calibration
{
    /// <summary>
    /// Değişiklik geçmişini ve denetim loglarını (Audit Logs) tutan havuz.
    /// </summary>
    public class CalibrationHistory
    {
        private readonly List<CalibrationTransaction> _committedTransactions = new List<CalibrationTransaction>();
        private readonly List<string> _auditLogs = new List<string>();

        public IReadOnlyList<CalibrationTransaction> CommittedTransactions => _committedTransactions;
        public IReadOnlyList<string> AuditLogs => _auditLogs;

        public void AddTransaction(CalibrationTransaction tx)
        {
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            _committedTransactions.Add(tx);

            string audit = $"[Transaction Committed] ID: {tx.TransactionId}, Değişiklik sayısı: {tx.Changes.Count}, Cilt: {tx.StartTime}";
            _auditLogs.Add(audit);
        }

        public void AddAuditLog(string log)
        {
            _auditLogs.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {log}");
        }

        public void Clear()
        {
            _committedTransactions.Clear();
            _auditLogs.Clear();
            AddAuditLog("Geçmiş log havuzu temizlendi.");
        }
    }
}
