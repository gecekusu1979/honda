using System;

namespace HondaTuner.Calibration.Maps
{
    /// <summary>
    /// Kalibrasyon tablosunun (Map) meta veri tanımını barındırır.
    /// </summary>
    public class MapDefinition
    {
        public string MapName { get; set; }
        public string EcuCompatibility { get; set; }
        public int Offset { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public string DataType { get; set; } = "Byte"; // Byte, Word vb.
        public string ByteOrder { get; set; } = "LittleEndian";
        public double ScaleFactor { get; set; } = 1.0;
        public double OffsetValue { get; set; } = 0.0;
        public string Unit { get; set; } = "Raw";
        public double MinimumValue { get; set; } = double.MinValue;
        public double MaximumValue { get; set; } = double.MaxValue;
    }
}
