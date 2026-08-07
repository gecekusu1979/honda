using System;
using System.Collections.Generic;
using HondaTuner.Calibration.Maps;
using HondaTuner.Calibration.Interpolation;

namespace HondaTuner.Core.Algorithms
{
    // ── Bilinear Trace Sonucu ──────────────────────────────────────
    public class TraceResult
    {
        public int ActiveRow { get; }
        public int ActiveCol { get; }

        /// <summary>Bilinear interpolasyon komşu ağırlıkları (toplam = 1.0)</summary>
        public List<CellWeight> Weights { get; }

        /// <summary>Eski uyumluluk — komşu koordinat listesi</summary>
        public List<Tuple<int, int>> Neighbors { get; }

        /// <summary>Kesirli satır pozisyonu (0.0 – rows-1)</summary>
        public double FractionalRow { get; }

        /// <summary>Kesirli sütun pozisyonu (0.0 – cols-1)</summary>
        public double FractionalCol { get; }

        public TraceResult(int activeRow, int activeCol,
                           List<CellWeight> weights,
                           List<Tuple<int, int>> neighbors,
                           double fractionalRow, double fractionalCol)
        {
            ActiveRow = activeRow;
            ActiveCol = activeCol;
            Weights = weights;
            Neighbors = neighbors;
            FractionalRow = fractionalRow;
            FractionalCol = fractionalCol;
        }
    }

    // ── Hücre Ağırlığı ────────────────────────────────────────────
    public class CellWeight
    {
        public int Row { get; set; }
        public int Col { get; set; }

        /// <summary>Bu hücrenin bilinear ağırlığı (0.0–1.0)</summary>
        public double Weight { get; set; }
    }

    // ── Hücre İstatistikleri ──────────────────────────────────────
    public class CellStats
    {
        public int HitCount { get; set; }
        public double TotalDwellMs { get; set; }
        public double MinAfr { get; set; } = double.MaxValue;
        public double MaxAfr { get; set; } = double.MinValue;
        public double SumAfr { get; set; }
        public double AvgAfr => HitCount > 0 ? SumAfr / HitCount : 0;

        public void RecordHit(double dwellMs, double afr)
        {
            HitCount++;
            TotalDwellMs += dwellMs;
            SumAfr += afr;
            if (afr < MinAfr) MinAfr = afr;
            if (afr > MaxAfr) MaxAfr = afr;
        }

        public void Reset()
        {
            HitCount = 0;
            TotalDwellMs = 0;
            MinAfr = double.MaxValue;
            MaxAfr = double.MinValue;
            SumAfr = 0;
        }
    }

    // ── Ana Motor ─────────────────────────────────────────────────
    public class VisualTraceEngine
    {
        private CellStats[,] _stats;
        private DateTime _lastTraceTime = DateTime.MinValue;

        /// <summary>Hücre istatistik matrisi</summary>
        public CellStats[,] Stats => _stats;

        /// <summary>İstatistik matrisini (yeniden) başlatır.</summary>
        public void InitStats(int rows, int cols)
        {
            _stats = new CellStats[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    _stats[r, c] = new CellStats();
            _lastTraceTime = DateTime.UtcNow;
        }

        /// <summary>Tüm istatistikleri sıfırlar.</summary>
        public void ResetStats()
        {
            if (_stats == null) return;
            for (int r = 0; r < _stats.GetLength(0); r++)
                for (int c = 0; c < _stats.GetLength(1); c++)
                    _stats[r, c].Reset();
        }

        /// <summary>
        /// Bilinear ağırlıklı hücre takibi yapar.
        /// MapDefinition, AxisDefinition ve InterpolationEngine kullanır.
        /// </summary>
        public static TraceResult TrackCell(double rpm, double load, TableDefinition table, IInterpolationEngine interpEngine)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (interpEngine == null) throw new ArgumentNullException(nameof(interpEngine));

            var result = interpEngine.Interpolate(rpm, load, table);

            var weights = new List<CellWeight>();
            for (int i = 0; i < 4; i++)
            {
                int rVal = result.NeighborCells[i][0];
                int cVal = result.NeighborCells[i][1];
                double wVal = result.CellWeights[i];
                if (wVal >= 0.0001)
                {
                    weights.Add(new CellWeight { Row = rVal, Col = cVal, Weight = wVal });
                }
            }

            var neighbors = new List<Tuple<int, int>>();
            int activeRow = result.ActiveRow;
            int activeCol = result.ActiveCol;

            for (int r = Math.Max(0, activeRow - 1); r <= Math.Min(table.Metadata.Rows - 1, activeRow + 1); r++)
            {
                for (int c = Math.Max(0, activeCol - 1); c <= Math.Min(table.Metadata.Columns - 1, activeCol + 1); c++)
                {
                    if (r == activeRow && c == activeCol) continue;
                    neighbors.Add(new Tuple<int, int>(r, c));
                }
            }

            int tlRow = result.NeighborCells[0][0];
            int tlCol = result.NeighborCells[0][1];

            return new TraceResult(
                activeRow,
                activeCol,
                weights,
                neighbors,
                tlRow + result.YRatio,
                tlCol + result.XRatio);
        }

        /// <summary>
        /// Eski uyumluluk sarmalayıcısı.
        /// </summary>
        public static TraceResult TrackCell(double rpm, double load, int[] rpmAxis, int[] loadAxis)
        {
            if (rpmAxis == null) throw new ArgumentNullException(nameof(rpmAxis));
            if (loadAxis == null) throw new ArgumentNullException(nameof(loadAxis));

            var xAxis = new AxisDefinition
            {
                Name = "RPM",
                Length = rpmAxis.Length,
                ConvertedValues = Array.ConvertAll(rpmAxis, x => (double)x)
            };
            var yAxis = new AxisDefinition
            {
                Name = "MAP",
                Length = loadAxis.Length,
                ConvertedValues = Array.ConvertAll(loadAxis, x => (double)x)
            };
            var def = new MapDefinition
            {
                MapName = "TraceCompatibilityMap",
                Rows = loadAxis.Length,
                Columns = rpmAxis.Length
            };

            var table = new TableDefinition(def, xAxis, yAxis);
            for (int r = 0; r < loadAxis.Length; r++)
                for (int c = 0; c < rpmAxis.Length; c++)
                    table.ConvertedCells[r, c] = 0.0;

            var interpEngine = new BilinearInterpolationEngine();
            return TrackCell(rpm, load, table, interpEngine);
        }

        /// <summary>
        /// Trace sonucunu hücre istatistiklerinde kaydeder.
        /// </summary>
        public void RecordTrace(TraceResult trace, double afr)
        {
            if (_stats == null || trace == null) return;

            var now = DateTime.UtcNow;
            double dwellMs = _lastTraceTime == DateTime.MinValue
                ? 100 // İlk çerçeve varsayılanı
                : (now - _lastTraceTime).TotalMilliseconds;
            _lastTraceTime = now;

            // Her ağırlığa göre orantılı kayıt
            foreach (var w in trace.Weights)
            {
                if (w.Row < 0 || w.Row >= _stats.GetLength(0)) continue;
                if (w.Col < 0 || w.Col >= _stats.GetLength(1)) continue;
                if (w.Weight <= 0.001) continue;

                _stats[w.Row, w.Col].RecordHit(dwellMs * w.Weight, afr);
            }
        }
    }
}
