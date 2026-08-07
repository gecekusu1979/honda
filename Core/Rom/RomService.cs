using System;
using System.IO;
using System.Linq;
using HondaTuner.Core.Container;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Core.Rom
{
    /// <summary>
    /// ROM Servisi — RomParser'ı sarmalayıp, DI bağımlılığı olarak sunan implementasyon.
    /// </summary>
    public class RomService : IRomService
    {
        private readonly RomParser _parser = new RomParser();

        public bool IsLoaded => _parser.IsLoaded;
        public EcuProfile Profile => _parser.Profile;
        public string FilePath => _parser.FilePath;
        public HondaTuner.Core.Metadata.EcuMetadata Metadata { get; set; } = new HondaTuner.Core.Metadata.EcuMetadata();

        public byte[] GetBuffer()
        {
            return _parser.GetRomBuffer();
        }

        public void SetBuffer(byte[] buffer)
        {
            _parser.SetRomBuffer(buffer);
        }

        public void LoadRom(string filePath, EcuProfile profile)
        {
            _parser.Load(filePath, profile);
            LoadMetadata(filePath);
        }

        public void SaveRom(string filePath)
        {
            var calService = ServiceContainer.Resolve<ICalibrationService>();
            // 1. Calibration Commit
            if (calService != null && calService.HasActiveTransaction)
            {
                calService.CommitTransaction();
            }

            byte[] buffer = GetBuffer();
            if (buffer == null || buffer.Length == 0)
                throw new InvalidOperationException("ROM içeriği yüklenmedi veya boş, kaydedilemez.");

            var checksumEngine = ServiceContainer.Resolve<Checksum.IChecksumEngine>();
            if (checksumEngine != null && Profile != null)
            {
                var defs = Profile.ChecksumDefinitions;
                if (defs != null && defs.Count > 0)
                {
                    // 2. Checksum Update (ve dolayısıyla Calculate içsel olarak)
                    foreach (var def in defs)
                    {
                        checksumEngine.Update(buffer, def);
                    }

                    // 3. Checksum Validate
                    if (!checksumEngine.VerifyBeforeSave(buffer, defs, out var results))
                    {
                        var invalidList = results.Where(r => !r.IsValid).ToList();
                        string failedMsgs = string.Join("; ", invalidList.Select(r => r.Message));
                        Logging.ApplicationLogger.Error("RomService", $"ROM Checksum doğrulaması başarısız oldu: {failedMsgs}");

                        // Audit log için CalibrationHistory'ye kayıt ekle
                        calService?.RecordChange(new CalibrationChange
                        {
                            Parameter = "ROM Save - Checksum Failure",
                            OldValue = "N/A",
                            NewValue = "Validation Failed: " + failedMsgs,
                            Offset = 0,
                            MapName = "System",
                            Source = "ChecksumEngine"
                        });

                        throw new InvalidOperationException($"Checksum doğrulaması başarısız olduğu için ROM kaydedilemez: {failedMsgs}");
                    }
                }
            }

            // 4. Set modified buffer back to parser
            SetBuffer(buffer);

            // 5. Write File
            _parser.Save(filePath);

            // 6. Write Companion Metadata File
            SaveMetadata(filePath);
        }

        public byte[,] ReadFuelMap()
        {
            return _parser.ReadFuelMap();
        }

        public void WriteFuelMap(byte[,] mapData)
        {
            _parser.WriteFuelMap(mapData);
        }

        public byte[,] ReadIgnitionMap()
        {
            return _parser.ReadIgnitionMap();
        }

        public void WriteIgnitionMap(byte[,] mapData)
        {
            _parser.WriteIgnitionMap(mapData);
        }

        public void LoadMetadata(string filePath)
        {
            try
            {
                string metaPath = Path.ChangeExtension(filePath, ".meta.json");
                if (File.Exists(metaPath))
                {
                    string json = File.ReadAllText(metaPath);
                    Metadata = HondaTuner.Core.Metadata.EcuMetadata.FromJson(json);
                }
                else
                {
                    Metadata = new HondaTuner.Core.Metadata.EcuMetadata();
                }
            }
            catch
            {
                Metadata = new HondaTuner.Core.Metadata.EcuMetadata();
            }
        }

        public void SaveMetadata(string filePath)
        {
            if (Metadata == null) return;
            try
            {
                string metaPath = Path.ChangeExtension(filePath, ".meta.json");
                string json = Metadata.ToJson();
                File.WriteAllText(metaPath, json);
            }
            catch (Exception ex)
            {
                Logging.ApplicationLogger.Error("RomService", $"Metadata kaydetme hatası: {ex.Message}");
            }
        }

        /// <summary>Doğrudan alt seviye parser'a erişim.</summary>
        public RomParser GetParser() => _parser;
    }
}
