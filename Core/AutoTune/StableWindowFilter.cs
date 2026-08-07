using System;
using System.Collections.Generic;
using System.Linq;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune
{
    public class StableWindowFilter
    {
        private readonly List<TelemetrySnapshot> _window = new List<TelemetrySnapshot>();
        private readonly object _lockObj = new object();

        public int WindowSize { get; set; } = 10;

        // Stability standard deviation thresholds
        public double MaxRpmStdDev { get; set; } = 150.0;
        public double MaxMapStdDev { get; set; } = 2.5; // kPa
        public double MaxTpsStdDev { get; set; } = 1.0; // %
        public double MaxEctStdDev { get; set; } = 0.5; // °C

        public bool AddSnapshot(TelemetrySnapshot snapshot, out List<TelemetrySnapshot> stableWindow)
        {
            stableWindow = null;
            if (snapshot == null) return false;

            lock (_lockObj)
            {
                _window.Add(snapshot);
                if (_window.Count > WindowSize)
                {
                    _window.RemoveAt(0);
                }

                if (_window.Count == WindowSize)
                {
                    if (CheckStability(out double rpmSd, out double mapSd, out double tpsSd, out double ectSd))
                    {
                        stableWindow = new List<TelemetrySnapshot>(_window);
                        return true;
                    }
                }
            }

            return false;
        }

        public void Clear()
        {
            lock (_lockObj)
            {
                _window.Clear();
            }
        }

        private bool CheckStability(out double rpmSd, out double mapSd, out double tpsSd, out double ectSd)
        {
            rpmSd = CalculateStdDev(_window.Select(f => f.RPM).ToList());
            mapSd = CalculateStdDev(_window.Select(f => f.MAP).ToList());
            tpsSd = CalculateStdDev(_window.Select(f => f.TPS).ToList());
            ectSd = CalculateStdDev(_window.Select(f => f.ECT).ToList());

            return rpmSd <= MaxRpmStdDev &&
                   mapSd <= MaxMapStdDev &&
                   tpsSd <= MaxTpsStdDev &&
                   ectSd <= MaxEctStdDev;
        }

        private double CalculateStdDev(List<double> values)
        {
            if (values.Count < 2) return 0.0;
            double avg = values.Average();
            double sum = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sum / (values.Count - 1));
        }
    }
}
