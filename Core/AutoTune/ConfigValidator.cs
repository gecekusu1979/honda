using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using HondaTuner.Core.Logging;

namespace HondaTuner.Core.AutoTune
{
    public static class ConfigValidator
    {
        public static bool ValidateProfiles(string filePath, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = $"Profiles dosyası mevcut değil: {filePath}";
                    return false;
                }

                string content = File.ReadAllText(filePath);
                using (var doc = JsonDocument.Parse(content))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        errorMessage = "Profiles configuration kök dizini bir dizi olmalıdır.";
                        return false;
                    }

                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        if (!elem.TryGetProperty("ProfileName", out var nameProp) || string.IsNullOrEmpty(nameProp.GetString()))
                        {
                            errorMessage = "ProfileName boş veya eksik.";
                            return false;
                        }

                        if (!elem.TryGetProperty("CorrectionRate", out var rateProp) || rateProp.GetDouble() <= 0)
                        {
                            errorMessage = $"{nameProp.GetString()}: CorrectionRate pozitif olmalıdır.";
                            return false;
                        }

                        if (!elem.TryGetProperty("MaxFuelCorrection", out var fuelProp) || fuelProp.GetDouble() < 0)
                        {
                            errorMessage = $"{nameProp.GetString()}: MaxFuelCorrection negatif olamaz.";
                            return false;
                        }

                        if (!elem.TryGetProperty("MaxIgnitionCorrection", out var ignProp) || ignProp.GetDouble() < 0)
                        {
                            errorMessage = $"{nameProp.GetString()}: MaxIgnitionCorrection negatif olamaz.";
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Profiles doğrulama hatası: {ex.Message}";
                return false;
            }
        }

        public static bool ValidateTargets(string filePath, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = $"Targets dosyası mevcut değil: {filePath}";
                    return false;
                }

                string content = File.ReadAllText(filePath);
                using (var doc = JsonDocument.Parse(content))
                {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("Version", out var versionProp) || string.IsNullOrEmpty(versionProp.GetString()))
                    {
                        errorMessage = "Targets: Version alanı eksik veya boş.";
                        return false;
                    }

                    // Version support compatibility check
                    string versionStr = versionProp.GetString();
                    if (!versionStr.StartsWith("1."))
                    {
                        errorMessage = $"Desteklenmeyen Targets Versiyonu: {versionStr}. Sadece 1.x desteklenmektedir.";
                        return false;
                    }

                    if (!root.TryGetProperty("RpmBins", out var rpmBinsProp) || rpmBinsProp.ValueKind != JsonValueKind.Array)
                    {
                        errorMessage = "Targets: RpmBins eksik veya hatalı.";
                        return false;
                    }

                    if (!root.TryGetProperty("LoadBins", out var loadBinsProp) || loadBinsProp.ValueKind != JsonValueKind.Array)
                    {
                        errorMessage = "Targets: LoadBins eksik veya hatalı.";
                        return false;
                    }

                    int rpmCount = rpmBinsProp.GetArrayLength();
                    int loadCount = loadBinsProp.GetArrayLength();

                    if (rpmCount == 0 || loadCount == 0)
                    {
                        errorMessage = "Bins boyutları sıfırdan büyük olmalıdır.";
                        return false;
                    }

                    string[] tables = { "AfrTargets", "LambdaTargets", "IgnitionTargets", "VeTargets" };
                    foreach (var tbl in tables)
                    {
                        if (!root.TryGetProperty(tbl, out var tblProp) || tblProp.ValueKind != JsonValueKind.Array)
                        {
                            errorMessage = $"Targets: {tbl} tablosu eksik veya hatalı.";
                            return false;
                        }

                        if (tblProp.GetArrayLength() != rpmCount)
                        {
                            errorMessage = $"Targets: {tbl} satır sayısı ({tblProp.GetArrayLength()}) RpmBins boyutuyla ({rpmCount}) uyuşmuyor.";
                            return false;
                        }

                        foreach (var row in tblProp.EnumerateArray())
                        {
                            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() != loadCount)
                            {
                                errorMessage = $"Targets: {tbl} sütun boyutu LoadBins boyutuyla ({loadCount}) uyuşmuyor.";
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Targets doğrulama hatası: {ex.Message}";
                return false;
            }
        }

        public static bool ValidateSafetyLimits(string filePath, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = $"Safety limits dosyası mevcut değil: {filePath}";
                    return false;
                }

                string content = File.ReadAllText(filePath);
                using (var doc = JsonDocument.Parse(content))
                {
                    var root = doc.RootElement;
                    string[] doubleFields = { "MaxAFRError", "MaxFuelCorrection", "MaxIgnitionDelta", "MaxECT", "MinVoltage" };
                    foreach (var f in doubleFields)
                    {
                        if (!root.TryGetProperty(f, out var prop) || prop.GetDouble() <= 0)
                        {
                            errorMessage = $"Safety: {f} alanı eksik, sıfır veya negatif.";
                            return false;
                        }
                    }

                    if (!root.TryGetProperty("MaxKnockCount", out var knockProp) || knockProp.GetInt32() < 0)
                    {
                        errorMessage = "Safety: MaxKnockCount eksik veya negatif.";
                        return false;
                    }

                    if (!root.TryGetProperty("MaxLatencyMs", out var latProp) || latProp.GetInt32() < 0)
                    {
                        errorMessage = "Safety: MaxLatencyMs eksik veya negatif.";
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Safety Limits doğrulama hatası: {ex.Message}";
                return false;
            }
        }
    }
}
