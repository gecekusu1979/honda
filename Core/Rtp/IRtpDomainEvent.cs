using System;

namespace HondaTuner.Core.Rtp
{
    public interface IRtpDomainEvent
    {
        string EventName { get; }
        DateTime Timestamp { get; }
        string Message { get; }
    }
}
