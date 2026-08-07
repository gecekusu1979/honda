using System;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Desteklenen araç tanı ve datalog protokol sınıfları.
    /// </summary>
    public enum ProtocolType
    {
        HondaOBD1,
        OBD2,
        KWP2000,
        UDS,
        Mock
    }

    /// <summary>
    /// Mesaj/Paket protokol paketleyicisi ve çözümleyicisi (OBD1, OBD2, UDS vb.).
    /// </summary>
    public interface IProtocol : IDisposable
    {
        ProtocolType Type { get; }
        ITransport Transport { get; }

        /// <summary>
        /// Protokol başlatma el sıkışmalarını gerçekleştirir.
        /// </summary>
        void InitializeProtocol();

        /// <summary>
        /// Belirli bir kanal parametresi için istek paketi yollar.
        /// </summary>
        void RequestParameter(string channelId);

        /// <summary>
        /// Gelen ham bayt dizininden ilgili kanala ait veriyi ve doğrulama durumunu çıkartır.
        /// </summary>
        bool TryReadPayload(string channelId, out byte[] rawPayload, out ushort crc, out byte checksum);
    }

    public class MockProtocol : IProtocol
    {
        public ProtocolType Type => ProtocolType.Mock;
        public ITransport Transport { get; }

        private readonly Random _rand = new Random();

        public MockProtocol(ITransport transport)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public void InitializeProtocol()
        {
            if (!Transport.IsOpen) Transport.Open();
        }

        public void RequestParameter(string channelId)
        {
            // Mock istek gönderme simülasyonu
            byte[] cmd = new byte[] { 0x01, 0x00 };
            Transport.Write(cmd, 0, cmd.Length);
        }

        public bool TryReadPayload(string channelId, out byte[] rawPayload, out ushort crc, out byte checksum)
        {
            // Rastgele dalgalanan ham veriler üreterek ECU datasını simüle eder
            rawPayload = new byte[2];
            switch (channelId)
            {
                case "RPM":
                    int rpmVal = _rand.Next(750, 8000);
                    rawPayload[0] = (byte)(rpmVal >> 8);
                    rawPayload[1] = (byte)(rpmVal & 0xFF);
                    break;
                case "TPS":
                    rawPayload[0] = (byte)_rand.Next(0, 255);
                    rawPayload[1] = 0x00;
                    break;
                case "MAP":
                    rawPayload[0] = (byte)_rand.Next(20, 240);
                    rawPayload[1] = 0x00;
                    break;
                case "ECT":
                case "IAT":
                    rawPayload[0] = (byte)_rand.Next(50, 150); // -40 offset ile 10 ile 110 derece arası
                    rawPayload[1] = 0x00;
                    break;
                case "Battery":
                    rawPayload[0] = (byte)_rand.Next(110, 145); // 0.1 scale ile 11.0V - 14.5V arası
                    rawPayload[1] = 0x00;
                    break;
                case "VehicleSpeed":
                    rawPayload[0] = (byte)_rand.Next(0, 180);
                    rawPayload[1] = 0x00;
                    break;
                case "InjectorDuty":
                    rawPayload[0] = (byte)_rand.Next(1, 85);
                    rawPayload[1] = 0x00;
                    break;
                case "IgnitionAdvance":
                    rawPayload[0] = (byte)_rand.Next(10, 90);
                    rawPayload[1] = 0x00;
                    break;
                case "AFR":
                    rawPayload[0] = (byte)_rand.Next(100, 200); // 10.0 - 20.0 AFR arası
                    rawPayload[1] = 0x00;
                    break;
                case "KnockCount":
                    rawPayload[0] = (byte)(_rand.Next(0, 100) < 5 ? 1 : 0); // Seyrek vuruntu
                    rawPayload[1] = 0x00;
                    break;
                default:
                    rawPayload[0] = 0x00;
                    rawPayload[1] = 0x00;
                    break;
            }

            // Basit bütünlük hesaplama simülasyonu
            crc = (ushort)((rawPayload[0] << 8) | rawPayload[1]);
            checksum = (byte)(rawPayload[0] ^ rawPayload[1]);
            return true;
        }

        public void Dispose()
        {
            Transport.Dispose();
        }
    }
}
