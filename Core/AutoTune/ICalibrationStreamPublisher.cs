using System;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationStreamPayload
    {
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public ParameterType Parameter { get; set; }
        public string MapAddress { get; set; }
        public double OldValue { get; set; }
        public double NewValue { get; set; }
        public double Confidence { get; set; }
        public string SafetyStatus { get; set; }
        public string ApprovalStatus { get; set; }
    }

    public interface ICalibrationStreamPublisher
    {
        void PublishProposed(CalibrationStreamPayload payload);
        void PublishApplied(CalibrationStreamPayload payload);
        void PublishRollback(string sessionId, string mapAddress, double rolledBackValue);
        event Action<CalibrationStreamPayload> OnMessagePublished;
    }
}
