using System;
using System.Diagnostics;
using System.Threading;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri sistemi çalışma sağlığı (dropped frames, timeouts, latency, CPU/RAM kullanımı)
    /// izleyen ve performans bozulmalarında uyarılar üreten servistir.
    /// </summary>
    public class TelemetryHealthMonitor : ITelemetryConsumer, IDisposable
    {
        private readonly ITelemetryBus _telemetryBus;
        private readonly System.Diagnostics.Process _currentProcess;
        private readonly Timer _healthTimer;

        // Eşik Değerleri (Configurable)
        public double MaxLatencyAllowedMs { get; set; } = 50.0;
        public double MaxDroppedFramesAllowed { get; set; } = 100;
        public double MinFrameRateAllowedHz { get; set; } = 10.0;

        // Son Ölçüm İstatistikleri
        public double CurrentCpuUsage { get; private set; }
        public long CurrentMemoryUsageBytes { get; private set; }
        public double CurrentJitterMs { get; private set; }
        public double CurrentFrameRateHz { get; private set; }
        public int TotalTimeoutsCount { get; private set; }

        private double _lastFrameElapsed = 0.0;
        private int _timeoutCount = 0;
        private readonly object _lock = new object();

        public TelemetryHealthMonitor(ITelemetryBus telemetryBus)
        {
            _telemetryBus = telemetryBus ?? throw new ArgumentNullException(nameof(telemetryBus));
            _currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            // Saniyede 1 kere CPU/RAM ve sistem kuyruğunu sorgula
            _healthTimer = new Timer(CheckSystemHealth, null, 1000, 1000);
            _telemetryBus.Subscribe(this);
        }

        public void Consume(TelemetryFrame frame)
        {
            if (frame == null) return;
            lock (_lock)
            {
                // Jitter (seğirme) hesaplama: İki kare arasındaki süre titremesi
                if (_lastFrameElapsed > 0.0)
                {
                    double currentDiff = frame.ElapsedTime - _lastFrameElapsed;
                    double expectedDiff = frame.UpdateRate > 0 ? (1.0 / frame.UpdateRate) : 0.01;
                    CurrentJitterMs = Math.Abs(currentDiff - expectedDiff) * 1000.0;
                }
                _lastFrameElapsed = frame.ElapsedTime;

                // Frame kalitesi timeout ise sayacı arttır
                if (frame.Status == ChannelStatus.Timeout)
                {
                    _timeoutCount++;
                    TotalTimeoutsCount++;

                    if (_timeoutCount > 5)
                    {
                        RaiseHealthWarning($"Sıralı Timeout Tespit Edildi: Kanal {frame.ChannelId} yanıt vermiyor.");
                    }
                }
                else
                {
                    _timeoutCount = 0;
                }
            }
        }

        public void ConsumeEvent(TelemetryEvent busEvent)
        {
            if (busEvent == null) return;

            if (busEvent.EventType == TelemetryEventType.ErrorOccurred)
            {
                RaiseHealthWarning($"Kritik Hata Olayı Algılandı: {busEvent.Message}");
            }
        }

        private void CheckSystemHealth(object state)
        {
            try
            {
                lock (_lock)
                {
                    _currentProcess.Refresh();

                    // Basit CPU ve Bellek kestirimi
                    CurrentMemoryUsageBytes = _currentProcess.PagedMemorySize64;
                    CurrentCpuUsage = double.NaN; // .NET Framework / Standard cross-platform CPU sorgusu tescili zordur, mock değerler yerleşebilir

                    var busMetrics = _telemetryBus.Metrics;
                    CurrentFrameRateHz = busMetrics.PublishedFramesPerSecond;

                    // Eşik kontrolleri
                    if (busMetrics.DroppedFramesCount > MaxDroppedFramesAllowed)
                    {
                        RaiseHealthError($"Çok fazla düşen veri karesi (Dropped Frames): {busMetrics.DroppedFramesCount}");
                    }

                    if (CurrentFrameRateHz > 0 && CurrentFrameRateHz < MinFrameRateAllowedHz)
                    {
                        RaiseHealthWarning($"Düşük Kare Hızı Algılandı: {CurrentFrameRateHz:F1} Hz");
                    }

                    if (busMetrics.AverageDispatchTimeMs > MaxLatencyAllowedMs)
                    {
                        RaiseHealthWarning($"Yüksek Dağıtım Gecikmesi: {busMetrics.AverageDispatchTimeMs:F2} ms");
                    }
                }
            }
            catch
            {
                // Yut
            }
        }

        private void RaiseHealthWarning(string message)
        {
            _telemetryBus.PublishEvent(new TelemetryEvent
            {
                EventType = TelemetryEventType.DiagnosticMessage,
                Timestamp = DateTime.UtcNow,
                Source = "TelemetryHealthMonitor",
                Message = $"[WARNING] {message}",
                Priority = MessagePriority.High
            });
        }

        private void RaiseHealthError(string message)
        {
            _telemetryBus.PublishEvent(new TelemetryEvent
            {
                EventType = TelemetryEventType.ErrorOccurred,
                Timestamp = DateTime.UtcNow,
                Source = "TelemetryHealthMonitor",
                Message = $"[ERROR] {message}",
                Priority = MessagePriority.Critical
            });
        }

        public void Dispose()
        {
            _healthTimer.Dispose();
            _telemetryBus.Unsubscribe(this);
            _currentProcess.Dispose();
        }
    }
}
