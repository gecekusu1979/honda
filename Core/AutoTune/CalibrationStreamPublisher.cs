using System;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationStreamPublisher : ICalibrationStreamPublisher
    {
        public event Action<CalibrationStreamPayload> OnMessagePublished;

        public void PublishProposed(CalibrationStreamPayload payload)
        {
            if (payload == null) return;
            ApplicationLogger.Info("CalibrationStreamPublisher", $"Öneri yayınlandı: {payload.Parameter} adres: {payload.MapAddress}");
            OnMessagePublished?.Invoke(payload);
        }

        public void PublishApplied(CalibrationStreamPayload payload)
        {
            if (payload == null) return;
            ApplicationLogger.Info("CalibrationStreamPublisher", $"Uygulanan değişiklik yayınlandı: {payload.Parameter} adres: {payload.MapAddress}");
            OnMessagePublished?.Invoke(payload);
        }

        public void PublishRollback(string sessionId, string mapAddress, double rolledBackValue)
        {
            var payload = new CalibrationStreamPayload
            {
                SessionId = sessionId,
                Timestamp = DateTime.Now,
                MapAddress = mapAddress,
                OldValue = rolledBackValue,
                NewValue = rolledBackValue,
                Confidence = 100.0,
                SafetyStatus = "Allow",
                ApprovalStatus = "Applied",
                Parameter = ParameterType.Limit
            };
            ApplicationLogger.Info("CalibrationStreamPublisher", $"Geri alma yayınlandı: {mapAddress} -> {rolledBackValue}");
            OnMessagePublished?.Invoke(payload);
        }
    }
}
