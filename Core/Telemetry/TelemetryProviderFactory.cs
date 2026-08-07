using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri sağlayıcı üretici arayüzü.
    /// </summary>
    public interface ITelemetryProviderFactory
    {
        ITelemetryProvider CreateProvider(string providerName);
    }

    public class TelemetryProviderFactory : ITelemetryProviderFactory
    {
        private readonly ITimeProvider _timeProvider;

        public TelemetryProviderFactory(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public ITelemetryProvider CreateProvider(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                return new MockProvider(_timeProvider);

            switch (providerName.ToLowerInvariant())
            {
                case "mock":
                case "mockprovider":
                    return new MockProvider(_timeProvider);
                case "obd2":
                case "obd2provider":
                    return new Obd2Provider(_timeProvider);
                case "rtp":
                case "rtpemulator":
                case "rtpemulatorprovider":
                    return new RtpEmulatorProvider(_timeProvider);
                case "can":
                case "canprovider":
                    return new CanProvider(_timeProvider);
                case "kline":
                case "klineprovider":
                    return new KLineProvider(_timeProvider);
                case "j2534":
                case "j2534provider":
                    return new J2534Provider(_timeProvider);
                default:
                    throw new ArgumentException($"Bilinmeyen sağlayıcı adı: {providerName}");
            }
        }
    }

    /// <summary>
    /// Bağlı donanımları veya bağlantı portlarını tarayarak (USB, Serial Obd2, RTP vb.)
    /// kullanılabilir telemetri sağlayıcılarını tespit eden arayüzdür.
    /// </summary>
    public interface ITelemetryProviderDiscovery
    {
        void Scan();
        void Refresh();
        IEnumerable<string> GetAvailableProviders();
        bool SupportsAutoDetection();
    }

    public class TelemetryProviderDiscovery : ITelemetryProviderDiscovery
    {
        private readonly List<string> _providers = new List<string>();
        private readonly object _lock = new object();

        public TelemetryProviderDiscovery()
        {
            Refresh();
        }

        public void Scan()
        {
            lock (_lock)
            {
                // Gelecekte USB, COM Port veya J2534 DLL taraması burada yapılacaktır.
                Refresh();
            }
        }

        public void Refresh()
        {
            lock (_lock)
            {
                _providers.Clear();
                _providers.Add("MockProvider");
                _providers.Add("Obd2Provider");
                _providers.Add("RtpEmulatorProvider");
                _providers.Add("CanProvider");
                _providers.Add("KLineProvider");
                _providers.Add("J2534Provider");
            }
        }

        public IEnumerable<string> GetAvailableProviders()
        {
            lock (_lock)
            {
                return new List<string>(_providers);
            }
        }

        public bool SupportsAutoDetection() => true;
    }
}
