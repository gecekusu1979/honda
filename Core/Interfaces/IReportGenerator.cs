using System.Collections.Generic;

namespace HondaTuner.Core.Interfaces
{
    /// <summary>
    /// Tune raporu oluşturucu — HTML ve PDF formatları destekler.
    /// </summary>
    public interface IReportGenerator
    {
        string GenerateReport(TuningSessionInfo session);
        void SaveToFile(string filePath, TuningSessionInfo session);
    }

    public class TuningSessionInfo
    {
        public string Vehicle { get; set; } = null!;
        public string Engine { get; set; } = null!;
        public string EcuCode { get; set; } = null!;
        public string TunerName { get; set; } = null!;
        public System.DateTime Date { get; set; }
        public byte[] OriginalRom { get; set; } = null!;
        public byte[] ModifiedRom { get; set; } = null!;
        public List<CalibrationChange> Changes { get; set; } = null!;
        public string Notes { get; set; } = null!;
    }
}