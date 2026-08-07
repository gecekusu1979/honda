using System;
using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public string EcuIdentifier { get; set; } = "P28-Mock";
        public string ActiveProfile { get; set; } = "Street";
        public string UserRole { get; set; } = "Beginner";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public string State { get; set; } = "Created"; // Lifecycle states (Created, Initializing, etc.)
        public AutoTuneOperatingMode OperatingMode { get; set; } = AutoTuneOperatingMode.Normal;

        private readonly List<TuneDecision> _decisions = new List<TuneDecision>();
        private readonly object _lockObj = new object();

        public IReadOnlyList<TuneDecision> Decisions
        {
            get
            {
                lock (_lockObj)
                {
                    return new List<TuneDecision>(_decisions).AsReadOnly();
                }
            }
        }

        public void AddDecision(TuneDecision decision)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            lock (_lockObj)
            {
                _decisions.Add(decision);
            }
        }

        public void ClearDecisions()
        {
            lock (_lockObj)
            {
                _decisions.Clear();
            }
        }
    }
}
