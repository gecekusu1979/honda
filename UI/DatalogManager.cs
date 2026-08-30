using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using HondaTuner.Core;

namespace HondaTuner.UI
{
    /// <summary>
    /// Datalog yöneticisi: gerçek SerialPort, simülasyon ve CSV Playback modu.
    /// Olay: DataReceived — telemetri verisi hazır.
    /// </summary>
    public class DatalogManager : IDisposable
    {
        public event Action<TelemetryFrame> DataReceived;

        private SerialPort _port;
        private readonly HondaTuner.Core.Protocol.Obd1FrameParser _obd1Parser;

        public DatalogManager()
        {
            _obd1Parser = new HondaTuner.Core.Protocol.Obd1FrameParser();
            _obd1Parser.OnFrameParsed += HandleObd1Frame;
        }

        private void HandleObd1Frame(byte[] buf)
        {
            var frame = new TelemetryFrame
            {
                Rpm = (buf[2] << 8 | buf[3]) * 0.25,
                Map = buf[4] * 0.78,   // 200 kPa max
                Speed = buf[5],
                Afr = 9.0 + buf[6] * 0.043,  // 9..20 aralığı
                Ect = buf[7] - 40,
                Iat = buf[8] - 40,
                Tps = buf[9] * 0.392,        // 255 -> 100%
                BatteryVolts = buf[10] * 0.07, // 255 -> 17.85V
                InjDuty = buf[11] * 0.392,     // 255 -> 100%
                IgnAdvance = buf[12] * 0.25,  // 255 -> 63.75 deg
                VtecStatus = ((buf[2] << 8 | buf[3]) * 0.25) >= 4800
            };

            if (IsRecording && _csvWriter != null)
            {
                _csvWriter.WriteLine($"{frame.Rpm:0},{frame.Map:0.0},{frame.Speed:0},{frame.Afr:0.00},{frame.Ect:0},{frame.Iat:0},{frame.Tps:0},{frame.BatteryVolts:0.00},{frame.InjDuty:0.0},{frame.IgnAdvance:0.0},{(frame.VtecStatus ? 1 : 0)}");
            }

            DataReceived?.Invoke(frame);
        }
        private CancellationTokenSource _simCts;
        private readonly Random _rng = new Random();

        // Simülatör durum değişkenleri (gerçekçi değişimler)
        private double _simRpm = 800;
        private double _simLoad = 30;
        private double _simSpeed = 0;
        private double _simAfr = 14.7;
        private double _simEct = 35;
        private double _simIat = 22;
        private double _simRpmTarget = 800;
        private int _simPhase = 0;   // 0=idle,1=accel,2=cruise,3=decel

        public bool IsRunning { get; private set; }
        public bool IsSimulation { get; private set; }

        // ── Gerçek Bağlantı ──────────────────────────────────────

        public void Connect(string portName)
        {
            Disconnect();
            _port = new SerialPort(portName, EcuConstants.Obd1BaudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 200,
                WriteTimeout = 200,
            };
            _port.DataReceived += OnSerialData;
            _port.Open();
            IsRunning = true;
            IsSimulation = false;
        }

        public bool IsRecording { get; private set; }
        private StreamWriter _csvWriter;

