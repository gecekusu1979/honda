using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Sağlayıcı bağlantı durumları yaşam döngüsü.
    /// </summary>
    public enum ProviderState
    {
        Created,
        Initializing,
        Streaming,
        Paused,
        Faulted,
        Disposed
    }

    /// <summary>
    /// Bir sağlayıcının yetenek ve özellik haritası.
    /// </summary>
    public class ProviderCapabilities
    {
        public bool SupportsHighSpeed { get; set; }     // 1000Hz+ Desteği var mı?
        public bool SupportsAutoDetection { get; set; } // Otomatik port bulma var mı?
        public bool IsEmulator { get; set; }            // Donanım simülatörü / emülatörü mü?
        public List<ProtocolType> SupportedProtocols { get; set; } = new List<ProtocolType>();
        public string ConnectionInterface { get; set; } // Serial, USB, Bluetooth, Socket vb.
    }

    /// <summary>
    /// Canlı veri trafiğini yöneten donanım veya yazılım arayüz sağlayıcı kontratıdır.
    /// </summary>
    public interface ITelemetryProvider : IDisposable
    {
        string Name { get; }
        ProviderState State { get; }
        ProviderCapabilities Capabilities { get; }
        IProtocol Protocol { get; }

        event Action<ProviderState> OnStateChanged;
        event Action<TelemetryFrame> OnFrameReceived;
        event Action<TelemetryEvent> OnDiagnosticEvent;

        void Connect();
        void Disconnect();
        void StartStreaming(IEnumerable<string> channelIds, int intervalMs);
        void StopStreaming();
        void PauseStreaming();
        void ResumeStreaming();
    }

    /// <summary>
    /// Temel sağlayıcı işlevlerini uygulayan soyut ana sınıf.
    /// </summary>
    public abstract class BaseTelemetryProvider : ITelemetryProvider
    {
        public abstract string Name { get; }

        private ProviderState _state = ProviderState.Created;
        public ProviderState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged?.Invoke(_state);
                    OnDiagnosticEvent?.Invoke(new TelemetryEvent
                    {
                        EventType = TelemetryEventType.ProviderStateChanged,
                        Timestamp = DateTime.UtcNow,
                        Payload = _state,
                        Source = Name,
                        Message = $"Sağlayıcı durumu {_state} olarak güncellendi.",
                        Priority = MessagePriority.Normal
                    });
                }
            }
        }

        public abstract ProviderCapabilities Capabilities { get; }
        public IProtocol Protocol { get; protected set; }

        public event Action<ProviderState> OnStateChanged;
        public event Action<TelemetryFrame> OnFrameReceived;
        public event Action<TelemetryEvent> OnDiagnosticEvent;

        protected readonly ITimeProvider TimeProvider;
        protected Thread StreamThread;
        protected CancellationTokenSource StreamCts;
        protected readonly object LockObj = new object();

        protected BaseTelemetryProvider(ITimeProvider timeProvider)
        {
            TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public virtual void Connect()
        {
            lock (LockObj)
            {
                if (State != ProviderState.Created && State != ProviderState.Disposed) return;
                State = ProviderState.Initializing;
                try
                {
                    Protocol.InitializeProtocol();
                    State = ProviderState.Paused;
                }
                catch (Exception ex)
                {
                    State = ProviderState.Faulted;
                    RaiseDiagnosticError(ex, "Connect hatası");
                    throw;
                }
            }
        }

        public virtual void Disconnect()
        {
            lock (LockObj)
            {
                StopStreaming();
                try
                {
                    Protocol.Transport.Close();
                }
                catch
                {
                    // Ignore close exceptions
                }
                State = ProviderState.Created;
            }
        }

        public virtual void StartStreaming(IEnumerable<string> channelIds, int intervalMs)
        {
            lock (LockObj)
            {
                if (State != ProviderState.Paused) return;

                StreamCts = new CancellationTokenSource();
                StreamThread = new Thread(() => StreamLoop(channelIds, intervalMs, StreamCts.Token))
                {
                    IsBackground = true,
                    Name = $"TelemetryStream_{Name}"
                };

                State = ProviderState.Streaming;
                StreamThread.Start();
            }
        }

        public virtual void StopStreaming()
        {
            lock (LockObj)
            {
                if (State != ProviderState.Streaming && State != ProviderState.Paused) return;

                if (StreamCts != null)
                {
                    StreamCts.Cancel();
                    StreamThread?.Join(1000);
                    StreamCts.Dispose();
                    StreamCts = null;
                }

                State = ProviderState.Paused;
            }
        }

        public virtual void PauseStreaming()
        {
            lock (LockObj)
            {
                if (State == ProviderState.Streaming)
                {
                    State = ProviderState.Paused;
                }
            }
        }

        public virtual void ResumeStreaming()
        {
            lock (LockObj)
            {
                if (State == ProviderState.Paused)
                {
                    State = ProviderState.Streaming;
                }
            }
        }

        protected abstract void StreamLoop(IEnumerable<string> channelIds, int intervalMs, CancellationToken token);

        protected void RaiseFrame(TelemetryFrame frame)
        {
            OnFrameReceived?.Invoke(frame);
        }

        protected void RaiseDiagnosticError(Exception ex, string msg)
        {
            OnDiagnosticEvent?.Invoke(new TelemetryEvent
            {
                EventType = TelemetryEventType.ErrorOccurred,
                Timestamp = DateTime.UtcNow,
                Payload = ex,
                Source = Name,
                Message = $"{msg}: {ex.Message}",
                Priority = MessagePriority.High
            });
        }

        public virtual void Dispose()
        {
            Disconnect();
            Protocol?.Dispose();
            State = ProviderState.Disposed;
        }
    }

    /// <summary>
    /// Gerçekçi motor simülasyonu üreten varsayılan telemetri sağlayıcısı.
    /// </summary>
    public class MockProvider : BaseTelemetryProvider
    {
        public override string Name => "MockProvider";

        private readonly ProviderCapabilities _capabilities = new ProviderCapabilities
        {
            SupportsHighSpeed = true,
            SupportsAutoDetection = true,
            IsEmulator = true,
            SupportedProtocols = new List<ProtocolType> { ProtocolType.Mock },
            ConnectionInterface = "Mock"
        };
        public override ProviderCapabilities Capabilities => _capabilities;

        private long _frameCount = 0;
        private readonly long _sessionStartTicks;

        public MockProvider(ITimeProvider timeProvider) : base(timeProvider)
        {
            _sessionStartTicks = TimeProvider.MonotonicTicks;
            Protocol = new MockProtocol(new MockTransport());
        }

        protected override void StreamLoop(IEnumerable<string> channelIds, int intervalMs, CancellationToken token)
        {
            try
            {
                var rand = new Random();
                while (!token.IsCancellationRequested)
                {
                    if (State == ProviderState.Streaming)
                    {
                        long loopStartTick = Stopwatch.GetTimestamp();

                        foreach (var chanId in channelIds)
                        {
                            if (token.IsCancellationRequested) break;

                            if (Protocol.TryReadPayload(chanId, out var payload, out var crc, out var cs))
                            {
                                var frame = TelemetryFramePool.Rent();
                                frame.ChannelId = chanId;
                                frame.FrameId = Interlocked.Increment(ref _frameCount);
                                frame.Source = Name;
                                frame.SourceId = "MOCK-5500-D16";
                                frame.SessionId = "SESSION-MOCK-99";
                                frame.Transport = "MemoryStream";
                                frame.Direction = FrameDirection.Rx;
                                frame.FrameType = "Datalog";

                                // Zaman damgalarını güncelle
                                frame.UtcTimestamp = TimeProvider.UtcNow;
                                frame.MonotonicTimestamp = TimeProvider.MonotonicTicks;
                                frame.ElapsedTime = TimeProvider.GetElapsedTime(_sessionStartTicks);

                                // Ham değer çözümleme
                                double val = 0.0;
                                if (chanId == "RPM")
                                {
                                    val = (payload[0] << 8) | payload[1];
                                }
                                else if (chanId == "AFR")
                                {
                                    val = payload[0] * 0.0549 + 8.0; // 8.0 - 22.0 AFR
                                }
                                else
                                {
                                    val = payload[0];
                                }

                                frame.RawValue = payload;
                                frame.Value = val;
                                frame.FilteredValue = val; // Hamm değer, filtreleme engine tarafından uygulanacak
                                frame.Quality = TelemetryQuality.Good;
                                frame.Status = ChannelStatus.Valid;
                                frame.Priority = chanId == "KnockCount" ? MessagePriority.Critical : MessagePriority.Normal;

                                frame.CRC = crc;
                                frame.Checksum = cs;
                                frame.Validation = ValidationStatus.Valid;
                                frame.SequenceNumber = (int)frame.FrameId;
                                frame.UpdateRate = 1000.0 / Math.Max(1, intervalMs);

                                RaiseFrame(frame);
                            }
                        }

                        // Döngü gecikmesi ayarlama
                        long loopElapsed = Stopwatch.GetTimestamp() - loopStartTick;
                        int elapsedMs = (int)(loopElapsed * 1000 / Stopwatch.Frequency);
                        int sleepTime = Math.Max(1, intervalMs - elapsedMs);
                        Thread.Sleep(sleepTime);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                State = ProviderState.Faulted;
                RaiseDiagnosticError(ex, "StreamLoop hatası");
            }
        }
    }

    /// <summary>
    /// OBD-II Telemetri Sağlayıcı şablonu.
    /// </summary>
    public class Obd2Provider : BaseTelemetryProvider
    {
        public override string Name => "Obd2Provider";
        public override ProviderCapabilities Capabilities => new ProviderCapabilities
        {
            SupportsHighSpeed = false,
            SupportsAutoDetection = true,
            IsEmulator = false,
            SupportedProtocols = new List<ProtocolType> { ProtocolType.OBD2 },
            ConnectionInterface = "SerialPort"
        };

        public Obd2Provider(ITimeProvider timeProvider) : base(timeProvider)
        {
            // TODO: Gerçek OBD2 desteği için MockProtocol yerine SerialPortTransport(portName, baudRate)
            // kullanarak Protocol'ü başlatın. Connect() sırasında gerçek port açılacak.
            Protocol = new MockProtocol(new MockTransport());
        }

        protected override void StreamLoop(IEnumerable<string> channelIds, int intervalMs, CancellationToken token)
        {
            throw new NotImplementedException("Real OBD2 streaming loop is not implemented in this version.");
        }
    }

    /// <summary>
    /// Real-Time Emulator Sağlayıcı şablonu.
    /// </summary>
    public class RtpEmulatorProvider : BaseTelemetryProvider
    {
        public override string Name => "RtpEmulatorProvider";
        public override ProviderCapabilities Capabilities => new ProviderCapabilities
        {
            SupportsHighSpeed = true,
            SupportsAutoDetection = false,
            IsEmulator = true,
            SupportedProtocols = new List<ProtocolType> { ProtocolType.HondaOBD1 },
            ConnectionInterface = "USB_FTDI"
        };

        public RtpEmulatorProvider(ITimeProvider timeProvider) : base(timeProvider)
        {
            // TODO: Gerçek RTP emülatör desteği için MockProtocol yerine ftdi/USB tabanlı
            // bir transport (OstrichUSBTransport vb.) ile Protocol'ü başlatın.
            Protocol = new MockProtocol(new MockTransport());
        }

        protected override void StreamLoop(IEnumerable<string> channelIds, int intervalMs, CancellationToken token)
        {
            throw new NotImplementedException("Real-Time Emulator telemetry streaming loop is not implemented in this version.");
        }
    }

    // Ek donanım hazırlık sınıfları (CAN, K-Line, J2534, KLine)
    public class CanProvider : BaseTelemetryProvider
    {
        public override string Name => "CanProvider";
        public override ProviderCapabilities Capabilities => new ProviderCapabilities();
        // TODO: CanProvider — Gerçek CAN bus desteği için MockProtocol yerine SocketCanTransport veya PCAN/VECTOR transport bağlayın.
        public CanProvider(ITimeProvider timeProvider) : base(timeProvider) { Protocol = new MockProtocol(new MockTransport()); }
        protected override void StreamLoop(IEnumerable<string> channelIds, int intervalMs, CancellationToken token)
        {
            throw new NotImplementedException("CAN telemetry streaming loop is not implemented in this version.");
        }
    }

    public class KLineProvider : BaseTelemetryProvider
    {
        public override string Name => "KLineProvider";
        public override ProviderCapabilities Capabilities => new ProviderCapabilities();
        // TODO: KLineProvider — Gerçek K-Line desteği için MockProtocol yerine SerialPort K-Line transport bağlayın.
        public KLineProvider(ITimeProvider timeProvider) : base(timeProvider) { Protocol = new MockProtocol(new MockTransport()); }
        protected override void StreamLoop(IEnumerable<string> channelIds, int intervalMs, CancellationToken token)
        {
            throw new NotImplementedException("K-Line telemetry streaming loop is not implemented in this version.");
        }
    }

    public class J2534Provider : BaseTelemetryProvider
    {
        public override string Name => "J2534Provider";
        public override ProviderCapabilities Capabilities => new ProviderCapabilities();
        // TODO: J2534Provider — Gerçek J2534 desteği için MockProtocol yerine PassThru DLL transport bağlayın.
        public J2534Provider(ITimeProvider timeProvider) : base(timeProvider) { Protocol = new MockProtocol(new MockTransport()); }
        protected override void StreamLoop(IEnumerable<string> channelIds, int intervalMs, CancellationToken token)
        {
            throw new NotImplementedException("J2534 telemetry streaming loop is not implemented in this version.");
        }
    }
}
