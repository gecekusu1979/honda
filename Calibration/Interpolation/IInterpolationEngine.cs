using System;
using HondaTuner.Calibration.Maps;

namespace HondaTuner.Calibration.Interpolation
{
    public class InterpolationResult
    {
        public int ActiveRow { get; set; }
        public int ActiveCol { get; set; }
        public double XRatio { get; set; }
        public double YRatio { get; set; }

        // 4 Komşu hücre: [Row, Col] şeklinde
        public int[][] NeighborCells { get; set; }
        // 4 Komşu ağırlığı (TL, TR, BL, BR)
        public double[] CellWeights { get; set; }
        public double CalculatedValue { get; set; }
        public double Confidence { get; set; } = 1.0;
    }

    /// <summary>
    /// Harita aramaları ve bilinear enterpolasyon için ortak ara yüz.
    /// </summary>
    public interface IInterpolationEngine
    {
        InterpolationResult Interpolate(double rpm, double load, TableDefinition table);
    }
}
