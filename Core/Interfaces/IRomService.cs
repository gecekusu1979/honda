namespace HondaTuner.Core.Interfaces
{
    /// <summary>
    /// ROM dosya operasyonları için servis arayüzü.
    /// Mevcut RomParser mantığını sarmalayan DI uyumlu soyutlama.
    /// </summary>
    public interface IRomService
    {
        bool IsLoaded { get; }
        bool IsReadOnly { get; }
        EcuProfile Profile { get; }
        string FilePath { get; }
        HondaTuner.Core.Metadata.EcuMetadata Metadata { get; set; }
        byte[] GetBuffer();
        void SetBuffer(byte[] buffer);
        void LoadRom(string filePath, EcuProfile profile);
        void SaveRom(string filePath);
        byte[,] ReadFuelMap();
        void WriteFuelMap(byte[,] mapData);
        byte[,] ReadIgnitionMap();
        void WriteIgnitionMap(byte[,] mapData);
        void SaveMetadata(string filePath);
        void LoadMetadata(string filePath);
        HondaTuner.Core.AutoTune.PhysicalWriterAck WritePhysical(HondaTuner.Hardware.EEPROM.IEepromProgrammer programmer);
    }
}
