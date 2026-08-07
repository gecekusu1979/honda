using System;
using System.IO;
using System.Text.Json;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri kanal ve profil yapılandırma dosyalarının çalışma zamanında dinamik olarak izlenmesini
    /// ve deşiklik durumunda otomatik doğrulanarak reload edilmesini sağlayan arayüzdür.
    /// </summary>
    public interface IConfigurationWatcher : IDisposable
    {
        event Action OnChannelsReloaded;
        event Action OnProfilesReloaded;

        void StartWatching(string channelsPath, string profilesPath);
        void StopWatching();
    }

    public class ConfigurationWatcher : IConfigurationWatcher
    {
        public event Action OnChannelsReloaded;
        public event Action OnProfilesReloaded;

        private FileSystemWatcher _channelsWatcher;
        private FileSystemWatcher _profilesWatcher;

        private string _channelsPath;
        private string _profilesPath;
        private readonly object _lock = new object();

        private DateTime _lastChannelsWrite = DateTime.MinValue;
        private DateTime _lastProfilesWrite = DateTime.MinValue;

        public void StartWatching(string channelsPath, string profilesPath)
        {
            lock (_lock)
            {
                StopWatching();

                _channelsPath = channelsPath;
                _profilesPath = profilesPath;

                if (File.Exists(_channelsPath))
                {
                    string dir = Path.GetDirectoryName(Path.GetFullPath(_channelsPath));
                    string filename = Path.GetFileName(_channelsPath);

                    _channelsWatcher = new FileSystemWatcher(dir, filename)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                    };
                    _channelsWatcher.Changed += OnChannelsFileChanged;
                    _channelsWatcher.EnableRaisingEvents = true;
                }

                if (File.Exists(_profilesPath))
                {
                    string dir = Path.GetDirectoryName(Path.GetFullPath(_profilesPath));
                    string filename = Path.GetFileName(_profilesPath);

                    _profilesWatcher = new FileSystemWatcher(dir, filename)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                    };
                    _profilesWatcher.Changed += OnProfilesFileChanged;
                    _profilesWatcher.EnableRaisingEvents = true;
                }
            }
        }

        public void StopWatching()
        {
            lock (_lock)
            {
                if (_channelsWatcher != null)
                {
                    _channelsWatcher.EnableRaisingEvents = false;
                    _channelsWatcher.Changed -= OnChannelsFileChanged;
                    _channelsWatcher.Dispose();
                    _channelsWatcher = null;
                }

                if (_profilesWatcher != null)
                {
                    _profilesWatcher.EnableRaisingEvents = false;
                    _profilesWatcher.Changed -= OnProfilesFileChanged;
                    _profilesWatcher.Dispose();
                    _profilesWatcher = null;
                }
            }
        }

        private void OnChannelsFileChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce double fired events
            lock (_lock)
            {
                var lastWrite = File.GetLastWriteTime(e.FullPath);
                if ((lastWrite - _lastChannelsWrite).TotalMilliseconds < 250) return;
                _lastChannelsWrite = lastWrite;
            }

            try
            {
                // Bir süre dosyanın bırakılmasını bekle
                System.Threading.Thread.Sleep(50);
                string content = File.ReadAllText(e.FullPath);

                // JSON'u doğrula
                using (var doc = JsonDocument.Parse(content))
                {
                    // Geçerli JSON, tetikle
                    OnChannelsReloaded?.Invoke();
                }
            }
            catch
            {
                // Geçersiz kanal yapılandırmasını yut ve eski yapılandırmayı koru
            }
        }

        private void OnProfilesFileChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                var lastWrite = File.GetLastWriteTime(e.FullPath);
                if ((lastWrite - _lastProfilesWrite).TotalMilliseconds < 250) return;
                _lastProfilesWrite = lastWrite;
            }

            try
            {
                System.Threading.Thread.Sleep(50);
                string content = File.ReadAllText(e.FullPath);

                // JSON'u doğrula
                using (var doc = JsonDocument.Parse(content))
                {
                    // Geçerli JSON, tetikle
                    OnProfilesReloaded?.Invoke();
                }
            }
            catch
            {
                // Geçersiz profil yapılandırmasını yut
            }
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
