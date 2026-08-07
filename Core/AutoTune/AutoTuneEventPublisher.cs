using System;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneEventPublisher : IAutoTuneEventPublisher
    {
        public event Action<IAutoTuneDomainEvent> OnEventPublished;

        public void Publish(IAutoTuneDomainEvent domainEvent)
        {
            if (domainEvent == null) return;
            ApplicationLogger.Info("AutoTuneEventPublisher", $"Domain olayı yayınlandı: {domainEvent.EventType} (Oturum: {domainEvent.SessionId})");
            OnEventPublished?.Invoke(domainEvent);
        }
    }
}
