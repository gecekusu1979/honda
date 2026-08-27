using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HondaTuner.Core.Container;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;
using HondaTuner.Hardware.Emulator;

namespace HondaTuner.Core.Rtp
{
    public class RtpCalibrationEngine : IRtpCalibrationEngine, IDisposable
    {
        private readonly ICalibrationService _calibrationService;
        private readonly IRomService _romService;
        private readonly IEmulator _emulator;

        private readonly RtpConfig _config;
        private readonly string _queueFilePath;

        private readonly Queue<CalibrationChange> _pendingQueue = new Queue<CalibrationChange>();
        private readonly Dictionary<int, byte> _lastWrittenBytes = new Dictionary<int, byte>();
        private readonly object _lockObj = new object();

        private RtpConnectionState _connectionState = RtpConnectionState.Disconnected;
        private bool _isSyncActive = false;
        private long _droppedWritesCount = 0;
        private double _avgSyncLatencyMs = 0;
        private long _retryCount = 0;
        private long _failureCount = 0;

        private Task _workerTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly AutoResetEvent _queueEvent = new AutoResetEvent(false);

        public RtpConnectionState ConnectionState
        {
            get { lock (_lockObj) return _connectionState; }
            private set
            {
                lock (_lockObj)
                {
                    if (_connectionState != value)
                    {
                        var oldState = _connectionState;
                        _connectionState = value;
                        OnRtpDomainEvent?.Invoke(new RtpDomainEventWrapper("StateChanged", $"RTP connection state changed from {oldState} to {_connectionState}"));
                    }
                }
            }
        }

        public bool IsSyncActive
        {
            get { lock (_lockObj) return _isSyncActive; }
        }

        public int QueueDepth
        {
            get { lock (_lockObj) return _pendingQueue.Count; }
        }

        public long DroppedWritesCount => Interlocked.Read(ref _droppedWritesCount);
        public double AvgSyncLatencyMs { get { lock (_lockObj) return _avgSyncLatencyMs; } }
        public long RetryCount => Interlocked.Read(ref _retryCount);
        public long FailureCount => Interlocked.Read(ref _failureCount);
        public RtpConfig Configuration => _config;

        public event Action<IRtpDomainEvent> OnRtpDomainEvent;

        public RtpCalibrationEngine()
            : this(
                ServiceContainer.Resolve<ICalibrationService>(),
                ServiceContainer.Resolve<IRomService>(),
                ServiceContainer.Resolve<IEmulator>()
            )
        {
        }

        public RtpCalibrationEngine(ICalibrationService calibrationService, IRomService romService, IEmulator emulator)
        {
            _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
            _romService = romService ?? throw new ArgumentNullException(nameof(romService));
            _emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));

            // Setup paths
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dbDir = Path.Combine(baseDir, "Database");
            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

            string configPath = Path.Combine(dbDir, "rtp_config.json");
            _queueFilePath = Path.Combine(dbDir, "pending_rtp_queue.json");

