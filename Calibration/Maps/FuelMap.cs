using System;

namespace HondaTuner.Calibration.Maps
{
    /// <summary>
    /// Yakıt Haritası (Fuel Map) implementasyonu. 8x8, 12x12, 16x16, 20x20 boyutlarını destekler.
    /// </summary>
    public class FuelMap : TableDefinition
    {
        public FuelMap(MapDefinition metadata, AxisDefinition xAxis, AxisDefinition yAxis)
            : base(metadata, xAxis, yAxis)
        {
            // Boyut kontrolü
            int r = metadata.Rows;
            int c = metadata.Columns;
            if ((r != 8 || c != 8) && (r != 12 || c != 12) && (r != 16 || c != 16) && (r != 20 || c != 20))
            {
                throw new ArgumentException($"Desteklenmeyen Fuel Map boyutu: {r}x{c}. Sadece 8x8, 12x12, 16x16, 20x20 desteklenir.");
            }
            RefreshConvertedCells();
        }

        /// <summary>
        /// Raw byte -> Scaled value -> Engineering unit (%) dönüşüm zinciri
        /// </summary>
        public double GetFuelPercentage(int row, int col)
        {
            if (row < 0 || row >= Metadata.Rows || col < 0 || col >= Metadata.Columns)
                throw new ArgumentOutOfRangeException("Hücre dizini harita sınırları dışındadır.");

            // Mühendislik birimi olarak yüzde (%) cinsinden dönüşüm
            // Örneğin: rawVal * 0.1
            return ConvertedCells[row, col];
        }

        public void SetFuelPercentage(int row, int col, double percentValue)
        {
            if (row < 0 || row >= Metadata.Rows || col < 0 || col >= Metadata.Columns)
                throw new ArgumentOutOfRangeException("Hücre dizini harita sınırları dışındadır.");

            if (percentValue < Metadata.MinimumValue || percentValue > Metadata.MaximumValue)
                throw new ArgumentOutOfRangeException(nameof(percentValue), $"Değer izin verilen sınırlar ({Metadata.MinimumValue} - {Metadata.MaximumValue}) dışındadır.");

            ConvertedCells[row, col] = percentValue;
            RefreshRawCells();
        }
    }
}
