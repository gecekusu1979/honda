namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri akışına abone olan sınıfların (UI, AutoTune, RTP, vb.) 
    /// uygulaması gereken standart veri tüketici arayüzüdür.
    /// </summary>
    public interface ITelemetryConsumer
    {
        /// <summary>
        /// Gelen telemetri data çerçevesini tüketir.
        /// </summary>
        void Consume(TelemetryFrame frame);

        /// <summary>
        /// Gelen sistem ve tanı olaylarını tüketir.
        /// </summary>
        void ConsumeEvent(TelemetryEvent busEvent);
    }
}
