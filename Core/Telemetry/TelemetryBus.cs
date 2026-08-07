using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HondaTuner.Core.Telemetry
{
    public class TelemetryBus : ITelemetryBus, IDisposable
    {
        private class Subscription
        {
            public ITelemetryConsumer Consumer { get; }
            public HashSet<string> Channels { get; }
            public double MinUpdateRate { get; }
            public Dictionary<string, DateTime> LastConsumeTime { get; } = new Dictionary<string, DateTime>();

            public Subscription(ITelemetryConsumer consumer, IEnumerable<string> channels, double minUpdateRate)
            {
                Consumer = consumer;
                Channels = channels != null ? new HashSet<string>(channels) : null;
                MinUpdateRate = minUpdateRate;
            }
        }

        private readonly ConcurrentDictionary<string, TelemetryFrame> _latestFrames = new ConcurrentDictionary<string, TelemetryFrame>();
        private readonly List<Subscription> _subscriptions = new List<Subscription>();
        private readonly object _subLock = new object();

        // Çift Kuyruk Tasarımı: Telemetri Verileri (Çok Hızlı) vs. Tanı Olayları
        private readonly BlockingCollection<TelemetryFrame> _telemetryQueue = new BlockingCollection<TelemetryFrame>(new ConcurrentQueue<TelemetryFrame>(), 10000);
        private readonly BlockingCollection<TelemetryEvent> _diagnosticQueue = new BlockingCollection<TelemetryEvent>(new ConcurrentQueue<TelemetryEvent>(), 1000);

        private readonly Thread _dispatchThread;
        private readonly Thread _diagnosticThread;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private BackpressurePolicy _backpressurePolicy = BackpressurePolicy.DropOldest;
        private readonly object _configLock = new object();

        // Performans Ölçüm Değişkenleri
        private int _publishedFramesCounter = 0;
        private int _droppedFramesCounter = 0;
        private int _totalPublishTimeTicks = 0;
        private double _maxPublishTimeMs = 0.0;
        private int _totalDispatchTimeTicks = 0;
        private int _dispatchedFramesCounter = 0;

        private readonly Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        private double _lastFpsCalculationTime = 0.0;

        public TelemetryBusMetrics Metrics { get; } = new TelemetryBusMetrics();

        private bool _isRunning = false;

        public TelemetryBus()
        {
            // Arka plan dağıtım işçileri (Worker Threads)
            _dispatchThread = new Thread(ProcessTelemetryQueue)
            {
                IsBackground = true,
                Name = "TelemetryBus_TelemetryDispatcher"
            };

            _diagnosticThread = new Thread(ProcessDiagnosticQueue)
            {
                IsBackground = true,
                Name = "TelemetryBus_DiagnosticDispatcher"
            };
        }

        public void Start()
        {
            lock (_configLock)
            {
                if (_isRunning) return;
                _isRunning = true;

                _dispatchThread.Start();
                _diagnosticThread.Start();

                PublishEvent(new TelemetryEvent
                {
                    EventType = TelemetryEventType.BusStarted,
                    Timestamp = DateTime.UtcNow,
                    Source = "TelemetryBus",
                    Message = "Event Bus başarıyla başlatıldı.",
                    Priority = MessagePriority.Normal
                });
            }
        }

        public void Stop()
        {
            lock (_configLock)
            {
                if (!_isRunning) return;
                _isRunning = false;

                PublishEvent(new TelemetryEvent
                {
                    EventType = TelemetryEventType.BusStopped,
                    Timestamp = DateTime.UtcNow,
                    Source = "TelemetryBus",
                    Message = "Event Bus durduruluyor.",
                    Priority = MessagePriority.Normal
                });

                _cts.Cancel();
                _telemetryQueue.CompleteAdding();
                _diagnosticQueue.CompleteAdding();

                if (_dispatchThread.IsAlive) _dispatchThread.Join(1000);
                if (_diagnosticThread.IsAlive) _diagnosticThread.Join(1000);
            }
        }

        public void SetBackpressurePolicy(BackpressurePolicy policy)
        {
            lock (_configLock)
            {
                _backpressurePolicy = policy;
            }
        }

        public TelemetrySnapshot GetSnapshot()
        {
            // Snapshot oluştururken thread-safe bir şekilde en son verileri çekiyoruz.
            DateTime now = DateTime.UtcNow;
            double rpm = GetChannelValue("RPM");
            double tps = GetChannelValue("TPS");
            double map = GetChannelValue("MAP");
            double ect = GetChannelValue("ECT");
            double iat = GetChannelValue("IAT");
            double battery = GetChannelValue("Battery");
            double speed = GetChannelValue("VehicleSpeed");
            double injectorDuty = GetChannelValue("InjectorDuty");
            double ignitionAdvance = GetChannelValue("IgnitionAdvance");
            double afr = GetChannelValue("AFR");
            double lambda = GetChannelValue("Lambda");
            int knock = (int)GetChannelValue("KnockCount");
            double stft = GetChannelValue("FuelTrimSTFT");
            double ltft = GetChannelValue("FuelTrimLTFT");
            double load = GetChannelValue("CalculatedLoad");

            bool closedLoop = GetChannelValue("FuelTrimSTFT") != 0.0 || GetChannelValue("FuelTrimLTFT") != 0.0;
            bool openLoop = !closedLoop;

            long seq = 0;
            if (_latestFrames.TryGetValue("RPM", out var rpmFrame))
            {
                seq = rpmFrame.SequenceNumber;
            }

            return new TelemetrySnapshot(
                "v2.0.0",
                now,
                seq,
                rpm,
                tps,
                map,
                ect,
                iat,
                battery,
                speed,
                injectorDuty,
                ignitionAdvance,
                afr,
                lambda,
                knock,
                stft,
                ltft,
                closedLoop,
                openLoop,
                load
            );
        }

        private double GetChannelValue(string channelId)
        {
            if (_latestFrames.TryGetValue(channelId, out var frame))
            {
                return frame.FilteredValue;
            }
            return 0.0;
        }

        public TelemetryFrame GetLatest(string channelId)
        {
            if (_latestFrames.TryGetValue(channelId, out var frame))
            {
                // Değeri kopyalayarak geri döndür
                var copy = TelemetryFramePool.Rent();
                copy.ChannelId = frame.ChannelId;
                copy.FrameId = frame.FrameId;
                copy.Source = frame.Source;
                copy.SourceId = frame.SourceId;
                copy.SessionId = frame.SessionId;
                copy.Transport = frame.Transport;
                copy.Direction = frame.Direction;
                copy.FrameType = frame.FrameType;
                copy.UtcTimestamp = frame.UtcTimestamp;
                copy.MonotonicTimestamp = frame.MonotonicTimestamp;
                copy.ElapsedTime = frame.ElapsedTime;
                copy.Value = frame.Value;
                copy.RawValue = frame.RawValue;
                copy.FilteredValue = frame.FilteredValue;
                copy.Quality = frame.Quality;
                copy.Status = frame.Status;
                copy.Priority = frame.Priority;
                copy.CRC = frame.CRC;
                copy.Checksum = frame.Checksum;
                copy.Validation = frame.Validation;
                copy.SequenceNumber = frame.SequenceNumber;
                copy.UpdateRate = frame.UpdateRate;
                return copy;
            }
            return null;
        }

        public void Publish(TelemetryFrame frame)
        {
            if (frame == null) return;

            long startTick = Stopwatch.GetTimestamp();

            // En son değeri güncelle
            _latestFrames[frame.ChannelId] = frame;

            bool added = false;
            while (!added && !_cts.IsCancellationRequested)
            {
                // Queue limitleri ve backpressure kontrolü
                if (_telemetryQueue.Count < 10000)
                {
                    added = _telemetryQueue.TryAdd(frame);
                }
                else
                {
                    BackpressurePolicy policy;
                    lock (_configLock)
                    {
                        policy = _backpressurePolicy;
                    }

                    switch (policy)
                    {
                        case BackpressurePolicy.DropOldest:
                            if (_telemetryQueue.TryTake(out var discarded))
                            {
                                Interlocked.Increment(ref _droppedFramesCounter);
                                TelemetryFramePool.Return(discarded);
                            }
                            added = _telemetryQueue.TryAdd(frame);
                            break;

                        case BackpressurePolicy.DropNewest:
                            Interlocked.Increment(ref _droppedFramesCounter);
                            TelemetryFramePool.Return(frame);
                            added = true; // Yutuldu
                            break;

                        case BackpressurePolicy.BlockPublisher:
                            // TryAdd metodu bloklanarak ekleme yapacak (1ms bekle veya spin)
                            Thread.Sleep(1);
                            break;

                        case BackpressurePolicy.ExpandQueue:
                            // expandQueue durumunda normal eklemeye zorla, BlockingCollection yerine ConcurrentQueue büyümesine izin ver
                            // Ancak sınırlandırılmış BlockingCollection kullandığımız için eklemeyi deniyoruz
                            Interlocked.Increment(ref _droppedFramesCounter);
                            TelemetryFramePool.Return(frame);
                            added = true;
                            break;
                    }
                }
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - startTick;
            double elapsedMs = (double)elapsedTicks * 1000 / Stopwatch.Frequency;

            Interlocked.Increment(ref _publishedFramesCounter);

            // Ortalama ve maksimum süre güncelle
            lock (_configLock)
            {
                _totalPublishTimeTicks += (int)elapsedTicks;
                if (elapsedMs > _maxPublishTimeMs) _maxPublishTimeMs = elapsedMs;
            }

            CalculateMetrics();
        }

        public Task PublishAsync(TelemetryFrame frame)
        {
            return Task.Run(() => Publish(frame));
        }

        public void PublishEvent(TelemetryEvent busEvent)
        {
            if (busEvent == null) return;
            _diagnosticQueue.TryAdd(busEvent);
        }

        public void Subscribe(ITelemetryConsumer consumer, IEnumerable<string> channels = null, double minUpdateRate = 0.0)
        {
            if (consumer == null) return;
            lock (_subLock)
            {
                // Mükerrer kaydı engelle
                _subscriptions.RemoveAll(s => s.Consumer == consumer);
                _subscriptions.Add(new Subscription(consumer, channels, minUpdateRate));
            }

            lock (_configLock)
            {
                Metrics.SubscribersCount = _subscriptions.Count;
            }
        }

        public void Unsubscribe(ITelemetryConsumer consumer)
        {
            if (consumer == null) return;
            lock (_subLock)
            {
                _subscriptions.RemoveAll(s => s.Consumer == consumer);
            }

            lock (_configLock)
            {
                Metrics.SubscribersCount = _subscriptions.Count;
            }
        }

        public void Flush()
        {
            while (_telemetryQueue.TryTake(out var frame))
            {
                TelemetryFramePool.Return(frame);
            }
            while (_diagnosticQueue.TryTake(out _)) { }

            _latestFrames.Clear();
        }

        private void ProcessTelemetryQueue()
        {
            try
            {
                foreach (var frame in _telemetryQueue.GetConsumingEnumerable(_cts.Token))
                {
                    long startTick = Stopwatch.GetTimestamp();

                    List<Subscription> subsCopy;
                    lock (_subLock)
                    {
                        subsCopy = new List<Subscription>(_subscriptions);
                    }

                    foreach (var sub in subsCopy)
                    {
                        try
                        {
                            // Kanal filtresi var mı?
                            if (sub.Channels != null && !sub.Channels.Contains(frame.ChannelId))
                            {
                                continue;
                            }

                            // Minimum Yenilenme Hızı kısıtlaması var mı?
                            if (sub.MinUpdateRate > 0.0)
                            {
                                DateTime now = DateTime.UtcNow;
                                if (sub.LastConsumeTime.TryGetValue(frame.ChannelId, out var lastTime))
                                {
                                    double secondsElapsed = (now - lastTime).TotalSeconds;
                                    double currentRate = secondsElapsed > 0 ? (1.0 / secondsElapsed) : 0.0;
                                    // Eğer mevcut hız izin verilenden fazla ise tüketimi kısıtla (örneğin 100Hz yerine 10Hz limit)
                                    // Sık tüketimi önleme mantığı: geçen süre thresholdun altındaysa pas geç
                                    double minSecondsBetweenFrames = 1.0 / sub.MinUpdateRate;
                                    if (secondsElapsed < minSecondsBetweenFrames)
                                    {
                                        continue;
                                    }
                                }
                                sub.LastConsumeTime[frame.ChannelId] = now;
                            }

                            sub.Consumer.Consume(frame);
                        }
                        catch (Exception ex)
                        {
                            // Hata durumunda tanı akışına gönder
                            PublishEvent(new TelemetryEvent
                            {
                                EventType = TelemetryEventType.ErrorOccurred,
                                Timestamp = DateTime.UtcNow,
                                Payload = ex,
                                Source = "TelemetryBus_Dispatcher",
                                Message = $"Tüketici Consume hatası: {ex.Message}",
                                Priority = MessagePriority.High
                            });
                        }
                    }

                    long elapsedTicks = Stopwatch.GetTimestamp() - startTick;

                    Interlocked.Increment(ref _dispatchedFramesCounter);
                    lock (_configLock)
                    {
                        _totalDispatchTimeTicks += (int)elapsedTicks;
                    }

                    // Tüketimi tamamlanan frame'i havuza geri kazandırıyoruz
                    TelemetryFramePool.Return(frame);
                }
            }
            catch (OperationCanceledException)
            {
                // Durduruldu
            }
        }

        private void ProcessDiagnosticQueue()
        {
            try
            {
                foreach (var diagEvent in _diagnosticQueue.GetConsumingEnumerable(_cts.Token))
                {
                    List<Subscription> subsCopy;
                    lock (_subLock)
                    {
                        subsCopy = new List<Subscription>(_subscriptions);
                    }

                    foreach (var sub in subsCopy)
                    {
                        try
                        {
                            sub.Consumer.ConsumeEvent(diagEvent);
                        }
                        catch
                        {
                            // Hata durumunda sessizce yut
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Durduruldu
            }
        }

        private void CalculateMetrics()
        {
            double elapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds - _lastFpsCalculationTime;
            if (elapsedSeconds >= 1.0)
            {
                lock (_configLock)
                {
                    double currentSecs = _fpsStopwatch.Elapsed.TotalSeconds;
                    double delta = currentSecs - _lastFpsCalculationTime;
                    _lastFpsCalculationTime = currentSecs;

                    int pubCount = Interlocked.Exchange(ref _publishedFramesCounter, 0);
                    int dispCount = Interlocked.Exchange(ref _dispatchedFramesCounter, 0);
                    int dropCount = Interlocked.Exchange(ref _droppedFramesCounter, 0);

                    Metrics.PublishedFramesPerSecond = pubCount / delta;
                    Metrics.QueueLength = _telemetryQueue.Count;
                    Metrics.DroppedFramesCount += dropCount;
                    Metrics.BusUtilization = (double)_telemetryQueue.Count / 10000;

                    if (pubCount > 0)
                    {
                        double totalMs = (double)_totalPublishTimeTicks * 1000 / (Stopwatch.Frequency * pubCount);
                        Metrics.AveragePublishTimeMs = totalMs;
                        _totalPublishTimeTicks = 0;
                    }
                    else
                    {
                        Metrics.AveragePublishTimeMs = 0.0;
                    }

                    if (dispCount > 0)
                    {
                        double totalMs = (double)_totalDispatchTimeTicks * 1000 / (Stopwatch.Frequency * dispCount);
                        Metrics.AverageDispatchTimeMs = totalMs;
                        _totalDispatchTimeTicks = 0;
                    }
                    else
                    {
                        Metrics.AverageDispatchTimeMs = 0.0;
                    }

                    Metrics.MaxPublishTimeMs = _maxPublishTimeMs;
                    _maxPublishTimeMs = 0.0;
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _telemetryQueue.Dispose();
            _diagnosticQueue.Dispose();
            _cts.Dispose();
        }
    }
}
