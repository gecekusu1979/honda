using System;
using System.Collections.Generic;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Calibration
{
    /// <summary>
    /// Kalibrasyon değişikliklerini işlem mantığında gruplayan sınıf (Transaction).
    /// </summary>
    public class CalibrationTransaction
    {
        public string TransactionId { get; } = Guid.NewGuid().ToString();
        public List<CalibrationChange> Changes { get; } = new List<CalibrationChange>();
        public DateTime StartTime { get; } = DateTime.Now;
        public bool IsCommitted { get; private set; }
        public bool IsRolledBack { get; private set; }

        public void AddChange(CalibrationChange change)
        {
            if (IsCommitted || IsRolledBack)
                throw new InvalidOperationException("İşlem tamamlandıktan sonra değişiklik eklenemez.");

            Changes.Add(change);
        }

        public void Commit()
        {
            if (IsCommitted || IsRolledBack)
                throw new InvalidOperationException("İşlem zaten tamamlanmış.");

            IsCommitted = true;
        }

        public void Rollback()
        {
            if (IsCommitted || IsRolledBack)
                throw new InvalidOperationException("İşlem zaten tamamlanmış.");

            IsRolledBack = true;
        }
    }
}
