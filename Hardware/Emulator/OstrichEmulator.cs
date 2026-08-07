using System;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Hardware.Emulator
{
    /// <summary>
    /// Ostrich RTP Emülatör İstemcisi — gerçek zamanlı ROM düzenleme.
    /// </summary>
    public class OstrichEmulator : IEmulator
    {
        public string DeviceName => "Ostrich 2.0 RTP Emulator";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        public void Connect()
        {
            SetState(ConnectionState.Connecting, "Emülatör aranıyor...");
            ApplicationLogger.Info("OstrichEmulator", "RTP emülatör bağlantısı simüle ediliyor.");
            SetState(ConnectionState.Connected, "Emülatör bağlandı.");
        }

        public void Disconnect()
        {
            SetState(ConnectionState.Disconnected, "Emülatör bağlantısı kesildi.");
        }

        public byte ReadByte(int offset)
        {
            EnsureConnected();
            // TODO: Gerçek emülatör okuma
            return 0;
        }

        public void WriteByte(int offset, byte value)
        {
            EnsureConnected();
            ApplicationLogger.Debug("OstrichEmulator", $"RTP Write: 0x{offset:X4} = 0x{value:X2}");
            // TODO: Gerçek emülatör yazma
        }

        public byte[] ReadBlock(int offset, int length)
        {
            EnsureConnected();
            return new byte[length];
        }

        public void WriteBlock(int offset, byte[] data)
        {
            EnsureConnected();
            ApplicationLogger.Debug("OstrichEmulator",
                $"RTP Block Write: 0x{offset:X4}, {data.Length} byte");
        }

        private void EnsureConnected()
        {
            if (State != ConnectionState.Connected)
                throw new InvalidOperationException("Emülatör bağlı değil.");
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
