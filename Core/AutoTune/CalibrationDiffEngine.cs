using System;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationDiffEngine : ICalibrationDiffEngine
    {
        public CalibrationDiffResult GenerateDiff(string mapName, int row, int col, double before, double after, ParameterType param)
        {
            double deltaVal = after - before;
            double deltaPct = 0.0;
            if (Math.Abs(before) > 0.0001)
            {
                deltaPct = (deltaVal / before) * 100.0;
            }

            return new CalibrationDiffResult
            {
                ParameterName = param.ToString(),
                MapName = mapName ?? "UnknownTable",
                Row = row,
                Col = col,
                BeforeValue = before,
                AfterValue = after,
                DeltaValue = deltaVal,
                DeltaPercent = deltaPct
            };
        }
    }
}
