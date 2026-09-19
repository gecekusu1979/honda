using System;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneDomainEvent : IAutoTuneDomainEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = null!;
        public string EcuIdentifier { get; set; } = null!;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string User { get; set; } = null!;
        public AutoTuneOperatingMode OperatingMode { get; set; }
        public string EventType { get; set; } = null!;
        public string Payload { get; set; } = null!;
    }
}