using System.Collections.Concurrent;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// TelemetryFrame nesnelerini yeniden kullanmak ve Garbage Collector (GC) üzerindeki
    /// baskıyı en aza indirmek için tasarlanmış thread-safe bir nesne havuzudur.
    /// </summary>
    public static class TelemetryFramePool
    {
        private static readonly ConcurrentBag<TelemetryFrame> _pool = new ConcurrentBag<TelemetryFrame>();
        private const int MaxPoolSize = 25000;

        /// <summary>
        /// Havuzdan temizlenmiş bir TelemetryFrame nesnesi alır. 
        /// Havuz boşsa yeni bir örnek oluşturur.
        /// </summary>
        public static TelemetryFrame Rent()
        {
            if (_pool.TryTake(out var frame))
            {
                frame.Reset();
                return frame;
            }
            return new TelemetryFrame();
        }

        /// <summary>
        /// Kullanımı tamamlanmış frame nesnesini havuza geri kazandırır.
        /// </summary>
        public static void Return(TelemetryFrame frame)
        {
            if (frame == null) return;
            if (_pool.Count < MaxPoolSize)
            {
                frame.Reset();
                _pool.Add(frame);
            }
        }

        /// <summary>
        /// Havuzu temizler.
        /// </summary>
        public static void Clear()
        {
            _pool.Clear();
        }
    }
}
