using System;

namespace HondaTuner.Calibration.Maps
{
    /// <summary>
    /// Harita eksenini (örneğin RPM, MAP, TPS) temsil eden tanım.
    /// </summary>
    public class AxisDefinition
    {
        public string Name { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public double ScaleFactor { get; set; } = 1.0;
        public string Unit { get; set; } = "Raw";

        public byte[] RawValues { get; set; }
        public double[] ConvertedValues { get; set; }

        public double ConvertRawToValue(byte raw)
        {
            return (raw * ScaleFactor);
        }

        public byte ConvertValueToRaw(double val)
        {
            double raw = val / ScaleFactor;
            if (raw < 0) raw = 0;
            if (raw > 255) raw = 255;
            return (byte)Math.Round(raw);
        }
    }
}
