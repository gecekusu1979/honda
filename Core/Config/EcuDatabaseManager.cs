using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.Config
{
    public class EcuDatabaseManager
    {
        private static readonly int[] StdRpmAxis =
        {
            500, 750, 1000, 1250, 1500, 2000, 2500, 3000,
            3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000
        };

        private static readonly int[] StdLoadAxis =
        {
            20, 30, 40, 50, 60, 70, 80, 90,
            100, 110, 120, 130, 140, 150, 160, 170
        };

        private static readonly int[] BSeriesRpmAxis =
        {
            500, 750, 1000, 1500, 2000, 2500, 3000, 3500,
            4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500
        };

        public class JsonProfileDto
        {
            public string ecuCode { get; set; }
            public string name { get; set; }
            public string engineCode { get; set; }
            public string casaTag { get; set; }
            public bool hasVtec { get; set; }
            public bool hasIab { get; set; }
            public int romSize { get; set; }
            public int fuelMapOffset { get; set; }
            public int fuelMapRows { get; set; }
            public int fuelMapCols { get; set; }
            public int ignMapOffset { get; set; }
            public int ignMapRows { get; set; }
            public int ignMapCols { get; set; }
            public int vtecRpmOffset { get; set; }
            public int vtecRpmMin { get; set; }
            public int vtecRpmMax { get; set; }
            public int vtecRpmDefault { get; set; }
            public int revLimitOffset { get; set; }
            public int revLimitMin { get; set; }
            public int revLimitMax { get; set; }
            public int revLimitDefault { get; set; }
            public int checksumOffset { get; set; }
        }

        public static List<EcuProfile> LoadProfilesFromDirectory(string databaseDir)
        {
            var profiles = new List<EcuProfile>();
            if (!Directory.Exists(databaseDir))
            {
                // Fallback to embedded profiles if Database dir is missing
                return new List<EcuProfile>(EcuProfiles.All);
            }

            string[] files = Directory.GetFiles(databaseDir, "*.json");
            foreach (var file in files)
            {
                if (Path.GetFileName(file).Equals("ecu_database.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    string json = File.ReadAllText(file);
                    var dto = JsonSerializer.Deserialize<JsonProfileDto>(json);
                    if (dto != null)
                    {
                        var rpmAxis = (dto.engineCode != null && dto.engineCode.StartsWith("B", StringComparison.OrdinalIgnoreCase))
                            ? BSeriesRpmAxis
                            : StdRpmAxis;

                        var profile = new EcuProfile(
                            dto.name,
                            dto.ecuCode,
                            dto.engineCode,
                            dto.casaTag,
                            dto.hasVtec,
                            dto.hasIab,
                            dto.romSize,
                            dto.fuelMapOffset,
                            dto.fuelMapRows,
                            dto.fuelMapCols,
                            dto.ignMapOffset,
                            dto.ignMapRows,
                            dto.ignMapCols,
                            rpmAxis,
                            StdLoadAxis,
                            dto.vtecRpmOffset,
                            dto.vtecRpmMin,
                            dto.vtecRpmMax,
                            dto.vtecRpmDefault,
                            dto.revLimitOffset,
                            dto.revLimitMin,
                            dto.revLimitMax,
                            dto.revLimitDefault,
                            dto.checksumOffset
                        );
                        profiles.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    ApplicationLogger.Error("EcuDatabaseManager", $"Failed to load XML/JSON profile {file}: {ex.Message}");
                }
            }

            if (profiles.Count == 0)
            {
                return new List<EcuProfile>(EcuProfiles.All);
            }

            return profiles;
        }
    }
}
