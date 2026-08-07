using System;

namespace HondaTuner.Core.AutoTune
{
    public interface IAutoTuneDomainEvent
    {
        string EventId { get; }
        string SessionId { get; }
        string EcuIdentifier { get; }
        DateTime Timestamp { get; }
        string User { get; }
        AutoTuneOperatingMode OperatingMode { get; }
        string EventType { get; }
        string Payload { get; }
    }
}
