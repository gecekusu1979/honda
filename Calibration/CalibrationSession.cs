using System;
using System.Collections.Generic;

namespace HondaTuner.Calibration
{
    /// <summary>
    /// Aktif kalibrasyon oturumunu temsil eder.
    /// </summary>
    public class CalibrationSession
    {
        public string SessionId { get; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; } = DateTime.Now;
        public string EcuCode { get; set; }
        public List<CalibrationTransaction> CommittedTransactions { get; } = new List<CalibrationTransaction>();

        public CalibrationSession(string ecuCode)
        {
            EcuCode = ecuCode;
        }

        public void AddTransaction(CalibrationTransaction tx)
        {
            if (tx == null) throw new ArgumentNullException(nameof(tx));
            if (!tx.IsCommitted) throw new InvalidOperationException("Yalnızca commit edilmiş işlemler oturuma eklenebilir.");
            CommittedTransactions.Add(tx);
        }
    }
}
