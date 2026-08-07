using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HondaTuner.Core;
using HondaTuner.Core.Logging;

namespace HondaTuner.Database
{
    /// <summary>
    /// JSON tabanlı ECU profillerini yöneten sınıf.
    /// Farklı JSON dosyalarını tarayarak dinamik olarak veritabanını genişletir.
    /// </summary>
    public class EcuDatabaseManager
    {
        private static readonly object LockObj = new object();
        private static EcuDatabaseManager _instance;
        private readonly List<EcuProfile> _dynamicProfiles = new List<EcuProfile>();
        private readonly List<JsonProfileData> _rawJsonProfiles = new List<JsonProfileData>();

        public static EcuDatabaseManager Instance
        {
            get
            {
                lock (LockObj)
                {
                    return _instance ??= new EcuDatabaseManager();
                }
            }
        }

        /// <summary>
        /// Geriye uyumluluk için statik çağrı. Dizin içindeki profilleri yükler ve liste olarak döner.
        /// </summary>
        public static List<EcuProfile> LoadProfilesFromDirectory(string directory)
        {
            Instance.LoadDatabase(directory);
            var list = new List<EcuProfile>();
            list.AddRange(Instance.DynamicProfiles);
            if (list.Count == 0)
            {
                list.AddRange(EcuProfiles.All);
            }
            return list;
        }

        public IReadOnlyList<EcuProfile> DynamicProfiles => _dynamicProfiles;
        public IReadOnlyList<JsonProfileData> RawJsonProfiles => _rawJsonProfiles;

        private EcuDatabaseManager() { }

        /// <summary>
        /// Belirtilen klasördeki tüm .json profil dosyalarını tarar ve yükler.
        /// </summary>
        public void LoadDatabase(string databaseDirectory)
        {
            var tempProfiles = new List<EcuProfile>();
            var tempRaw = new List<JsonProfileData>();

            if (!Directory.Exists(databaseDirectory))
            {
                ApplicationLogger.Warn("EcuDatabaseManager", $"Veritabanı dizini bulunamadı: {databaseDirectory}");
                Directory.CreateDirectory(databaseDirectory);
            }

            var files = Directory.GetFiles(databaseDirectory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    LoadProfilesFromJsonInternal(content, tempProfiles, tempRaw);
                }
                catch (Exception ex)
                {
                    ApplicationLogger.Error("EcuDatabaseManager", $"Dosya yükleme hatası ({Path.GetFileName(file)}): {ex.Message}");
                }
            }

            lock (LockObj)
            {
                _dynamicProfiles.Clear();
                _dynamicProfiles.AddRange(tempProfiles);

                _rawJsonProfiles.Clear();
                _rawJsonProfiles.AddRange(tempRaw);
            }

            ApplicationLogger.Info("EcuDatabaseManager", $"Toplam {tempProfiles.Count} dinamik ECU profili yüklendi.");
        }

        /// <summary>
        /// JSON içeriğinden profilleri yükler. Tek nesne veya nesne dizisi destekler.
        /// </summary>
        public void LoadProfilesFromJson(string jsonContent)
        {
            lock (LockObj)
            {
                LoadProfilesFromJsonInternal(jsonContent, _dynamicProfiles, _rawJsonProfiles);
            }
        }

        private void LoadProfilesFromJsonInternal(string jsonContent, List<EcuProfile> targetProfiles, List<JsonProfileData> targetRaw)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                if (jsonContent.TrimStart().StartsWith("["))
                {
                    var profiles = JsonSerializer.Deserialize<List<JsonProfileData>>(jsonContent, options);
                    if (profiles != null)
                    {
                        foreach (var p in profiles)
                        {
                            AddRawProfileInternal(p, targetProfiles, targetRaw);
                        }
                    }
                }
                else
                {
                    var profile = JsonSerializer.Deserialize<JsonProfileData>(jsonContent, options);
                    if (profile != null)
                    {
                        AddRawProfileInternal(profile, targetProfiles, targetRaw);
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("EcuDatabaseManager", $"JSON ayrıştırma hatası: {ex.Message}");
            }
        }