        public void StartRecording(string filePath)
        {
            try
            {
                _csvWriter = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                _csvWriter.WriteLine("Rpm,Map,Speed,Afr,Ect,Iat,Tps,BatteryVolts,InjDuty,IgnAdvance,VtecStatus");
                IsRecording = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERR] Failed to start log recording: {ex.Message}");
            }
        }

        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;
            try
            {
                _csvWriter?.Flush();
                _csvWriter?.Close();
                _csvWriter?.Dispose();
                _csvWriter = null;
            }
            catch { }
        }

        private void OnSerialData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_port != null && _port.BytesToRead > 0)
                {
                    int bytesToRead = _port.BytesToRead;
                    byte[] buffer = new byte[bytesToRead];
                    _port.Read(buffer, 0, bytesToRead);
                    _obd1Parser.Write(buffer);
                }
            }
            catch { /* port hatası yoksay */ }
        }

        // ── Simülasyon ───────────────────────────────────────────

        public void StartSimulation()
        {
            Disconnect();
            _simCts = new CancellationTokenSource();
            IsRunning = true;
            IsSimulation = true;

            Task.Run(async () =>
            {
                int tick = 0;
                while (!_simCts.Token.IsCancellationRequested)
                {
                    tick++;
                    // Her 80 tickte bir faz değiştir
                    if (tick % 80 == 0)
                    {
                        _simPhase = (_simPhase + 1) % 4;
                        _simRpmTarget = _simPhase switch
                        {
                            0 => 800,
                            1 => 2000 + _rng.NextDouble() * 5000,
                            2 => 3000 + _rng.NextDouble() * 2000,
                            3 => 800 + _rng.NextDouble() * 1500,
                            _ => 800
                        };
                    }

                    // Devir hedefi yavaşça yaklaş
                    double rpmDiff = _simRpmTarget - _simRpm;
                    _simRpm += rpmDiff * 0.04 + (_rng.NextDouble() - 0.5) * 40;
                    _simRpm = Math.Max(600, Math.Min(9000, _simRpm));

                    // Yük = devire bağlı (yaklaşık)
                    _simLoad = 20 + (_simRpm / 9000.0) * 120 + (_rng.NextDouble() - 0.5) * 20;
                    _simLoad = Math.Max(15, Math.Min(185, _simLoad));

                    // Hız = kademeli
                    double speedTarget = _simPhase == 3 ? 0 : _simRpm * 0.025;
                    _simSpeed += (speedTarget - _simSpeed) * 0.01;
                    _simSpeed = Math.Max(0, Math.Min(260, _simSpeed));

                    // AFR — zengin/fakir simülasyonu
                    double afrTarget = _simRpm > 6000 ? 12.5 + _rng.NextDouble() * 1.5 :
                                       _simRpm < 1000 ? 13.0 + _rng.NextDouble() * 1.0 :
                                                        14.2 + _rng.NextDouble() * 1.0;
                    _simAfr += (afrTarget - _simAfr) * 0.08;
                    _simAfr = Math.Max(10, Math.Min(20, _simAfr));

                    // ECT — ısınma simülasyonu (35°C'den 90°C'ye)
                    if (_simEct < 90) _simEct += 0.03;
                    _simEct = Math.Min(105, _simEct);

                    _simIat = 20 + _rng.NextDouble() * 15;

                    // TPS / Volts / Duty / Advance simulation
                    double simTps = (_simRpm > 1000) ? Math.Min(100.0, ((_simRpm - 800.0) / 8200.0) * 100.0 + (_simLoad / 3.0)) : 0.0;
                    simTps = Math.Max(0.0, simTps);
                    double simBattery = 13.5 + (_rng.NextDouble() - 0.5) * 0.3;
                    double simInjDuty = Math.Min(95.0, (_simRpm * _simLoad) / 11000.0);
                    double simIgnAdvance = Math.Max(10.0, Math.Min(45.0, 16.0 + (_simRpm / 1500.0) * 4.0 - (_simLoad / 15.0)));
                    bool simVtecStatus = _simRpm >= 4800;

                    var frame = new TelemetryFrame
                    {
                        Rpm = _simRpm,
                        Map = _simLoad,
                        Speed = _simSpeed,
                        Afr = _simAfr,
                        Ect = _simEct,
                        Iat = _simIat,
                        Tps = simTps,
                        BatteryVolts = simBattery,
                        InjDuty = simInjDuty,
                        IgnAdvance = simIgnAdvance,
                        VtecStatus = simVtecStatus
                    };

                    if (IsRecording && _csvWriter != null)
                    {
                        _csvWriter.WriteLine($"{frame.Rpm:0},{frame.Map:0.0},{frame.Speed:0},{frame.Afr:0.00},{frame.Ect:0},{frame.Iat:0},{frame.Tps:0},{frame.BatteryVolts:0.00},{frame.InjDuty:0.0},{frame.IgnAdvance:0.0},{(frame.VtecStatus ? 1 : 0)}");
                    }

                    DataReceived?.Invoke(frame);
                    await Task.Delay(80, _simCts.Token).ContinueWith(_ => { });
                }
            }, _simCts.Token);
        }

        public void Disconnect()
        {
            StopPlayback();
            IsRunning = false;
            _simCts?.Cancel();
            _simCts = null;

            if (_port != null)
            {
                _port.DataReceived -= OnSerialData;
                try { if (_port.IsOpen) _port.Close(); } catch { }
                _port.Dispose();
                _port = null;
            }
        }

        // ── CSV Playback (Datalog Geri Oynatma) ─────────────────

        /// <summary>Oynatma state'i: None, Playing, Paused.</summary>
        public enum PlaybackState { None, Playing, Paused }

        /// <summary>Oynatma yüklü CSV satırları listesi.</summary>
        private List<TelemetryFrame> _playbackFrames = new List<TelemetryFrame>();

        /// <summary>Geçerli oynatma pozisyonu (frame index).</summary>
        private int _playbackPosition = 0;

        /// <summary>Oynatma arka plan görevi için iptal tokeni.</summary>
        private CancellationTokenSource _playbackCts;

        /// <summary>Geçerli oynatma durumu.</summary>
        public PlaybackState State { get; private set; } = PlaybackState.None;

        /// <summary>Toplam yüklü frame sayısı.</summary>
        public int PlaybackFrameCount => _playbackFrames.Count;

        /// <summary>Geçerli oynatma pozisyonu.</summary>
        public int PlaybackPosition => _playbackPosition;

        /// <summary>Oynatma pozisyonu değiştiğinde GUI'ye bildirir.</summary>
        public event Action<int> PlaybackPositionChanged;

        /// <summary>CSV telemetri dosyasını playback listesine yükler.</summary>
        public bool LoadCsv(string filePath)
        {
            try
            {
                var frames = new List<TelemetryFrame>();
                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                for (int i = 1; i < lines.Length; i++) // satır 0 = başlık
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length < 11) continue;
                    frames.Add(new TelemetryFrame
                    {
                        Rpm = ParseD(parts[0]),
                        Map = ParseD(parts[1]),
                        Speed = ParseD(parts[2]),
                        Afr = ParseD(parts[3]),
                        Ect = ParseD(parts[4]),
                        Iat = ParseD(parts[5]),
                        Tps = ParseD(parts[6]),
                        BatteryVolts = ParseD(parts[7]),
                        InjDuty = ParseD(parts[8]),
                        IgnAdvance = ParseD(parts[9]),
                        VtecStatus = parts[10].Trim() == "1"
                    });
                }
                _playbackFrames = frames;
                _playbackPosition = 0;
                State = PlaybackState.Paused;
                PlaybackPositionChanged?.Invoke(_playbackPosition);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERR] CSV yükleme hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>Oynatmayı başlatır veya duraklatılmış oynatmayı devam ettirir.</summary>
        public void Play()
        {
            if (_playbackFrames.Count == 0) return;
            if (State == PlaybackState.Playing) return;
            State = PlaybackState.Playing;
            _playbackCts?.Cancel();
            _playbackCts = new CancellationTokenSource();
            var token = _playbackCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _playbackPosition < _playbackFrames.Count)
                {
                    TelemetryFrame frame = _playbackFrames[_playbackPosition];
                    DataReceived?.Invoke(frame);
                    PlaybackPositionChanged?.Invoke(_playbackPosition);
                    _playbackPosition++;

                    if (_playbackPosition >= _playbackFrames.Count)
                    {
                        // Oynatma sonu — otomatik dur
                        State = PlaybackState.Paused;
                        _playbackPosition = _playbackFrames.Count - 1;
                        PlaybackPositionChanged?.Invoke(_playbackPosition);
                        break;
                    }

                    await Task.Delay(80, token).ContinueWith(_ => { });
                }
            }, token);
        }

        /// <summary>Oynatmayı duraklatır (pozisyon korunur).</summary>
        public void Pause()
        {
            if (State != PlaybackState.Playing) return;
            State = PlaybackState.Paused;
            _playbackCts?.Cancel();
            _playbackCts = null;
        }

        /// <summary>Oynatmayı belirli bir frame'e konumlandırır (Seek).</summary>
        public void SeekTo(int frameIndex)
        {
            if (_playbackFrames.Count == 0) return;
            _playbackPosition = Math.Max(0, Math.Min(frameIndex, _playbackFrames.Count - 1));
            // Anlık frame'i DataReceived üzerinden yayınla (geçmişi canlı gibi gösterir)
            DataReceived?.Invoke(_playbackFrames[_playbackPosition]);
            PlaybackPositionChanged?.Invoke(_playbackPosition);
        }

        /// <summary>Playback altyapısını durdurur ve sıfırlar.</summary>
        public void StopPlayback()
        {
            _playbackCts?.Cancel();
            _playbackCts = null;
            State = PlaybackState.None;
            _playbackPosition = 0;
            _playbackFrames.Clear();
        }

        private static double ParseD(string s)
        {
            if (double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
                return v;
            return 0.0;
        }

        public void Dispose() => Disconnect();
    }

    /// <summary>Tek bir telemetri örneği.</summary>
    public class TelemetryFrame
    {
        public double Rpm { get; set; }
        public double Map { get; set; }   // kPa
        public double Speed { get; set; }   // km/h
        public double Afr { get; set; }
        public double Ect { get; set; }   // °C
        public double Iat { get; set; }   // °C
        public double Tps { get; set; }   // % (0-100)
        public double BatteryVolts { get; set; } // V
        public double InjDuty { get; set; } // %
        public double IgnAdvance { get; set; } // Degrees
        public bool VtecStatus { get; set; }
    }
}
