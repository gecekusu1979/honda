using System;
using System.IO;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Fiziksel bağlantı katmanı soyutlaması (USB, Serial, TCP vb.).
    /// </summary>
    public interface ITransport : IDisposable
    {
        string Name { get; }
        bool IsOpen { get; }
        void Open();
        void Close();
        void Write(byte[] data, int offset, int count);
        int Read(byte[] buffer, int offset, int count);
        void Flush();
    }

    /// <summary>
    /// Simüle edilmiş veya fiziksel bir Mock Transport implementasyonu.
    /// </summary>
    public class MockTransport : ITransport
    {
        private readonly MemoryStream _inStream = new MemoryStream();
        private readonly MemoryStream _outStream = new MemoryStream();
        private bool _isOpen = false;

        public string Name => "MockTransport";
        public bool IsOpen => _isOpen;

        public void Open()
        {
            _isOpen = true;
        }

        public void Close()
        {
            _isOpen = false;
        }

        public void Write(byte[] data, int offset, int count)
        {
            if (!_isOpen) throw new InvalidOperationException("Transport bağlı değil.");
            _outStream.Write(data, offset, count);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!_isOpen) throw new InvalidOperationException("Transport bağlı değil.");
            // Mock veriler üretir ya da boş döner
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = 0x00;
            }
            return count;
        }

        public void Flush()
        {
            _inStream.SetLength(0);
            _outStream.SetLength(0);
        }

        public void Dispose()
        {
            Close();
            _inStream.Dispose();
            _outStream.Dispose();
        }
    }
}
