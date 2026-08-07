using System;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Hardware.EEPROM
{
    /// <summary>
    /// TL866 / CH341A programlayıcı istemcisi.
    /// Her yazma öncesi otomatik yedekleme yapar.
    /// </summary>
    public class Tl866Programmer : IEepromProgrammer
    {
        public string DeviceName => "TL866II Plus / CH341A Programmer";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        private byte[] _lastBackup;

        public void Connect()
        {
            SetState(ConnectionState.Connecting, "Programlayıcı aranıyor...");
            // TODO: USB HID aygıt tarama
            ApplicationLogger.Info("Tl866Programmer", "Programlayıcı bağlantısı simüle ediliyor.");
            SetState(ConnectionState.Connected, "Programlayıcı bağlandı.");
        }

        public void Disconnect()
        {
            SetState(ConnectionState.Disconnected, "Programlayıcı bağlantısı kesildi.");
            ApplicationLogger.Info("Tl866Programmer", "Bağlantı kapatıldı.");
        }

        public byte[] ReadChip(int romLength)
        {
            if (State != ConnectionState.Connected)
                throw new InvalidOperationException("Programlayıcı bağlı değil.");

            ApplicationLogger.Info("Tl866Programmer", $"Chip okunuyor: {romLength} byte");
            // TODO: Gerçek USB okuma
            return new byte[romLength];
        }

        public void EraseChip()
        {
            if (State != ConnectionState.Connected)
                throw new InvalidOperationException("Programlayıcı bağlı değil.");

            ApplicationLogger.Info("Tl866Programmer", "Chip siliniyor...");
            // TODO: Gerçek silme komutu
        }

        public void WriteChip(byte[] romData)
        {
            if (State != ConnectionState.Connected)
                throw new InvalidOperationException("Programlayıcı bağlı değil.");

            // Otomatik yedekleme
            _lastBackup = ReadChip(romData.Length);
            ApplicationLogger.Info("Tl866Programmer",
                $"Yazma öncesi yedek alındı ({_lastBackup.Length} byte)");

            ApplicationLogger.Info("Tl866Programmer", $"Chip yazılıyor: {romData.Length} byte");
            // TODO: Gerçek USB yazma
        }

        public bool VerifyChip(byte[] expectedData)
        {
            if (State != ConnectionState.Connected)
                throw new InvalidOperationException("Programlayıcı bağlı değil.");

            byte[] actual = ReadChip(expectedData.Length);
            for (int i = 0; i < expectedData.Length; i++)
            {
                if (actual[i] != expectedData[i])
                {
                    ApplicationLogger.Error("Tl866Programmer",
                        $"Doğrulama hatası: offset 0x{i:X4} beklenen=0x{expectedData[i]:X2} okunan=0x{actual[i]:X2}");
                    return false;
                }
            }

            ApplicationLogger.Info("Tl866Programmer", "Doğrulama başarılı.");
            return true;
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
