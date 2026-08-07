namespace HondaTuner.Core.Interfaces
{
    /// <summary>
    /// AutoTune motoru — Wideband AFR verisine göre düzeltme önerir.
    /// Otomatik ROM yazma yapmaz, kullanıcı onayı gerektirir.
    /// </summary>
    public interface IAutoTuneEngine
    {
        CorrectionSuggestion ProcessFrame(TelemetryFrameData frame, double targetAfr);
        bool IsEnabled { get; set; }
    }

    public class TelemetryFrameData
    {
        public double Rpm { get; set; }
        public double Map { get; set; }
        public double Tps { get; set; }
        public double Afr { get; set; }
        public double Ect { get; set; }
        public double Iat { get; set; }
        public double BatteryVolts { get; set; }
        public double InjDuty { get; set; }
        public double IgnAdvance { get; set; }
        public bool VtecActive { get; set; }
    }

    public class CorrectionSuggestion
    {
        public int TargetRow { get; set; }
        public int TargetCol { get; set; }
        public double PercentAdjustment { get; set; }
        public string Direction { get; set; } // "Richen" or "Lean"
        public bool IsValid { get; set; }
        public string RejectionReason { get; set; }
    }
}
