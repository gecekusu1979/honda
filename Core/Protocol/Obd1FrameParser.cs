using System;

namespace HondaTuner.Core.Protocol
{
    public class Obd1FrameParser
    {
        private readonly byte[] _ringBuffer = new byte[32];
        private int _writeIndex = 0;
        private int _count = 0;

        public event Action<byte[]> OnFrameParsed;

        public void Write(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            foreach (var b in data)
            {
                WriteByte(b);
            }
        }

        public void WriteByte(byte b)
        {
            _ringBuffer[_writeIndex] = b;
            _writeIndex = (_writeIndex + 1) % 32;
            if (_count < 32)
            {
                _count++;
            }

            ProcessBuffer();
        }

        private void ProcessBuffer()
        {
            while (_count >= 14)
            {
                int oldestIndex = (_writeIndex - _count + 32) % 32;
                int secondIndex = (oldestIndex + 1) % 32;

                byte b1 = _ringBuffer[oldestIndex];
                byte b2 = _ringBuffer[secondIndex];

                if (b1 == 0xFF && b2 == 0xFE)
                {
                    byte[] tempFrame = new byte[14];
                    int sum = 0;
                    for (int i = 0; i < 13; i++)
                    {
                        int idx = (oldestIndex + i) % 32;
                        tempFrame[i] = _ringBuffer[idx];
                        sum += tempFrame[i];
                    }

                    int checksumIdx = (oldestIndex + 13) % 32;
                    tempFrame[13] = _ringBuffer[checksumIdx];

                    byte calculatedChecksum = (byte)(sum % 256);
                    if (calculatedChecksum == tempFrame[13])
                    {
                        OnFrameParsed?.Invoke(tempFrame);
                        _count -= 14;
                    }
                    else
                    {
                        _count--; // Checksum mismatch, advance 1 byte
                    }
                }
                else
                {
                    _count--; // Sync mismatch, advance 1 byte
                }
            }
        }
    }
}
