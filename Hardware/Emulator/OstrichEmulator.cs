using System;
using System.IO.Ports;
using System.Linq;
using Microsoft.Win32;
using System.Threading;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

/*
 * Moates Ostrich 2.0 / Racerom RoadRunner — Real-Time Programming (RTP) Emulator
 * Connection: USB-Serial (FTDI chip inside Ostrich)
 * Baud: 115200, 8-N-1
 *
 * Protocol (from open-source HTS / community reverse engineering):
 *   INIT:  send 0x01, expect echo 0x01 within 200ms
 *   WRITE: [0x02][ADDR_HI][ADDR_LO][BYTE]            → echo OK
 *   BLOCK: [0x03][ADDR_HI][ADDR_LO][LEN_HI][LEN_LO][DATA...] → echo OK
 *   READ:  [0x04][ADDR_HI][ADDR_LO][LEN_HI][LEN_LO] → DATA bytes
 *   READB: [0x05][ADDR_HI][ADDR_LO]                 → single BYTE
 *
 * REQUIRES TESTING WITH REAL MOATES OSTRICH HARDWARE.
 * Protocol based on open-source HondaTuningSuite (HTS) reference.
 */

namespace HondaTuner.Hardware.Emulator
{
    public class OstrichEmulator : IEmulator
    {
        private const int BAUD_RATE = 115200;
        private const int TIMEOUT_MS = 500;
        private const int INIT_DELAY_MS = 200;
        private const byte CMD_INIT = 0x01;
        private const byte CMD_WRITE_BYTE = 0x02;
        private const byte CMD_WRITE_BLOCK = 0x03;
        private const byte CMD_READ_BLOCK = 0x04;
        private const byte CMD_READ_BYTE = 0x05;
        private const byte ACK = 0x06;

        public string DeviceName => "Moates Ostrich 2.0 RTP Emulator";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        private SerialPort _port;
        private readonly object _portLock = new object();
        private string _portName;

        public void Connect()
        {
            SetState(ConnectionState.Connecting, "Ostrich aranıyor...");
            ApplicationLogger.Info("OstrichEmulator", "Ostrich emülatörü aranıyor (USB-Serial)...");

            try
            {
                _portName = AutoDetectOstrichPort();

                if (string.IsNullOrEmpty(_portName))
                    throw new InvalidOperationException(
                        "Ostrich 2.0 cihazı bulunamadı. USB sürücüsünün (FTDI) kurulu olduğundan emin olun.");

                ApplicationLogger.Info("OstrichEmulator", $"Ostrich port bulundu: {_portName}");

                _port = new SerialPort(_portName, BAUD_RATE, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = TIMEOUT_MS,
                    WriteTimeout = TIMEOUT_MS
                };
                _port.Open();
                Thread.Sleep(INIT_DELAY_MS);

                // Flush
                while (_port.BytesToRead > 0) _port.ReadByte();

                // Send handshake
                _port.Write(new byte[] { CMD_INIT }, 0, 1);
                Thread.Sleep(100);

                // Expect echo
                if (_port.BytesToRead > 0)
                {
                    int echo = _port.ReadByte();
                    if (echo != CMD_INIT)
                        throw new InvalidOperationException($"Ostrich handshake başarısız: echo=0x{echo:X2}");
                }

                SetState(ConnectionState.Connected, "Ostrich bağlandı.");
                ApplicationLogger.Info("OstrichEmulator", "Ostrich 2.0 bağlantısı başarılı.");
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("OstrichEmulator", $"Bağlantı hatası: {ex.Message}");
                CleanupPort();
                SetState(ConnectionState.Error, ex.Message);
                throw;
            }
        }

        public void Disconnect()
        {
            CleanupPort();
            SetState(ConnectionState.Disconnected, "Ostrich bağlantısı kesildi.");
            ApplicationLogger.Info("OstrichEmulator", "Bağlantı kapatıldı.");
        }

        public byte ReadByte(int offset)
        {
            EnsureConnected();
            if (offset < 0 || offset > 0xFFFF)
                throw new ArgumentOutOfRangeException(nameof(offset));

            lock (_portLock)
            {
                byte[] cmd = { CMD_READ_BYTE, (byte)(offset >> 8), (byte)(offset & 0xFF) };
                _port.Write(cmd, 0, cmd.Length);
                Thread.Sleep(5);
                int b = _port.ReadByte();
                if (b < 0) throw new TimeoutException("Ostrich ReadByte: cevap yok.");
                return (byte)b;
            }
        }

        public void WriteByte(int offset, byte value)
        {
            EnsureConnected();
            if (offset < 0 || offset > 0xFFFF)
                throw new ArgumentOutOfRangeException(nameof(offset));

            lock (_portLock)
            {
                byte[] cmd = { CMD_WRITE_BYTE, (byte)(offset >> 8), (byte)(offset & 0xFF), value };
                _port.Write(cmd, 0, cmd.Length);
                Thread.Sleep(3);
                // Drain ACK byte
                if (_port.BytesToRead > 0) _port.ReadByte();
            }
        }

