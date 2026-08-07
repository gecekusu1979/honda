using System;

namespace HondaTuner.Calibration.Maps
{
    /// <summary>
    /// Diğer genel ayar tabloları için yedek harita (Generic Map) sınıfı.
    /// </summary>
    public class GenericMap : TableDefinition
    {
        public GenericMap(MapDefinition metadata, AxisDefinition xAxis, AxisDefinition yAxis)
            : base(metadata, xAxis, yAxis)
        {
            RefreshConvertedCells();
        }
    }
}
