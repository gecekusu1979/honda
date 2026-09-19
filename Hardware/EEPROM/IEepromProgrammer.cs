using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Rom.Patch;
using HondaTuner.Core.AutoTune;

namespace HondaTuner.Hardware.EEPROM
{
    /// <summary>
    /// EEPROM programlayıcı arayüzü.
    /// SST27SF512, 28C256, 29F512 chip desteği.
    /// </summary>
    public interface IEepromProgrammer : IHardwareDevice
    {
        byte[] ReadChip(int romLength);
        PhysicalWriterAck WriteChip(PatchTransaction transaction);
        void EraseChip();
        bool VerifyChip(byte[] expectedData);
    }
}
