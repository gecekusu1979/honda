namespace HondaTuner.Hardware.EEPROM
{
    /// <summary>
    /// EEPROM programlayıcı arayüzü.
    /// SST27SF512, 28C256, 29F512 chip desteği.
    /// </summary>
    public interface IEepromProgrammer : Core.Interfaces.IHardwareDevice
    {
        byte[] ReadChip(int romLength);
        HondaTuner.Core.AutoTune.PhysicalWriterAck WriteChip(Core.Rom.Patch.PatchTransaction transaction);
        void EraseChip();
        bool VerifyChip(byte[] expectedData);
    }
}
