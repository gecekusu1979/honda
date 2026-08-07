using System;

namespace HondaTuner.Calibration.Maps
{
    /// <summary>
    /// Ateşleme Haritası (Ignition Map) implementasyonu. 
    /// Avans dereceleri (advance degree), negatif ateşleme zamanlaması (negative timing) ve ondalık hassasiyeti (decimal precision) destekler.
    /// </summary>
    public class IgnitionMap : TableDefinition
    {
        public IgnitionMap(MapDefinition metadata, AxisDefinition xAxis, AxisDefinition yAxis)
            : base(metadata, xAxis, yAxis)
        {
            RefreshConvertedCells();
        }

        /// <summary>
        /// Ateşleme avansı açısını derece cinsinden alır (negatif değerler gecikmeyi -retard- temsil eder).
        /// </summary>
        public double GetIgnitionTiming(int row, int col)
        {
            if (row < 0 || row >= Metadata.Rows || col < 0 || col >= Metadata.Columns)
                throw new ArgumentOutOfRangeException("Hücre dizini harita sınırları dışındadır.");

            return ConvertedCells[row, col];
        }

        public void SetIgnitionTiming(int row, int col, double timingDegrees)
        {
            if (row < 0 || row >= Metadata.Rows || col < 0 || col >= Metadata.Columns)
                throw new ArgumentOutOfRangeException("Hücre dizini harita sınırları dışındadır.");

            if (timingDegrees < Metadata.MinimumValue || timingDegrees > Metadata.MaximumValue)
                throw new ArgumentOutOfRangeException(nameof(timingDegrees), $"Değer izin verilen sınırlar ({Metadata.MinimumValue} - {Metadata.MaximumValue}) dışındadır.");

            ConvertedCells[row, col] = timingDegrees;
            RefreshRawCells();
        }
    }
}