            // Load and validate config
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    _config = JsonSerializer.Deserialize<RtpConfig>(json);
                    RtpConfigValidator.Validate(_config);
                }
                catch (Exception ex)
                {
                    ApplicationLogger.Error("RtpCalibrationEngine", $"RTP Configuration validation failed: {ex.Message}");
                    throw;
                }
            }
            else
            {
                // Fallback / generate default
                _config = new RtpConfig
                {
                    RetryCount = 3,
                    WriteTimeoutMs = 250,
                    PacketSize = 64,
                    SyncIntervalMs = 50,
                    BatchingPolicy = "CoalesceConsecutive",
                    QueueLimit = 1000,
                    BackpressurePolicy = "DropOldest"
                };
                try
                {
                    string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(configPath, json);
                }
                catch (Exception ex)
                {
                    ApplicationLogger.Warn("RtpCalibrationEngine", $"Failed to save default RtpConfig: {ex.Message}");
                }
            }

            // Restore persistent queue if exists
            LoadQueueFromDisk();

            // Subscribe to calibration changes
            _calibrationService.OnCalibrationChanged += HandleCalibrationChanged;

            // Start background sync loop
            _workerTask = Task.Run(SyncLoop, _cts.Token);
        }

        public void ConnectEmulator()
        {
            lock (_lockObj)
            {
                if (ConnectionState != RtpConnectionState.Disconnected && ConnectionState != RtpConnectionState.Faulted)
                {
                    return;
                }

                ConnectionState = RtpConnectionState.Connecting;
                ApplicationLogger.Info("RtpCalibrationEngine", "Connecting to RTP emulator...");

                try
                {
                    _emulator.Connect();
                    if (_emulator.State != HondaTuner.Core.Interfaces.ConnectionState.Connected)
                    {
                        throw new IOException("Emulator is disconnected after Connect call.");
                    }

                    ConnectionState = RtpConnectionState.Connected;
                    OnRtpDomainEvent?.Invoke(new ConnectionEstablishedEvent());
                    ApplicationLogger.Info("RtpCalibrationEngine", "RTP emulator connected successfully.");
                }
                catch (Exception ex)
                {
                    ConnectionState = RtpConnectionState.Faulted;
                    Interlocked.Increment(ref _failureCount);
                    OnRtpDomainEvent?.Invoke(new ConnectionLostEvent($"Connection initiation failed: {ex.Message}"));
                    ApplicationLogger.Error("RtpCalibrationEngine", $"Failed to connect to emulator: {ex.Message}");
                    throw;
                }
            }
        }

        public void DisconnectEmulator()
        {
            lock (_lockObj)
            {
                if (ConnectionState == RtpConnectionState.Disconnected) return;

                DisableSync();

                try
                {
                    _emulator.Disconnect();
                }
                catch (Exception ex)
                {
                    ApplicationLogger.Warn("RtpCalibrationEngine", $"Exception during emulator disconnect: {ex.Message}");
                }

                ConnectionState = RtpConnectionState.Disconnected;
                OnRtpDomainEvent?.Invoke(new ConnectionLostEvent("Explicit user disconnect"));
                ApplicationLogger.Info("RtpCalibrationEngine", "RTP Emulator disconnected.");
            }
        }

        public void EnableSync()
        {
            lock (_lockObj)
            {
                if (ConnectionState != RtpConnectionState.Connected && ConnectionState != RtpConnectionState.Paused)
                {
                    throw new InvalidOperationException("Cannot activate sync. Emulator must be connected.");
                }

                _isSyncActive = true;
                ConnectionState = RtpConnectionState.Synchronizing;
                OnRtpDomainEvent?.Invoke(new SyncStartedEvent());
                ApplicationLogger.Info("RtpCalibrationEngine", "Real-time synchronization active.");
                _queueEvent.Set(); // Trigger immediate evaluation
            }
        }

        public void DisableSync()
        {
            lock (_lockObj)
            {
                if (!_isSyncActive) return;

                _isSyncActive = false;
                if (ConnectionState == RtpConnectionState.Synchronizing)
                {
                    ConnectionState = RtpConnectionState.Paused;
                }
                ApplicationLogger.Info("RtpCalibrationEngine", "Real-time synchronization paused.");
            }
        }

        public void SyncFullCalibration()
        {
            lock (_lockObj)
            {
                if (ConnectionState != RtpConnectionState.Connected && ConnectionState != RtpConnectionState.Synchronizing)
                {
                    throw new InvalidOperationException("Emulator must be connected to sync full calibration.");
                }

                if (!_romService.IsLoaded)
                {
                    throw new InvalidOperationException("No ROM loaded to synchronize.");
                }

                ApplicationLogger.Info("RtpCalibrationEngine", "Performing full ROM upload to emulator...");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                var stateBefore = ConnectionState;
                ConnectionState = RtpConnectionState.Synchronizing;

                byte[] romBuffer = _romService.GetBuffer();
                int retries = _config.RetryCount;

                while (true)
                {
                    try
                    {
                        // Safeblock write check
                        _emulator.WriteBlock(0, romBuffer);

                        // Verification read-back
                        byte[] readBack = _emulator.ReadBlock(0, romBuffer.Length);
                        if (readBack == null || readBack.Length != romBuffer.Length)
                        {
                            throw new IOException("Read-back verification length mismatch.");
                        }

                        for (int i = 0; i < romBuffer.Length; i++)
                        {
                            if (romBuffer[i] != readBack[i])
                            {
                                throw new IOException($"Full sync verification mismatch at index {i}. Expected: {romBuffer[i]}, Actual: {readBack[i]}");
                            }
                        }

                        // Sync verified successful!
                        OnRtpDomainEvent?.Invoke(new SyncCompletedEvent());
                        ApplicationLogger.Info("RtpCalibrationEngine", $"Full ROM sync completed and verified in {watch.ElapsedMilliseconds} ms.");

                        // Pop entries from pending since the entire ROM state is now pushed
                        lock (_pendingQueue)
                        {
                            _pendingQueue.Clear();
                            SaveQueueToDisk();
                        }

                        // Seed idempotence
                        _lastWrittenBytes.Clear();
                        for (int i = 0; i < romBuffer.Length; i++)
                        {
                            _lastWrittenBytes[i] = romBuffer[i];
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        if (retries > 0)
                        {
                            retries--;
                            Interlocked.Increment(ref _retryCount);
                            ApplicationLogger.Warn("RtpCalibrationEngine", $"Full ROM synchronization failed. Retrying... ({retries} left). Error: {ex.Message}");
                            Thread.Sleep(50);
                        }
                        else
                        {
                            ConnectionState = RtpConnectionState.Faulted;
                            Interlocked.Increment(ref _failureCount);
                            _isSyncActive = false;
                            OnRtpDomainEvent?.Invoke(new SyncFailedEvent($"Full ROM Sync failed: {ex.Message}"));
                            throw;
                        }
                    }
                }

                if (_isSyncActive) ConnectionState = RtpConnectionState.Synchronizing;
                else ConnectionState = stateBefore;
            }
        }

        private void HandleCalibrationChanged(CalibrationChange change)
        {
            if (change == null) return;

            lock (_lockObj)
            {
                // Idempotence Suppression Filter
                if (byte.TryParse(change.NewValue, out byte val))
                {
                    if (_lastWrittenBytes.TryGetValue(change.Offset, out byte lastVal) && lastVal == val)
                    {
                        // Duplicate; skip enqueuing to prevent redundant bus traffic
                        return;
                    }
                }

                lock (_pendingQueue)
                {
                    if (_pendingQueue.Count >= _config.QueueLimit)
                    {
                        ApplyBackpressurePolicy(change);
                    }
                    else
                    {
                        _pendingQueue.Enqueue(change);
                    }
                    SaveQueueToDisk();
                }
                _queueEvent.Set();
            }
        }

        private void ApplyBackpressurePolicy(CalibrationChange change)
        {
            switch (_config.BackpressurePolicy)
            {
                case "DropOldest":
                    if (_pendingQueue.Count > 0)
                    {
                        _pendingQueue.Dequeue();
                        Interlocked.Increment(ref _droppedWritesCount);
                    }
                    _pendingQueue.Enqueue(change);
                    break;

                case "RejectNewest":
                    Interlocked.Increment(ref _droppedWritesCount);
                    break;

                case "BlockProducer":
                    // Spins or waits briefly under safety lock (max 100ms)
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    while (_pendingQueue.Count >= _config.QueueLimit && watch.ElapsedMilliseconds < 100)
                    {
                        Monitor.Exit(_pendingQueue);
                        Thread.Sleep(5);
                        Monitor.Enter(_pendingQueue);
                    }
                    if (_pendingQueue.Count < _config.QueueLimit)
                    {
                        _pendingQueue.Enqueue(change);
                    }
                    else
                    {
                        Interlocked.Increment(ref _droppedWritesCount); // Fallback
                    }
                    break;

                default:
                    // DropOldest fallback
                    if (_pendingQueue.Count > 0) _pendingQueue.Dequeue();
                    _pendingQueue.Enqueue(change);
                    break;
            }
        }

        private void SyncLoop()
        {
            var waitHandles = new WaitHandle[] { _queueEvent, _cts.Token.WaitHandle };

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    WaitHandle.WaitAny(waitHandles, _config.SyncIntervalMs);

                    if (_cts.Token.IsCancellationRequested) break;

                    bool runSync = false;
                    lock (_lockObj)
                    {
                        runSync = _isSyncActive && _emulator.State == HondaTuner.Core.Interfaces.ConnectionState.Connected &&
                                  (ConnectionState == RtpConnectionState.Synchronizing || ConnectionState == RtpConnectionState.Connected);
                    }

                    if (!runSync) continue;

                    List<CalibrationChange> batch = null;
                    lock (_pendingQueue)
                    {
                        if (_pendingQueue.Count > 0)
                        {
                            batch = _pendingQueue.ToList();
                        }
                    }

                    if (batch == null || batch.Count == 0) continue;

                    ProcessBatchWork(batch);
                }
                catch (Exception ex)
                {
                    ApplicationLogger.Error("RtpCalibrationEngine", $"RtpSyncLoop background thread error: {ex.Message}");
                }
            }
        }

        private void ProcessBatchWork(List<CalibrationChange> batch)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (_config.BatchingPolicy == "CoalesceConsecutive")
                {
                    var coalescedGroups = CoalesceChanges(batch);
                    foreach (var group in coalescedGroups)
                    {
                        PerformBlockWriteVerified(group.Offset, group.Data);
                    }
                }
                else
                {
                    foreach (var change in batch)
                    {
                        PerformSingleWriteVerified(change);
                    }
                }

                // Successful sync step: clear these changes from queue
                lock (_pendingQueue)
                {
                    for (int i = 0; i < batch.Count; i++)
                    {
                        if (_pendingQueue.Count > 0) _pendingQueue.Dequeue();
                    }
                    SaveQueueToDisk();
                }

                // Record stats
                watch.Stop();
                lock (_lockObj)
                {
                    double currentMs = watch.Elapsed.TotalMilliseconds;
                    _avgSyncLatencyMs = _avgSyncLatencyMs == 0 ? currentMs : (_avgSyncLatencyMs * 0.9) + (currentMs * 0.1);
                }
            }
            catch (Exception ex)
            {
                // Disconnect or trigger retry routines
                Interlocked.Increment(ref _failureCount);
                ApplicationLogger.Error("RtpCalibrationEngine", $"Batch write execution failed: {ex.Message}");

                // Auto stop on repeated failure
                lock (_lockObj)
                {
                    _isSyncActive = false;
                    ConnectionState = RtpConnectionState.Faulted;
                    OnRtpDomainEvent?.Invoke(new SyncFailedEvent(ex.Message));
                    OnRtpDomainEvent?.Invoke(new ConnectionLostEvent("RTP processing error"));
                }
            }
        }

        private List<CoalescedWriteBlock> CoalesceChanges(List<CalibrationChange> batch)
        {
            var result = new List<CoalescedWriteBlock>();
            if (batch == null || batch.Count == 0) return result;

            byte[] romBuffer = _romService.GetBuffer();

            // Sort changes by offset
            var sortedChanges = batch.OrderBy(c => c.Offset).ToList();

            int startOffset = sortedChanges[0].Offset;
            int currentEndOffset = startOffset; // Inclusive segment end

            for (int i = 1; i < sortedChanges.Count; i++)
            {
                int nextOffset = sortedChanges[i].Offset;
                // Coalesce limit: if gap is within O(PacketSize) or smaller gap
                if (nextOffset - currentEndOffset <= 4)
                {
                    currentEndOffset = nextOffset;
                }
                else
                {
                    // Push completed block
                    int length = currentEndOffset - startOffset + 1;
                    byte[] data = new byte[length];
                    Array.Copy(romBuffer, startOffset, data, 0, length);

                    result.Add(new CoalescedWriteBlock(startOffset, data));

                    // Start new segment
                    startOffset = nextOffset;
                    currentEndOffset = startOffset;
                }
            }

            // Push final group
            int finalLength = currentEndOffset - startOffset + 1;
            byte[] finalData = new byte[finalLength];
            Array.Copy(romBuffer, startOffset, finalData, 0, finalLength);
            result.Add(new CoalescedWriteBlock(startOffset, finalData));

            return result;
        }

        private void PerformSingleWriteVerified(CalibrationChange change)
        {
            byte[] romBuffer = _romService.GetBuffer();
            byte val = romBuffer[change.Offset];

            int retries = _config.RetryCount;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            while (true)
            {
                try
                {
                    _emulator.WriteByte(change.Offset, val);

                    // Verification
                    byte ackVal = _emulator.ReadByte(change.Offset);
                    if (ackVal != val)
                    {
                        throw new IOException($"Verify read-back failed at offset 0x{change.Offset:X4}. Write: {val}, Ack: {ackVal}");
                    }

                    // Store confirmed value in register
                    lock (_lockObj)
                    {
                        _lastWrittenBytes[change.Offset] = val;
                    }

                    watch.Stop();
                    OnRtpDomainEvent?.Invoke(new CalibrationSentEvent(change, watch.Elapsed.TotalMilliseconds));
                    break;
                }
                catch (Exception ex)
                {
                    if (retries > 0)
                    {
                        retries--;
                        Interlocked.Increment(ref _retryCount);
                        Thread.Sleep(20);
                    }
                    else
                    {
                        throw new IOException($"Verification write failed after max retries: {ex.Message}", ex);
                    }
                }
            }
        }

        private void PerformBlockWriteVerified(int offset, byte[] data)
        {
            int retries = _config.RetryCount;
            while (true)
            {
                try
                {
                    _emulator.WriteBlock(offset, data);

                    // Verification Read-back
                    byte[] readBack = _emulator.ReadBlock(offset, data.Length);
                    if (readBack == null || readBack.Length != data.Length)
                    {
                        throw new IOException("Batch block read verification length mismatch.");
                    }

                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != readBack[i])
                        {
                            throw new IOException($"Batch block verify mismatch at relative index {i}. Expected: {data[i]}, Actual: {readBack[i]}");
                        }
                    }

                    // Register
                    lock (_lockObj)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            _lastWrittenBytes[offset + i] = data[i];
                        }
                    }

                    break;
                }
                catch (Exception ex)
                {
                    if (retries > 0)
                    {
                        retries--;
                        Interlocked.Increment(ref _retryCount);
                        Thread.Sleep(20);
                    }
                    else
                    {
                        throw new IOException($"Batch block verification write failed: {ex.Message}", ex);
                    }
                }
            }
        }

        private void SaveQueueToDisk()
        {
            try
            {
                lock (_pendingQueue)
                {
                    var list = _pendingQueue.ToList();
                    string json = JsonSerializer.Serialize(list);
                    File.WriteAllText(_queueFilePath, json);
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Warn("RtpCalibrationEngine", $"Could not save persistent sync queue: {ex.Message}");
            }
        }

        private void LoadQueueFromDisk()
        {
            try
            {
                if (File.Exists(_queueFilePath))
                {
                    string json = File.ReadAllText(_queueFilePath);
                    var list = JsonSerializer.Deserialize<List<CalibrationChange>>(json);
                    if (list != null && list.Count > 0)
                    {
                        lock (_pendingQueue)
                        {
                            _pendingQueue.Clear();
                            foreach (var change in list)
                            {
                                _pendingQueue.Enqueue(change);
                            }
                        }
                        ApplicationLogger.Info("RtpCalibrationEngine", $"Recovered {list.Count} pending changes from persistent queue on disk.");
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Warn("RtpCalibrationEngine", $"Failed to restore persistent sync queue: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _queueEvent.Set();

            try
            {
                _workerTask?.Wait(250);
            }
            catch { }

            _calibrationService.OnCalibrationChanged -= HandleCalibrationChanged;
            _queueEvent.Dispose();
            _cts.Dispose();
        }

        private class CoalescedWriteBlock
        {
            public int Offset { get; }
            public byte[] Data { get; }

            public CoalescedWriteBlock(int offset, byte[] data)
            {
                Offset = offset;
                Data = data;
            }
        }

        private class RtpDomainEventWrapper : IRtpDomainEvent
        {
            public string EventName { get; }
            public DateTime Timestamp { get; } = DateTime.Now;
            public string Message { get; }

            public RtpDomainEventWrapper(string name, string message)
            {
                EventName = name;
                Message = message;
            }
        }
    }
}
