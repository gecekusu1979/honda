namespace HondaTuner.Hardware.Emulator
{
    /// <summary>
    /// RTP (Real-Time Programming) emülatör arayüzü.
    /// Motor çalışırken gerçek zamanlı kalibrasyon desteği sağlar.
    /// </summary>
    public interface IEmulator : Core.Interfaces.IHardwareDevice
    {
        byte ReadByte(int offset);
        void WriteByte(int offset, byte value);
        byte[] ReadBlock(int offset, int length);
        void WriteBlock(int offset, byte[] data);
    }
}
