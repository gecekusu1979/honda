using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri verilerini (anlık veya oturum bazlı) gerçek zamanlı analiz ederek
    /// vuruntu haritalama, yakıt hatası kestirimi veya yapay zeka tabanlı anomali algılama
    /// yapan sınıflar için analiz modülü arayüzüdür.
    /// </summary>
    public interface ITelemetryAnalyzer
    {
        string Name { get; }

        /// <summary>
        /// Gelen telemetri snapshot özetini analiz hattında işler.
        /// </summary>
        void Analyze(TelemetrySnapshot snapshot);
    }

    /// <summary>
    /// Birden fazla analizcisinin ardışık olarak (pipeline) çalışmasını sağlayan yönetim sınıfıdır.
    /// </summary>
    public class TelemetryAnalyzerPipeline
    {
        private readonly List<ITelemetryAnalyzer> _analyzers = new List<ITelemetryAnalyzer>();
        private readonly object _lock = new object();

        public void AddAnalyzer(ITelemetryAnalyzer analyzer)
        {
            if (analyzer == null) return;
            lock (_lock)
            {
                if (!_analyzers.Contains(analyzer))
                {
                    _analyzers.Add(analyzer);
                }
            }
        }

        public void RemoveAnalyzer(ITelemetryAnalyzer analyzer)
        {
            if (analyzer == null) return;
            lock (_lock)
            {
                _analyzers.Remove(analyzer);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _analyzers.Clear();
            }
        }

        /// <summary>
        /// Pipeline içindeki tüm analizcileri sırasıyla koşturur.
        /// </summary>
        public void Execute(TelemetrySnapshot snapshot)
        {
            if (snapshot == null) return;

            List<ITelemetryAnalyzer> copy;
            lock (_lock)
            {
                copy = new List<ITelemetryAnalyzer>(_analyzers);
            }

            foreach (var analyzer in copy)
            {
                try
                {
                    analyzer.Analyze(snapshot);
                }
                catch
                {
                    // Herhangi bir analizci hatası pipeline hatasına yol açmamalıdır.
                }
            }
        }
    }
}
