using System;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneDomainEvent : IAutoTuneDomainEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; }
        public string EcuIdentifier { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string User { get; set; }
        public AutoTuneOperatingMode OperatingMode { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
    }
}
