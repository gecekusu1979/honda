using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace HondaTuner.Core.Localization
{
    public static class L
    {
        private static Dictionary<string, string> _translations = new Dictionary<string, string>();
        public static string CurrentLanguage { get; private set; } = "tr";

        public static void SetLanguage(string lang)
        {
            CurrentLanguage = lang.ToLower();
            LoadTranslations();
        }

        public static string Get(string key)
        {
            if (_translations.TryGetValue(key, out string value))
            {
                return value;
            }
            return key;
        }

        private static void LoadTranslations()
        {
            _translations.Clear();
            string dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            string fileName = $"Strings.{CurrentLanguage}.resx";
            string filePath = Path.Combine(dbDir, fileName);

            if (!File.Exists(filePath))
            {
                LoadDefaults();
                try
                {
                    Directory.CreateDirectory(dbDir);
                    SaveResx(filePath);
                }
                catch { }
                return;
            }

            try
            {
                var doc = XDocument.Load(filePath);
                if (doc.Root != null)
                {
                    foreach (var dataElement in doc.Root.Elements("data"))
                    {
                        string name = dataElement.Attribute("name")?.Value;
                        string val = dataElement.Element("value")?.Value;
                        if (!string.IsNullOrEmpty(name) && val != null)
                        {
                            _translations[name] = val;
                        }
                    }
                }
            }
            catch
            {
                LoadDefaults();
            }
        }

        private static void SaveResx(string filePath)
        {
            var doc = new XDocument(
                new XElement("root",
                    new XElement("resheader", new XAttribute("name", "resmimetype"), new XElement("value", "text/microsoft-resx")),
                    new XElement("resheader", new XAttribute("name", "version"), new XElement("value", "2.0")),
                    new XElement("resheader", new XAttribute("name", "reader"), new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
                    new XElement("resheader", new XAttribute("name", "writer"), new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))
                )
            );

            foreach (var kvp in _translations)
            {
                doc.Root.Add(
                    new XElement("data",
                        new XAttribute("name", kvp.Key),
                        new XAttribute("xml:space", "preserve"),
                        new XElement("value", kvp.Value)
                    )
                );
            }

            doc.Save(filePath);
        }

        private static void LoadDefaults()
        {
            if (CurrentLanguage == "en")
            {
                _translations["safety_banner_lean"] = "🚨 LEAN CONDITION DETECTED! RISK OF FLAME-OUT / ENGINE HAS BEEN SAFETY SHIELDED!";
                _translations["safety_banner_overboost"] = "🚨 OVERBOOST DETECTED! RISK OF ENGINE DAMAGE / BOOST CUT ACTIVE!";
                _translations["safety_banner_oil_temp"] = "🚨 HIGH OIL TEMPERATURE DETECTED! MOTOR LIMP MODE ACTIVE!";
                _translations["safety_banner_low_oil_press"] = "🚨 CRITICAL LOW OIL PRESSURE DETECTED! ENGINE PROTECTION CUT!";
                _translations["menu_telemetry"] = "Live Telemetry";
                _translations["menu_autotune"] = "AutoTune Results";
                _translations["btn_start"] = "Start Connection";
                _translations["btn_stop"] = "Disconnect";
            }
            else
            {
                _translations["safety_banner_lean"] = "🚨 FAKİR KARIŞIM ALGILANDI! AŞIRI HARARET / MOTOR KORUMAYA ALINDI!";
                _translations["safety_banner_overboost"] = "🚨 YÜKSEK BOOST ALGILANDI! MOTOR HASARI RİSKİ / BOOST KESİLDİ!";
                _translations["safety_banner_oil_temp"] = "🚨 YÜKSEK YAĞ SICAKLIĞI ALGILANDI! MOTOR LİMP MODUNA ALINDI!";
                _translations["safety_banner_low_oil_press"] = "🚨 KRİTİK DÜŞÜK YAĞ BASINCI ALGILANDI! MOTOR KORUMAYA ALINDI!";
                _translations["menu_telemetry"] = "Canlı Telemetri";
                _translations["menu_autotune"] = "AutoTune Sonuçları";
                _translations["btn_start"] = "Bağlantıyı Başlat";
                _translations["btn_stop"] = "Bağlantıyı Kes";
            }
        }
    }
}
