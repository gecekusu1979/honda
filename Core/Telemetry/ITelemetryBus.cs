using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri kuyruğunun kapasitesinin dolması durumunda uygulanacak politika.
    /// </summary>
    public enum BackpressurePolicy
    {
        DropOldest,
        DropNewest,
        BlockPublisher,
        ExpandQueue
    }

    /// <summary>
    /// Event Bus'ın çalışma performans ve istatistik verileri.
    /// </summary>
    public class TelemetryBusMetrics
    {
        public double PublishedFramesPerSecond { get; set; }
        public int SubscribersCount { get; set; }
        public int QueueLength { get; set; }
        public double AverageDispatchTimeMs { get; set; }
        public int DroppedFramesCount { get; set; }
        public double AveragePublishTimeMs { get; set; }
        public double MaxPublishTimeMs { get; set; }
        public double BusUtilization { get; set; } // 0.0 - 1.0 (Kuyruk doluluk oranı)
    }

    /// <summary>
    /// Telemetri kanalları, tanı yolları ve servis dışı olay haberleşmelerini 
    /// yöneten merkezi Event Bus arayüzüdür.
    /// </summary>
    public interface ITelemetryBus
    {
        /// <summary>
        /// Bus çalışma performans metrikleri.
        /// </summary>
        TelemetryBusMetrics Metrics { get; }

        /// <summary>
        /// Canlı veritabanındaki son durum özetini çeker.
        /// </summary>
        TelemetrySnapshot GetSnapshot();

        /// <summary>
        /// Belirli bir kanaldan gelen son yayınlanmış veri çerçevesini çeker.
        /// </summary>
        TelemetryFrame GetLatest(string channelId);

        /// <summary>
        /// Telemetri verilerini ve olay akışlarını abonelere dağıtır.
        /// </summary>
        void Publish(TelemetryFrame frame);

        /// <summary>
        /// Telemetri verilerini asenkron olarak yayınlar.
        /// </summary>
        Task PublishAsync(TelemetryFrame frame);

        /// <summary>
        /// Tanı (Diagnostic) ve Sistem olaylarını Event Bus üzerinde yayınlar.
        /// Olaylar, telemetri çerçevelerinden ayrı bir Diagnostic Stream üzerinden dağıtılır.
        /// </summary>
        void PublishEvent(TelemetryEvent busEvent);

        /// <summary>
        /// Aboneyi belirli kanallar ve minimum yenilenme hızı kısıtlaması ile kaydeder.
        /// </summary>
        void Subscribe(ITelemetryConsumer consumer, IEnumerable<string> channels = null, double minUpdateRate = 0.0);

        /// <summary>
        /// Abonenin üyeliğini sonlandırır.
        /// </summary>
        void Unsubscribe(ITelemetryConsumer consumer);

        /// <summary>
        /// Bus üzerinde bekleyen tüm mesajları temizler.
        /// </summary>
        void Flush();

        /// <summary>
        /// Bus'ı başlatır ve lifecycle olaylarını ateşler.
        /// </summary>
        void Start();

        /// <summary>
        /// Bus'ı durdurur ve lifecycle olaylarını ateşler.
        /// </summary>
        void Stop();

        /// <summary>
        /// Sürekli değişen backpressure politikasını ayarlar.
        /// </summary>
        void SetBackpressurePolicy(BackpressurePolicy policy);
    }
}