        public byte[] ReadBlock(int offset, int length)
        {
            EnsureConnected();
            if (offset < 0 || length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

            lock (_portLock)
            {
                byte[] cmd =
                {
                    CMD_READ_BLOCK,
                    (byte)(offset >> 8), (byte)(offset & 0xFF),
                    (byte)(length >> 8), (byte)(length & 0xFF)
                };
                _port.Write(cmd, 0, cmd.Length);

                byte[] result = new byte[length];
                int received = 0;
                var deadline = DateTime.UtcNow.AddMilliseconds(TIMEOUT_MS * 4);
                while (received < length && DateTime.UtcNow < deadline)
                {
                    if (_port.BytesToRead > 0)
                        result[received++] = (byte)_port.ReadByte();
                    else
                        Thread.Sleep(2);
                }
                if (received < length)
                    throw new TimeoutException($"Ostrich ReadBlock: eksik veri {received}/{length}");
                return result;
            }
        }

        public void WriteBlock(int offset, byte[] data)
        {
            EnsureConnected();
            if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));

            // Write in 256-byte chunks to avoid serial buffer overflow
            const int chunkSize = 256;
            for (int pos = 0; pos < data.Length; pos += chunkSize)
            {
                int len = Math.Min(chunkSize, data.Length - pos);
                WriteBlockChunk(offset + pos, data, pos, len);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private void WriteBlockChunk(int offset, byte[] data, int dataOffset, int length)
        {
            lock (_portLock)
            {
                byte[] cmd = new byte[5 + length];
                cmd[0] = CMD_WRITE_BLOCK;
                cmd[1] = (byte)(offset >> 8);
                cmd[2] = (byte)(offset & 0xFF);
                cmd[3] = (byte)(length >> 8);
                cmd[4] = (byte)(length & 0xFF);
                Buffer.BlockCopy(data, dataOffset, cmd, 5, length);

                _port.Write(cmd, 0, cmd.Length);
                Thread.Sleep(5);
                // Drain ACK
                if (_port.BytesToRead > 0) _port.ReadByte();
            }
        }

        /// <summary>
        /// Scans COM ports for an FTDI-based device (Ostrich uses FT232R).
        /// Returns the first match or null.
        /// </summary>
        private static string AutoDetectOstrichPort()
        {
            try
            {
                // Read COM port assignments from Windows Registry (no System.Management required)
                // HKLM\SYSTEM\CurrentControlSet\Enum\USB — look for FTDI USB serial devices
                using RegistryKey usbKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Enum\USB");
                if (usbKey != null)
                {
                    foreach (string vid in usbKey.GetSubKeyNames())
                    {
                        // FTDI VID is 0403, common clones use 1A86 (CH340)
                        if (!vid.StartsWith("VID_0403", StringComparison.OrdinalIgnoreCase) &&
                            !vid.StartsWith("VID_1A86", StringComparison.OrdinalIgnoreCase))
                            continue;

                        using RegistryKey vidKey = usbKey.OpenSubKey(vid);
                        if (vidKey == null) continue;
                        foreach (string devKey in vidKey.GetSubKeyNames())
                        {
                            using RegistryKey dev = vidKey.OpenSubKey($@"{devKey}\Device Parameters");
                            if (dev == null) continue;
                            string portName = dev.GetValue("PortName")?.ToString();
                            if (!string.IsNullOrEmpty(portName) && portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                            {
                                ApplicationLogger.Info("OstrichEmulator", $"Registry: FTDI port bulundu: {portName}");
                                return portName;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Warn("OstrichEmulator", $"Registry port tarama hatası: {ex.Message}");
            }

            // Fallback: try all COM ports, test handshake
            foreach (string port in SerialPort.GetPortNames().OrderBy(p => p))
            {
                if (TestHandshake(port)) return port;
            }
            return null;
        }

        private static bool TestHandshake(string portName)
        {
            try
            {
                using var sp = new SerialPort(portName, BAUD_RATE, Parity.None, 8, StopBits.One)
                { ReadTimeout = 200, WriteTimeout = 200 };
                sp.Open();
                sp.Write(new byte[] { CMD_INIT }, 0, 1);
                Thread.Sleep(150);
                if (sp.BytesToRead > 0)
                {
                    int echo = sp.ReadByte();
                    if (echo == CMD_INIT) return true;
                }
                sp.Close();
            }
            catch { }
            return false;
        }

        private void EnsureConnected()
        {
            if (State != ConnectionState.Connected || _port == null || !_port.IsOpen)
                throw new InvalidOperationException("Ostrich emülatörü bağlı değil.");
        }

        private void CleanupPort()
        {
            try { _port?.Close(); } catch { }
            _port = null;
        }

        private void SetState(ConnectionState newState, string msg)
        {
            var old = State;
            State = newState;
            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            { OldState = old, NewState = newState, Message = msg });
        }
    }
}
