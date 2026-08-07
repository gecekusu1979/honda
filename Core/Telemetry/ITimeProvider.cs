using System;
using System.Diagnostics;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri zaman damgası standart kaynağı.
    /// Farklı zamanlama modlarını (gerçek zamanlı, dosya oynatma, simülasyon) destekler.
    /// </summary>
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
        long MonotonicTicks { get; }
        double GetElapsedTime(long startTicks);
        void SetElapsedTime(double seconds); // ReplayClock veya simülatör zaman kaydırmaları için
    }

    public class SystemClock : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public long MonotonicTicks => DateTime.UtcNow.Ticks;
        public double GetElapsedTime(long startTicks) => (double)(MonotonicTicks - startTicks) / 10000000.0;
        public void SetElapsedTime(double seconds) { }
    }

    public class HighResolutionClock : ITimeProvider
    {
        private static readonly double TicksToSeconds = 1.0 / Stopwatch.Frequency;

        public DateTime UtcNow => DateTime.UtcNow;
        public long MonotonicTicks => Stopwatch.GetTimestamp();

        public double GetElapsedTime(long startTicks)
        {
            long elapsedTicks = MonotonicTicks - startTicks;
            return elapsedTicks * TicksToSeconds;
        }

        public void SetElapsedTime(double seconds) { }
    }

    public class ReplayClock : ITimeProvider
    {
        private DateTime _utcNow = DateTime.UtcNow;
        private double _elapsedTime = 0.0;
        private long _monotonicOffset = 0;

        public DateTime UtcNow
        {
            get => _utcNow;
            set => _utcNow = value;
        }

        public long MonotonicTicks => _monotonicOffset;

        public double GetElapsedTime(long startTicks)
        {
            return _elapsedTime;
        }

        public void SetElapsedTime(double seconds)
        {
            _elapsedTime = seconds;
            _monotonicOffset = (long)(seconds * 1000.0); // ms hassasiyetinde tick
        }

        public void Advance(double seconds)
        {
            _elapsedTime += seconds;
            _utcNow = _utcNow.AddSeconds(seconds);
            _monotonicOffset += (long)(seconds * 1000.0);
        }
    }

    public class ExternalClock : ITimeProvider
    {
        private DateTime _utcNow = DateTime.UtcNow;
        private long _monotonicTicks = 0;
        private double _elapsedTime = 0.0;

        public DateTime UtcNow => _utcNow;
        public long MonotonicTicks => _monotonicTicks;

        public void Update(DateTime utcNow, long monotonicTicks, double elapsedTime)
        {
            _utcNow = utcNow;
            _monotonicTicks = monotonicTicks;
            _elapsedTime = elapsedTime;
        }

        public double GetElapsedTime(long startTicks) => _elapsedTime;
        public void SetElapsedTime(double seconds) => _elapsedTime = seconds;
    }
}
