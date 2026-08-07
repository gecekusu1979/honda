using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.AutoTune
{
    public class EnvironmentalContext
    {
        public double Temperature { get; set; }
        public double Altitude { get; set; }
        public double Humidity { get; set; }
        public string FuelType { get; set; } = "E10";
        public string OperatingConditions { get; set; } = "Normal";
    }

    public class MemoryEntry
    {
        public string ParameterName { get; set; }
        public string MapName { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public double AppliedCorrection { get; set; }
        public bool Success { get; set; }
        public EnvironmentalContext Environment { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AdaptiveMemory
    {
        public int SchemaVersion { get; set; } = 2; // Matches v2 requirement

        [JsonInclude]
        [JsonPropertyName("_entries")]
        public List<MemoryEntry> _entries = new List<MemoryEntry>();

        private readonly object _lockObj = new object();
        public int MaxHistorySize { get; set; } = 1000;

        [JsonIgnore]
        public IReadOnlyList<MemoryEntry> Entries
        {
            get
            {
                lock (_lockObj)
                {
                    return new List<MemoryEntry>(_entries).AsReadOnly();
                }
            }
        }

        public void Learn(string parameterName, string mapName, int row, int col, double correction, bool success, EnvironmentalContext env)
        {
            lock (_lockObj)
            {
                var entry = new MemoryEntry
                {
                    ParameterName = parameterName,
                    MapName = mapName,
                    Row = row,
                    Col = col,
                    AppliedCorrection = correction,
                    Success = success,
                    Environment = env ?? new EnvironmentalContext(),
                    Timestamp = DateTime.Now
                };

                _entries.Add(entry);

                // Enforce history size limit (oldest items first)
                while (_entries.Count > MaxHistorySize)
                {
                    _entries.RemoveAt(0);
                }
            }
        }

        public void Reset()
        {
            lock (_lockObj)
            {
                _entries.Clear();
                ApplicationLogger.Info("AdaptiveMemory", "Öğrenilmiş adaptif hafıza sıfırlandı.");
            }
        }

        public string Export()
        {
            lock (_lockObj)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                return JsonSerializer.Serialize(this, options);
            }
        }

        public void Import(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
                throw new ArgumentException("İçerik boş olamaz.", nameof(jsonContent));

            lock (_lockObj)
            {
                try
                {
                    using (var doc = JsonDocument.Parse(jsonContent))
                    {
                        var root = doc.RootElement;
                        int importedVersion = 1;
                        if (root.TryGetProperty("SchemaVersion", out var versionProp))
                        {
                            importedVersion = versionProp.GetInt32();
                        }

                        // Version migrations
                        if (importedVersion == 1)
                        {
                            MigrateV1ToV2(root);
                        }
                        else if (importedVersion == 2)
                        {
                            var tempMemory = JsonSerializer.Deserialize<AdaptiveMemory>(jsonContent);
                            if (tempMemory != null)
                            {
                                SchemaVersion = tempMemory.SchemaVersion;
                                _entries = tempMemory._entries ?? new List<MemoryEntry>();
                                MaxHistorySize = tempMemory.MaxHistorySize;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"Bilinmeyen veya uyumsuz adaptif hafıza versiyonu: {importedVersion}");
                        }
                    }
                }
                catch (Exception ex) when (!(ex is NotSupportedException))
                {
                    throw new InvalidDataException($"Adaptif hafıza içe aktarılamadı: {ex.Message}", ex);
                }
            }
        }

        private void MigrateV1ToV2(JsonElement root)
        {
            // V1 format is assumed to contain "Entries" list but environment context properties are missing or simplified.
            // We migrate them by populating default environmental conditions.
            SchemaVersion = 2; // Migrate to v2
            _entries.Clear();

            if (root.TryGetProperty("_entries", out var entriesProp) || root.TryGetProperty("Entries", out entriesProp))
            {
                foreach (var item in entriesProp.EnumerateArray())
                {
                    var entry = new MemoryEntry
                    {
                        ParameterName = item.TryGetProperty("ParameterName", out var pName) ? pName.GetString() : "Unknown",
                        MapName = item.TryGetProperty("MapName", out var mName) ? mName.GetString() : "Unknown",
                        Row = item.TryGetProperty("Row", out var r) ? r.GetInt32() : 0,
                        Col = item.TryGetProperty("Col", out var c) ? c.GetInt32() : 0,
                        AppliedCorrection = item.TryGetProperty("AppliedCorrection", out var corr) ? corr.GetDouble() : 0.0,
                        Success = item.TryGetProperty("Success", out var succ) ? succ.GetBoolean() : true,
                        Timestamp = item.TryGetProperty("Timestamp", out var ts) ? ts.GetDateTime() : DateTime.Now,
                        Environment = new EnvironmentalContext
                        {
                            Temperature = 25.0, // Default fallback ambient temp
                            Altitude = 0.0,
                            Humidity = 50.0,
                            FuelType = "E10",
                            OperatingConditions = "Migrated from V1"
                        }
                    };
                    _entries.Add(entry);
                }
            }
            ApplicationLogger.Info("AdaptiveMemory", $"V1 hafıza verisi V2 şemasına başarıyla taşındı. Toplam kayıt: {_entries.Count}");
        }
    }
}
