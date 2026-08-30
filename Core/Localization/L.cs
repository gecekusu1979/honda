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
            LoadDefaults();

            string dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            string fileName = $"Strings.{CurrentLanguage}.resx";
            string filePath = Path.Combine(dbDir, fileName);

            if (!File.Exists(filePath))
            {
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
                // Alınan hatalarda varsayılanları koru
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
                _translations["safety_banner_limits"] = "🚨 EMERGENCY SHIELD: PARAMETERS OUT OF RANGE!";
                _translations["slow_safety_banner_retard"] = "⚠️ RETARD PROTECTION: SPARK ANGLE TIMING RETARDED DUE TO HEAT/KNOCK (-{0:F1}°)";
                _translations["menu_telemetry"] = "Live Telemetry";
                _translations["menu_autotune"] = "AutoTune Results";
                _translations["btn_start"] = "🔌 Connect Live";
                _translations["btn_stop"] = "⏹ Disconnect";
                _translations["btn_simulate"] = "🎮 Start Simulation";

                // Menus
                _translations["menu_file"] = "File";
                _translations["menu_open"] = "Open...";
                _translations["menu_save"] = "Save";
                _translations["menu_save_as"] = "Save As...";
                _translations["menu_undo"] = "Undo";
                _translations["menu_exit"] = "Exit";
                _translations["menu_tools"] = "Tools";
                _translations["menu_select_vehicle"] = "Select Vehicle/ECU...";
                _translations["menu_apply_basemap"] = "Tuning Assistant: Apply Basemap";
                _translations["menu_wideband_corr"] = "Wideband AFR Correction";
                _translations["menu_verify_checksum"] = "Verify Checksum";
                _translations["menu_reset_stock"] = "Reset to Stock";
                _translations["menu_ecu_profile"] = "Select ECU Profile";
                _translations["menu_language"] = "🌐 Language";

                // Tabs
                _translations["tab_fuel"] = "⛽ Fuel";
                _translations["tab_ignition"] = "⚡ Ignition";
                _translations["tab_tuning_assistant"] = "🧠 Tuning Assistant";
                _translations["tab_diff"] = "🔍 Diff";
                _translations["tab_telemetry"] = "📊 Telemetry";
                _translations["tab_part_viewer"] = "🔩 3D Part";
                _translations["tab_autotune"] = "🚀 AutoTune";
                _translations["tab_project_pinout"] = "✏️ Project & Pinout";
                _translations["tab_analysis_decompiler"] = "🔍 Analysis & Decompiler";
                _translations["tab_adv_fuel"] = "🚀 Advanced Fuel";
                _translations["tab_adv_ignition"] = "⚡ Advanced Ignition";
                _translations["tab_vtec_boost"] = "🏁 VTEC & Boost";
                _translations["tab_engine_protection"] = "🛡️ Engine Protection";
                _translations["tab_diagnostics_a2l"] = "📶 Diagnostics & A2L";
                _translations["tab_dyno_logs"] = "📊 Dyno & Logs";
                _translations["tab_hardware_control"] = "🔌 Hardware Control";

                // Playback
                _translations["btn_load_csv"] = "📂 Load CSV";
                _translations["btn_playback"] = "▶ Play";
                _translations["btn_pause"] = "⏸ Pause";
                _translations["btn_resume"] = "▶ Resume";

                // AutoTune Buttons
                _translations["btn_at_start"] = "▶ Start";
                _translations["btn_at_pause"] = "⏸ Pause";
                _translations["btn_at_resume"] = "▶ Resume";
                _translations["btn_at_stop"] = "⏹ Stop";
            }
            else
            {
                _translations["safety_banner_lean"] = "🚨 FAKİR KARIŞIM ALGILANDI! AŞIRI HARARET / MOTOR KORUMAYA ALINDI!";
                _translations["safety_banner_overboost"] = "🚨 YÜKSEK BOOST ALGILANDI! MOTOR HASARI RİSKİ / BOOST KESİLDİ!";
                _translations["safety_banner_oil_temp"] = "🚨 YÜKSEK YAĞ SICAKLIĞI ALGILANDI! MOTOR LİMP MODUNA ALINDI!";
                _translations["safety_banner_low_oil_press"] = "🚨 KRİTİK DÜŞÜK YAĞ BASINCI ALGILANDI! MOTOR KORUMAYA ALINDI!";
                _translations["safety_banner_limits"] = "🚨 ACİL MOTOR KORUMA: LİMİTLER AŞILDI!";
                _translations["slow_safety_banner_retard"] = "⚠️ GECİKMELİ KORUMA: SICAKLIK/VURUNTU NEDENİYLE AVANS KISILIYOR (-{0:F1}°)";
                _translations["menu_telemetry"] = "Canlı Telemetri";
                _translations["menu_autotune"] = "AutoTune Sonuçları";
                _translations["btn_start"] = "🔌 Canlı Bağlan";
                _translations["btn_stop"] = "⏹ Bağlantıyı Kes";
                _translations["btn_simulate"] = "🎮 Simülasyon Başlat";

                // Menus
                _translations["menu_file"] = "Dosya";
                _translations["menu_open"] = "Aç…";
                _translations["menu_save"] = "Kaydet";
                _translations["menu_save_as"] = "Farklı Kaydet…";
                _translations["menu_undo"] = "Geri Al (Undo)";
                _translations["menu_exit"] = "Çıkış";
                _translations["menu_tools"] = "Araçlar";
                _translations["menu_select_vehicle"] = "Araç / ECU Seç…";
                _translations["menu_apply_basemap"] = "Tuning Asistanı: Basemap Uygula";
                _translations["menu_wideband_corr"] = "Wideband AFR Düzeltmesi";
                _translations["menu_verify_checksum"] = "Checksum Doğrula";
                _translations["menu_reset_stock"] = "Stock'a Döndür";
                _translations["menu_ecu_profile"] = "ECU Profili Seç";
                _translations["menu_language"] = "🌐 Dil";

                // Tabs
                _translations["tab_fuel"] = "⛽ Yakıt";
                _translations["tab_ignition"] = "⚡ Ateşleme";
                _translations["tab_tuning_assistant"] = "🧠 Tuning Asistanı";
                _translations["tab_diff"] = "🔍 Diff";
                _translations["tab_telemetry"] = "📊 Telemetri";
                _translations["tab_part_viewer"] = "🔩 3D Parça";
                _translations["tab_autotune"] = "🚀 AutoTune";
                _translations["tab_project_pinout"] = "✏️ Proje & Pinout";
                _translations["tab_analysis_decompiler"] = "🔍 Analiz & Decompiler";
                _translations["tab_adv_fuel"] = "🚀 Advanced Fuel";
                _translations["tab_adv_ignition"] = "⚡ Advanced Ignition";
                _translations["tab_vtec_boost"] = "🏁 VTEC & Boost";
                _translations["tab_engine_protection"] = "🛡️ Engine Protection";
                _translations["tab_diagnostics_a2l"] = "📶 Diagnostics & A2L";
                _translations["tab_dyno_logs"] = "📊 Dyno & Logs";
                _translations["tab_hardware_control"] = "🔌 Donanım Kontrol";

                // Playback
                _translations["btn_load_csv"] = "📂 CSV Yükle";
                _translations["btn_playback"] = "▶ Oynat";
                _translations["btn_pause"] = "⏸ Duraklat";
                _translations["btn_resume"] = "▶ Devam Et";

                // AutoTune Buttons
                _translations["btn_at_start"] = "▶ Başlat";
                _translations["btn_at_pause"] = "⏸ Duraklat";
                _translations["btn_at_resume"] = "▶ Devam Et";
                _translations["btn_at_stop"] = "⏹ Durdur";
            }
        }
    }
}
