using System;
using System.IO.Ports;
using System.Threading;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

/*
 * HONDA OBD1 SERIAL PROTOCOL — 1992-1995 Civic/Integra/Prelude
 * Physical: FTDI USB-Serial adapter + MAX232 level shifter
 * Pin 9 (K-Line) = Signal, Pin 4/5 = GND, Pin 14 = +12V
 *
 * Protocol Init:
 *   1. Open port at 5 baud (manual toggle)
 *   2. Send 0x01 (wake-up byte)
 *   3. Wait ~2s for ECU boot
 *   4. Switch to 9600 baud 8-N-1
 *   5. Full request/response cycle
 *
 * REQUIRES TESTING WITH REAL OBD1 HARDWARE.
 * Protocol based on open-source HTS / Crome reverse engineering references.
 */

namespace HondaTuner.Hardware.OBD
{
    public class RealObd1Connection : IObdConnection
    {
        // ── Honda OBD1 sensor request codes ──
        private const byte CMD_RPM = 0x20;
        private const byte CMD_SPEED = 0x21;
        private const byte CMD_TPS = 0x22;
        private const byte CMD_ECT = 0x23;
        private const byte CMD_IAT = 0x24;
        private const byte CMD_MAP = 0x25;
        private const byte CMD_BATTERY = 0x26;
        private const byte CMD_O2 = 0x27;
        private const byte CMD_INJ_DURATION = 0x28;
        private const byte CMD_IGN_ADVANCE = 0x29;

        private const int INIT_BAUD = 5;
        private const int RUN_BAUD = 9600;
        private const int INIT_DELAY_MS = 2000;
        private const int READ_TIMEOUT_MS = 500;
        private const int WRITE_TIMEOUT_MS = 200;
        private const int MaxRetries = 3;

        // ECT/IAT thermistor lookup table (raw byte → °C)
        // Source: Honda P28 ECU service manual
        private static readonly int[] TempTable = new int[]
        {
            -40,-30,-20,-10, 0, 10, 20, 30, 40, 50,
             60, 70, 80, 90, 100, 110, 120, 130, 140, 150
        };
        private static readonly byte[] TempRaw = new byte[]
        {
            255, 241, 224, 204, 181, 156, 128, 100, 76, 57,
             43, 32, 24, 18, 14, 11, 9, 7, 6, 5
        };

        public string DeviceName => "Honda OBD1 Serial Interface";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;
        public event EventHandler<TelemetryFrameData> FrameReceived;

        private SerialPort _port;
        private Thread _streamThread;
        private volatile bool _streaming;
        private string _portName;
        private int _baudRate;
        private int _retryCount;

        public void Connect()
        {
            if (string.IsNullOrWhiteSpace(_portName))
            {
                SetState(ConnectionState.Error, "Port adı belirtilmedi.");
                return;
            }
            Open(_portName, _baudRate > 0 ? _baudRate : RUN_BAUD);
        }

        public void Open(string portName, int baudRate)
        {
            _portName = portName;
            _baudRate = baudRate > 0 ? baudRate : RUN_BAUD;
            _retryCount = 0;

            SetState(ConnectionState.Connecting, $"Honda OBD1 başlatılıyor: {portName}");
            ApplicationLogger.Info("RealObd1Connection", $"5-baud init başlatılıyor → {portName}");

            try
            {
                // ── Step 1: 5-baud initialisation (manual bit-bang) ──
                // We open at a standard baud then drive DTR/RTS lines to
                // simulate the 5-baud 0x01 byte (8 bits @ 5 baud = 1.6 s).
                // This is the standard Honda OBD1 K-Line wake-up sequence.
                using (var initPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One))
                {
                    initPort.DtrEnable = false;
                    initPort.RtsEnable = false;
                    initPort.Open();

                    // Bit-bang 0x01 at ~5 baud: start bit (low) + 8 data bits + stop bit (high)
                    // Bit period = 200 ms @ 5 baud
                    const int bitPeriod = 200;

                    // Start bit (low)
                    initPort.BreakState = true;
                    Thread.Sleep(bitPeriod);

                    // Bit 0 = 1 (stop break → high)
                    initPort.BreakState = false;
                    Thread.Sleep(bitPeriod);

                    // Bits 1-7 = 0 (break again = low)
                    initPort.BreakState = true;
                    Thread.Sleep(bitPeriod * 7);

                    // Stop bit (high / idle)
                    initPort.BreakState = false;
                    Thread.Sleep(bitPeriod);

                    initPort.Close();
                }

                ApplicationLogger.Info("RealObd1Connection", "5-baud 0x01 gönderildi. ECU uyandırma bekleniyor...");
                Thread.Sleep(INIT_DELAY_MS);

                // ── Step 2: Open at normal 9600 baud ──
                _port = new SerialPort(portName, _baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = READ_TIMEOUT_MS,
                    WriteTimeout = WRITE_TIMEOUT_MS
                };
                _port.Open();

                // ── Step 3: Drain any ECU init echo bytes ──
                Thread.Sleep(100);
                while (_port.BytesToRead > 0)
                    _port.ReadByte();

                SetState(ConnectionState.Connected, "Honda OBD1 bağlantısı kuruldu.");
                ApplicationLogger.Info("RealObd1Connection", "OBD1 bağlantısı başarılı.");

                // ── Step 4: Start streaming thread ──
                _streaming = true;
                _streamThread = new Thread(StreamLoop) { IsBackground = true, Name = "OBD1-Stream" };
                _streamThread.Start();
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("RealObd1Connection", $"Bağlantı hatası: {ex.Message}");
                CleanupPort();
                HandleConnectionError();
            }
        }

