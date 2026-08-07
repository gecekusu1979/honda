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
        public string Vehicle { get; set; }
        public string Engine { get; set; }
        public string EcuCode { get; set; }
        public string TunerName { get; set; }
        public System.DateTime Date { get; set; }
        public byte[] OriginalRom { get; set; }
        public byte[] ModifiedRom { get; set; }
        public List<CalibrationChange> Changes { get; set; }
        public string Notes { get; set; }
    }
}
