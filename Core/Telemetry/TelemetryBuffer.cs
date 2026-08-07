using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri verilerini belirli bir boyutta saklayan, FIFO (First-In-First-Out) çalışan,
    /// limit aşımında eski verilerin üzerine yazan (overwrite) thread-safe dairesel bellek (Ring Buffer) yapısıdır.
    /// </summary>
    public class TelemetryBuffer
    {
        private TelemetryFrame[] _buffer;
        private int _head = 0;
        private int _tail = 0;
        private int _count = 0;
        private readonly int _capacity;
        private readonly object _lock = new object();

        public int Capacity => _capacity;
        public int Count { get { lock (_lock) { return _count; } } }

        public TelemetryBuffer(int capacity = 10000)
        {
            _capacity = capacity > 0 ? capacity : 10000;
            _buffer = new TelemetryFrame[_capacity];
        }

        /// <summary>
        /// Kuyruğa yeni bir veri yerleştirir. Doyuma ulaşıldığında en eski verinin üzerine yazar.
        /// </summary>
        public void Enqueue(TelemetryFrame frame)
        {
            if (frame == null) return;

            lock (_lock)
            {
                // Eğer kuyruk dolu ise, en eski nesneyi havuza geri gönderip üzerine yazacağız.
                if (_count == _capacity)
                {
                    var oldFrame = _buffer[_head];
                    _buffer[_head] = null; // Bellek sızıntısını önlemek için kaldır
                    TelemetryFramePool.Return(oldFrame); // Havuza geri kazandır

                    _head = (_head + 1) % _capacity;
                    _count--;
                }

                _buffer[_tail] = frame;
                _tail = (_tail + 1) % _capacity;
                _count++;
            }
        }

        /// <summary>
        /// En eski veriyi okur ve kuyruktan çıkarır. Veri yoksa null döner.
        /// </summary>
        public TelemetryFrame Dequeue()
        {
            lock (_lock)
            {
                if (_count == 0) return null;

                var frame = _buffer[_head];
                _buffer[_head] = null;
                _head = (_head + 1) % _capacity;
                _count--;
                return frame;
            }
        }

        /// <summary>
        /// Tüm buffer verilerini sırasıyla kopyalayarak listeler.
        /// </summary>
        public List<TelemetryFrame> GetAll()
        {
            lock (_lock)
            {
                var result = new List<TelemetryFrame>(_count);
                for (int i = 0; i < _count; i++)
                {
                    int index = (_head + i) % _capacity;
                    result.Add(_buffer[index]); // NOT: nesne referansı havuzlanmış olabilir, dikkatli olunmalıdır
                }
                return result;
            }
        }

        /// <summary>
        /// Buffer içeriğini temizler ve tüm nesneleri havuza geri kazandırır.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                for (int i = 0; i < _capacity; i++)
                {
                    if (_buffer[i] != null)
                    {
                        TelemetryFramePool.Return(_buffer[i]);
                        _buffer[i] = null;
                    }
                }
                _head = 0;
                _tail = 0;
                _count = 0;
            }
        }
    }
}
