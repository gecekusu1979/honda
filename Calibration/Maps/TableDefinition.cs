using System;

namespace HondaTuner.Calibration.Maps
{
    /// <summary>
    /// Eksenleri ve hücreleriyle birlikte bir kalibrasyon tablosunu sarmalar.
    /// </summary>
    public class TableDefinition
    {
        public MapDefinition Metadata { get; set; }
        public AxisDefinition XAxis { get; set; } // Genellikle RPM
        public AxisDefinition YAxis { get; set; } // Genellikle MAP veya TPS

        public byte[,] RawCells { get; set; }
        public double[,] ConvertedCells { get; set; }

        public TableDefinition(MapDefinition metadata, AxisDefinition xAxis, AxisDefinition yAxis)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            XAxis = xAxis ?? throw new ArgumentNullException(nameof(xAxis));
            YAxis = yAxis ?? throw new ArgumentNullException(nameof(yAxis));

            RawCells = new byte[metadata.Rows, metadata.Columns];
            ConvertedCells = new double[metadata.Rows, metadata.Columns];
        }

        public void RefreshConvertedCells()
        {
            for (int r = 0; r < Metadata.Rows; r++)
            {
                for (int c = 0; c < Metadata.Columns; c++)
                {
                    ConvertedCells[r, c] = RawCells[r, c] * Metadata.ScaleFactor + Metadata.OffsetValue;
                }
            }
        }

        public void RefreshRawCells()
        {
            for (int r = 0; r < Metadata.Rows; r++)
            {
                for (int c = 0; c < Metadata.Columns; c++)
                {
                    double rawVal = (ConvertedCells[r, c] - Metadata.OffsetValue) / Metadata.ScaleFactor;
                    if (rawVal < 0) rawVal = 0;
                    if (rawVal > 255) rawVal = 255;
                    RawCells[r, c] = (byte)Math.Round(rawVal);
                }
            }
        }
    }
}