        private void AddRawProfileInternal(JsonProfileData p, List<EcuProfile> targetProfiles, List<JsonProfileData> targetRaw)
        {
            if (p == null || string.IsNullOrEmpty(p.EcuCode)) return;

            var mapped = p.ToEcuProfile();
            var existingProfile = targetProfiles.Find(x => x.EcuCode.Equals(mapped.EcuCode, StringComparison.OrdinalIgnoreCase));
            if (existingProfile != null)
            {
                int existingValue = (existingProfile.Maps?.Count ?? 0) + (existingProfile.SupportedPatches?.Count ?? 0);
                int newValue = (mapped.Maps?.Count ?? 0) + (mapped.SupportedPatches?.Count ?? 0);
                if (existingValue > newValue)
                {
                    // Mevcut profil daha detaylı (harita veya yama sayısı daha fazla), üzerine yazma
                    return;
                }
            }

            // Mükerrer kayıtlardan kurtul
            targetRaw.RemoveAll(x => x.EcuCode == p.EcuCode);
            targetRaw.Add(p);

            targetProfiles.RemoveAll(x => x.EcuCode == mapped.EcuCode);
            targetProfiles.Add(mapped);
        }

        private void AddRawProfile(JsonProfileData p)
        {
            lock (LockObj)
            {
                AddRawProfileInternal(p, _dynamicProfiles, _rawJsonProfiles);
            }
        }

