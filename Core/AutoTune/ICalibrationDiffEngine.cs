namespace HondaTuner.Core.AutoTune
{
    public class CalibrationDiffResult
    {
        public string ParameterName { get; set; }
        public string MapName { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public double BeforeValue { get; set; }
        public double AfterValue { get; set; }
        public double DeltaValue { get; set; }
        public double DeltaPercent { get; set; }
    }

    public interface ICalibrationDiffEngine
    {
        CalibrationDiffResult GenerateDiff(string mapName, int row, int col, double before, double after, ParameterType param);
    }
}
