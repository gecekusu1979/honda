using HondaTuner.Core.Interfaces;

namespace HondaTuner.Hardware.OBD
{
    /// <summary>
    /// OBD1 bağlantı arayüzü — seri port üzerinden canlı veri okuma.
    /// </summary>
    public interface IObdConnection : IHardwareDevice
    {
        void Open(string portName, int baudRate);
        TelemetryFrameData ReadFrame();
    }
}