        public void Disconnect()
        {
            ApplicationLogger.Info("RealObd1Connection", "Bağlantı kapatılıyor.");
            _streaming = false;
            _streamThread?.Join(1000);
            CleanupPort();
            SetState(ConnectionState.Disconnected, "Bağlantı kapatıldı.");
        }

        /// <summary>
        /// Reads a single full telemetry frame by querying all sensor codes.
        /// Called directly (blocking) or indirectly from the stream thread.
        /// </summary>
        public TelemetryFrameData ReadFrame()
        {
            if (State != ConnectionState.Connected || _port == null)
            {
                ApplicationLogger.Warn("RealObd1Connection", "Frame okunamadı — bağlantı yok.");
                return null;
            }

            try
            {
                int rawRpm = QuerySensor(CMD_RPM, 2);
                int rawSpeed = QuerySensor(CMD_SPEED, 1);
                int rawTps = QuerySensor(CMD_TPS, 1);
                int rawEct = QuerySensor(CMD_ECT, 1);
                int rawIat = QuerySensor(CMD_IAT, 1);
                int rawMap = QuerySensor(CMD_MAP, 1);
                int rawBatt = QuerySensor(CMD_BATTERY, 1);
                int rawO2 = QuerySensor(CMD_O2, 1);
                int rawInj = QuerySensor(CMD_INJ_DURATION, 2);
                int rawIgn = QuerySensor(CMD_IGN_ADVANCE, 1);

                // rawSpeed available (km/h) — VehicleSpeed not in TelemetryFrameData DTO
                _ = rawSpeed;

                return new TelemetryFrameData
                {
                    Rpm = DecodeRpm(rawRpm),
                    Tps = Math.Round(rawTps / 255.0 * 100.0, 1),
                    Ect = DecodeThermoTable(rawEct),
                    Iat = DecodeThermoTable(rawIat),
                    Map = rawMap,
                    BatteryVolts = Math.Round(rawBatt * 0.0784, 2),
                    Afr = DecodeAfr(rawO2),
                    InjDuty = Math.Round(rawInj / 10.0, 1),
                    IgnAdvance = rawIgn,
                    VtecActive = false
                };
            }
            catch (TimeoutException)
            {
                ApplicationLogger.Warn("RealObd1Connection", "ECU cevap zaman aşımı.");
                return null;
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("RealObd1Connection", $"Frame okuma hatası: {ex.Message}");
                HandleConnectionError();
                return null;
            }
        }

        // ── Private Helpers ──────────────────────────────────────────────

        private void StreamLoop()
        {
            ApplicationLogger.Info("RealObd1Connection", "Canlı veri stream başladı.");
            while (_streaming && State == ConnectionState.Connected)
            {
                var frame = ReadFrame();
                if (frame != null)
                    FrameReceived?.Invoke(this, frame);
                Thread.Sleep(50); // ~20 Hz
            }
            ApplicationLogger.Info("RealObd1Connection", "Stream sonlandı.");
        }

        /// <summary>
        /// Sends a single-byte request code and reads <paramref name="responseBytes"/> bytes back.
        /// </summary>
        private int QuerySensor(byte command, int responseBytes)
        {
            _port.Write(new byte[] { command }, 0, 1);
            Thread.Sleep(10); // let ECU process

            int result = 0;
            for (int i = 0; i < responseBytes; i++)
            {
                int b = _port.ReadByte();
                if (b < 0) throw new TimeoutException("ECU yanıt vermedi.");
                result = (result << 8) | (b & 0xFF);
            }
            return result;
        }

        private static double DecodeRpm(int raw)
        {
            // Honda OBD1: RPM = raw_value / 4  (16-bit big-endian)
            return raw / 4.0;
        }

        private static double DecodeAfr(int rawO2)
        {
            // Narrowband O2: 0 = rich (≈11.0), 255 = lean (≈18.0)
            // Linear approximation for display purposes
            return Math.Round(11.0 + (rawO2 / 255.0) * 7.0, 2);
        }

        private static double DecodeThermoTable(int raw)
        {
            // Walk the thermistor table — nearest match
            int best = 0;
            int bestDiff = int.MaxValue;
            for (int i = 0; i < TempRaw.Length; i++)
            {
                int diff = Math.Abs(TempRaw[i] - raw);
                if (diff < bestDiff) { bestDiff = diff; best = i; }
            }
            // Interpolate between nearest two points if not exact edge
            if (best < TempTable.Length - 1 && bestDiff > 0)
            {
                int lo = TempRaw[best];
                int hi = TempRaw[best + 1];
                if (hi != lo)
                {
                    double frac = (double)(raw - lo) / (hi - lo);
                    return TempTable[best] + frac * (TempTable[best + 1] - TempTable[best]);
                }
            }
            return TempTable[best];
        }

        private void HandleConnectionError()
        {
            _retryCount++;
            if (_retryCount < MaxRetries)
            {
                ApplicationLogger.Warn("RealObd1Connection",
                    $"Yeniden deneniyor ({_retryCount}/{MaxRetries})...");
                SetState(ConnectionState.Connecting, $"Yeniden deneme {_retryCount}/{MaxRetries}");
            }
            else
            {
                CleanupPort();
                SetState(ConnectionState.TimedOut, "Maksimum deneme sayısına ulaşıldı.");
            }
        }

        private void CleanupPort()
        {
            try { _port?.Close(); } catch { }
            _port = null;
        }

        private void SetState(ConnectionState newState, string message)
        {
            var old = State;
            State = newState;
            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                OldState = old,
                NewState = newState,
                Message = message
            });
        }
    }
}
