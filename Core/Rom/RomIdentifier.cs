using System;
using System.Collections.Generic;
using System.Linq;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.Rom
{
    /// <summary>
    /// Kural tabanlı ECU ROM Tanımlama Motoru.
    /// Yüklenen binary dosyasını 10 farklı kritere göre puanlar ve en uygun ECU profilini belirler.
    /// </summary>
    public class RomIdentifier : IRomIdentifier
    {
        public RomAnalysisResult IdentifyRom(byte[] romData, IEnumerable<EcuProfile> knownProfiles)
        {
            var result = new RomAnalysisResult
            {
                RomSize = romData?.Length ?? 0,
                ObdType = "OBD1",
                CompatibilityScore = 0,
                Confidence = 0,
                IsMismatch = true,
                Feedback = "Tanımlanamayan ROM.",
                MatchedRules = new List<string>(),
                UnsupportedRules = new List<string>()
            };

            if (romData == null || romData.Length == 0)
            {
                result.Feedback = "Dosya boş veya okunamadı.";
                return result;
            }

            EcuProfile bestProfile = null;
            double bestScore = 0;
            var bestMatchedRules = new List<string>();
            var bestUnsupportedRules = new List<string>();

            // Tüm bilinen ve dinamik olarak yüklenmiş profilleri tara
            foreach (var profile in knownProfiles)
            {
                var matched = new List<string>();
                var unsupported = new List<string>();
                double score = CalculateConfidenceScore(romData, profile, matched, unsupported);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestProfile = profile;
                    bestMatchedRules = matched;
                    bestUnsupportedRules = unsupported;
                }
            }

            if (bestProfile != null && bestScore >= 35.0)
            {
                result.EcuCode = bestProfile.EcuCode;
                result.EngineCode = bestProfile.EngineCode;
                result.MatchedProfile = bestProfile;
                result.CompatibilityScore = Math.Round(bestScore, 1);
                result.Confidence = Math.Round(bestScore, 1);
                result.IsMismatch = bestScore < 70.0;
                result.MatchedRules = bestMatchedRules;
                result.UnsupportedRules = bestUnsupportedRules;

                if (bestScore >= 85.0)
                {
                    result.Feedback = $"✅ ROM Otomatik Tanındı: {bestProfile.Name} (%{bestScore:F0} Uyumlu)";
                }
                else
                {
                    result.Feedback = $"⚠️ Kısmi ROM Uyumu: {bestProfile.Name} (%{bestScore:F0} Uyumlu)";
                }

                ApplicationLogger.Info("RomIdentifier", $"ECU Tanımlandı: {result.EcuCode} (Confidence: %{result.Confidence:F1})");
            }
            else
            {
                result.Feedback = "❌ ROM Tanımlanamadı. Lütfen manuel ECU seçimi yapın.";
                ApplicationLogger.Warn("RomIdentifier", "ROM imzası hiçbir ECU profiliyle eşleşmedi.");
            }

            return result;
        }

        private double CalculateConfidenceScore(byte[] data, EcuProfile profile, List<string> matched, List<string> unsupported)
        {
            double totalWeight = 0;
            double earnedWeight = 0;

            // 1. ROM size (Ağırlık: 15)
            totalWeight += 15;
            if (data.Length == profile.RomSize)
            {
                earnedWeight += 15;
                matched.Add("ROM Size Match");
            }
            else
            {
                unsupported.Add("ROM Size Mismatch");
                return 0; // Boyut uyuşmuyorsa direkt elenir
            }

            // 2. Header Signature / Pattern (Ağırlık: 15)
            totalWeight += 15;
            // OBD1 Honda ROM'larında genellikle 0x0000 - 0x0040 civarı başlık içerir.
            // Dummy header kontrolü yapıyoruz.
            bool headerMatch = false;
            if (data.Length >= 50)
            {
                // Basit bir arama: ROM içinde ECU kodunun geçip geçmediğine bak
                string romText = System.Text.Encoding.ASCII.GetString(data, 0, Math.Min(data.Length, 128));
                if (romText.Contains(profile.EcuCode))
                {
                    headerMatch = true;
                }
            }
            if (headerMatch)
            {
                earnedWeight += 15;
                matched.Add("Header Signature Match");
            }
            else
            {
                unsupported.Add("Header Signature Mismatch / Absent");
            }

            // 3. Checksum Address (Ağırlık: 10)
            totalWeight += 10;
            if (profile.ChecksumOffset > 0 && profile.ChecksumOffset < data.Length)
            {
                earnedWeight += 10;
                matched.Add("Checksum Offset Valid");
            }
            else
            {
                unsupported.Add("Checksum Offset Out of Bounds");
            }

            // 4. Fuel Map Offset (Ağırlık: 15)
            totalWeight += 15;
            int fuelMapLength = profile.FuelMapRows * profile.FuelMapCols;
            if (profile.FuelMapOffset > 0 && profile.FuelMapOffset + fuelMapLength <= data.Length)
            {
                // Flat data kontrolü: Harita tamamen boş/FF olmamalı
                if (HasDataVariation(data, profile.FuelMapOffset, fuelMapLength))
                {
                    earnedWeight += 15;
                    matched.Add("Fuel Map Offset & Data Valid");
                }
                else
                {
                    earnedWeight += 5;
                    matched.Add("Fuel Map Offset Valid (Flat Data)");
                }
            }
            else
            {
                unsupported.Add("Fuel Map Offset Out of Bounds");
            }

            // 5. Ignition Map Offset (Ağırlık: 15)
            totalWeight += 15;
            int ignMapLength = profile.IgnMapRows * profile.IgnMapCols;
            if (profile.IgnMapOffset > 0 && profile.IgnMapOffset + ignMapLength <= data.Length)
            {
                if (HasDataVariation(data, profile.IgnMapOffset, ignMapLength))
                {
                    earnedWeight += 15;
                    matched.Add("Ignition Map Offset & Data Valid");
                }
                else
                {
                    earnedWeight += 5;
                    matched.Add("Ignition Map Offset Valid (Flat Data)");
                }
            }
            else
            {
                unsupported.Add("Ignition Map Offset Out of Bounds");
            }

            // 6. VTEC Offset (Ağırlık: 10)
            totalWeight += 10;
            if (profile.HasVtec)
            {
                if (profile.VtecRpmOffset > 0 && profile.VtecRpmOffset + 1 < data.Length)
                {
                    int vtecRpm = (data[profile.VtecRpmOffset] << 8) | data[profile.VtecRpmOffset + 1];
                    if (vtecRpm >= profile.VtecRpmMin && vtecRpm <= profile.VtecRpmMax)
                    {
                        earnedWeight += 10;
                        matched.Add("VTEC Address & Value Valid");
                    }
                    else
                    {
                        earnedWeight += 3;
                        matched.Add("VTEC Address Valid (Out-of-range Value)");
                    }
                }
                else
                {
                    unsupported.Add("VTEC Offset Out of Bounds");
                }
            }
            else
            {
                // VTEC içermeyen ECU için bu adım eşleşmiş sayılır
                earnedWeight += 10;
                matched.Add("VTEC Control Not Required");
            }

            // 7. Rev Limit Offset (Ağırlık: 10)
            totalWeight += 10;
            if (profile.RevLimitOffset > 0 && profile.RevLimitOffset + 1 < data.Length)
            {
                int revLimit = (data[profile.RevLimitOffset] << 8) | data[profile.RevLimitOffset + 1];
                if (revLimit >= profile.RevLimitMin && revLimit <= profile.RevLimitMax)
                {
                    earnedWeight += 10;
                    matched.Add("Rev Limit Address & Value Valid");
                }
                else
                {
                    earnedWeight += 3;
                    matched.Add("Rev Limit Address Valid (Out-of-range Value)");
                }
            }
            else
            {
                unsupported.Add("Rev Limit Offset Out of Bounds");
            }

            // 8. Speed Limit Offset (Ağırlık: 5)
            totalWeight += 5;
            // OBD1 speed limiter offset genellikle 0x1FAC (8108) civarıdır.
            int speedLimitOffset = 0x1FAC;
            if (speedLimitOffset + 1 < data.Length)
            {
                int speed = (data[speedLimitOffset] << 8) | data[speedLimitOffset + 1];
                if (speed >= 50 && speed <= 300)
                {
                    earnedWeight += 5;
                    matched.Add("Speed Limit Valid");
                }
                else
                {
                    unsupported.Add("Speed Limit Value Invalid");
                }
            }
            else
            {
                unsupported.Add("Speed Limit Offset Out of Bounds");
            }

            // 9. Known Byte Patterns / Signature Bytes (Ağırlık: 10)
            totalWeight += 10;
            // 0x0010 (16) offsetindeki signature byte dizisini tara (DatabaseUpdater/ecu_database.json'da da tanımlı)
            bool sigMatch = false;
            int sigOffset = 16;
            byte[] expectedSig = profile.SignatureBytes;
            if (expectedSig != null && expectedSig.Length > 0 && sigOffset + expectedSig.Length <= data.Length)
            {
                bool match = true;
                for (int i = 0; i < expectedSig.Length; i++)
                {
                    if (data[sigOffset + i] != expectedSig[i])
                    {
                        match = false;
                        break;
                    }
                }
                sigMatch = match;
            }

            if (sigMatch)
            {
                earnedWeight += 10;
                matched.Add("Known Signature Bytes Match");
            }
            else
            {
                unsupported.Add("Signature Bytes Mismatch");
            }

            // Ağırlıklı oran üzerinden % Skoru döndür
            return (earnedWeight / totalWeight) * 100.0;
        }

        private bool HasDataVariation(byte[] data, int offset, int length)
        {
            if (offset + length > data.Length) return false;
            byte first = data[offset];
            for (int i = 1; i < length; i++)
            {
                if (data[offset + i] != first) return true;
            }
            return false;
        }
    }
}