        /// <summary>
        /// EcuCode bazında dinamik profili getirir.
        /// </summary>
        public EcuProfile GetProfile(string ecuCode)
        {
            lock (LockObj)
            {
                return _dynamicProfiles.Find(x => x.EcuCode.Equals(ecuCode, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    /// <summary>
    /// ecu_database.json dosyasının birebir C# veri yapısı eşleşmesi.
    /// Supports legacy keys in separate profile jsons as well.
    /// </summary>
    public class JsonProfileData
    {
        public string EcuCode { get; set; }
        public string EngineCode { get; set; }
        public int RomSize { get; set; }
        public System.Text.Json.JsonElement Checksum { get; set; }
        public System.Text.Json.JsonElement ChecksumOffset { get; set; }
        public int FuelMapOffset { get; set; }
        public int FuelMapRows { get; set; }
        public int FuelMapColumns { get; set; }
        public int FuelMapCols { get; set; }
        public int FuelAxisOffset { get; set; }
        public int IgnitionOffset { get; set; }
        public int IgnMapOffset { get; set; }
        public int IgnitionRows { get; set; }
        public int IgnMapRows { get; set; }
        public int IgnitionColumns { get; set; }
        public int IgnMapCols { get; set; }
        public int IgnitionAxisOffset { get; set; }
        public int VtecOffset { get; set; }
        public int VtecRpmOffset { get; set; }
        public int RevLimitOffset { get; set; }
        public int SpeedLimitOffset { get; set; }
        public int KnockOffset { get; set; }
        public int InjectorOffset { get; set; }
        public int IdleOffset { get; set; }
        public bool? HasVtec { get; set; }
        public bool? HasIab { get; set; }
        public Dictionary<string, bool> Capabilities { get; set; }
        public string ChecksumAlgorithm { get; set; }
        public int[] SignatureBytes { get; set; }
        public string HeaderPattern { get; set; }
        public List<string> SupportedFeatures { get; set; }
        public List<string> RomLayouts { get; set; }
        public List<MapDefinitionJson> Maps { get; set; }
        public ChecksumJsonData ChecksumData { get; set; }
        public List<ChecksumJsonData> Checksums { get; set; }
        public List<EcuPatchMappingJson> SupportedPatches { get; set; }


        public EcuProfile ToEcuProfile()
        {
            // Ortak Eksen Sabitleri
            int[] stdRpmAxis = { 500, 750, 1000, 1250, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000 };
            int[] stdLoadAxis = { 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170 };

            // B-serisi için yüksek RPM bandı
            int[] bSeriesRpmAxis = { 500, 750, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500 };
            int[] selectedRpmAxis = EngineCode != null && EngineCode.StartsWith("B", StringComparison.OrdinalIgnoreCase)
                ? bSeriesRpmAxis
                : stdRpmAxis;

            bool hasVtec = (Capabilities != null && Capabilities.TryGetValue("vtec", out bool v) && v) || (HasVtec ?? false);
            bool hasIab = (Capabilities != null && Capabilities.TryGetValue("iab", out bool i) && i) || (HasIab ?? false);

            int resolvedChecksumOffset = 0;
            if (Checksum.ValueKind != System.Text.Json.JsonValueKind.Undefined && Checksum.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                resolvedChecksumOffset = GetChecksumOffset(Checksum);
            }
            else if (ChecksumOffset.ValueKind != System.Text.Json.JsonValueKind.Undefined && ChecksumOffset.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                resolvedChecksumOffset = GetChecksumOffset(ChecksumOffset);
            }

            var profile = new EcuProfile(
                name: $"{EcuCode} / {EngineCode} — {HeaderPattern ?? "Generic"}",
                ecuCode: EcuCode,
                engineCode: EngineCode,
                casaTag: $"Dynamic Profile {EcuCode}",
                hasVtec: hasVtec,
                hasIab: hasIab,
                romSize: RomSize,
                fuelMapOffset: FuelMapOffset,
                fuelMapRows: FuelMapRows,
                fuelMapCols: FuelMapColumns > 0 ? FuelMapColumns : (FuelMapCols > 0 ? FuelMapCols : 16),
                ignMapOffset: IgnitionOffset > 0 ? IgnitionOffset : (IgnMapOffset > 0 ? IgnMapOffset : 7744),
                ignMapRows: IgnitionRows > 0 ? IgnitionRows : (IgnMapRows > 0 ? IgnMapRows : 16),
                ignMapCols: IgnitionColumns > 0 ? IgnitionColumns : (IgnMapCols > 0 ? IgnMapCols : 16),
                rpmAxis: selectedRpmAxis,
                loadAxis: stdLoadAxis,
                vtecRpmOffset: VtecOffset > 0 ? VtecOffset : VtecRpmOffset,
                vtecRpmMin: hasVtec ? 1000 : 0,
                vtecRpmMax: hasVtec ? 9000 : 0,
                vtecRpmDefault: hasVtec ? 4800 : 0,
                revLimitOffset: RevLimitOffset,
                revLimitMin: 4000,
                revLimitMax: 10000,
                revLimitDefault: 7200,
                checksumOffset: resolvedChecksumOffset
            );

            // V2 dynamic fields mapping
            if (SignatureBytes != null)
            {
                byte[] sigBytes = new byte[SignatureBytes.Length];
                for (int idx = 0; idx < SignatureBytes.Length; idx++)
                {
                    sigBytes[idx] = (byte)SignatureBytes[idx];
                }
                profile.SignatureBytes = sigBytes;
            }
            else
            {
                profile.SignatureBytes = Array.Empty<byte>();
            }

            profile.ChecksumAlgorithm = ChecksumAlgorithm ?? "Xor8";
            profile.SpeedLimiterOffset = SpeedLimitOffset != 0 ? SpeedLimitOffset : 0x1FAC;
            profile.KnockOffset = KnockOffset != 0 ? KnockOffset : 0x1FB6;
            profile.InjectorOffset = InjectorOffset != 0 ? InjectorOffset : 0x1D80;
            profile.IdleOffset = IdleOffset != 0 ? IdleOffset : 0x1E80;
            profile.HeaderPattern = HeaderPattern ?? string.Empty;
            profile.FuelAxisOffset = FuelAxisOffset != 0 ? FuelAxisOffset : (FuelMapOffset - 64);
            profile.IgnitionAxisOffset = IgnitionAxisOffset != 0 ? IgnitionAxisOffset : (profile.IgnMapOffset - 64);

            // Maps listesi dolduruluyor
            if (Maps != null)
            {
                foreach (var m in Maps)
                {
                    int offsetVal = 0;
                    if (!string.IsNullOrEmpty(m.Offset))
                    {
                        if (m.Offset.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            offsetVal = Convert.ToInt32(m.Offset.Substring(2), 16);
                        else
                            offsetVal = Convert.ToInt32(m.Offset);
                    }

                    profile.Maps.Add(new HondaTuner.Calibration.Maps.MapDefinition
                    {
                        MapName = m.Name,
                        EcuCompatibility = EcuCode,
                        Offset = offsetVal,
                        Rows = m.Rows,
                        Columns = m.Columns,
                        ScaleFactor = m.Scale,
                        Unit = m.Unit,
                        MinimumValue = m.Name.Contains("Fuel") ? 0 : -10,
                        MaximumValue = m.Name.Contains("Fuel") ? 25.5 : 60
                    });
                }
            }

            if (profile.Maps.Count == 0)
            {
                profile.Maps.Add(new HondaTuner.Calibration.Maps.MapDefinition
                {
                    MapName = "Fuel",
                    EcuCompatibility = EcuCode,
                    Offset = FuelMapOffset,
                    Rows = FuelMapRows > 0 ? FuelMapRows : 16,
                    Columns = FuelMapColumns > 0 ? FuelMapColumns : 16,
                    ScaleFactor = 0.1,
                    Unit = "Percent",
                    MinimumValue = 0,
                    MaximumValue = 25.5
                });
                profile.Maps.Add(new HondaTuner.Calibration.Maps.MapDefinition
                {
                    MapName = "Ignition",
                    EcuCompatibility = EcuCode,
                    Offset = IgnitionOffset,
                    Rows = IgnitionRows > 0 ? IgnitionRows : 16,
                    Columns = IgnitionColumns > 0 ? IgnitionColumns : 16,
                    ScaleFactor = 0.25,
                    Unit = "Degrees",
                    MinimumValue = -10,
                    MaximumValue = 60
                });
            }

            // ChecksumDefinitions dolduruluyor
            var checksumList = new List<ChecksumJsonData>();
            if (Checksums != null)
            {
                checksumList.AddRange(Checksums);
            }
            if (ChecksumData != null)
            {
                checksumList.Add(ChecksumData);
            }

            foreach (var c in checksumList)
            {
                var algo = HondaTuner.Core.Rom.Checksum.ChecksumAlgorithm.Xor8;
                if (Enum.TryParse<HondaTuner.Core.Rom.Checksum.ChecksumAlgorithm>(c.Algorithm, true, out var parsedAlgo))
                {
                    algo = parsedAlgo;
                }

                var def = new HondaTuner.Core.Rom.Checksum.ChecksumDefinition
                {
                    ChecksumType = c.ChecksumType ?? "Main",
                    Algorithm = algo,
                    ChecksumAddress = ParseOffset(c.Address),
                    ChecksumSize = c.Size > 0 ? c.Size : 1,
                    RangeStart = ParseOffset(c.RangeStart),
                    RangeEnd = ParseOffset(c.RangeEnd),
                    ByteOrder = c.ByteOrder ?? "LittleEndian",
                    ExpectedValue = c.ExpectedValue,
                    Seed = (byte)c.Seed
                };

                if (c.ExcludeRanges != null)
                {
                    foreach (var er in c.ExcludeRanges)
                    {
                        def.ExcludeRanges.Add(new HondaTuner.Core.Rom.Checksum.ExcludeRange
                        {
                            Start = ParseOffset(er.Start),
                            End = ParseOffset(er.End)
                        });
                    }
                }

                profile.ChecksumDefinitions.Add(def);
            }

            if (profile.ChecksumDefinitions.Count == 0)
            {
                profile.ChecksumDefinitions.Add(new HondaTuner.Core.Rom.Checksum.ChecksumDefinition
                {
                    ChecksumType = "Main",
                    Algorithm = HondaTuner.Core.Rom.Checksum.ChecksumAlgorithm.Xor8,
                    ChecksumAddress = profile.ChecksumOffset > 0 ? profile.ChecksumOffset : 0x7FFF,
                    ChecksumSize = 1,
                    RangeStart = 0x0000,
                    RangeEnd = profile.ChecksumOffset > 0 ? profile.ChecksumOffset - 1 : 0x7FFE
                });
            }

            if (this.SupportedPatches != null)
            {
                foreach (var sp in this.SupportedPatches)
                {
                    profile.SupportedPatches.Add(new HondaTuner.Core.Rom.Patch.EcuPatchMapping
                    {
                        PatchId = sp.PatchId,
                        Offset = ParseOffset(sp.Offset),
                        RequiredFeature = sp.RequiredFeature
                    });
                }
            }

            return profile;
        }

        private static int ParseOffset(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt32(s.Substring(2), 16);
            return Convert.ToInt32(s);
        }

        private static int GetChecksumOffset(System.Text.Json.JsonElement el)
        {
            if (el.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return el.GetInt32();
            }
            if (el.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                string s = el.GetString();
                if (s != null && s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt32(s.Substring(2), 16);
                int.TryParse(s, out int val);
                return val;
            }
            return 0;
        }
    }

    public class MapDefinitionJson
    {
        public string Name { get; set; }
        public string Offset { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public double Scale { get; set; }
        public string Unit { get; set; }
    }

    public class ChecksumJsonData
    {
        public string ChecksumType { get; set; }
        public string Algorithm { get; set; }
        public string Address { get; set; }
        public int Size { get; set; } = 1;
        public string RangeStart { get; set; }
        public string RangeEnd { get; set; }
        public string ByteOrder { get; set; } = "LittleEndian";
        public int ExpectedValue { get; set; }
        public int Seed { get; set; }
        public List<ExcludeRangeJson> ExcludeRanges { get; set; }
    }

    public class ExcludeRangeJson
    {
        public string Start { get; set; }
        public string End { get; set; }
    }

    public class EcuPatchMappingJson
    {
        public string PatchId { get; set; }
        public string Offset { get; set; }
        public string RequiredFeature { get; set; }
    }
}
