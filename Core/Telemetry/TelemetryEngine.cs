using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HondaTuner.Core.Telemetry
{
    public class TelemetryEngine : ITelemetryEngine
    {
        public ITelemetryProvider ActiveProvider { get; private set; }
        public ITelemetryBus Bus { get; }
        public IAccessControl Access { get; }
        public TelemetryAnalyzerPipeline Analyzers { get; } = new TelemetryAnalyzerPipeline();

        public event Action OnConfigReloaded;

        private readonly ITimeProvider _timeProvider;
        private readonly ITelemetryProviderFactory _providerFactory;
        private readonly IConfigurationWatcher _configWatcher;

        private Dictionary<string, TelemetryChannel> _channels = new Dictionary<string, TelemetryChannel>();
        private List<string> _profiles = new List<string>();
        private string _activeProfileId = "Street";
        private readonly ConcurrentDictionary<string, ITelemetryFilter> _filters = new ConcurrentDictionary<string, ITelemetryFilter>();
        private readonly List<IComputedChannelPlugin> _computedPlugins = new List<IComputedChannelPlugin>();

        private readonly object _stateLock = new object();
        private readonly string _channelsFilePath;
        private readonly string _profilesFilePath;

        public TelemetryEngine(
            ITelemetryBus bus,
            IAccessControl access,
            ITimeProvider timeProvider,
            ITelemetryProviderFactory providerFactory,
            IConfigurationWatcher configWatcher)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Access = access ?? throw new ArgumentNullException(nameof(access));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _configWatcher = configWatcher ?? throw new ArgumentNullException(nameof(configWatcher));

            _channelsFilePath = GetFilePath("telemetry_channels.json");
            _profilesFilePath = GetFilePath("telemetry_profiles.json");

            LoadConfiguration();

            // Karar izleyicisini dinle
            _configWatcher.OnChannelsReloaded += HandleChannelsReload;
            _configWatcher.OnProfilesReloaded += HandleProfilesReload;
            _configWatcher.StartWatching(_channelsFilePath, _profilesFilePath);

            // Varsayılan sağlayıcıyı seç
            SelectProvider("MockProvider");
        }

        private void LoadConfiguration()
        {
            lock (_stateLock)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    options.Converters.Add(new JsonStringEnumConverter());

                    if (File.Exists(_channelsFilePath))
                    {
                        string content = File.ReadAllText(_channelsFilePath);
                        var channelsList = JsonSerializer.Deserialize<List<TelemetryChannel>>(content, options);
                        if (channelsList != null)
                        {
                            var newChannels = new Dictionary<string, TelemetryChannel>();
                            foreach (var ch in channelsList)
                            {
                                newChannels[ch.ChannelId] = ch;
                            }
                            _channels = newChannels;
                        }
                    }

                    if (File.Exists(_profilesFilePath))
                    {
                        string content = File.ReadAllText(_profilesFilePath);
                        using (var doc = JsonDocument.Parse(content))
                        {
                            var root = doc.RootElement;
                            if (root.TryGetProperty("Profiles", out var profilesElem))
                            {
                                var newProfiles = new List<string>();
                                foreach (var p in profilesElem.EnumerateArray())
                                {
                                    newProfiles.Add(p.GetProperty("ProfileId").GetString());
                                }
                                _profiles = newProfiles;
                            }
                        }
                    }

                    // Filtreleri güncel kanallara göre yenile
                    _filters.Clear();
                    _computedPlugins.Clear();

                    foreach (var kvp in _channels)
                    {
                        var ch = kvp.Value;
                        // Formül tanımlıysa computed eklentisini ekle
                        if (!string.IsNullOrWhiteSpace(ch.Formula))
                        {
                            _computedPlugins.Add(new FormulaComputedPlugin(ch.ChannelId, ch.Formula));
                        }

                        // İlgili filtreleri bağla (Varsayılan olarak hafif Moving Average)
                        if (ch.Group == "Engine" || ch.Group == "Sensors")
                        {
                            _filters[ch.ChannelId] = TelemetryFilterFactory.Create(FilterType.MovingAverage, 3);
                        }
                        else
                        {
                            _filters[ch.ChannelId] = TelemetryFilterFactory.Create(FilterType.NoFilter);
                        }
                    }
                }
                catch
                {
                    // Hatalı yüklemede sessiz kal
                }
            }
        }

        private void HandleChannelsReload()
        {
            LoadConfiguration();
            OnConfigReloaded?.Invoke();
        }

        private void HandleProfilesReload()
        {
            LoadConfiguration();
            OnConfigReloaded?.Invoke();
        }

        public void SelectProvider(string providerName)
        {
            lock (_stateLock)
            {
                bool wasStreaming = false;
                if (ActiveProvider != null)
                {
                    wasStreaming = ActiveProvider.State == ProviderState.Streaming;
                    ActiveProvider.OnFrameReceived -= HandleFrameReceived;
                    ActiveProvider.OnDiagnosticEvent -= HandleDiagnosticEvent;
                    ActiveProvider.Dispose();
                }

                ActiveProvider = _providerFactory.CreateProvider(providerName);
                ActiveProvider.OnFrameReceived += HandleFrameReceived;
                ActiveProvider.OnDiagnosticEvent += HandleDiagnosticEvent;

                if (wasStreaming)
                {
                    ActiveProvider.Connect();
                    StartDatalogging();
                }
            }
        }

        public void SetActiveProfile(string profileId)
        {
            lock (_stateLock)
            {
                if (!_profiles.Contains(profileId))
                    throw new ArgumentException($"Bilinmeyen profil ID'si: {profileId}");

                _activeProfileId = profileId;

                Bus.PublishEvent(new TelemetryEvent
                {
                    EventType = TelemetryEventType.ProfileChanged,
                    Timestamp = DateTime.UtcNow,
                    Source = "TelemetryEngine",
                    Message = $"Aktif profil {profileId} olarak değiştirildi.",
                    Priority = MessagePriority.Normal
                });

                // Eğer aktif canlı veri akışı varsa, sağlayıcıyı yeni kanal kümesiyle güncelle
                if (ActiveProvider != null && ActiveProvider.State == ProviderState.Streaming)
                {
                    StopDatalogging();
                    StartDatalogging();
                }
            }
        }

        public void StartDatalogging(int intervalMs = 10)
        {
            lock (_stateLock)
            {
                if (ActiveProvider == null) return;

                if (ActiveProvider.State == ProviderState.Created || ActiveProvider.State == ProviderState.Faulted)
                {
                    ActiveProvider.Connect();
                }

                var enabledChannels = GetEnabledChannels();
                ActiveProvider.StartStreaming(enabledChannels, intervalMs);

                Bus.Start();
                Bus.PublishEvent(new TelemetryEvent
                {
                    EventType = TelemetryEventType.SessionStarted,
                    Timestamp = DateTime.UtcNow,
                    Source = "TelemetryEngine",
                    Message = "Canlı veri akışı (Datalogging) başlatıldı.",
                    Priority = MessagePriority.Normal
                });
            }
        }

        public void StopDatalogging()
        {
            lock (_stateLock)
            {
                if (ActiveProvider == null) return;
                ActiveProvider.StopStreaming();

                Bus.PublishEvent(new TelemetryEvent
                {
                    EventType = TelemetryEventType.SessionStopped,
                    Timestamp = DateTime.UtcNow,
                    Source = "TelemetryEngine",
                    Message = "Canlı veri akışı durduruldu.",
                    Priority = MessagePriority.Normal
                });
                Bus.Stop();
            }
        }

        public void PauseDatalogging()
        {
            lock (_stateLock)
            {
                ActiveProvider?.PauseStreaming();
            }
        }

        public void ResumeDatalogging()
        {
            lock (_stateLock)
            {
                ActiveProvider?.ResumeStreaming();
            }
        }

        private IEnumerable<string> GetEnabledChannels()
        {
            // Profilden aktif kanalları yükle
            try
            {
                string text = File.ReadAllText(_profilesFilePath);
                using (var doc = JsonDocument.Parse(text))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("Profiles", out var profilesElem))
                    {
                        foreach (var p in profilesElem.EnumerateArray())
                        {
                            if (p.GetProperty("ProfileId").GetString() == _activeProfileId)
                            {
                                var list = new List<string>();
                                foreach (var ch in p.GetProperty("EnabledChannels").EnumerateArray())
                                {
                                    list.Add(ch.GetString());
                                }
                                return list;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }

            // Hata çıkarsa standart kanal listesi döner
            return _channels.Keys.Where(k => _channels[k].Formula == null);
        }

        private void HandleFrameReceived(TelemetryFrame frame)
        {
            // 1. Filtrele
            if (_filters.TryGetValue(frame.ChannelId, out var filter))
            {
                frame.FilteredValue = filter.Filter(frame.Value);
            }
            else
            {
                frame.FilteredValue = frame.Value;
            }

            // 2. Event Bus üzerinde yayınla
            Bus.Publish(frame);

            // 3. Hesaplanan kanalların güncellenmesi
            EvaluateComputedChannels();

            // 4. Analiz hattını tetikle (Her frame geldiğinde snapshot alıp paslarız)
            var snapshot = Bus.GetSnapshot();
            Analyzers.Execute(snapshot);
        }

        private void EvaluateComputedChannels()
        {
            var snapshot = Bus.GetSnapshot();
            foreach (var plugin in _computedPlugins)
            {
                double calculatedVal = plugin.Calculate(snapshot, id =>
                {
                    var f = Bus.GetLatest(id);
                    if (f != null)
                    {
                        double v = f.FilteredValue;
                        TelemetryFramePool.Return(f);
                        return v;
                    }
                    return 0.0;
                });

                var compFrame = TelemetryFramePool.Rent();
                compFrame.ChannelId = plugin.OutputChannelId;
                compFrame.FrameId = frameIdCounter++;
                compFrame.Source = "TelemetryEngine_Computed";
                compFrame.SourceId = "CPU_EVALUATOR";
                compFrame.SessionId = ResolveActiveSessionId();
                compFrame.Transport = "Internal";
                compFrame.Direction = FrameDirection.Rx;
                compFrame.FrameType = "Calculated";
                compFrame.UtcTimestamp = _timeProvider.UtcNow;
                compFrame.MonotonicTimestamp = _timeProvider.MonotonicTicks;
                compFrame.ElapsedTime = snapshot.Sequence * 0.01; // sequence'a göre ms dönüşümü / approx

                compFrame.Value = calculatedVal;
                compFrame.FilteredValue = calculatedVal;
                compFrame.Quality = TelemetryQuality.Good;
                compFrame.Status = ChannelStatus.Calculated;
                compFrame.Priority = MessagePriority.Normal;
                compFrame.CRC = 0;
                compFrame.Checksum = 0;
                compFrame.Validation = ValidationStatus.Valid;
                compFrame.SequenceNumber = (int)compFrame.FrameId;
                compFrame.UpdateRate = 50.0;

                Bus.Publish(compFrame);
            }
        }
        private long frameIdCounter = 1000000;

        // Stable GUID for this engine instance, used when no AutoTune session is active
        private readonly string _engineSessionId = Guid.NewGuid().ToString();

        /// <summary>
        /// Returns the active AutoTune session ID, or falls back to the engine's own GUID.
        /// </summary>
        private string ResolveActiveSessionId()
        {
            try
            {
                var autoTuneEngine = HondaTuner.Core.Container.ServiceContainer
                    .Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();
                if (autoTuneEngine is HondaTuner.Core.AutoTune.AutoTuneEngine ate && ate.ActiveSession != null)
                    return ate.ActiveSession.SessionId;
            }
            catch { /* No session or ServiceContainer not ready */ }
            return _engineSessionId;
        }

        private void HandleDiagnosticEvent(TelemetryEvent diagEvent)
        {
            Bus.PublishEvent(diagEvent);
        }

        public IReadOnlyDictionary<string, TelemetryChannel> GetChannels()
        {
            lock (_stateLock)
            {
                return new Dictionary<string, TelemetryChannel>(_channels);
            }
        }

        public IReadOnlyList<string> GetProfiles()
        {
            lock (_stateLock)
            {
                return new List<string>(_profiles);
            }
        }

        private string GetFilePath(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", fileName);
            if (File.Exists(path)) return path;

            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(path)) return path;

            path = Path.Combine(Directory.GetCurrentDirectory(), "Database", fileName);
            if (File.Exists(path)) return path;

            path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            if (File.Exists(path)) return path;

            return path;
        }

        public void Dispose()
        {
            _configWatcher.OnChannelsReloaded -= HandleChannelsReload;
            _configWatcher.OnProfilesReloaded -= HandleProfilesReload;
            _configWatcher.Dispose();

            if (ActiveProvider != null)
            {
                ActiveProvider.OnFrameReceived -= HandleFrameReceived;
                ActiveProvider.OnDiagnosticEvent -= HandleDiagnosticEvent;
                ActiveProvider.Dispose();
            }
        }
    }
}
