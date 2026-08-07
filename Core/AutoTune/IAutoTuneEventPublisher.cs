using System;

namespace HondaTuner.Core.AutoTune
{
    public interface IAutoTuneEventPublisher
    {
        void Publish(IAutoTuneDomainEvent domainEvent);
        event Action<IAutoTuneDomainEvent> OnEventPublished;
    }
}
