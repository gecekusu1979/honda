using System;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri mesaj önceliği.
    /// </summary>
    public enum MessagePriority
    {
        Critical,
        High,
        Normal,
        Low
    }

    /// <summary>
    /// Telemetri verisinin bütünlük doğrulama durumu.
    /// </summary>
    public enum ValidationStatus
    {
        NotValidated,
        Valid,
        Invalid
    }

    /// <summary>
    /// Telemetri verisinin akış yönü.
    /// </summary>
    public enum FrameDirection
    {
        Rx, // Sunucu / Cihazdan gelen veri (Receive)
        Tx  // Cihaza gönderilen komut veya kalibrasyon (Transmit)
    }

    /// <summary>
    /// Tek bir zaman diliminde üretilmiş, kanala özel telemetri veri çerçevesidir.
    /// </summary>
    public class TelemetryFrame
    {
        // Temel Kimlikler
        public string ChannelId { get; set; }
        public long FrameId { get; set; }
        public string Source { get; set; }        // Örn. "MockProvider", "OBDII_ComPort3"
        public string SourceId { get; set; }      // Cihaz seri numarası veya benzersiz ID'si
        public string SessionId { get; set; }     // Bağlantı oturumu benzersiz ID'si
        public string Transport { get; set; }     // Örn. "SerialPort", "USB_FTDI", "Mock"
        public FrameDirection Direction { get; set; }
        public string FrameType { get; set; }     // Örn. "Datalog", "DTC", "Acknowledge"

        // Zaman Damgaları (Sistem genelinde tek tip ITimeProvider kullanılır)
        public DateTime UtcTimestamp { get; set; }
        public long MonotonicTimestamp { get; set; } // Yüksek çözünürlüklü CPU tick veya ms sayacı
        public double ElapsedTime { get; set; }      // Oturum başlangıcından itibaren geçen süre (saniye)

        // Değerler
        public double Value { get; set; }            // Ölçeklendirilmiş ve filtrelenmiş son değer
        public byte[] RawValue { get; set; }        // ECU'dan gelen ham baytlar
        public double FilteredValue { get; set; }   // Filtre sonrası değer
        public TelemetryQuality Quality { get; set; }
        public ChannelStatus Status { get; set; }
        public MessagePriority Priority { get; set; }

        // Bütünlük Doğrulama
        public ushort CRC { get; set; }
        public byte Checksum { get; set; }
        public ValidationStatus Validation { get; set; }

        // Akış Verimliliği
        public int SequenceNumber { get; set; }
        public double UpdateRate { get; set; }       // Hz cinsinden anlık yenilenme hızı

        /// <summary>
        /// Frame nesnesini havuzdan geri alındığında sıfırlamak için kullanılır.
        /// </summary>
        public void Reset()
        {
            ChannelId = null;
            FrameId = 0;
            Source = null;
            SourceId = null;
            SessionId = null;
            Transport = null;
            Direction = FrameDirection.Rx;
            FrameType = null;
            UtcTimestamp = default;
            MonotonicTimestamp = 0;
            ElapsedTime = 0.0;
            Value = 0.0;
            RawValue = null;
            FilteredValue = 0.0;
            Quality = TelemetryQuality.Good;
            Status = ChannelStatus.Valid;
            Priority = MessagePriority.Normal;
            CRC = 0;
            Checksum = 0;
            Validation = ValidationStatus.NotValidated;
            SequenceNumber = 0;
            UpdateRate = 0.0;
        }
    }
}
