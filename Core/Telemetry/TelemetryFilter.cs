using System;
using System.Collections.Generic;
using System.Linq;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri sinyal filtreleme türleri.
    /// </summary>
    public enum FilterType
    {
        NoFilter,
        MovingAverage,
        Median,
        LowPass,
        HighPass
    }

    /// <summary>
    /// Telemetri kanalları için sinyal gürültüsü azaltma filtre arayüzü.
    /// </summary>
    public interface ITelemetryFilter
    {
        FilterType Type { get; }

        /// <summary>
        /// Gelen yeni değeri filtreler ve işlenmiş değeri geri döndürür.
        /// </summary>
        double Filter(double newValue);

        /// <summary>
        /// Filtre belleğini temizler.
        /// </summary>
        void Reset();
    }

    public static class TelemetryFilterFactory
    {
        public static ITelemetryFilter Create(FilterType type, double parameter = 0.0)
        {
            switch (type)
            {
                case FilterType.MovingAverage:
                    int windowSize = parameter > 0 ? (int)parameter : 5;
                    return new MovingAverageFilter(windowSize);
                case FilterType.Median:
                    int medWindow = parameter > 0 ? (int)parameter : 5;
                    return new MedianFilter(medWindow);
                case FilterType.LowPass:
                    double alpha = parameter > 0.0 && parameter <= 1.0 ? parameter : 0.2;
                    return new LowPassFilter(alpha);
                case FilterType.HighPass:
                    double hpAlpha = parameter > 0.0 && parameter <= 1.0 ? parameter : 0.8;
                    return new HighPassFilter(hpAlpha);
                case FilterType.NoFilter:
                default:
                    return new NoFilter();
            }
        }
    }

    public class NoFilter : ITelemetryFilter
    {
        public FilterType Type => FilterType.NoFilter;
        public double Filter(double newValue) => newValue;
        public void Reset() { }
    }

    public class MovingAverageFilter : ITelemetryFilter
    {
        private readonly int _windowSize;
        private readonly Queue<double> _history = new Queue<double>();
        private double _sum = 0.0;
        private readonly object _lock = new object();

        public FilterType Type => FilterType.MovingAverage;

        public MovingAverageFilter(int windowSize)
        {
            _windowSize = Math.Max(1, windowSize);
        }

        public double Filter(double newValue)
        {
            lock (_lock)
            {
                _history.Enqueue(newValue);
                _sum += newValue;

                if (_history.Count > _windowSize)
                {
                    _sum -= _history.Dequeue();
                }

                return _sum / _history.Count;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _history.Clear();
                _sum = 0.0;
            }
        }
    }

    public class MedianFilter : ITelemetryFilter
    {
        private readonly int _windowSize;
        private readonly List<double> _history = new List<double>();
        private readonly object _lock = new object();

        public FilterType Type => FilterType.Median;

        public MedianFilter(int windowSize)
        {
            _windowSize = Math.Max(1, windowSize);
        }

        public double Filter(double newValue)
        {
            lock (_lock)
            {
                _history.Add(newValue);
                if (_history.Count > _windowSize)
                {
                    _history.RemoveAt(0);
                }

                var sorted = new List<double>(_history);
                sorted.Sort();

                int count = sorted.Count;
                if (count % 2 == 1)
                {
                    return sorted[count / 2];
                }
                else
                {
                    return (sorted[(count / 2) - 1] + sorted[count / 2]) / 2.0;
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _history.Clear();
            }
        }
    }

    public class LowPassFilter : ITelemetryFilter
    {
        private readonly double _alpha;
        private double? _lastValue;
        private readonly object _lock = new object();

        public FilterType Type => FilterType.LowPass;

        public LowPassFilter(double alpha)
        {
            _alpha = Math.Clamp(alpha, 0.0, 1.0);
        }

        public double Filter(double newValue)
        {
            lock (_lock)
            {
                if (!_lastValue.HasValue)
                {
                    _lastValue = newValue;
                    return newValue;
                }

                double filtered = (_alpha * newValue) + ((1.0 - _alpha) * _lastValue.Value);
                _lastValue = filtered;
                return filtered;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _lastValue = null;
            }
        }
    }

    public class HighPassFilter : ITelemetryFilter
    {
        private readonly double _alpha;
        private double? _lastRaw;
        private double? _lastFiltered;
        private readonly object _lock = new object();

        public FilterType Type => FilterType.HighPass;

        public HighPassFilter(double alpha)
        {
            _alpha = Math.Clamp(alpha, 0.0, 1.0);
        }

        public double Filter(double newValue)
        {
            lock (_lock)
            {
                if (!_lastRaw.HasValue)
                {
                    _lastRaw = newValue;
                    _lastFiltered = newValue;
                    return newValue;
                }

                double filtered = _alpha * ((_lastFiltered.Value) + newValue - _lastRaw.Value);
                _lastRaw = newValue;
                _lastFiltered = filtered;
                return filtered;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _lastRaw = null;
                _lastFiltered = null;
            }
        }
    }
}
