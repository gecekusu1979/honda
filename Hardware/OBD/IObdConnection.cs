namespace HondaTuner.Hardware.OBD
{
    /// <summary>
    /// OBD1 bağlantı arayüzü — seri port üzerinden canlı veri okuma.
    /// </summary>
    public interface IObdConnection : Core.Interfaces.IHardwareDevice
    {
        void Open(string portName, int baudRate);
        Core.Interfaces.TelemetryFrameData ReadFrame();
    }
}
