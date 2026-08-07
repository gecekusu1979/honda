using System;

namespace HondaTuner.Calibration.Ignition
{
    public class CanSensorDecoder
    {
        public string SensorName { get; set; }
        public uint FrameId { get; set; }
        public int StartBit { get; set; } // 0 - 63
        public int BitLength { get; set; } // e.g., 8, 12, 16 bits
        public bool IsBigEndian { get; set; }
        public double Scale { get; set; } = 1.0;
        public double Offset { get; set; } = 0.0;
        public string Unit { get; set; }

        public CanSensorDecoder() { }

        public CanSensorDecoder(string name, uint frameId, int startBit, int bitLength, bool bigEndian, double scale, double offset, string unit)
        {
            SensorName = name;
            FrameId = frameId;
            StartBit = startBit;
            BitLength = bitLength;
            IsBigEndian = bigEndian;
            Scale = scale;
            Offset = offset;
            Unit = unit;
        }

        // 8 Byte CAN Verisini Çözümleme
        public double Decode(byte[] frameData)
        {
            if (frameData == null || frameData.Length < 8) return 0.0;

            // 8 adet byte'ı tek bir ulong (64-bit) bit örüntüsüne birleştir
            ulong bits = 0;
            if (IsBigEndian)
            {
                for (int i = 0; i < 8; i++)
                {
                    bits = (bits << 8) | frameData[i];
                }

                // Big Endian (Motorola) formatında bit kaydırma ters yönlü hizalanabilir
                // Çoğu ECU standart bitmask'ine göre düzeltelim:
                // StartBit en yüksek anlamlı biti (MSB) gösterir.
                int shift = 64 - (StartBit + BitLength);
                ulong mask = (1UL << BitLength) - 1;
                ulong extracted = (bits >> shift) & mask;
                return (extracted * Scale) + Offset;
            }
            else
            {
                // Intel (Little Endian) formatında:
                for (int i = 7; i >= 0; i--)
                {
                    bits = (bits << 8) | frameData[i];
                }

                ulong mask = (1UL << BitLength) - 1;
                ulong extracted = (bits >> StartBit) & mask;
                return (extracted * Scale) + Offset;
            }
        }
    }
}
