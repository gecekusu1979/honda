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
                catch (System.Exception ex) { HondaTuner.Core.Logging.ApplicationLogger.Warn("SilentCatch", $"Beklenmeyen ic hata gizlendi: $($ex.Message)"); }
                return;
            }

            try
            {
                var doc = XDocument.Load(filePath);
                if (doc.Root != null)
                {
                    bool isVersion8 = false;
                    foreach (var header in doc.Root.Elements("resheader"))
                    {
                        if (header.Attribute("name")?.Value == "version" && header.Element("value")?.Value == "9.2")
                        {
                            isVersion8 = true;
                            break;
                        }
                    }

                    if (!isVersion8)
                    {
                        SaveResx(filePath);
                        return;
                    }
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
                // AlÄ±nan hatalarda varsayÄ±lanlarÄ± koru
            }
        }

        private static void SaveResx(string filePath)
        {
            var doc = new XDocument(
                new XElement("root",
                    new XElement("resheader", new XAttribute("name", "resmimetype"), new XElement("value", "text/microsoft-resx")),
                    new XElement("resheader", new XAttribute("name", "version"), new XElement("value", "9.2")),
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
                _translations["safety_banner_lean"] = "ğŸš¨ LEAN CONDITION DETECTED! RISK OF FLAME-OUT / ENGINE HAS BEEN SAFETY SHIELDED!";
                _translations["safety_banner_overboost"] = "ğŸš¨ OVERBOOST DETECTED! RISK OF ENGINE DAMAGE / BOOST CUT ACTIVE!";
                _translations["safety_banner_oil_temp"] = "ğŸš¨ HIGH OIL TEMPERATURE DETECTED! MOTOR LIMP MODE ACTIVE!";
                _translations["safety_banner_low_oil_press"] = "ğŸš¨ CRITICAL LOW OIL PRESSURE DETECTED! ENGINE PROTECTION CUT!";
                _translations["safety_banner_limits"] = "ğŸš¨ EMERGENCY SHIELD: PARAMETERS OUT OF RANGE!";
                _translations["slow_safety_banner_retard"] = "âš ï¸ RETARD PROTECTION: SPARK ANGLE TIMING RETARDED DUE TO HEAT/KNOCK (-{0:F1}Â°)";
                _translations["menu_telemetry"] = "Live Telemetry";
                _translations["menu_autotune"] = "AutoTune Results";
                _translations["btn_start"] = "ğŸ”Œ Connect Live";
                _translations["btn_stop"] = "â¹ Disconnect";
                _translations["btn_simulate"] = "ğŸ® Start Simulation";

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
                _translations["menu_language"] = "ğŸŒ Language";

                // Tabs
                _translations["tab_fuel"] = "â›½ Fuel";
                _translations["tab_ignition"] = "âš¡ Ignition";
                _translations["tab_tuning_assistant"] = "ğŸ§  Tuning Assistant";
                _translations["tab_diff"] = "ğŸ” Diff";
                _translations["tab_telemetry"] = "ğŸ“Š Telemetry";
                _translations["tab_part_viewer"] = "ğŸ”© 3D Part";
                _translations["tab_autotune"] = "ğŸš€ AutoTune";
                _translations["tab_project_pinout"] = "âœï¸ Project & Pinout";
                _translations["tab_analysis_decompiler"] = "ğŸ” Analysis & Decompiler";
                _translations["tab_adv_fuel"] = "ğŸš€ Advanced Fuel";
                _translations["tab_adv_ignition"] = "âš¡ Advanced Ignition";
                _translations["tab_vtec_boost"] = "ğŸ VTEC & Boost";
                _translations["tab_engine_protection"] = "ğŸ›¡ï¸ Engine Protection";
                _translations["tab_diagnostics_a2l"] = "ğŸ“¶ Diagnostics & A2L";
                _translations["tab_dyno_logs"] = "ğŸ“Š Dyno & Logs";
                _translations["tab_hardware_control"] = "ğŸ”Œ Hardware Control";

                // Playback
                _translations["btn_load_csv"] = "ğŸ“‚ Load CSV";
                _translations["btn_playback"] = "â–¶ Play";
                _translations["btn_pause"] = "â¸ Pause";
                _translations["btn_resume"] = "â–¶ Resume";

                // AutoTune Buttons
                _translations["btn_at_start"] = "â–¶ Start";
                _translations["btn_at_pause"] = "â¸ Pause";
                _translations["btn_at_resume"] = "â–¶ Resume";
                _translations["btn_at_stop"] = "â¹ Stop";

                // Hardware Control Tab
                _translations["CH341A EEPROM ProgramlayÄ±cÄ±"] = "CH341A EEPROM Programmer";
                _translations["Ã‡ip Tipi:"] = "Chip Type:";
                _translations["BaÄŸlan"] = "Connect";
                _translations["BaÄŸlantÄ±yÄ± Kes"] = "Disconnect";
                _translations["Ã‡ipten Oku"] = "Read Chip";
                _translations["Ã‡ipe Yaz"] = "Write Chip";
                _translations["Ã‡ipi Sil"] = "Erase Chip";
                _translations["Ã‡ipi DoÄŸrula"] = "Verify Chip";
                _translations["Ä°ÅŸlem KaydÄ±:"] = "Operation Log:";
                _translations["Ä°lerleme:"] = "Progress:";
                _translations["CanlÄ± OBD1 ArÄ±za KodlarÄ± (DTC)"] = "Live OBD1 Diagnostic Trouble Codes (DTC)";
                _translations["OBD1 seri portu seÃ§in, baÄŸlantÄ± kurun, sonra kodu okuyun."] = "Select OBD1 serial port, connect, then read codes.";
                _translations["ArÄ±za KodlarÄ±nÄ± Oku"] = "Read DTCs";
                _translations["ArÄ±za KodlarÄ±nÄ± Temizle"] = "Clear DTCs";
                _translations["AkÃ¼ VoltajÄ±:"] = "Battery Voltage:";

                // Dynamic Status labels
                _translations["BaÄŸlantÄ± Durumu:"] = "Connection Status:";
                _translations["Kuyruk DerinliÄŸi:"] = "Queue Depth:";
                _translations["Ortalama Gecikme:"] = "Average Latency:";
                _translations["Hata / Yeniden Deneme:"] = "Error / Retry:";
                _translations["DÃ¼ÅŸen Yazmalar:"] = "Dropped Writes:";
                _translations["Tuning Kalite Skoru:"] = "Tuning Quality Score:";
                _translations["AraÃ§ seÃ§ilmedi"] = "No vehicle selected";
                _translations["Status:"] = "Status:";
                _translations["Durum:"] = "Status:";
                _translations["GÃ¼venlik Durumu:"] = "Safety Status:";
                _translations["KullanÄ±cÄ± RolÃ¼:"] = "User Role:";
                _translations["ECU BaÄŸlantÄ±sÄ±:"] = "ECU Connection:";

                // Nested UserControl Headers & Tabs
                _translations["ğŸ“¶ Protokol & DonanÄ±m ArayÃ¼zleri"] = "ğŸ“¶ Protocols & Hardware Interfaces";
                _translations["A2L Veri TabanÄ± / TanÄ±mlamalar (ASAP2)"] = "A2L Database / Definitions (ASAP2)";
                _translations["Freeze Frame Verileri (DTC Hata AnÄ±)"] = "Freeze Frame Data (DTC Freeze)";
                _translations["ğŸ VTEC Solenoid Limitleri"] = "ğŸ VTEC Solenoid Limits";
                _translations["ğŸ“ˆ Target Boost (RPM vs Gear)"] = "ğŸ“ˆ Target Boost (RPM vs Gear)";
                _translations["SensÃ¶r Limitleri & GÃ¼venlik Kesicileri"] = "Sensor Limits & Safety Cuts";
                _translations["Termal Limitler & Avans KÄ±sma"] = "Thermal Limits & Ignition Pulls";
                _translations["SimÃ¼lasyon GiriÅŸleri"] = "Simulation Inputs";
                _translations["SimÃ¼lasyon Ã‡Ä±kÄ±ÅŸlarÄ±"] = "Simulation Outputs";
                _translations["Knock Koruma"] = "Knock Protection";
                _translations["DonanÄ±m Ã–z-Testi BaÅŸlat"] = "Start Hardware Self-Test";
                _translations["Protokol SeÃ§in:"] = "Select Protocol:";
                _translations["A2L Bilgileri"] = "A2L Info";
                _translations["KarÅŸÄ±laÅŸtÄ±rÄ±lan Dosya:"] = "Compared File:";
                _translations["Fark Tablosu"] = "Difference Table";
                _translations["Hedef KarÄ±ÅŸÄ±m (Target AFR):"] = "Target Mixture (Target AFR):";
                _translations["Ã–lÃ§Ã¼len KarÄ±ÅŸÄ±m (Wideband AFR):"] = "Measured Mixture (Wideband AFR):";
                _translations["Ä°ÅŸlem YarÄ±Ã§apÄ± (Radius):"] = "Processing Radius (Radius):";
                _translations["EnjektÃ¶r Boyutu (cc):"] = "Injector Size (cc):";
                _translations["MAP SensÃ¶rÃ¼ Ã‡Ã¶zÃ¼nÃ¼rlÃ¼ÄŸÃ¼:"] = "MAP Sensor Resolution:";
                _translations["Ã–nerilen DÃ¼zeltme YÃ¼zdesi:"] = "Recommended Correction %:";
                _translations["Uygula"] = "Apply";
                _translations["KÄ±lavuzlar & Sihirbazlar"] = "Wizards & Guides";
                _translations["Asistan NotlarÄ±"] = "Assistant Notes";
                _translations["Enjeksiyon ZamanlamasÄ±"] = "Injection Timing";
                _translations["IsÄ±nma ZenginleÅŸtirmesi"] = "Warm-up Enrichment";
                _translations["HÄ±zlanma ZenginleÅŸtirmesi"] = "Acceleration Enrichment";
                _translations["ECT Avans DÃ¼zeltmesi"] = "ECT Ignition Retard";
                _translations["IAT Avans DÃ¼zeltmesi"] = "IAT Ignition Retard";
                _translations["Dwell SÃ¼resi"] = "Dwell Duration";
                _translations["Solenoid AktifleÅŸme RPM'i:"] = "Solenoid Trigger RPM:";
                _translations["Minimum HÄ±z:"] = "Minimum Speed:";
                _translations["Port:"] = "Port:";
                _translations["Dynamic Dyno Canvas"] = "Dynamic Dyno Canvas";
                _translations["Datalog Playback / KayÄ±t GÃ¼nlÃ¼ÄŸÃ¼"] = "Datalog Playback / Recording Log";
                _translations["SÃ¼rÃ¼m GeÃ§miÅŸi & Dallanma"] = "Version History & Branching";
                _translations["Disconnected"] = "Disconnected";
                _translations["Connected"] = "Connected";
                _translations["Connecting"] = "Connecting";
                _translations["Synchronizing"] = "Synchronizing";
                _translations["Paused"] = "Paused";
                _translations["Faulted"] = "Faulted";

                // ComboBox Items & Descriptions
                _translations["Asistan Kilavuzu"] = "Assistant Guide";
                _translations["Gelismis Ayarlar"] = "Advanced Settings";
                _translations["Sihirbazlar"] = "Wizards";
                _translations["Gelismis Patch Merkezi"] = "Advanced Patch Center";
                _translations["Stock / gunluk kullanim"] = "Stock / daily use";
                _translations["iES VTEC yumurta kasa sokak ayari"] = "iES VTEC street tune";
                _translations["Atmosferik performans"] = "N/A Performance";
                _translations["Turbo guvenli basemap"] = "Turbo safe basemap";
                _translations["Ekonomi / dusuk tuketim"] = "Economy / low consumption";

                // Labels in BuildAssistantPage
                _translations["Gorunum Secenegi"] = "View Option";
                _translations["Basemap hedefi"] = "Basemap Target";
                _translations["EnjektÃ¶r cc"] = "Injector Size (cc)";
                _translations["MAP sensÃ¶rÃ¼ bar"] = "MAP Sensor (bar)";
                _translations["Power AFR hedefi"] = "Power AFR Target";
                _translations["Wideband yakÄ±t dÃ¼zeltme"] = "Wideband Fuel Correction";
                _translations["Ã–lÃ§Ã¼len AFR"] = "Measured AFR";
                _translations["RPM"] = "RPM";
                _translations["Load kPa"] = "Load kPa";
                _translations["Etki alanÄ±"] = "Processing Radius";
                _translations["AFR DÃ¼zelt"] = "Correct AFR";
                _translations["Basemap Uygula"] = "Apply Basemap";
                _translations["NotlarÄ± Yenile"] = "Refresh Notes";

                // Status Bar & Helper Warns
                _translations["Aktif profil:"] = "Active profile:";
                _translations["AraÃ§:"] = "Vehicle:";
                _translations["disclaimer_notes"] = "ROM file is provided by the user. The application does not download or distribute copyrighted stock ROMs; it generates test maps / basemaps and operates on the ROM you provide.";
                _translations["rom_not_loaded_status"] = "ROM not loaded. Start with File â†’ Open.";
                _translations["checksum_ok"] = "âœ… Checksum OK";
                _translations["checksum_error"] = "âŒ Checksum ERROR";
                _translations["rom_warn_msg"] = "Please load a ROM file first.";
                _translations["rom_warn_title"] = "No ROM";
                _translations["discard_confirm_msg"] = "There are unsaved changes. Continue?";
                _translations["discard_confirm_title"] = "Warning";
                _translations["aktif"] = "active";
                _translations["aktif deÄŸil"] = "inactive";
                _translations["BaÄŸlandÄ±"] = "Connected";
                _translations["BaÄŸlanÄ±yor..."] = "Connecting...";
                _translations["Hata"] = "Error";
                _translations["BaÄŸlÄ± DeÄŸil"] = "Disconnected";
                _translations["ONLINE"] = "ONLINE";
                _translations["OFFLINE"] = "OFFLINE";
                _translations["BAÄLANIYOR"] = "CONNECTING";
                _translations["HATA"] = "ERROR";
                _translations["SÄ°MÃœLASYON"] = "SIMULATION";
                _translations["CANLI"] = "LIVE";
            }
            else
            {
                _translations["safety_banner_lean"] = "ğŸš¨ FAKÄ°R KARIÅIM ALGILANDI! AÅIRI HARARET / MOTOR KORUMAYA ALINDI!";
                _translations["safety_banner_overboost"] = "ğŸš¨ YÃœKSEK BOOST ALGILANDI! MOTOR HASARI RÄ°SKÄ° / BOOST KESÄ°LDÄ°!";
                _translations["safety_banner_oil_temp"] = "ğŸš¨ YÃœKSEK YAÄ SICAKLIÄI ALGILANDI! MOTOR LÄ°MP MODUNA ALINDI!";
                _translations["safety_banner_low_oil_press"] = "ğŸš¨ KRÄ°TÄ°K DÃœÅÃœK YAÄ BASINCI ALGILANDI! MOTOR KORUMAYA ALINDI!";
                _translations["safety_banner_limits"] = "ğŸš¨ ACÄ°L MOTOR KORUMA: LÄ°MÄ°TLER AÅILDI!";
                _translations["slow_safety_banner_retard"] = "âš ï¸ GECÄ°KMELÄ° KORUMA: SICAKLIK/VURUNTU NEDENÄ°YLE AVANS KISILIYOR (-{0:F1}Â°)";
                _translations["menu_telemetry"] = "CanlÄ± Telemetri";
                _translations["menu_autotune"] = "AutoTune SonuÃ§larÄ±";
                _translations["btn_start"] = "ğŸ”Œ CanlÄ± BaÄŸlan";
                _translations["btn_stop"] = "â¹ BaÄŸlantÄ±yÄ± Kes";
                _translations["btn_simulate"] = "ğŸ® SimÃ¼lasyon BaÅŸlat";

                // Menus
                _translations["menu_file"] = "Dosya";
                _translations["menu_open"] = "AÃ§â€¦";
                _translations["menu_save"] = "Kaydet";
                _translations["menu_save_as"] = "FarklÄ± Kaydetâ€¦";
                _translations["menu_undo"] = "Geri Al (Undo)";
                _translations["menu_exit"] = "Ã‡Ä±kÄ±ÅŸ";
                _translations["menu_tools"] = "AraÃ§lar";
                _translations["menu_select_vehicle"] = "AraÃ§ / ECU SeÃ§â€¦";
                _translations["menu_apply_basemap"] = "Tuning AsistanÄ±: Basemap Uygula";
                _translations["menu_wideband_corr"] = "Wideband AFR DÃ¼zeltmesi";
                _translations["menu_verify_checksum"] = "Checksum DoÄŸrula";
                _translations["menu_reset_stock"] = "Stock'a DÃ¶ndÃ¼r";
                _translations["menu_ecu_profile"] = "ECU Profili SeÃ§";
                _translations["menu_language"] = "ğŸŒ Dil";

                // Tabs
                _translations["tab_fuel"] = "â›½ YakÄ±t";
                _translations["tab_ignition"] = "âš¡ AteÅŸleme";
                _translations["tab_tuning_assistant"] = "ğŸ§  Tuning AsistanÄ±";
                _translations["tab_diff"] = "ğŸ” Diff";
                _translations["tab_telemetry"] = "ğŸ“Š Telemetri";
                _translations["tab_part_viewer"] = "ğŸ”© 3D ParÃ§a";
                _translations["tab_autotune"] = "ğŸš€ AutoTune";
                _translations["tab_project_pinout"] = "âœï¸ Proje & Pinout";
                _translations["tab_analysis_decompiler"] = "ğŸ” Analiz & Decompiler";
                _translations["tab_adv_fuel"] = "ğŸš€ GeliÅŸmiÅŸ YakÄ±t";
                _translations["tab_adv_ignition"] = "âš¡ GeliÅŸmiÅŸ AteÅŸleme";
                _translations["tab_vtec_boost"] = "ğŸ VTEC & Boost";
                _translations["tab_engine_protection"] = "ğŸ›¡ï¸ Motor KorumasÄ±";
                _translations["tab_diagnostics_a2l"] = "ğŸ“¶ Diagnostics & A2L";
                _translations["tab_dyno_logs"] = "ğŸ“Š Dyno & Loglar";
                _translations["tab_hardware_control"] = "ğŸ”Œ DonanÄ±m Kontrol";

                // Playback
                _translations["btn_load_csv"] = "ğŸ“‚ CSV YÃ¼kle";
                _translations["btn_playback"] = "â–¶ Oynat";
                _translations["btn_pause"] = "â¸ Duraklat";
                _translations["btn_resume"] = "â–¶ Devam Et";

                // AutoTune Buttons
                _translations["btn_at_start"] = "â–¶ BaÅŸlat";
                _translations["btn_at_pause"] = "â¸ Duraklat";
                _translations["btn_at_resume"] = "â–¶ Devam Et";
                _translations["btn_at_stop"] = "â¹ Durdur";

                // Hardware Control Tab
                _translations["CH341A EEPROM ProgramlayÄ±cÄ±"] = "CH341A EEPROM ProgramlayÄ±cÄ±";
                _translations["Ã‡ip Tipi:"] = "Ã‡ip Tipi:";
                _translations["BaÄŸlan"] = "BaÄŸlan";
                _translations["BaÄŸlantÄ±yÄ± Kes"] = "BaÄŸlantÄ±yÄ± Kes";
                _translations["Ã‡ipten Oku"] = "Ã‡ipten Oku";
                _translations["Ã‡ipe Yaz"] = "Ã‡ipe Yaz";
                _translations["Ã‡ipi Sil"] = "Ã‡ipi Sil";
                _translations["Ã‡ipi DoÄŸrula"] = "Ã‡ipi DoÄŸrula";
                _translations["Ä°ÅŸlem KaydÄ±:"] = "Ä°ÅŸlem KaydÄ±:";
                _translations["Ä°lerleme:"] = "Ä°lerleme:";
                _translations["CanlÄ± OBD1 ArÄ±za KodlarÄ± (DTC)"] = "CanlÄ± OBD1 ArÄ±za KodlarÄ± (DTC)";
                _translations["OBD1 seri portu seÃ§in, baÄŸlantÄ± kurun, sonra kodu okuyun."] = "OBD1 seri portu seÃ§in, baÄŸlantÄ± kurun, sonra kodu okuyun.";
                _translations["ArÄ±za KodlarÄ±nÄ± Oku"] = "ArÄ±za KodlarÄ±nÄ± Oku";
                _translations["ArÄ±za KodlarÄ±nÄ± Temizle"] = "ArÄ±za KodlarÄ±nÄ± Temizle";
                _translations["AkÃ¼ VoltajÄ±:"] = "AkÃ¼ VoltajÄ±:";

                // Dynamic Status labels
                _translations["BaÄŸlantÄ± Durumu:"] = "BaÄŸlantÄ± Durumu:";
                _translations["Kuyruk DerinliÄŸi:"] = "Kuyruk DerinliÄŸi:";
                _translations["Ortalama Gecikme:"] = "Ortalama Gecikme:";
                _translations["Hata / Yeniden Deneme:"] = "Hata / Yeniden Deneme:";
                _translations["DÃ¼ÅŸen Yazmalar:"] = "DÃ¼ÅŸen Yazmalar:";
                _translations["Tuning Kalite Skoru:"] = "Tuning Kalite Skoru:";
                _translations["AraÃ§ seÃ§ilmedi"] = "AraÃ§ seÃ§ilmedi";
                _translations["Status:"] = "Durum:";
                _translations["Durum:"] = "Durum:";
                _translations["GÃ¼venlik Durumu:"] = "GÃ¼venlik Durumu:";
                _translations["KullanÄ±cÄ± RolÃ¼:"] = "KullanÄ±cÄ± RolÃ¼:";
                _translations["ECU BaÄŸlantÄ±sÄ±:"] = "ECU BaÄŸlantÄ±sÄ±:";

                // Nested UserControl Headers & Tabs
                _translations["ğŸ“¶ Protokol & DonanÄ±m ArayÃ¼zleri"] = "ğŸ“¶ Protokol & DonanÄ±m ArayÃ¼zleri";
                _translations["A2L Veri TabanÄ± / TanÄ±mlamalar (ASAP2)"] = "A2L Veri TabanÄ± / TanÄ±mlamalar (ASAP2)";
                _translations["Freeze Frame Verileri (DTC Hata AnÄ±)"] = "Freeze Frame Verileri (DTC Hata AnÄ±)";
                _translations["ğŸ VTEC Solenoid Limitleri"] = "ğŸ VTEC Solenoid Limitleri";
                _translations["ğŸ“ˆ Target Boost (RPM vs Gear)"] = "ğŸ“ˆ Target Boost (RPM vs Gear)";
                _translations["SensÃ¶r Limitleri & GÃ¼venlik Kesicileri"] = "SensÃ¶r Limitleri & GÃ¼venlik Kesicileri";
                _translations["Termal Limitler & Avans KÄ±sma"] = "Termal Limitler & Avans KÄ±sma";
                _translations["SimÃ¼lasyon GiriÅŸleri"] = "SimÃ¼lasyon GiriÅŸleri";
                _translations["SimÃ¼lasyon Ã‡Ä±kÄ±ÅŸlarÄ±"] = "SimÃ¼lasyon Ã‡Ä±kÄ±ÅŸlarÄ±";
                _translations["Knock Koruma"] = "Knock Koruma";
                _translations["DonanÄ±m Ã–z-Testi BaÅŸlat"] = "DonanÄ±m Ã–z-Testi BaÅŸlat";
                _translations["Protokol SeÃ§in:"] = "Protokol SeÃ§in:";
                _translations["A2L Bilgileri"] = "A2L Bilgileri";
                _translations["KarÅŸÄ±laÅŸtÄ±rÄ±lan Dosya:"] = "KarÅŸÄ±laÅŸtÄ±rÄ±lan Dosya:";
                _translations["Fark Tablosu"] = "Fark Tablosu";
                _translations["Hedef KarÄ±ÅŸÄ±m (Target AFR):"] = "Hedef KarÄ±ÅŸÄ±m (Target AFR):";
                _translations["Ã–lÃ§Ã¼len KarÄ±ÅŸÄ±m (Wideband AFR):"] = "Ã–lÃ§Ã¼len KarÄ±ÅŸÄ±m (Wideband AFR):";
                _translations["Ä°ÅŸlem YarÄ±Ã§apÄ± (Radius):"] = "Ä°ÅŸlem YarÄ±Ã§apÄ± (Radius):";
                _translations["EnjektÃ¶r Boyutu (cc):"] = "EnjektÃ¶r Boyutu (cc):";
                _translations["MAP SensÃ¶rÃ¼ Ã‡Ã¶zÃ¼nÃ¼rlÃ¼ÄŸÃ¼:"] = "MAP SensÃ¶rÃ¼ Ã‡Ã¶zÃ¼nÃ¼rlÃ¼ÄŸÃ¼:";
                _translations["Ã–nerilen DÃ¼zeltme YÃ¼zdesi:"] = "Ã–nerilen DÃ¼zeltme YÃ¼zdesi:";
                _translations["Uygula"] = "Uygula";
                _translations["KÄ±lavuzlar & Sihirbazlar"] = "KÄ±lavuzlar & Sihirbazlar";
                _translations["Asistan NotlarÄ±"] = "Asistan NotlarÄ±";
                _translations["Enjeksiyon ZamanlamasÄ±"] = "Enjeksiyon ZamanlamasÄ±";
                _translations["IsÄ±nma ZenginleÅŸtirmesi"] = "IsÄ±nma ZenginleÅŸtirmesi";
                _translations["HÄ±zlanma ZenginleÅŸtirmesi"] = "HÄ±zlanma ZenginleÅŸtirmesi";
                _translations["ECT Avans DÃ¼zeltmesi"] = "ECT Avans DÃ¼zeltmesi";
                _translations["IAT Avans DÃ¼zeltmesi"] = "IAT Avans DÃ¼zeltmesi";
                _translations["Dwell SÃ¼resi"] = "Dwell SÃ¼resi";
                _translations["Solenoid AktifleÅŸme RPM'i:"] = "Solenoid AktifleÅŸme RPM'i:";
                _translations["Minimum HÄ±z:"] = "Minimum HÄ±z:";
                _translations["Port:"] = "Port:";
                _translations["Dynamic Dyno Canvas"] = "Dynamic Dyno Canvas";
                _translations["Datalog Playback / KayÄ±t GÃ¼nlÃ¼ÄŸÃ¼"] = "Datalog Playback / KayÄ±t GÃ¼nlÃ¼ÄŸÃ¼";
                _translations["SÃ¼rÃ¼m GeÃ§miÅŸi & Dallanma"] = "SÃ¼rÃ¼m GeÃ§miÅŸi & Dallanma";
                _translations["Disconnected"] = "BaÄŸlantÄ± Yok";
                _translations["Connected"] = "BaÄŸlÄ±";
                _translations["Connecting"] = "BaÄŸlanÄ±yor";
                _translations["Synchronizing"] = "Senkronize Ediliyor";
                _translations["Paused"] = "DuraklatÄ±ldÄ±";
                _translations["Faulted"] = "HatalÄ±";

                // ComboBox Items & Descriptions
                _translations["Asistan Kilavuzu"] = "Asistan KÄ±lavuzu";
                _translations["Gelismis Ayarlar"] = "GeliÅŸmiÅŸ Ayarlar";
                _translations["Sihirbazlar"] = "Sihirbazlar";
                _translations["Gelismis Patch Merkezi"] = "GeliÅŸmiÅŸ Patch Merkezi";
                _translations["Stock / gunluk kullanim"] = "Stock / GÃ¼nlÃ¼k KullanÄ±m";
                _translations["iES VTEC yumurta kasa sokak ayari"] = "iES VTEC Sokak AyarÄ±";
                _translations["Atmosferik performans"] = "Atmosferik Performans";
                _translations["Turbo guvenli basemap"] = "Turbo GÃ¼venli Basemap";
                _translations["Ekonomi / dusuk tuketim"] = "Ekonomi / DÃ¼ÅŸÃ¼k TÃ¼ketim";

                // Labels in BuildAssistantPage
                _translations["Gorunum Secenegi"] = "GÃ¶rÃ¼nÃ¼m SeÃ§eneÄŸi";
                _translations["Basemap hedefi"] = "Basemap Hedefi";
                _translations["EnjektÃ¶r cc"] = "EnjektÃ¶r Boyutu (cc)";
                _translations["MAP sensÃ¶rÃ¼ bar"] = "MAP SensÃ¶rÃ¼ (bar)";
                _translations["Power AFR hedefi"] = "Power AFR Hedefi";
                _translations["Wideband yakÄ±t dÃ¼zeltme"] = "Wideband YakÄ±t DÃ¼zeltme";
                _translations["Ã–lÃ§Ã¼len AFR"] = "Ã–lÃ§Ã¼len AFR";
                _translations["RPM"] = "RPM";
                _translations["Load kPa"] = "Load (kPa)";
                _translations["Etki alanÄ±"] = "Etki AlanÄ±";
                _translations["AFR DÃ¼zelt"] = "AFR DÃ¼zelt";
                _translations["Basemap Uygula"] = "Basemap Uygula";
                _translations["NotlarÄ± Yenile"] = "NotlarÄ± Yenile";

                // Status Bar & Helper Warns
                _translations["Aktif profil:"] = "Aktif profil:";
                _translations["AraÃ§:"] = "AraÃ§:";
                _translations["disclaimer_notes"] = "ROM dosyasÄ± kullanÄ±cÄ±dan alÄ±nÄ±r. Uygulama telifli stock ROM indirmez veya daÄŸÄ±tmaz; test/basemap Ã¼retir ve kendi okuduÄŸun ROM Ã¼zerinde Ã§alÄ±ÅŸÄ±r.";
                _translations["rom_not_loaded_status"] = "ROM yÃ¼klenmedi. Dosya â†’ AÃ§ ile baÅŸlayÄ±n.";
                _translations["checksum_ok"] = "âœ… Checksum OK";
                _translations["checksum_error"] = "âŒ Checksum HATA";
                _translations["rom_warn_msg"] = "Ã–nce bir ROM dosyasÄ± yÃ¼kleyin.";
                _translations["rom_warn_title"] = "ROM Yok";
                _translations["discard_confirm_msg"] = "KaydedilmemiÅŸ deÄŸiÅŸiklikler var. Devam et?";
                _translations["discard_confirm_title"] = "UyarÄ±";
                _translations["aktif"] = "aktif";
                _translations["aktif deÄŸil"] = "aktif deÄŸil";
                _translations["BaÄŸlandÄ±"] = "BaÄŸlandÄ±";
                _translations["BaÄŸlanÄ±yor..."] = "BaÄŸlanÄ±yor...";
                _translations["Hata"] = "Hata";
                _translations["BaÄŸlÄ± DeÄŸil"] = "BaÄŸlÄ± DeÄŸil";
                _translations["ONLINE"] = "ONLINE";
                _translations["OFFLINE"] = "OFFLINE";
                _translations["BAÄLANIYOR"] = "BAÄLANIYOR";
                _translations["HATA"] = "HATA";
                _translations["SÄ°MÃœLASYON"] = "SÄ°MÃœLASYON";
                _translations["CANLI"] = "CANLI";
            }

            LoadSharedDefaults();
        }

        private static void LoadSharedDefaults()
        {
            AddShared("status_offline", "Ã‡EVRÄ°MDIÅI", "OFFLINE");
            AddShared("status_online", "Ã‡EVRÄ°MÄ°Ã‡Ä°", "ONLINE");
            AddShared("status_connecting", "BAÄLANIYOR", "CONNECTING");
            AddShared("status_error", "HATA", "ERROR");
            AddShared("status_disconnected", "BAÄLI DEÄÄ°L", "DISCONNECTED");

            AddShared("status_prog_offline", "ğŸ”Œ PROG: Ã‡EVRÄ°MDIÅI", "ğŸ”Œ PROG: OFFLINE");
            AddShared("status_prog_online", "ğŸ”Œ PROG: Ã‡EVRÄ°MÄ°Ã‡Ä°", "ğŸ”Œ PROG: ONLINE");
            AddShared("status_prog_connecting", "ğŸ”Œ PROG: BAÄLANIYOR", "ğŸ”Œ PROG: CONNECTING");
            AddShared("status_prog_error", "ğŸ”Œ PROG: HATA", "ğŸ”Œ PROG: ERROR");

            AddShared("status_emu_offline", "ğŸ® EMU: Ã‡EVRÄ°MDIÅI", "ğŸ® EMU: OFFLINE");
            AddShared("status_emu_online", "ğŸ® EMU: Ã‡EVRÄ°MÄ°Ã‡Ä°", "ğŸ® EMU: ONLINE");
            AddShared("status_emu_connecting", "ğŸ® EMU: BAÄLANIYOR", "ğŸ® EMU: CONNECTING");
            AddShared("status_emu_error", "ğŸ® EMU: HATA", "ğŸ® EMU: ERROR");

            AddShared("status_simulation", "ğŸŸ¢ SÄ°MÃœLASYON", "ğŸŸ¢ SIMULATION");
            AddShared("status_live", "ğŸ”´ CANLI", "ğŸ”´ LIVE");
            AddShared("status_connection_state", "BaÄŸlantÄ± Durumu", "Connection State");
            AddShared("status_queue_depth", "Kuyruk DerinliÄŸi", "Queue Depth");
            AddShared("status_average_latency", "Ortalama Gecikme", "Average Latency");
            AddShared("status_error_retry", "Hata / Yeniden Deneme", "Error / Retry");
            AddShared("status_dropped_writes", "DÃ¼ÅŸen Yazmalar", "Dropped Writes");
            AddShared("status_connected", "â— BaÄŸlandÄ±", "â— Connected");

            AddShared("status_auto_tune_off", "Durum: OFF", "Status: OFF");
            AddShared("status_auto_tune_running", "Durum: SÃ¼rÃ¼yor", "Status: Running");
            AddShared("status_auto_tune_paused", "Durum: AskÄ±da", "Status: Paused");
            AddShared("status_auto_tune_stopped", "Durum: Durduruldu", "Status: Stopped");
            AddShared("status_safety_safe", "GÃ¼venlik Durumu: SAFE", "Safety Status: SAFE");
            AddShared("status_safety_unknown", "GÃ¼venlik Durumu: --", "Safety Status: --");
            AddShared("status_safety_violation", "GÃ¼venlik Durumu: VIOLATION", "Safety Status: VIOLATION");
            AddShared("status_user_role", "KullanÄ±cÄ± RolÃ¼", "User Role");
            AddShared("status_user_role_professional", "KullanÄ±cÄ± RolÃ¼: Professional", "User Role: Professional");
            AddShared("status_ecu_connection", "ECU BaÄŸlantÄ±sÄ±", "ECU Connection");
            AddShared("status_ecu_connection_none", "ECU BaÄŸlantÄ±sÄ±: Yok", "ECU Connection: None");
            AddShared("status_tuning_quality", "Tuning Kalite Skoru", "Tuning Quality Score");
            AddShared("status_tuning_quality_value", "Tuning Kalite Skoru: {0:0.0}%", "Tuning Quality Score: {0:0.0}%");
            AddShared("warn_safety_violation", "[WARN] Safety Violation: {0}", "[WARN] Safety Violation: {0}");

            AddShared("autotune_commit", "Commit", "Commit");
            AddShared("autotune_tune", "Tune", "Tune");
            AddShared("autotune_map", "Map", "Map");
            AddShared("autotune_wildcard", "[*,*]", "[*,*]");
            AddShared("autotune_unknown", "--", "--");
            AddShared("autotune_success_rate", "100%", "100%");
            AddShared("autotune_richen", "ZenginleÅŸtir", "Richen");
            AddShared("autotune_leanen", "FakirleÅŸtir", "Lean");
            AddShared("autotune_live_status_title", "CanlÄ± Durum ve GÃ¼venlik Limitleri", "Live Status and Safety Limits");
            AddShared("btn_at_start", "â–¶ BaÅŸlat", "â–¶ Start");
            AddShared("btn_at_pause", "â¸ Duraklat", "â¸ Pause");
            AddShared("btn_at_resume", "â–¶ Devam Et", "â–¶ Resume");
            AddShared("btn_at_stop", "â¹ Durdur", "â¹ Stop");
            AddShared("autotune_user_advanced", "Advanced", "Advanced");
            AddShared("autotune_user_beginner", "Beginner", "Beginner");

            AddShared("csv_loaded_status", "CSV yÃ¼klendi", "CSV loaded");
            AddShared("autotune_session_started", "AutoTune Oturumu BaÅŸlatÄ±ldÄ±", "AutoTune Session Started");
            AddShared("autotune_session_paused", "AutoTune Oturumu DuraklatÄ±ldÄ±", "AutoTune Session Paused");
            AddShared("autotune_session_resumed", "AutoTune Oturumu Devam Ettiriliyor", "AutoTune Session Resumed");
            AddShared("autotune_session_stopped", "AutoTune Oturumu Durduruldu", "AutoTune Session Stopped");
            AddShared("wideband_correction_applied", "Wideband AFR Ã¶lÃ§Ã¼mÃ¼ne gÃ¶re yakÄ±t haritasÄ± dÃ¼zeltildi.", "Fuel map corrected according to wideband AFR measurement.");
            AddShared("stock_restored_status", "â†©  Stock ROM'a dÃ¶ndÃ¼rÃ¼ldÃ¼.", "â†©  Restored to stock ROM.");
            AddShared("status_empty", "", "");

            AddShared("CH341A EEPROM ProgramlayÄ±cÄ±", "CH341A EEPROM ProgramlayÄ±cÄ±", "CH341A EEPROM Programmer");
            AddShared("Ã‡ip Tipi:", "Ã‡ip Tipi:", "Chip Type:");
            AddShared("BaÄŸlan", "BaÄŸlan", "Connect");
            AddShared("BaÄŸlantÄ±yÄ± Kes", "BaÄŸlantÄ±yÄ± Kes", "Disconnect");
            AddShared("Ã‡ipten Oku", "Ã‡ipten Oku", "Read Chip");
            AddShared("Ã‡ipe Yaz", "Ã‡ipe Yaz", "Write Chip");
            AddShared("Ã‡ipi Sil", "Ã‡ipi Sil", "Erase Chip");
            AddShared("Ã‡ipi DoÄŸrula", "Ã‡ipi DoÄŸrula", "Verify Chip");
            AddShared("Ä°ÅŸlem KaydÄ±:", "Ä°ÅŸlem KaydÄ±:", "Operation Log:");
            AddShared("Ä°lerleme:", "Ä°lerleme:", "Progress:");
            AddShared("CanlÄ± OBD1 ArÄ±za KodlarÄ± (DTC)", "CanlÄ± OBD1 ArÄ±za KodlarÄ± (DTC)", "Live OBD1 Diagnostic Trouble Codes (DTC)");
            AddShared("OBD1 seri portu seÃ§in, baÄŸlantÄ± kurun, sonra kodu okuyun.", "OBD1 seri portu seÃ§in, baÄŸlantÄ± kurun, sonra kodu okuyun.", "Select the OBD1 serial port, connect, then read the code.");
            AddShared("ArÄ±za KodlarÄ±nÄ± Oku", "ArÄ±za KodlarÄ±nÄ± Oku", "Read DTCs");
            AddShared("ArÄ±za KodlarÄ±nÄ± Temizle", "ArÄ±za KodlarÄ±nÄ± Temizle", "Clear DTCs");
            AddShared("AkÃ¼ VoltajÄ±:", "AkÃ¼ VoltajÄ±:", "Battery Voltage:");
            AddShared("Ã‡alÄ±ÅŸma Modu:", "Ã‡alÄ±ÅŸma Modu:", "Working Mode:");
            AddShared("Ayar Profili:", "Ayar Profili:", "Profile:");
            AddShared("KullanÄ±cÄ± RolÃ¼:", "KullanÄ±cÄ± RolÃ¼:", "User Role:");
            AddShared("CanlÄ± Durum ve GÃ¼venlik Limitleri", "CanlÄ± Durum ve GÃ¼venlik Limitleri", "Live Status and Safety Limits");
            AddShared("RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator");
            AddShared("BaÄŸlantÄ± Durumu:", "BaÄŸlantÄ± Durumu:", "Connection State:");
            AddShared("Kuyruk DerinliÄŸi:", "Kuyruk DerinliÄŸi:", "Queue Depth:");
            AddShared("Ortalama Gecikme:", "Ortalama Gecikme:", "Average Latency:");
            AddShared("Hata / Yeniden Deneme:", "Hata / Yeniden Deneme:", "Error / Retry:");
            AddShared("DÃ¼ÅŸen Yazmalar:", "DÃ¼ÅŸen Yazmalar:", "Dropped Writes:");
            AddShared("Gorunum Secenegi", "GÃ¶rÃ¼nÃ¼m SeÃ§eneÄŸi", "View Option");
            AddShared("Basemap hedefi", "Basemap Hedefi", "Basemap Target");
            AddShared("Basemap Uygula", "Basemap Uygula", "Apply Basemap");
            AddShared("NotlarÄ± Yenile", "NotlarÄ± Yenile", "Refresh Notes");
            AddShared("AFR DÃ¼zelt", "AFR DÃ¼zelt", "Correct AFR");
            AddShared("Wideband yakÄ±t dÃ¼zeltme", "Wideband YakÄ±t DÃ¼zeltme", "Wideband fuel correction");
            AddShared("Ã–lÃ§Ã¼len AFR", "Ã–lÃ§Ã¼len AFR", "Measured AFR");
            AddShared("Load kPa", "Load kPa", "Load kPa");
            AddShared("Etki alanÄ±", "Etki AlanÄ±", "Processing radius");

            AddShared("ğŸš¨ Limit & Emniyet AyarlarÄ±", "ğŸš¨ Limit & Emniyet AyarlarÄ±", "ğŸš¨ Limit & Safety Settings");
            AddShared("ğŸŒ¡ï¸ Termal DÃ¼zeltmeler & IAT/EGT", "ğŸŒ¡ï¸ Termal DÃ¼zeltmeler & IAT/EGT", "ğŸŒ¡ï¸ Thermal Corrections & IAT/EGT");
            AddShared("ğŸ® GÃ¼venlik Koruma SimÃ¼latÃ¶rÃ¼", "ğŸ® GÃ¼venlik Koruma SimÃ¼latÃ¶rÃ¼", "ğŸ® Safety Protection Simulator");
            AddShared("ğŸš¨ Genel GÃ¼venlik Limitleri", "ğŸš¨ Genel GÃ¼venlik Limitleri", "ğŸš¨ General Safety Limits");
            AddShared("ğŸ“ˆ RPM vs Min YaÄŸ BasÄ±ncÄ± SÄ±nÄ±r EÄŸrisi", "ğŸ“ˆ RPM vs Min YaÄŸ BasÄ±ncÄ± SÄ±nÄ±r EÄŸrisi", "ğŸ“ˆ RPM vs Minimum Oil Pressure Curve");
            AddShared("ğŸŒ¡ï¸ Termal YÃ¶netim & IAT DÃ¼zeltmeleri", "ğŸŒ¡ï¸ Termal YÃ¶netim & IAT DÃ¼zeltmeleri", "ğŸŒ¡ï¸ Thermal Management & IAT Corrections");
            AddShared("ğŸ•¹ï¸ SimÃ¼lasyon SÃ¼rÃ¼ÅŸ Parametreleri", "ğŸ•¹ï¸ SimÃ¼lasyon SÃ¼rÃ¼ÅŸ Parametreleri", "ğŸ•¹ï¸ Simulation Driving Parameters");
            AddShared("ğŸ›¡ï¸ Koruma Emniyet DurumlarÄ±", "ğŸ›¡ï¸ Koruma Emniyet DurumlarÄ±", "ğŸ›¡ï¸ Safety Protection States");
            AddShared("ğŸ”„ AlarmlarÄ± SÄ±fÄ±rla / Koruma Reset", "ğŸ”„ AlarmlarÄ± SÄ±fÄ±rla / Koruma Reset", "ğŸ”„ Reset Alarms / Protection Reset");
            AddShared("Ã–zel koruma eÅŸiklerinde bir problem algÄ±lanmadÄ±.", "Ã–zel koruma eÅŸiklerinde bir problem algÄ±lanmadÄ±.", "No issue detected at the custom protection thresholds.");
            AddShared("Aktif Limit Devri:", "Aktif Limit Devri:", "Active Limit RPM:");
            AddShared("Toplam Avans KÄ±sma:", "Toplam Avans KÄ±sma:", "Total Timing Pull:");
            AddShared("EGT YakÄ±t ArtÄ±ÅŸÄ±:", "EGT YakÄ±t ArtÄ±ÅŸÄ±:", "EGT Fuel Enrichment:");
            AddShared("Fan RÃ¶lesi Ã‡Ä±kÄ±ÅŸÄ±:", "Fan RÃ¶lesi Ã‡Ä±kÄ±ÅŸÄ±:", "Fan Relay Output:");
            AddShared("Protokol & DonanÄ±m ArayÃ¼zleri", "Protokol & DonanÄ±m ArayÃ¼zleri", "Protocols & Hardware Interfaces");
            AddShared("A2L Veri TabanÄ± / TanÄ±mlamalar (ASAP2)", "A2L Veri TabanÄ± / TanÄ±mlamalar (ASAP2)", "A2L Database / Definitions (ASAP2)");
            AddShared("Freeze Frame Verileri (DTC Hata AnÄ±)", "Freeze Frame Verileri (DTC Hata AnÄ±)", "Freeze Frame Data (DTC Event)");
            AddShared("ğŸ“¶ Protokol & DonanÄ±m ArayÃ¼zleri", "ğŸ“¶ Protokol & DonanÄ±m ArayÃ¼zleri", "ğŸ“¶ Protocol & Hardware Interfaces");
            AddShared("ğŸ“· Freeze Frame GÃ¼nlÃ¼kleri", "ğŸ“· Freeze Frame GÃ¼nlÃ¼kleri", "ğŸ“· Freeze Frame Logs");
            AddShared("ğŸ“ Standartlar & A2L Export", "ğŸ“ Standartlar & A2L Export", "ğŸ“ Standards & A2L Export");
            AddShared("DonanÄ±m ArayÃ¼zÃ¼:", "DonanÄ±m ArayÃ¼zÃ¼:", "Hardware Interface:");
            AddShared("Datalog Baud Rate:", "Datalog Baud Rate:", "Datalog Baud Rate:");
            AddShared("WiFi IP Address:", "WiFi IP Address:", "WiFi IP Address:");
            AddShared("WiFi Port:", "WiFi Port:", "WiFi Port:");
            AddShared("âš¡ ECU Diagnostic Self-Test BaÅŸlat", "âš¡ ECU Diagnostic Self-Test BaÅŸlat", "âš¡ Start ECU Diagnostic Self-Test");
            AddShared("ğŸ–¥ï¸ CanlÄ± Ä°letiÅŸim & Hata TanÄ± Konsolu", "ğŸ–¥ï¸ CanlÄ± Ä°letiÅŸim & Hata TanÄ± Konsolu", "ğŸ–¥ï¸ Live Communication & Diagnostics Console");
            AddShared("ArÄ±za Kodu SeÃ§:", "ArÄ±za Kodu SeÃ§:", "Select DTC:");
            AddShared("ğŸ’¥ Hata Tetikle (Freeze Frame)", "ğŸ’¥ Hata Tetikle (Freeze Frame)", "ğŸ’¥ Trigger Fault (Freeze Frame)");
            AddShared("ğŸ’¾ A2L Harita DosyasÄ± (.a2l) Ä°hraÃ§ Et", "ğŸ’¾ A2L Harita DosyasÄ± (.a2l) Ä°hraÃ§ Et", "ğŸ’¾ Export A2L Map File (.a2l)");
            AddShared("ArÄ±za Kodu", "ArÄ±za Kodu", "Fault Code");
            AddShared("Tetiklenme Saati", "Tetiklenme Saati", "Trigger Time");
            AddShared("Motor Devri (RPM)", "Motor Devri (RPM)", "Engine RPM");
            AddShared("Su SÄ±caklÄ±ÄŸÄ± (Â°C)", "Su SÄ±caklÄ±ÄŸÄ± (Â°C)", "Coolant Temp (Â°C)");
            AddShared("Intake SÄ±caklÄ±ÄŸÄ± (Â°C)", "Intake SÄ±caklÄ±ÄŸÄ± (Â°C)", "Intake Temp (Â°C)");
            AddShared("HÄ±z (km/h)", "HÄ±z (km/h)", "Speed (km/h)");
            AddShared("Boost MAP (kPa)", "Boost MAP (kPa)", "Boost MAP (kPa)");
            AddShared("ğŸ“ PROJE VE MOTOR META VERÄ°LERÄ°", "ğŸ“ PROJE VE MOTOR META VERÄ°LERÄ°", "ğŸ“ PROJECT AND ENGINE METADATA");
            AddShared("ECU Seri NumarasÄ±:", "ECU Seri NumarasÄ±:", "ECU Serial Number:");
            AddShared("Hardware Revizyonu:", "Hardware Revizyonu:", "Hardware Revision:");
            AddShared("Åasi NumarasÄ± (VIN):", "Åasi NumarasÄ± (VIN):", "VIN Number:");
            AddShared("Kasa Kodu (Ã–rn: EG6, EK4):", "Kasa Kodu (Ã–rn: EG6, EK4):", "Chassis Code (e.g. EG6, EK4):");
            AddShared("SÄ±kÄ±ÅŸtÄ±rma OranÄ± (:1):", "SÄ±kÄ±ÅŸtÄ±rma OranÄ± (:1):", "Compression Ratio (:1):");
            AddShared("Eksantrik Profili:", "Eksantrik Profili:", "Camshaft Profile:");
            AddShared("ÅanzÄ±man Tipi:", "ÅanzÄ±man Tipi:", "Gearbox Type:");
            AddShared("Ä°ndÃ¼ksiyon TÃ¼rÃ¼:", "Ä°ndÃ¼ksiyon TÃ¼rÃ¼:", "Induction Type:");
            AddShared("ğŸ’¾ DeÄŸiÅŸiklikleri Kaydet", "ğŸ’¾ DeÄŸiÅŸiklikleri Kaydet", "ğŸ’¾ Save Changes");
            AddShared("âš¡ CanlÄ± Analiz", "âš¡ CanlÄ± Analiz", "âš¡ Live Analysis");
            AddShared("ğŸ”Œ OBD1 ECU Pinout", "ğŸ”Œ OBD1 ECU Pinout", "ğŸ”Œ OBD1 ECU Pinout");
            AddShared("Ara:", "Ara:", "Search:");
            AddShared("Soket:", "Soket:", "Connector:");
            AddShared("Hepsi", "Hepsi", "All");
            AddShared("Soket A", "Soket A", "Socket A");
            AddShared("Soket B", "Soket B", "Socket B");
            AddShared("Soket D", "Soket D", "Socket D");
            AddShared("Pin", "Pin", "Pin");
            AddShared("Sembol", "Sembol", "Symbol");
            AddShared("Sinyal TÃ¼rÃ¼", "Sinyal TÃ¼rÃ¼", "Signal Type");
            AddShared("Kablo Rengi", "Kablo Rengi", "Wire Color");
            AddShared("AÃ§Ä±klama", "AÃ§Ä±klama", "Description");
            AddShared("ğŸ VTEC GeÃ§iÅŸ KoÅŸullarÄ±", "ğŸ VTEC GeÃ§iÅŸ KoÅŸullarÄ±", "ğŸ VTEC Transition Conditions");
            AddShared("VTEC Minimum Devir (RPM):", "VTEC Minimum Devir (RPM):", "VTEC Minimum RPM:");
            AddShared("VTEC Minimum HÄ±z (km/h):", "VTEC Minimum HÄ±z (km/h):", "VTEC Minimum Speed (km/h):");
            AddShared("VTEC Engellenen Vites SeÃ§enekleri (Gear Lockout out):", "VTEC Engellenen Vites SeÃ§enekleri (Gear Lockout):", "VTEC Locked Gear Options (Gear Lockout):");
            AddShared("1. Vites", "1. Vites", "1st Gear");
            AddShared("2. Vites", "2. Vites", "2nd Gear");
            AddShared("3. Vites", "3. Vites", "3rd Gear");
            AddShared("4. Vites", "4. Vites", "4th Gear");
            AddShared("5. Vites", "5. Vites", "5th Gear");
            AddShared("6. Vites", "6. Vites", "6th Gear");
            AddShared("ğŸ•¹ï¸ SÃ¼rÃ¼ÅŸ SimÃ¼latÃ¶r Girdileri", "ğŸ•¹ï¸ SÃ¼rÃ¼ÅŸ SimÃ¼latÃ¶r Girdileri", "ğŸ•¹ï¸ Driving Simulator Inputs");
            AddShared("Motor Devri (RPM):", "Motor Devri (RPM):", "Engine RPM:");
            AddShared("AraÃ§ HÄ±zÄ± (km/h):", "AraÃ§ HÄ±zÄ± (km/h):", "Vehicle Speed (km/h):");
            AddShared("Aktif Vites (Gear):", "Aktif Vites (Gear):", "Current Gear:");
            AddShared("âš¡ Scramble Boost DÃ¼ÄŸmesi (GeÃ§ici Avans / Boost)", "âš¡ Scramble Boost DÃ¼ÄŸmesi (GeÃ§ici Avans / Boost)", "âš¡ Scramble Boost Button (Temporary Advance / Boost)");
            AddShared("âš ï¸ KaÃ§ak / Wastegate Hortum YÄ±rtÄ±lmasÄ± SimÃ¼lasyonu", "âš ï¸ KaÃ§ak / Wastegate Hortum YÄ±rtÄ±lmasÄ± SimÃ¼lasyonu", "âš ï¸ Leak / Wastegate Hose Tear Simulation");
            AddShared("ğŸ•¹ï¸ Solenoid & PID Kontrol Ã‡Ä±ktÄ±larÄ±", "ğŸ•¹ï¸ Solenoid & PID Kontrol Ã‡Ä±ktÄ±larÄ±", "ğŸ•¹ï¸ Solenoid & PID Control Outputs");
            AddShared("Hedef Turbo BasÄ±ncÄ±:", "Hedef Turbo BasÄ±ncÄ±:", "Target Boost:");
            AddShared("Aktif Turbo BasÄ±ncÄ±:", "Aktif Turbo BasÄ±ncÄ±:", "Actual Boost:");
            AddShared("Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:");
            AddShared("VTEC Valf Sinyali (Solenoid):", "VTEC Valf Sinyali (Solenoid):", "VTEC Valve Signal (Solenoid):");
            AddShared("âš¡ PASÄ°F (VTEC LOCK)", "âš¡ PASÄ°F (VTEC LOCK)", "âš¡ INACTIVE (VTEC LOCK)");
            AddShared("âœ… Wastegate Sistemi GÃ¼venli AralÄ±kta Ã‡alÄ±ÅŸÄ±yor", "âœ… Wastegate Sistemi GÃ¼venli AralÄ±kta Ã‡alÄ±ÅŸÄ±yor", "âœ… Wastegate System Operating Within Safe Range");
            AddShared("ğŸï¸ Virtual Dyno Parametreleri", "ğŸï¸ Virtual Dyno Parametreleri", "ğŸï¸ Virtual Dyno Parameters");
            AddShared("AraÃ§ AÄŸÄ±rlÄ±ÄŸÄ± (Kg):", "AraÃ§ AÄŸÄ±rlÄ±ÄŸÄ± (Kg):", "Vehicle Weight (Kg):");
            AddShared("Aktarma KaybÄ± (%):", "Aktarma KaybÄ± (%):", "Drivetrain Loss (%):");
            AddShared("DÃ¼zeltme StandardÄ±:", "DÃ¼zeltme StandardÄ±:", "Correction Standard:");
            AddShared("SimÃ¼le Manifold BasÄ±ncÄ± (Boost):", "SimÃ¼le Manifold BasÄ±ncÄ± (Boost):", "Simulated Manifold Pressure (Boost):");
            AddShared("âš¡ Sanal Dyno Testini Ã‡alÄ±ÅŸtÄ±r", "âš¡ Sanal Dyno Testini Ã‡alÄ±ÅŸtÄ±r", "âš¡ Run Virtual Dyno Test");
            AddShared("Azami GÃ¼Ã§: -- HP @ -- RPM | Azami Tork: -- Nm", "Azami GÃ¼Ã§: -- HP @ -- RPM | Azami Tork: -- Nm", "Peak Power: -- HP @ -- RPM | Peak Torque: -- Nm");
            AddShared("ğŸ“ˆ Sanal GÃ¼Ã§ / Tork Ã‡Ä±ktÄ± Tablosu", "ğŸ“ˆ Sanal GÃ¼Ã§ / Tork Ã‡Ä±ktÄ± Tablosu", "ğŸ“ˆ Virtual Power / Torque Output Table");
            AddShared("â±ï¸ Pist PerformansÄ± & Vites GeÃ§iÅŸ Ã–lÃ§er", "â±ï¸ Pist PerformansÄ± & Vites GeÃ§iÅŸ Ã–lÃ§er", "â±ï¸ Track Performance & Shift Timer");
            AddShared("Lastik Ã‡apÄ± (Ä°nÃ§):", "Lastik Ã‡apÄ± (Ä°nÃ§):", "Tyre Diameter (Inch):");
            AddShared("ÅanzÄ±man Vites OranÄ±:", "ÅanzÄ±man Vites OranÄ±:", "Gear Ratio:");
            AddShared("Ayna Mahruti OranÄ±:", "Ayna Mahruti OranÄ±:", "Final Drive Ratio:");
            AddShared("ğŸš€ 0 - 100 km/h HÄ±zlanma:", "ğŸš€ 0 - 100 km/h HÄ±zlanma:", "ğŸš€ 0 - 100 km/h Acceleration:");
            AddShared("âœˆï¸ 100 - 200 km/h HÄ±zlanma:", "âœˆï¸ 100 - 200 km/h HÄ±zlanma:", "âœˆï¸ 100 - 200 km/h Acceleration:");
            AddShared("ğŸ”Œ Vites GeÃ§iÅŸ YavaÅŸlamasÄ±:", "ğŸ”Œ Vites GeÃ§iÅŸ YavaÅŸlamasÄ±:", "ğŸ”Œ Shift Delay:");
            AddShared("ğŸŒ¿ Kalibrasyon SÃ¼rÃ¼m KontrolÃ¼ (Branching)", "ğŸŒ¿ Kalibrasyon SÃ¼rÃ¼m KontrolÃ¼ (Branching)", "ğŸŒ¿ Calibration Version Control (Branching)");
            AddShared("Aktif Dal (Branch):", "Aktif Dal (Branch):", "Active Branch:");
            AddShared("Yeni Dal OluÅŸtur:", "Yeni Dal OluÅŸtur:", "Create New Branch:");
            AddShared("â• Dal AÃ§", "â• Dal AÃ§", "â• Open Branch");
            AddShared("HafÄ±za Commit AÃ§Ä±klamasÄ±:", "HafÄ±za Commit AÃ§Ä±klamasÄ±:", "Memory Commit Description:");
            AddShared("ğŸ’¾ Commit", "ğŸ’¾ Commit", "ğŸ’¾ Commit");
            AddShared("ğŸ” RAM DeÄŸer Watchdog (MCU Mercek)", "ğŸ” RAM DeÄŸer Watchdog (MCU Mercek)", "ğŸ” RAM Value Watchdog (MCU Lens)");
            AddShared("CanlÄ± OBD1 ArÄ±za KodlarÄ± (DTC)", "CanlÄ± OBD1 ArÄ±za KodlarÄ± (DTC)", "Live OBD1 Diagnostic Trouble Codes (DTC)");
            AddShared("PCB / Map", "PCB / Map", "PCB / Map");

            // --- USER INTERFACE APP ADDITIONS ---
            // Main Tab Buttons
            AddShared("tab_fuel", "â›½ YakÄ±t", "â›½ Fuel");
            AddShared("tab_ignition", "âš¡ AteÅŸleme", "âš¡ Ignition");
            AddShared("tab_tuning_assistant", "ğŸ§  Tuning AsistanÄ±", "ğŸ§  Tuning Assistant");
            AddShared("tab_diff", "ğŸ” Diff", "ğŸ” Diff");
            AddShared("tab_telemetry", "ğŸ“Š Telemetri", "ğŸ“Š Telemetry");
            AddShared("tab_part_viewer", "ğŸ”© 3D ParÃ§a", "ğŸ”© 3D Part");
            AddShared("tab_autotune", "ğŸš€ AutoTune", "ğŸš€ AutoTune");
            AddShared("tab_project_pinout", "âœï¸ Proje & Pinout", "âœï¸ Project & Pinout");
            AddShared("tab_analysis_decompiler", "ğŸ” Analiz & Decompiler", "ğŸ” Analysis & Decompiler");
            AddShared("tab_adv_fuel", "ğŸš€ Advanced Fuel", "ğŸš€ Advanced Fuel");
            AddShared("tab_adv_ignition", "âš¡ Advanced Ignition", "âš¡ Advanced Ignition");
            AddShared("tab_vtec_boost", "ğŸ VTEC & Boost", "ğŸ VTEC & Boost");
            AddShared("tab_engine_protection", "ğŸ›¡ï¸ Engine Protection", "ğŸ›¡ï¸ Engine Protection");
            AddShared("tab_diagnostics_a2l", "ğŸ“¶ Diagnostics & A2L", "ğŸ“¶ Diagnostics & A2L");
            AddShared("tab_dyno_logs", "ğŸ“Š Dyno, Logs & Branching", "ğŸ“Š Dyno & Logs");
            AddShared("tab_hardware_control", "ğŸ”Œ DonanÄ±m Kontrol", "ğŸ”Œ Hardware Control");

            // Sub Tab pages in Advanced Fuel
            AddShared("â›½ Alpha-N VE", "â›½ Alpha-N VE", "â›½ Alpha-N VE");
            AddShared("ğŸ”Œ MAF Ã–lÃ§eÄŸi", "ğŸ”Œ MAF Ã–lÃ§eÄŸi", "ğŸ”Œ MAF Scale");
            AddShared("ğŸŒ¡ï¸ SoÄŸuk Ã‡alÄ±ÅŸma & DÃ¼zeltmeler", "ğŸŒ¡ï¸ SoÄŸuk Ã‡alÄ±ÅŸma & DÃ¼zeltmeler", "ğŸŒ¡ï¸ Cold Start & Corrections");
            AddShared("âš¡ CanlÄ± EnjektÃ¶r & DÃ¼zeltme SimÃ¼latÃ¶rÃ¼", "âš¡ CanlÄ± EnjektÃ¶r & DÃ¼zeltme SimÃ¼latÃ¶rÃ¼", "âš¡ Live Injector Simulator");
            AddShared("Taban YakÄ±t SÃ¼resi (ms):", "Taban YakÄ±t SÃ¼resi (ms):", "Base Fuel Duration (ms):");
            AddShared("Motor SÄ±caklÄ±k (Â°C ECT):", "Motor SÄ±caklÄ±k (Â°C ECT):", "Engine Temp (Â°C ECT):");
            AddShared("YakÄ±t BasÄ±ncÄ± (psi - Aktif):", "YakÄ±t BasÄ±ncÄ± (psi - Aktif):", "Fuel Pressure (psi - Active):");
            AddShared("Hedef YakÄ±t BasÄ±ncÄ± (psi):", "Hedef YakÄ±t BasÄ±ncÄ± (psi):", "Target Fuel Pressure (psi):");
            AddShared("Gaz DeÄŸiÅŸim HÄ±zÄ± (dTPS %/s):", "Gaz DeÄŸiÅŸim HÄ±zÄ± (dTPS %/s):", "Throttle Change Rate (dTPS %/s):");
            AddShared("Alpha-N YakÄ±t Modunu Kullan (TPS vs RPM)", "Alpha-N YakÄ±t Modunu Kullan (TPS vs RPM)", "Use Alpha-N Fuel Mode (TPS vs RPM)");
            AddShared("ğŸ’¥ Gaz PedalÄ±na HÄ±zlÄ±ca Bas (Throttle Step Sim)", "ğŸ’¥ Gaz PedalÄ±na HÄ±zlÄ±ca Bas (Throttle Step Sim)", "ğŸ’¥ Blast Throttle (Throttle Step Sim)");
            AddShared("KÄ±sa Enjeksiyon Eklemesi (adder):", "KÄ±sa Enjeksiyon Eklemesi (adder):", "Short Pulse Adder (adder):");
            AddShared("GeÃ§ici YakÄ±t Havuzu (acc):", "GeÃ§ici YakÄ±t Havuzu (acc):", "Transient Fuel Pool (acc):");
            AddShared("Nihai Enjeksiyon SÃ¼resi (PW):", "Nihai Enjeksiyon SÃ¼resi (PW):", "Final Pulse Width (PW):");
            AddShared("EnjektÃ¶r GÃ¶rev DÃ¶ngÃ¼sÃ¼ (Duty):", "EnjektÃ¶r GÃ¶rev DÃ¶ngÃ¼sÃ¼ (Duty):", "Injector Duty Cycle (Duty):");
            AddShared("âœ… GÃ¶rev DÃ¶ngÃ¼sÃ¼ GÃ¼venli Limit AralÄ±ÄŸÄ±nda", "âœ… GÃ¶rev DÃ¶ngÃ¼sÃ¼ GÃ¼venli Limit AralÄ±ÄŸÄ±nda", "âœ… Duty Cycle Within Safe Limit Range");
            AddShared("alarm_injector_saturation", "ğŸš¨ KRÄ°TÄ°K: ENJEKTÃ–R DOYUMA ULAÅTI (%{0})!", "ğŸš¨ CRITICAL: INJECTOR SATURATED (%{0})!");

            // Sub Tab pages in Advanced Ignition
            AddShared("âš¡ Ã‡alÄ±ÅŸtÄ±rma & Silindir DÃ¼zeltmeleri", "âš¡ Ã‡alÄ±ÅŸtÄ±rma & Silindir DÃ¼zeltmeleri", "âš¡ Cranking & Cylinder Offsets");
            AddShared("ğŸ”Œ SensÃ¶r Kalibrasyon EÄŸrisi", "ğŸ”Œ SensÃ¶r Kalibrasyon EÄŸrisi", "ğŸ”Œ Sensor Calibration Curve");
            AddShared("ğŸ“¡ CAN Bus Kod Ã‡Ã¶zÃ¼cÃ¼", "ğŸ“¡ CAN Bus Kod Ã‡Ã¶zÃ¼cÃ¼", "ğŸ“¡ CAN Bus Decoder");
            AddShared("ğŸ§  MBT Avans Ã–nerici", "ğŸ§  MBT Avans Ã–nerici", "ğŸ§  MBT Advance Advisor");
            AddShared("ğŸ”‘ Ã‡alÄ±ÅŸtÄ±rma AnÄ± Avans HaritasÄ±", "ğŸ”‘ Ã‡alÄ±ÅŸtÄ±rma AnÄ± Avans HaritasÄ±", "ğŸ”‘ Cranking Ignition Map");
            AddShared("ğŸ”¥ Bireysel Silindir Avans DÃ¼zeltmeleri", "ğŸ”¥ Bireysel Silindir Avans DÃ¼zeltmeleri", "ğŸ”¥ Individual Cylinder Ignition Trims");
            AddShared("Silindir 1:", "Silindir 1:", "Cylinder 1:");
            AddShared("Silindir 2:", "Silindir 2:", "Cylinder 2:");
            AddShared("Silindir 3:", "Silindir 3:", "Cylinder 3:");
            AddShared("Silindir 4:", "Silindir 4:", "Cylinder 4:");
            AddShared("SensÃ¶r Tipi Kalibrasyon EÄŸrisi SeÃ§in:", "SensÃ¶r Tipi Kalibrasyon EÄŸrisi SeÃ§in:", "Select Sensor Type Calibration Curve:");
            AddShared("ğŸ”Œ Sinyal Linearizasyon SimÃ¼lasyonu", "ğŸ”Œ Sinyal Linearizasyon SimÃ¼lasyonu", "ğŸ”Œ Signal Linearization Simulation");
            AddShared("Analog Voltaj GiriÅŸi (0.0V - 5.0V):", "Analog Voltaj GiriÅŸi (0.0V - 5.0V):", "Analog Voltage Input (0.0V - 5.0V):");
            AddShared("Okunan Fiziksel DeÄŸer:", "Okunan Fiziksel DeÄŸer:", "Read Physical Value:");
            AddShared("ğŸ“¡ CAN Bus Ã‡erÃ§eve Ã‡Ã¶zÃ¼mleme TanÄ±mlarÄ±", "ğŸ“¡ CAN Bus Ã‡erÃ§eve Ã‡Ã¶zÃ¼mleme TanÄ±mlarÄ±", "ğŸ“¡ CAN Bus Frame Decode Definitions");
            AddShared("Frame ID (HEX):", "Frame ID (HEX):", "Frame ID (HEX):");
            AddShared("BaÅŸlangÄ±Ã§ Biti (Start Bit):", "BaÅŸlangÄ±Ã§ Biti (Start Bit):", "Start Bit:");
            AddShared("Bit UzunluÄŸu (Bit Len):", "Bit UzunluÄŸu (Bit Len):", "Bit Length:");
            AddShared("Ã‡arpan KatsayÄ± (Scale):", "Ã‡arpan KatsayÄ± (Scale):", "Scale Factor:");
            AddShared("Kayma KatsayÄ± (Offset):", "Kayma KatsayÄ± (Offset):", "Offset:");
            AddShared("Is Motorla Format (Big Endian)", "Is Motorla Format (Big Endian)", "Is Motorola Format (Big Endian)");
            AddShared("ğŸ“¡ CAN Mesaj Paketi CanlÄ± SimÃ¼lasyonu", "ğŸ“¡ CAN Mesaj Paketi CanlÄ± SimÃ¼lasyonu", "ğŸ“¡ CAN Message Packet Live Simulation");
            AddShared("SimÃ¼le Edilen 8-Byte Ã‡erÃ§eve Mesaj (Hex):", "SimÃ¼le Edilen 8-Byte Ã‡erÃ§eve Mesaj (Hex):", "Simulated 8-Byte Frame Message (Hex):");
            AddShared("Ã‡Ã¶zÃ¼mlenen SensÃ¶r Ã‡Ä±ktÄ±sÄ± (EGT):", "Ã‡Ã¶zÃ¼mlenen SensÃ¶r Ã‡Ä±ktÄ±sÄ± (EGT):", "Decoded Sensor Output (EGT):");
            AddShared("ğŸ§  MBT AteÅŸleme SimÃ¼lasyon Girdileri", "ğŸ§  MBT AteÅŸleme SimÃ¼lasyon Girdileri", "ğŸ§  MBT Ignition Simulation Inputs");
            AddShared("Emme Manifold YÃ¼kÃ¼ (kPa):", "Emme Manifold YÃ¼kÃ¼ (kPa):", "Intake Manifold Load (kPa):");
            AddShared("YakÄ±t Oktan OranÄ± (RON):", "YakÄ±t Oktan OranÄ± (RON):", "Fuel Octane Rating (RON):");
            AddShared("Mevcut Avans DeÄŸeri (Â°):", "Mevcut Avans DeÄŸeri (Â°):", "Current Advance Value (Â°):");
            AddShared("ğŸ§  AteÅŸleme Optimizasyon KararÄ±", "ğŸ§  AteÅŸleme Optimizasyon KararÄ±", "ğŸ§  Ignition Optimization Decision");
            AddShared("Modellenen Teorik MBT AvansÄ±:", "Modellenen Teorik MBT AvansÄ±:", "Modeled Theoretical MBT Advance:");
            AddShared("Sapma (Current - MBT):", "Sapma (Current - MBT):", "Deviation (Current - MBT):");
            AddShared("ğŸ”§ Avans DÃ¼zeltmesini Haritada Otomatik Ayarla", "ğŸ”§ Avans DÃ¼zeltmesini Haritada Otomatik Ayarla", "ğŸ”§ Auto Adjust Advance Trim on Map");

            // MetadataControl
            AddShared("ğŸ“ PROJE VE MOTOR META VERÄ°LERÄ°", "ğŸ“ PROJE VE MOTOR META VERÄ°LERÄ°", "ğŸ“ PROJECT AND ENGINE METADATA");
            AddShared("ECU Seri NumarasÄ±:", "ECU Seri NumarasÄ±:", "ECU Serial Number:");
            AddShared("Hardware Revizyonu:", "Hardware Revizyonu:", "Hardware Revision:");
            AddShared("Åasi NumarasÄ± (VIN):", "Åasi NumarasÄ± (VIN):", "Chassis Number (VIN):");
            AddShared("Kasa Kodu (Ã–rn: EG6, EK4):", "Kasa Kodu (Ã–rn: EG6, EK4):", "Chassis Code (e.g. EG6, EK4):");
            AddShared("SÄ±kÄ±ÅŸtÄ±rma OranÄ± (:1):", "SÄ±kÄ±ÅŸtÄ±rma OranÄ± (:1):", "Compression Ratio (:1):");
            AddShared("Eksantrik Profili:", "Eksantrik Profili:", "Camshaft Profile:");
            AddShared("ÅanzÄ±man Tipi:", "ÅanzÄ±man Tipi:", "Gearbox Type:");
            AddShared("Ä°ndÃ¼ksiyon TÃ¼rÃ¼:", "Ä°ndÃ¼ksiyon TÃ¼rÃ¼:", "Induction Type:");
            AddShared("ğŸ’¾ DeÄŸiÅŸiklikleri Kaydet", "ğŸ’¾ DeÄŸiÅŸiklikleri Kaydet", "ğŸ’¾ Save Changes");
            AddShared("âš¡ CanlÄ± Analiz", "âš¡ CanlÄ± Analiz", "âš¡ Live Analysis");
            AddShared("ğŸ”Œ OBD1 ECU Pinout", "ğŸ”Œ OBD1 ECU Pinout", "ğŸ”Œ OBD1 ECU Pinout");
            AddShared("Ara:", "Ara:", "Search:");
            AddShared("Soket:", "Soket:", "Connector:");

            // DiagnosticsControl
            AddShared("ğŸ“¶ Protokol & DonanÄ±m ArayÃ¼zleri", "ğŸ“¶ Protokol & DonanÄ±m ArayÃ¼zleri", "ğŸ“¶ Protocol & Hardware Interfaces");
            AddShared("ğŸ“· Freeze Frame GÃ¼nlÃ¼kleri", "ğŸ“· Freeze Frame GÃ¼nlÃ¼kleri", "ğŸ“· Freeze Frame Logs");
            AddShared("ğŸ“ Standartlar & A2L Export", "ğŸ“ Standartlar & A2L Export", "ğŸ“ Calibration Standards & A2L Export");
            AddShared("ğŸ“¡ DÃ¶nÃ¼ÅŸtÃ¼rÃ¼cÃ¼ & Protokol ArayÃ¼zÃ¼", "ğŸ“¡ DÃ¶nÃ¼ÅŸtÃ¼rÃ¼cÃ¼ & Protokol ArayÃ¼zÃ¼", "ğŸ“¡ Converter & Protocol Interface");
            AddShared("WiFi IP Address:", "WiFi IP Address:", "WiFi IP Address:");
            AddShared("WiFi Port:", "WiFi Port:", "WiFi Port:");
            AddShared("âš¡ ECU Diagnostic Self-Test BaÅŸlat", "âš¡ ECU Diagnostic Self-Test BaÅŸlat", "âš¡ Start ECU Diagnostic Self-Test");
            AddShared("ğŸ–¥ï¸ CanlÄ± Ä°letiÅŸim & Hata TanÄ± Konsolu", "ğŸ–¥ï¸ CanlÄ± Ä°letiÅŸim & Hata TanÄ± Konsolu", "ğŸ–¥ï¸ Live Communication & Diagnostics Console");
            AddShared("ğŸ“· Hata Kodu DondurulmuÅŸ Veri Ã‡erÃ§eveleri (Freeze Frames)", "ğŸ“· Hata Kodu DondurulmuÅŸ Veri Ã‡erÃ§eveleri (Freeze Frames)", "ğŸ“· Freeze Frame Diagnostic Data Logs");
            AddShared("ArÄ±za Kodu SeÃ§:", "ArÄ±za Kodu SeÃ§:", "Select Trouble Code:");
            AddShared("ğŸ’¥ Hata Tetikle (Freeze Frame)", "ğŸ’¥ Hata Tetikle (Freeze Frame)", "ğŸ’¥ Trigger Fault (Freeze Frame)");
            AddShared("ğŸ“ ASAM MCD-2 MC (A2L) Kalibrasyon Standart TanÄ±mlarÄ±", "ğŸ“ ASAM MCD-2 MC (A2L) Kalibrasyon Standart TanÄ±mlarÄ±", "ğŸ“ ASAM MCD-2 MC (A2L) Calibration Standards");
            AddShared("ğŸ’¾ A2L Harita DosyasÄ± (.a2l) Ä°hraÃ§ Et", "ğŸ’¾ A2L Harita DosyasÄ± (.a2l) Ä°hraÃ§ Et", "ğŸ’¾ Export A2L Map File (.a2l)");

            // DynoLogsControl & Versioning
            AddShared("ğŸ“Š Virtual Dyno & GÃ¼Ã§ AnalizÃ¶rÃ¼", "ğŸ“Š Virtual Dyno & GÃ¼Ã§ AnalizÃ¶rÃ¼", "ğŸ“Š Virtual Dyno & Power Analyzer");
            AddShared("â±ï¸ Pist SÃ¼rÃ¼ÅŸ & Performans", "â±ï¸ Pist SÃ¼rÃ¼ÅŸ & Performans", "â±ï¸ Track Stats & Performance");
            AddShared("ğŸŒ¿ Versiyon & RAM Watchdog", "ğŸŒ¿ Versiyon & RAM Watchdog", "ğŸŒ¿ Versioning & RAM Watchdog");
            AddShared("Lastik Ã‡apÄ± (Ä°nÃ§):", "Lastik Ã‡apÄ± (Ä°nÃ§):", "Tyre Diameter (Inches):");
            AddShared("ÅanzÄ±man Vites OranÄ±:", "ÅanzÄ±man Vites OranÄ±:", "Gearbox Ratio:");
            AddShared("Ayna Mahruti OranÄ±:", "Ayna Mahruti OranÄ±:", "Final Drive Ratio:");
            AddShared("ğŸš€ 0 - 100 km/h HÄ±zlanma:", "ğŸš€ 0 - 100 km/h HÄ±zlanma:", "ğŸš€ 0 - 100 km/h Acceleration:");
            AddShared("âœˆï¸ 100 - 200 km/h HÄ±zlanma:", "âœˆï¸ 100 - 200 km/h HÄ±zlanma:", "âœˆï¸ 100 - 200 km/h Acceleration:");
            AddShared("ğŸ”Œ Vites GeÃ§iÅŸ YavaÅŸlamasÄ±:", "ğŸ”Œ Vites GeÃ§iÅŸ YavaÅŸlamasÄ±:", "ğŸ”Œ Gear Shift Delay:");
            AddShared("Azami Krank GÃ¼cÃ¼:", "Azami Krank GÃ¼cÃ¼:", "Max Crank Power:");
            AddShared("Azami Krank Torku:", "Azami Krank Torku:", "Max Crank Torque:");

            // Grids & ListViews Columns
            AddShared("Zaman", "Zaman", "Time");
            AddShared("Tip", "Tip", "Type");
            AddShared("Harita", "Harita", "Map");
            AddShared("HÃ¼cre [R, C]", "HÃ¼cre [R, C]", "Cell [R, C]");
            AddShared("Sapma", "Sapma", "Dev");
            AddShared("DÃ¼zeltme", "DÃ¼zeltme", "Corr");
            AddShared("GÃ¼ven Skoru", "GÃ¼ven Skoru", "Confidence");
            AddShared("Durum", "Durum", "Status");
            AddShared("YÃ¼k (kPa)", "YÃ¼k (kPa)", "Load (kPa)");
            AddShared("Hedef AFR", "Hedef AFR", "Target AFR");
            AddShared("Ã–lÃ§Ã¼len AFR", "Ã–lÃ§Ã¼len AFR", "Measured AFR");
            AddShared("Ã–neri", "Ã–neri", "Suggestion");
            AddShared("DÃ¼zeltme %", "DÃ¼zeltme %", "Correction %");
            AddShared("ArÄ±za Kodu", "ArÄ±za Kodu", "Fault Code");
            AddShared("Tetiklenme Saati", "Tetiklenme Saati", "Trigger Time");
            AddShared("Su SÄ±caklÄ±ÄŸÄ± (Â°C)", "Su SÄ±caklÄ±ÄŸÄ± (Â°C)", "Coolant Temp (Â°C)");
            AddShared("Intake SÄ±caklÄ±ÄŸÄ± (Â°C)", "Intake SÄ±caklÄ±ÄŸÄ± (Â°C)", "Intake Temp (Â°C)");
            AddShared("HÄ±z (km/h)", "HÄ±z (km/h)", "Speed (km/h)");
            AddShared("Sembol", "Sembol", "Symbol");
            AddShared("Sinyal TÃ¼rÃ¼", "Sinyal TÃ¼rÃ¼", "Signal Type");
            AddShared("Kablo Rengi", "Kablo Rengi", "Wire Color");
            AddShared("Devir (RPM)", "Devir (RPM)", "Engine RPM");
            AddShared("WHP (Teker)", "WHP (Teker)", "WHP (Wheel)");
            AddShared("DeÄŸiÅŸken", "DeÄŸiÅŸken", "Variable");
            AddShared("CanlÄ± DeÄŸer", "CanlÄ± DeÄŸer", "Live Value");

            // Dynamic runtime status strings (set in timer ticks, cannot use Tag mechanism)
            AddShared("duty_cycle_safe", "âœ… GÃ¶rev DÃ¶ngÃ¼sÃ¼ GÃ¼venli Limit AralÄ±ÄŸÄ±nda", "âœ… Duty Cycle Within Safe Limit Range");
            AddShared("vtec_active", "ğŸ”¥ AKTÄ°F VTEC (12V)", "ğŸ”¥ VTEC ACTIVE (12V)");
            AddShared("vtec_inactive", "âš¡ PASÄ°F (VTEC LOCK)", "âš¡ INACTIVE (VTEC LOCK)");
            AddShared("wg_system_safe", "âœ… Wastegate Sistemi GÃ¼venli AralÄ±kta Ã‡alÄ±ÅŸÄ±yor", "âœ… Wastegate System Within Safe Range");
            AddShared("fan_relay_on", "ğŸ”¥ ETKÄ°N (RÃ¶le ON)", "ğŸ”¥ ACTIVE (Relay ON)");
            AddShared("fan_relay_off", "PASÄ°F (RÃ¶le OFF)", "INACTIVE (Relay OFF)");

            // AdvancedFuel simulator labels (static init label)
            AddShared("fuel_safe_status_init", "âœ… GÃ¶rev DÃ¶ngÃ¼sÃ¼ GÃ¼venli Limit AralÄ±ÄŸÄ±nda", "âœ… Duty Cycle Within Safe Limit Range");

            // EngineProtection safety status labels
            AddShared("ep_system_safe", "âœ… SYSTEM SAFE", "âœ… SYSTEM SAFE");
            AddShared("ep_fuel_cut", "ğŸš¨ FUEL CUT / ACÄ°L DURUM!", "ğŸš¨ FUEL CUT / EMERGENCY ALERT!");
            AddShared("ep_limp_mode", "âš ï¸ MOTOR LÄ°MP MODDA", "âš ï¸ ENGINE LIMP MODE ACTIVE");
            AddShared("ep_power_reduction", "âš ï¸ GÃœÃ‡ AZALTILIYOR", "âš ï¸ POWER REDUCTION ACTIVE");

            // MBT Optimizer deviation labels
            AddShared("mbt_above", "{0:F1}Â° (MBT Ãœzeri)", "+{0:F1}Â° (Above MBT)");
            AddShared("mbt_retarded", "{0:F1}Â° (Gecikmeli)", "{0:F1}Â° (Retarded)");

            // DynoLogs performance labels
            AddShared("dyno_max_power_fmt", "Azami Krank GÃ¼cÃ¼: {0} HP @ {1} RPM\nAzami Krank Torku: {2} Nm", "Peak Crank Power: {0} HP @ {1} RPM\nPeak Crank Torque: {2} Nm");
            AddShared("dyno_time_seconds", "{0} saniye", "{0} seconds");
            AddShared("dyno_shift_ms", "{0} ms (Clutch drop delay)", "{0} ms (Clutch drop delay)");

            // VtecBoost / Alarm
            AddShared("wg_failure_alarm_prefix", "ğŸš¨ ÅARJ ALARMI:", "ğŸš¨ WG ALARM:");

            // Diagnostics console messages (runtime)
            AddShared("diag_protocol_changed", "[SYSTEM] Ä°letiÅŸim protokolÃ¼ deÄŸiÅŸtirildi: ", "[SYSTEM] Protocol changed: ");
            AddShared("diag_selftest_start", "[SYSTEM] Cihaz Ã¶z-teÅŸhis taramasÄ± baÅŸlatÄ±lÄ±yor...", "[SYSTEM] Starting device self-test scan...");
            AddShared("diag_a2l_saved", "A2L dosyasÄ± baÅŸarÄ±yla kaydedildi!", "A2L file saved successfully!");
            AddShared("diag_a2l_saved_title", "Bilgi", "Info");
            AddShared("diag_can_error", "Hata (GeÃ§ersiz veri)", "Error (Invalid data)");

            // MBT apply button message
            AddShared("mbt_apply_msg_fmt", "Zamanlama BaÅŸarÄ±yla KararlaÅŸtÄ±rÄ±ldÄ±: {0:F1}Â° avans aktif tabloya referans atandÄ± ve patch edildi.", "Timing locked: {0:F1}Â° advance applied to active map.");
            AddShared("mbt_apply_title", "FlaÅŸ Avans DÃ¼zeltmesi", "Flash Timing Correction");

            // â”€â”€â”€ ReverseControl (Analysis & Decompiler tab) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("rc_scan_btn", "ğŸ” ROM'u Analiz Et & Tara", "ğŸ” Scan & Analyze ROM");
            AddShared("rc_filter_lbl", "Filtrele:", "Filter:");
            AddShared("rc_col_maptype", "Harita Tipi", "Map Type");
            AddShared("rc_col_dims", "Boyutlar", "Dimensions");
            AddShared("rc_col_confidence", "GÃ¼venilirlik", "Confidence");
            AddShared("rc_col_desc", "AÃ§Ä±klama", "Description");
            AddShared("rc_axes_title", "Eksen Analiz Motoru", "Axis Analysis Engine");
            AddShared("rc_rpm_axis_init", "RPM Ekseni: SeÃ§ilmedi", "RPM Axis: Not selected");
            AddShared("rc_load_axis_init", "Load Ekseni: SeÃ§ilmedi", "Load Axis: Not selected");
            AddShared("rc_rpm_axis_unsearched", "RPM Ekseni: AranmadÄ±", "RPM Axis: Not searched");
            AddShared("rc_load_axis_unsearched", "Load Ekseni: AranmadÄ±", "Load Axis: Not searched");
            AddShared("rc_rpm_axis_notfound", "RPM Ekseni: BulunamadÄ±", "RPM Axis: Not found");
            AddShared("rc_load_axis_notfound", "Load Ekseni: BulunamadÄ±", "Load Axis: Not found");
            AddShared("rc_axis_scan_btn", "Eksen TaramasÄ± Yap", "Scan Axes");
            AddShared("rc_decompiler_title", "Decompiler & Register Trace AkÄ±ÅŸÄ±", "Decompiler & Register Trace");
            AddShared("rc_routine_lbl", "Rutin:", "Routine:");
            AddShared("rc_routine_vtec", "VTEC YÃ¶netimi", "VTEC Management");
            AddShared("rc_routine_revcut", "Devir Kesici", "Rev Limiter");
            AddShared("rc_routine_checksum", "Checksum KontrolÃ¼", "Checksum Verification");
            AddShared("rc_address_lbl", "Adres (Hex):", "Address (Hex):");
            AddShared("rc_adopt_btn", "ğŸ“¥ SeÃ§ilen HaritayÄ± ECU Profiline Entegre Et", "ğŸ“¥ Integrate Selected Map to ECU Profile");
            AddShared("rc_no_rom_msg", "Ã–ncelikle bir ROM dosyasÄ± yÃ¼klemelisiniz!", "Please load a ROM file first!");
            AddShared("rc_no_rom_title", "Hata", "Error");
            AddShared("rc_scan_done", "\n\nHarita taramasÄ± tamamlandÄ±! Soldaki listeden incelemek istediÄŸiniz aday haritayÄ± seÃ§in.", "\n\nMap scan complete! Select a candidate map from the list on the left to inspect.");
            AddShared("rc_axis_results_header", "=== EKSEN HARÄ°TALAMA SONUÃ‡LARI ===", "=== AXIS MAPPING RESULTS ===");
            AddShared("rc_axis_notfound_msg", "Monoton olarak artÄ±ÅŸ gÃ¶steren uygun eksen adresleri tespit edilemedi.", "No monotonically increasing axis addresses could be detected.");
            AddShared("rc_axis_notfound_title", "Bilgi", "Info");
            AddShared("rc_addr_invalid_msg", "Adres geÃ§ersiz! LÃ¼tfen 16'lÄ±k (Hex) formatta girin (Ã¶rn: 1FC0).", "Invalid address! Please enter in hex format (e.g. 1FC0).");
            AddShared("rc_addr_invalid_title", "Hata", "Error");
            AddShared("rc_adopt_already_msg", "Bu adresteki harita zaten ECU profiline eklenmiÅŸ durumda!", "A map at this address is already in the ECU profile!");
            AddShared("rc_adopt_already_title", "Bilgi", "Info");
            AddShared("rc_adopt_success_title", "Profil Entegrasyonu BaÅŸarÄ±lÄ±", "Profile Integration Successful");

            // â”€â”€â”€ MainForm BuildPatchCenter â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("pc_available_patches", "KullanÄ±labilir Yamalar", "Available Patches");
            AddShared("pc_patch_details", "Yama DetaylarÄ± ve Ã–nizleme", "Patch Details & Preview");
            AddShared("pc_apply_patch_btn", "YamayÄ± Uygula", "Apply Patch");
            AddShared("pc_rollback_patch_btn", "Geri Al (Rollback)", "Rollback");
            AddShared("pc_audit_log_lbl", "Yama Log KayÄ±tlarÄ±", "Patch Audit Log");
            AddShared("pc_col_time", "Zaman", "Time");
            AddShared("pc_col_patch_id", "Yama ID", "Patch ID");
            AddShared("pc_col_result", "SonuÃ§", "Result");
            AddShared("pc_no_patches", "Bu ECU profili iÃ§in kullanÄ±labilir yama bulunamadÄ±.", "No patches available for this ECU profile.");

            // â”€â”€â”€ MainForm BuildCalibrationWizards â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("wiz_inj_header", "ğŸ§ª EnjektÃ¶r Ã–lÃ§ekleme SihirbazÄ±", "ğŸ§ª Injector Scaling Wizard");
            AddShared("wiz_old_inj", "Eski EnjektÃ¶r Boyutu:", "Old Injector Size:");
            AddShared("wiz_new_inj", "Yeni EnjektÃ¶r Boyutu:", "New Injector Size:");
            AddShared("wiz_scale_inj_btn", "EnjektÃ¶rleri Ã–lÃ§ekle", "Scale Injectors");
            AddShared("wiz_map_header", "ğŸ”Œ MAP SensÃ¶rÃ¼ Kalibrasyon SihirbazÄ±", "ğŸ”Œ MAP Sensor Calibration Wizard");
            AddShared("wiz_new_map_lbl", "Yeni MAP SensÃ¶rÃ¼ SeÃ§in:", "Select New MAP Sensor:");
            AddShared("wiz_calibrate_map_btn", "YÃ¼k Eksenini Kalibre Et", "Calibrate Load Axis");
            AddShared("wiz_inj_ok_msg", "EnjektÃ¶r Ã¶lÃ§ekleme baÅŸarÄ±yla uygulandÄ±!\nÃ–lÃ§ek oranÄ±: {0:F3}\nYakÄ±t haritasÄ± gÃ¼ncellendi.", "Injector scaling applied!\nScale ratio: {0:F3}\nFuel map updated.");
            AddShared("wiz_inj_ok_title", "BaÅŸarÄ±lÄ±", "Success");
            AddShared("wiz_map_ok_msg", "MAP sensÃ¶rÃ¼ baÅŸarÄ±yla kalibre edildi!\nYeni YÃ¼k ekseni (kPa) 20 - {0} aralÄ±ÄŸÄ±na Ã¶lÃ§eklendi.", "MAP sensor calibrated!\nNew load axis (kPa) scaled 20 - {0}.");
            AddShared("wiz_map_ok_title", "BaÅŸarÄ±lÄ±", "Success");

            // â”€â”€â”€ MainForm BuildVtecPanel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("vtec_panel_load_lbl", "YÃ¼k EÅŸiÄŸi:", "Load Threshold:");
            AddShared("vtec_panel_speed_lbl", "HÄ±z SÄ±nÄ±rÄ±:", "Speed Limit:");

            // â”€â”€â”€ BuildAdvancedTuningControls / Launch Control â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("adv_launch_control", "Launch Control (2-Step) Aktif", "Launch Control (2-Step) Active");
            AddShared("adv_limit_rpm_lbl", "SÄ±nÄ±r Devri:", "Limit RPM:");
            AddShared("adv_speed_thresh_lbl", "HÄ±z EÅŸiÄŸi:", "Speed Threshold:");
            AddShared("adv_dtc_header", "ğŸ”§ DTC (ArÄ±za IÅŸÄ±ÄŸÄ±) Devre DÄ±ÅŸÄ± BÄ±rakma", "ğŸ”§ DTC (CEL) Disable");
            AddShared("adv_bypass_knock", "Vuruntu SensÃ¶rÃ¼nÃ¼ Bypass Et (Knock Sensor CEL 23)", "Bypass Knock Sensor (CEL 23)");
            AddShared("adv_bypass_vtec_sw", "VTEC YaÄŸ BasÄ±ncÄ± MÃ¼ÅŸÃ¼rÃ¼nÃ¼ Bypass Et (VTEC Switch CEL 22)", "Bypass VTEC Oil Pressure Switch (CEL 22)");
            AddShared("adv_bypass_o2_heater", "Oksijen SensÃ¶rÃ¼ IsÄ±tÄ±cÄ±sÄ±nÄ± Bypass Et (O2 Heater CEL 41)", "Bypass O2 Sensor Heater (CEL 41)");
            AddShared("adv_bypass_eld", "ELD - Elektriksel YÃ¼k DedektÃ¶rÃ¼nÃ¼ Bypass Et (ELD CEL 20)", "Bypass ELD Electrical Load Detector (CEL 20)");

            // â”€â”€â”€ Ignition grid / map loading placeholder â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("map_waiting", "Harita verisi bekleniyor...", "Waiting for map data...");

            // â”€â”€â”€ Direct Turkish Strings to English Translations â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("Harita verisi bekleniyor...", "Harita verisi bekleniyor...", "Waiting for map data...");
            AddShared("Launch Control (2-Step) Aktif", "Launch Control (2-Step) Aktif", "Launch Control (2-Step) Active");
            AddShared("SÄ±nÄ±r Devri:", "SÄ±nÄ±r Devri:", "Limit RPM:");
            AddShared("HÄ±z EÅŸiÄŸi:", "HÄ±z EÅŸiÄŸi:", "Speed Threshold:");
            AddShared("ğŸ”§ DTC (ArÄ±za IÅŸÄ±ÄŸÄ±) Devre DÄ±ÅŸÄ± BÄ±rakma", "ğŸ”§ DTC (ArÄ±za IÅŸÄ±ÄŸÄ±) Devre DÄ±ÅŸÄ± BÄ±rakma", "ğŸ”§ DTC (CEL) Disable");
            AddShared("Vuruntu SensÃ¶rÃ¼nÃ¼ Bypass Et (Knock Sensor CEL 23)", "Vuruntu SensÃ¶rÃ¼nÃ¼ Bypass Et (Knock Sensor CEL 23)", "Bypass Knock Sensor (CEL 23)");
            AddShared("VTEC YaÄŸ BasÄ±nÃ§ MÃ¼ÅŸÃ¼rÃ¼nÃ¼ Bypass Et (VTEC Switch CEL 22)", "VTEC YaÄŸ BasÄ±nÃ§ MÃ¼ÅŸÃ¼rÃ¼nÃ¼ Bypass Et (VTEC Switch CEL 22)", "Bypass VTEC Oil Pressure Switch (CEL 22)");
            AddShared("VTEC YaÄŸ BasÄ±ncÄ± MÃ¼ÅŸÃ¼rÃ¼nÃ¼ Bypass Et (VTEC Switch CEL 22)", "VTEC YaÄŸ BasÄ±ncÄ± MÃ¼ÅŸÃ¼rÃ¼nÃ¼ Bypass Et (VTEC Switch CEL 22)", "Bypass VTEC Oil Pressure Switch (CEL 22)");
            AddShared("Oksijen SensÃ¶rÃ¼ IsÄ±tÄ±cÄ±sÄ±nÄ± Bypass Et (O2 Heater CEL 41)", "Oksijen SensÃ¶rÃ¼ IsÄ±tÄ±cÄ±sÄ±nÄ± Bypass Et (O2 Heater CEL 41)", "Bypass O2 Sensor Heater (CEL 41)");
            AddShared("ELD - Elektriksel YÃ¼k DedektÃ¶rÃ¼nÃ¼ Bypass Et (ELD CEL 20)", "ELD - Elektriksel YÃ¼k DedektÃ¶rÃ¼nÃ¼ Bypass Et (ELD CEL 20)", "Bypass ELD Electrical Load Detector (CEL 20)");

            AddShared("ğŸ§ª EnjektÃ¶r Ã–lÃ§ekleme SihirbazÄ±", "ğŸ§ª EnjektÃ¶r Ã–lÃ§ekleme SihirbazÄ±", "ğŸ§ª Injector Scaling Wizard");
            AddShared("Eski EnjektÃ¶r Boyutu:", "Eski EnjektÃ¶r Boyutu:", "Old Injector Size:");
            AddShared("Yeni EnjektÃ¶r Boyutu:", "Yeni EnjektÃ¶r Boyutu:", "New Injector Size:");
            AddShared("EnjektÃ¶rleri Ã–lÃ§ekle", "EnjektÃ¶rleri Ã–lÃ§ekle", "Scale Injectors");
            AddShared("ğŸ”Œ MAP SensÃ¶rÃ¼ Kalibrasyon SihirbazÄ±", "ğŸ”Œ MAP SensÃ¶rÃ¼ Kalibrasyon SihirbazÄ±", "ğŸ”Œ MAP Sensor Calibration Wizard");
            AddShared("Yeni MAP SensÃ¶rÃ¼ SeÃ§in:", "Yeni MAP SensÃ¶rÃ¼ SeÃ§in:", "Select New MAP Sensor:");
            AddShared("YÃ¼k Eksenini Kalibre Et", "YÃ¼k Eksenini Kalibre Et", "Calibrate Load Axis");

            AddShared("Stok 1-Bar (20 - 105 kPa)", "Stok 1-Bar (20 - 105 kPa)", "Stock 1-Bar (20 - 105 kPa)");
            AddShared("Motorola 2.5-Bar (20 - 250 kPa)", "Motorola 2.5-Bar (20 - 250 kPa)", "Motorola 2.5-Bar (20 - 250 kPa)");
            AddShared("Omnipower 3-Bar (20 - 300 kPa)", "Omnipower 3-Bar (20 - 300 kPa)", "Omnipower 3-Bar (20 - 300 kPa)");
            AddShared("Omnipower 4-Bar (20 - 400 kPa)", "Omnipower 4-Bar (20 - 400 kPa)", "Omnipower 4-Bar (20 - 400 kPa)");

            AddShared("KullanÄ±labilir Yamalar", "KullanÄ±labilir Yamalar", "Available Patches");
            AddShared("Yama DetaylarÄ± ve Ã–nizleme", "Yama DetaylarÄ± ve Ã–nizleme", "Patch Details & Preview");
            AddShared("YamayÄ± Uygula", "YamayÄ± Uygula", "Apply Patch");
            AddShared("Geri Al (Rollback)", "Geri Al (Rollback)", "Rollback");
            AddShared("Yama Log KayÄ±tlarÄ±", "Yama Log KayÄ±tlarÄ±", "Patch Audit Log");
            AddShared("Zaman", "Zaman", "Time");
            AddShared("Yama ID", "Yama ID", "Patch ID");
            AddShared("SonuÃ§", "SonuÃ§", "Result");

            AddShared("âš¡ VTEC RPM:", "âš¡ VTEC RPM:", "âš¡ VTEC RPM:");
            AddShared("YÃ¼k EÅŸiÄŸi:", "YÃ¼k EÅŸiÄŸi:", "Load Threshold:");
            AddShared("Rev Limit:", "Rev Limit:", "Rev Limit:");
            AddShared("HÄ±z SÄ±nÄ±rÄ±:", "HÄ±z SÄ±nÄ±rÄ±:", "Speed Limit:");
            AddShared("Inj. Dead:", "Inj. Dead:", "Inj. Dead:");

            AddShared("ğŸš—  AraÃ§ SeÃ§", "ğŸš—  AraÃ§ SeÃ§", "ğŸš—  Select Vehicle");
            AddShared("AraÃ§ SeÃ§", "AraÃ§ SeÃ§", "Select Vehicle");

            AddShared("Harita Tipi", "Harita Tipi", "Map Type");
            AddShared("Boyutlar", "Boyutlar", "Dimensions");
            AddShared("GÃ¼venilirlik", "GÃ¼venilirlik", "Confidence");
            AddShared("AÃ§Ä±klama", "AÃ§Ä±klama", "Description");

            AddShared("RPM Ekseni: SeÃ§ilmedi", "RPM Ekseni: SeÃ§ilmedi", "RPM Axis: Not Selected");
            AddShared("Load Ekseni: SeÃ§ilmedi", "Load Ekseni: SeÃ§ilmedi", "Load Axis: Not Selected");
            AddShared("RPM Ekseni: AranmadÄ±", "RPM Ekseni: AranmadÄ±", "RPM Axis: Not Searched");
            AddShared("Load Ekseni: AranmadÄ±", "Load Ekseni: AranmadÄ±", "Load Axis: Not Searched");
            AddShared("RPM Ekseni: BulunamadÄ±", "RPM Ekseni: BulunamadÄ±", "RPM Axis: Not Found");
            AddShared("Load Ekseni: BulunamadÄ±", "Load Ekseni: BulunamadÄ±", "Load Axis: Not Found");
            AddShared("RPM Ekseni", "RPM Ekseni", "RPM Axis");
            AddShared("Load Ekseni", "Load Ekseni", "Load Axis");

            AddShared("VTEC YÃ¶netimi", "VTEC YÃ¶netimi", "VTEC Management");
            AddShared("Devir Kesici", "Devir Kesici", "Rev Limiter");
            AddShared("Checksum KontrolÃ¼", "Checksum KontrolÃ¼", "Checksum Verification");

            // â”€â”€â”€ ECU Profile CasaTags â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("EG kasa 1.5L VTEC-E (YÃ¼ksek Verimli)", "EG kasa 1.5L VTEC-E (YÃ¼ksek Verimli)", "EG chassis 1.5L VTEC-E (High Efficiency)");
            AddShared("EG kasa 1.5L Non-VTEC", "EG kasa 1.5L Non-VTEC", "EG chassis 1.5L Non-VTEC");
            AddShared("EG/EK kasa 1.6L SOHC VTEC", "EG/EK kasa 1.6L SOHC VTEC", "EG/EK chassis 1.6L SOHC VTEC");
            AddShared("DC2 Integra GS-R 1.7L DOHC VTEC (1992-93)", "DC2 Integra GS-R 1.7L DOHC VTEC (1992-93)", "DC2 Integra GS-R 1.7L DOHC VTEC (1992-93)");
            AddShared("DC2 Integra GSR 1.8L DOHC VTEC + IAB (1994-95)", "DC2 Integra GSR 1.8L DOHC VTEC + IAB (1994-95)", "DC2 Integra GSR 1.8L DOHC VTEC + IAB (1994-95)");
            AddShared("DC2 Integra LS/GS 1.8L DOHC Non-VTEC", "DC2 Integra LS/GS 1.8L DOHC Non-VTEC", "DC2 Integra LS/GS 1.8L DOHC Non-VTEC");
            AddShared("BB Prelude 2.2L DOHC VTEC (1993-95)", "BB Prelude 2.2L DOHC VTEC (1993-95)", "BB Prelude 2.2L DOHC VTEC (1993-95)");

            // â”€â”€â”€ 3D Parts & Notes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            AddShared("3D MODEL SEÃ‡Ä°MÄ°", "3D MODEL SEÃ‡Ä°MÄ°", "3D MODEL SELECTION");
            AddShared("ğŸ§   ECU Ana KartÄ±", "ğŸ§   ECU Ana KartÄ±", "ğŸ§   ECU Motherboard");
            AddShared("ğŸ§   ECU (Geri)", "ğŸ§   ECU (Geri)", "ğŸ§   ECU (Back)");
            AddShared("ğŸ’¾  EEPROM Ã‡ip", "ğŸ’¾  EEPROM Ã‡ip", "ğŸ’¾  EEPROM Chip");
            AddShared("ğŸ”Œ  OBD1 KonnektÃ¶r", "ğŸ”Œ  OBD1 KonnektÃ¶r", "ğŸ”Œ  OBD1 Connector");
            AddShared("ğŸŒ¡ï¸  MAP SensÃ¶rÃ¼", "ğŸŒ¡ï¸  MAP SensÃ¶rÃ¼", "ğŸŒ¡ï¸  MAP Sensor");
            AddShared("â›½  EnjektÃ¶r", "â›½  EnjektÃ¶r", "â›½  Injector");
            AddShared("âš™ï¸  DistribÃ¼tÃ¶r", "âš™ï¸  DistribÃ¼tÃ¶r", "âš™ï¸  Distributor");
            AddShared("ğŸ”©  B16 FWD Motor", "ğŸ”©  B16 FWD Motor", "ğŸ”©  B16 FWD Engine");
            AddShared("ğŸ“  PROJE PARÃ‡ALARI", "ğŸ“  PROJE PARÃ‡ALARI", "ğŸ“  PROJECT PARTS");
            AddShared("Sol: dÃ¶ndÃ¼r  |  SaÄŸ: kaydÄ±r  |  Tekerlek: zoom", "Sol: dÃ¶ndÃ¼r  |  SaÄŸ: kaydÄ±r  |  Tekerlek: zoom", "Left: rotate  |  Right: pan  |  Wheel: zoom");

            // 3D Parts list descriptions (rendered in GDI+)
            AddShared("ECU Ana KartÄ± (Honda P28)", "ECU Ana KartÄ± (Honda P28)", "ECU Motherboard (Honda P28)");
            AddShared("EEPROM Ã‡ip (28C256)", "EEPROM Ã‡ip (28C256)", "EEPROM Chip (28C256)");
            AddShared("OBD1 KonnektÃ¶r (3-Plug)", "OBD1 KonnektÃ¶r (3-Plug)", "OBD1 Connector (3-Plug)");
            AddShared("MAP SensÃ¶rÃ¼ (1 bar)", "MAP SensÃ¶rÃ¼ (1 bar)", "MAP Sensor (1 bar)");
            AddShared("EnjektÃ¶r (240cc EV1)", "EnjektÃ¶r (240cc EV1)", "Injector (240cc EV1)");
            AddShared("DistribÃ¼tÃ¶r (TDC SensÃ¶rlÃ¼)", "DistribÃ¼tÃ¶r (TDC SensÃ¶rlÃ¼)", "Distributor (with TDC)");
            AddShared("Motor (B16 FWD)", "Motor (B16 FWD)", "Engine (B16 FWD)");

            // 3D Parts Tech notes
            AddShared("16 MHz NEC V25 Â· 32KB Â· OBD1", "16 MHz NEC V25 Â· 32KB Â· OBD1", "16 MHz NEC V25 Â· 32KB Â· OBD1");
            AddShared("DIP-28 Â· 32KB Â· 5V Â· In-circuit", "DIP-28 Â· 32KB Â· 5V Â· In-circuit", "DIP-28 Â· 32KB Â· 5V Â· In-circuit");
            AddShared("3-plug A/B/C Â· Jumper hafÄ±za", "3-plug A/B/C Â· Jumper hafÄ±za", "3-plug A/B/C Â· Jumper memory");
            AddShared("0â€“200 kPa Â· 5V Â· Barometrik", "0â€“200 kPa Â· 5V Â· Barometrik", "0â€“200 kPa Â· 5V Â· Barometric");
            AddShared("EV1 Â· 12Î© Â· 240cc Â· 4/6 adet", "EV1 Â· 12Î© Â· 240cc Â· 4/6 adet", "EV1 Â· 12Î© Â· 240cc Â· 4/6 pcs");
            AddShared("TDC Â· CYP/CKP dahili Â· Bobin", "TDC Â· CYP/CKP dahili Â· Bobin", "TDC Â· CYP/CKP internal Â· Coil");
            AddShared("1.6L DOHC VTEC Â· B16A Â· FWD", "1.6L DOHC VTEC Â· B16A Â· FWD", "1.6L DOHC VTEC Â· B16A Â· FWD");

            // Project Parts (3D Part Tab)
            AddShared("ğŸ”§ PROJE PARÃ‡ALARI", "ğŸ”§ PROJE PARÃ‡ALARI", "ğŸ”§ PROJECT PARTS");
            AddShared("ECU", "ECU", "ECU");
            AddShared("Hondata S300, Neptune RTP, Crome Pro, HTS", "Hondata S300, Neptune RTP, Crome Pro, HTS", "Hondata S300, Neptune RTP, Crome Pro, HTS");
            AddShared("Emme", "Emme", "Intake");
            AddShared("Cold Air Intake, Skunk2 Pro, K&N filtre", "Cold Air Intake, Skunk2 Pro, K&N filtre", "Cold Air Intake, Skunk2 Pro, K&N filter");
            AddShared("Gaz KelebeÄŸi", "Gaz KelebeÄŸi", "Throttle Body");
            AddShared("B16/B18 62 mm throttle body", "B16/B18 62 mm throttle body", "B16/B18 62mm throttle body");
            AddShared("Emme Manifoldu", "Emme Manifoldu", "Intake Manifold");
            AddShared("D16Y8 veya Skunk2", "D16Y8 veya Skunk2", "D16Y8 or Skunk2");
            AddShared("Egzoz", "Egzoz", "Exhaust");
            AddShared("4-2-1 Header, 2.25\" dÃ¼z hat", "4-2-1 Header, 2.25\" dÃ¼z hat", "4-2-1 Header, 2.25\" straight pipe");
            AddShared("Egzantrik", "Egzantrik", "Camshaft");
            AddShared("Delta Cam, Bisimoto Stage 1", "Delta Cam, Bisimoto Stage 1", "Delta Cam, Bisimoto Stage 1");
            AddShared("YakÄ±t", "YakÄ±t", "Fuel");
            AddShared("Walbro 255, bÃ¼yÃ¼k enjektÃ¶r", "Walbro 255, bÃ¼yÃ¼k enjektÃ¶r", "Walbro 255, larger injectors");
            AddShared("AteÅŸleme", "AteÅŸleme", "Ignition");
            AddShared("NGK Iridium, MSD", "NGK Iridium, MSD", "NGK Iridium, MSD");
            AddShared("Volan", "Volan", "Flywheel");
            AddShared("HafifletilmiÅŸ Volan", "HafifletilmiÅŸ Volan", "Lightweight Flywheel");
            AddShared("Debriyaj", "Debriyaj", "Clutch");
            AddShared("Exedy Stage 1", "Exedy Stage 1", "Exedy Stage 1");
            AddShared("SÃ¼spansiyon", "SÃ¼spansiyon", "Suspension");
            AddShared("BC Racing, Tein, D2", "BC Racing, Tein, D2", "BC Racing, Tein, D2");
            AddShared("Fren", "Fren", "Brakes");
            AddShared("Integra DC2 veya Civic VTi disk", "Integra DC2 veya Civic VTi disk", "Integra DC2 or Civic VTi discs");
            AddShared("Turbo", "Turbo", "Turbo");
            AddShared("TD04, GT2554R, GT2860", "TD04, GT2554R, GT2860", "TD04, GT2554R, GT2860");

            // Telemetry Sensors & Units
            AddShared("ğŸŒ¡ ECT (Motor SÄ±caklÄ±ÄŸÄ±):", "ğŸŒ¡ ECT (Motor SÄ±caklÄ±ÄŸÄ±):", "ğŸŒ¡ ECT (Engine Temp):");
            AddShared("ğŸ’¨ IAT (Emme HavasÄ± SÄ±caklÄ±ÄŸÄ±):", "ğŸ’¨ IAT (Emme HavasÄ± SÄ±caklÄ±ÄŸÄ±):", "ğŸ’¨ IAT (Intake Air Temp):");

            // Diff View
            AddShared("Diff gÃ¶rÃ¼nÃ¼mÃ¼ â€” Ã¶nce bir ROM yÃ¼kleyin", "Diff gÃ¶rÃ¼nÃ¼mÃ¼ â€” Ã¶nce bir ROM yÃ¼kleyin", "Diff view â€” load a ROM first");
            AddShared("ğŸ“‹ STOCK", "ğŸ“‹ STOCK", "ğŸ“‹ STOCK");
            AddShared("âš¡ DELTA (Î”)", "âš¡ DELTA (Î”)", "âš¡ DELTA (Î”)");
            AddShared("âœï¸ MODIFIED", "âœï¸ MODIFIED", "âœï¸ MODIFIED");
            AddShared("DeÄŸiÅŸen", "DeÄŸiÅŸen", "Changed");
            AddShared("Ort. Î”", "Ort. Î”", "Avg. Î”");
            AddShared("Maks. Î”", "Maks. Î”", "Max. Î”");
            AddShared("Stock vs Modified", "Stock vs Modified", "Stock vs Modified");

            // AutoTune
            AddShared("Ã–nerilen ve Uygulanan Kararlar", "Ã–nerilen ve Uygulanan Kararlar", "Recommended & Applied Decisions");
            AddShared("CanlÄ± AutoTune DÃ¼zeltme Ã–nerileri (Son 50 Ã–neri)", "CanlÄ± AutoTune DÃ¼zeltme Ã–nerileri (Son 50 Ã–neri)", "Live AutoTune Correction Suggestions (Last 50)");
            AddShared("YÃ¼k (kPa)", "YÃ¼k (kPa)", "Load (kPa)");
            AddShared("Hedef AFR", "Hedef AFR", "Target AFR");
            AddShared("Ã–lÃ§Ã¼len AFR", "Ã–lÃ§Ã¼len AFR", "Measured AFR");
            AddShared("Ã–neri", "Ã–neri", "Suggestion");
            AddShared("DÃ¼zeltme %", "DÃ¼zeltme %", "Correction %");
            AddShared("BaÄŸlantÄ± Durumu: Disconnected", "BaÄŸlantÄ± Durumu: Disconnected", "Connection Status: Disconnected");
            AddShared("Kuyruk DerinliÄŸi: 0 eleman", "Kuyruk DerinliÄŸi: 0 eleman", "Queue Depth: 0 elements");
            AddShared("Ortalama Gecikme: 0.0 ms", "Ortalama Gecikme: 0.0 ms", "Average Latency: 0.0 ms");
            AddShared("Hata / Yeniden Deneme: 0 / 0", "Hata / Yeniden Deneme: 0 / 0", "Errors / Retries: 0 / 0");
            AddShared("DÃ¼ÅŸen Yazmalar: 0", "DÃ¼ÅŸen Yazmalar: 0", "Dropped Writes: 0");
            AddShared("COM Port:", "COM Port:", "COM Port:");
            AddShared("DOSYA BULUNAMADI", "DOSYA BULUNAMADI", "FILE NOT FOUND");
            AddShared("HATA:", "HATA:", "ERROR:");
            AddShared("Disconnected", "BaÄŸlantÄ± Kesildi", "Disconnected");
            AddShared("Connecting", "BaÄŸlanÄ±yor", "Connecting");
            AddShared("Connected", "BaÄŸlandÄ±", "Connected");
            AddShared("Synchronizing", "Senkronize Ediliyor", "Synchronizing");
            AddShared("Paused", "DuraklatÄ±ldÄ±", "Paused");
            AddShared("Faulted", "Hata Durumu", "Faulted");
            AddShared("eleman", "eleman", "elements");
            AddShared("Zaman", "Zaman", "Time");
            AddShared("Tip", "Tip", "Type");
            AddShared("Harita", "Harita", "Map");
            AddShared("HÃ¼cre [R, C]", "HÃ¼cre [R, C]", "Cell [R, C]");
            AddShared("Sapma", "Sapma", "Deviation");
            AddShared("DÃ¼zeltme", "DÃ¼zeltme", "Correction");
            AddShared("GÃ¼ven Skoru", "GÃ¼ven Skoru", "Confidence");
            AddShared("Durum", "Durum", "Status");
            AddShared("RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator");
            AddShared("Real-Time Calibration Sync Etkin", "Real-Time Calibration Sync Etkin", "Real-Time Calibration Sync Active");
            AddShared("â–¶ Emulator BaÄŸlan", "â–¶ Emulator BaÄŸlan", "â–¶ Connect Emulator");
            AddShared("ğŸ”„ TÃ¼m ROM'u Senkronize Et (Upload)", "ğŸ”„ TÃ¼m ROM'u Senkronize Et (Upload)", "ğŸ”„ Sync Full ROM (Upload)");
            AddShared("â¹ BaÄŸlantÄ±yÄ± Kes", "â¹ BaÄŸlantÄ±yÄ± Kes", "â¹ Disconnect");

            // Advanced Fuel UI 
            AddShared("Kademe", "Kademe", "Index");
            AddShared("MAF SensÃ¶r (Volt)", "MAF SensÃ¶r (Volt)", "MAF Sensor (Volt)");
            AddShared("Hava Debisi (g/s)", "Hava Debisi (g/s)", "Air Flow (g/s)");
            AddShared("Hararet (Â°C ECT)", "Hararet (Â°C ECT)", "Coolant Temp (Â°C ECT)");
            AddShared("SÄ±caklÄ±k YakÄ±t Ã‡arpanÄ±", "SÄ±caklÄ±k YakÄ±t Ã‡arpanÄ±", "ECT Fuel Multiplier");
            AddShared("TPS %", "TPS %", "TPS (%)");
            AddShared("âš¡ CanlÄ± EnjektÃ¶r & DÃ¼zeltme SimÃ¼latÃ¶rÃ¼", "âš¡ CanlÄ± EnjektÃ¶r & DÃ¼zeltme SimÃ¼latÃ¶rÃ¼", "âš¡ Live Injector & Correction Simulator");
            AddShared("Motor Devri (RPM):", "Motor Devri (RPM):", "Engine Speed (RPM):");
            AddShared("Taban YakÄ±t SÃ¼resi (ms):", "Taban YakÄ±t SÃ¼resi (ms):", "Base Fuel Pulse (ms):");
            AddShared("Motor SÄ±caklÄ±k (Â°C ECT):", "Motor SÄ±caklÄ±k (Â°C ECT):", "Coolant Temp (Â°C ECT):");
            AddShared("YakÄ±t BasÄ±ncÄ± (psi - Aktif):", "YakÄ±t BasÄ±ncÄ± (psi - Aktif):", "Fuel Pressure (psi - Active):");
            AddShared("Hedef YakÄ±t BasÄ±ncÄ± (psi):", "Hedef YakÄ±t BasÄ±ncÄ± (psi):", "Target Fuel Pressure (psi):");
            AddShared("Gaz DeÄŸiÅŸim HÄ±zÄ± (dTPS %/s):", "Gaz DeÄŸiÅŸim HÄ±zÄ± (dTPS %/s):", "Throttle Change Rate (dTPS %/s):");
            AddShared("Alpha-N YakÄ±t Modunu Kullan (TPS vs RPM)", "Alpha-N YakÄ±t Modunu Kullan (TPS vs RPM)", "Use Alpha-N Fuel Mode (TPS vs RPM)");
            AddShared("ğŸ’¥ Gaz PedalÄ±na HÄ±zlÄ±ca Bas (Throttle Step Sim)", "ğŸ’¥ Gaz PedalÄ±na HÄ±zlÄ±ca Bas (Throttle Step Sim)", "ğŸ’¥ Blast Throttle (Throttle Step Sim)");
            AddShared("KÄ±sa Enjeksiyon Eklemesi (adder):", "KÄ±sa Enjeksiyon Eklemesi (adder):", "Short Pulse Adder:");
            AddShared("GeÃ§ici YakÄ±t Havuzu (acc):", "GeÃ§ici YakÄ±t Havuzu (acc):", "Transient Fuel Pool (acc):");
            AddShared("Nihai Enjeksiyon SÃ¼resi (PW):", "Nihai Enjeksiyon SÃ¼resi (PW):", "Final Pulse Width (PW):");
            AddShared("EnjektÃ¶r GÃ¶rev DÃ¶ngÃ¼sÃ¼ (Duty):", "EnjektÃ¶r GÃ¶rev DÃ¶ngÃ¼sÃ¼ (Duty):", "Injector Duty Cycle (Duty):");

            // Advanced Ignition UI
            AddShared("âš¡ Ã‡alÄ±ÅŸtÄ±rma & Silindir DÃ¼zeltmeleri", "âš¡ Ã‡alÄ±ÅŸtÄ±rma & Silindir DÃ¼zeltmeleri", "âš¡ Cranking & Cylinder Offsets");
            AddShared("ğŸ”Œ SensÃ¶r Kalibrasyon EÄŸrisi", "ğŸ”Œ SensÃ¶r Kalibrasyon EÄŸrisi", "ğŸ”Œ Sensor Calibration Curve");
            AddShared("ğŸ“¡ CAN Bus Kod Ã‡Ã¶zÃ¼cÃ¼", "ğŸ“¡ CAN Bus Kod Ã‡Ã¶zÃ¼cÃ¼", "ğŸ“¡ CAN Bus Decoder");
            AddShared("ğŸ§  MBT Avans Ã–nerici", "ğŸ§  MBT Avans Ã–nerici", "ğŸ§  MBT Advance Recommender");
            AddShared("ğŸ”‘ Ã‡alÄ±ÅŸtÄ±rma AnÄ± Avans HaritasÄ±", "ğŸ”‘ Ã‡alÄ±ÅŸtÄ±rma AnÄ± Avans HaritasÄ±", "ğŸ”‘ Cranking Timing Map");
            AddShared("ECT Hararet (Â°C)", "ECT Hararet (Â°C)", "ECT Temp (Â°C)");
            AddShared("AteÅŸleme AvansÄ± (Â°)", "AteÅŸleme AvansÄ± (Â°)", "Ignition Advance (Â°)");
            AddShared("ğŸ”¥ Bireysel Silindir Avans DÃ¼zeltmeleri", "ğŸ”¥ Bireysel Silindir Avans DÃ¼zeltmeleri", "ğŸ”¥ Individual Cylinder Advance Corrections");
            AddShared("Silindir 1:", "Silindir 1:", "Cylinder 1:");
            AddShared("Silindir 2:", "Silindir 2:", "Cylinder 2:");
            AddShared("Silindir 3:", "Silindir 3:", "Cylinder 3:");
            AddShared("Silindir 4:", "Silindir 4:", "Cylinder 4:");
            AddShared("SensÃ¶r Tipi Kalibrasyon EÄŸrisi SeÃ§in:", "SensÃ¶r Tipi Kalibrasyon EÄŸrisi SeÃ§in:", "Select Sensor Calibration Curve Preset:");
            AddShared("ğŸ”Œ Sinyal Linearizasyon SimÃ¼lasyonu", "ğŸ”Œ Sinyal Linearizasyon SimÃ¼lasyonu", "ğŸ”Œ Signal Linearization Simulation");
            AddShared("Analog Voltaj GiriÅŸi (0.0V - 5.0V):", "Analog Voltaj GiriÅŸi (0.0V - 5.0V):", "Analog Voltage Input (0.0V - 5.0V):");
            AddShared("Okunan Fiziksel DeÄŸer:", "Okunan Fiziksel DeÄŸer:", "Physical Value Read:");
            AddShared("ğŸ“¡ CAN Bus Ã‡erÃ§eve Ã‡Ã¶zÃ¼mleme TanÄ±mlarÄ±", "ğŸ“¡ CAN Bus Ã‡erÃ§eve Ã‡Ã¶zÃ¼mleme TanÄ±mlarÄ±", "ğŸ“¡ CAN Bus Frame Parsing Definitions");
            AddShared("Frame ID (HEX):", "Frame ID (HEX):", "Frame ID (HEX):");
            AddShared("BaÅŸlangÄ±Ã§ Biti (Start Bit):", "BaÅŸlangÄ±Ã§ Biti (Start Bit):", "Start Bit:");
            AddShared("Bit UzunluÄŸu (Bit Len):", "Bit UzunluÄŸu (Bit Len):", "Bit Length:");
            AddShared("Ã‡arpan KatsayÄ± (Scale):", "Ã‡arpan KatsayÄ± (Scale):", "Scaling Factor:");
            AddShared("Kayma KatsayÄ± (Offset):", "Kayma KatsayÄ± (Offset):", "Offset:");
            AddShared("Is Motorla Format (Big Endian)", "Is Motorla Format (Big Endian)", "Is Motorola Format (Big Endian)");
            AddShared("ğŸ“¡ CAN Mesaj Paketi CanlÄ± SimÃ¼lasyonu", "ğŸ“¡ CAN Mesaj Paketi CanlÄ± SimÃ¼lasyonu", "ğŸ“¡ CAN Message Packet Live Simulation");
            AddShared("SimÃ¼le Edilen 8-Byte Ã‡erÃ§eve Mesaj (Hex):", "SimÃ¼le Edilen 8-Byte Ã‡erÃ§eve Mesaj (Hex):", "Simulated 8-Byte Frame Message (Hex):");
            AddShared("Ã‡Ã¶zÃ¼mlenen SensÃ¶r Ã‡Ä±ktÄ±sÄ± (EGT):", "Ã‡Ã¶zÃ¼mlenen SensÃ¶r Ã‡Ä±ktÄ±sÄ± (EGT):", "Decoded Sensor Output (EGT):");
            AddShared("ğŸ§  MBT AteÅŸleme SimÃ¼lasyon Girdileri", "ğŸ§  MBT AteÅŸleme SimÃ¼lasyon Girdileri", "ğŸ§  MBT Ignition Simulation Inputs");
            AddShared("Emme Manifold YÃ¼kÃ¼ (kPa):", "Emme Manifold YÃ¼kÃ¼ (kPa):", "Intake Manifold Load (kPa):");
            AddShared("YakÄ±t Oktan OranÄ± (RON):", "YakÄ±t Oktan OranÄ± (RON):", "Fuel Octane Rating (RON):");
            AddShared("Mevcut Avans DeÄŸeri (Â°):", "Mevcut Avans DeÄŸeri (Â°):", "Current Advance Value (Â°):");
            AddShared("ğŸ§  AteÅŸleme Optimizasyon KararÄ±", "ğŸ§  AteÅŸleme Optimizasyon KararÄ±", "ğŸ§  Ignition Optimization Decision");
            AddShared("Modellenen Teorik MBT AvansÄ±:", "Modellenen Teorik MBT AvansÄ±:", "Modeled Theoretical MBT Advance:");
            AddShared("Sapma (Current - MBT):", "Sapma (Current - MBT):", "Deviation (Current - MBT):");
            AddShared("â„¹ï¸ OPTÄ°MÄ°ZASYON: Avans MBT'nin Ã‡ok Gerisinde. GÃ¼Ã§ Kazanmak Ä°Ã§in AvansÄ± ArtÄ±rÄ±n.", "â„¹ï¸ OPTÄ°MÄ°ZASYON: Avans MBT'nin Ã‡ok Gerisinde. GÃ¼Ã§ Kazanmak Ä°Ã§in AvansÄ± ArtÄ±rÄ±n.", "â„¹ï¸ OPTIMIZATION: Advance is far behind MBT. Increase advance to gain power.");
            AddShared("ğŸ”§ Avans DÃ¼zeltmesini Haritada Otomatik Ayarla", "ğŸ”§ Avans DÃ¼zeltmesini Haritada Otomatik Ayarla", "ğŸ”§ Auto-Adjust Advance Correction on Map");
            AddShared("SensÃ¶r Sinyali (Volt)", "SensÃ¶r Sinyali (Volt)", "Sensor Signal (Volt)");

            // VTEC Boost Control
            AddShared("ğŸ VTEC Solenoid Limitleri", "ğŸ VTEC Solenoid Limitleri", "ğŸ VTEC Solenoid Limits");
            AddShared("ğŸ“ˆ Target Boost (RPM vs Gear)", "ğŸ“ˆ Target Boost (RPM vs Gear)", "ğŸ“ˆ Target Boost (RPM vs Gear)");
            AddShared("ğŸ”Œ Base WG Solenoid Duty", "ğŸ”Œ Base WG Solenoid Duty", "ğŸ”Œ Base WG Solenoid Duty");
            AddShared("ğŸ•¹ï¸ Dynamic Solenoid SimÃ¼latÃ¶r", "ğŸ•¹ï¸ Dynamic Solenoid SimÃ¼latÃ¶r", "ğŸ•¹ï¸ Dynamic Solenoid Simulator");
            AddShared("ğŸ VTEC GeÃ§iÅŸ KoÅŸullarÄ±", "ğŸ VTEC GeÃ§iÅŸ KoÅŸullarÄ±", "ğŸ VTEC Transition Conditions");
            AddShared("VTEC Minimum Devir (RPM):", "VTEC Minimum Devir (RPM):", "VTEC Minimum RPM:");
            AddShared("VTEC Minimum HÄ±z (km/h):", "VTEC Minimum HÄ±z (km/h):", "VTEC Minimum Speed (km/h):");
            AddShared("VTEC Engellenen Vites SeÃ§enekleri (Gear Lockout out):", "VTEC Engellenen Vites SeÃ§enekleri (Gear Lockout out):", "VTEC Gear Lockout:");
            AddShared("1. Vites", "1. Vites", "1st Gear");
            AddShared("2. Vites", "2. Vites", "2nd Gear");
            AddShared("3. Vites", "3. Vites", "3rd Gear");
            AddShared("4. Vites", "4. Vites", "4th Gear");
            AddShared("5. Vites", "5. Vites", "5th Gear");
            AddShared("6. Vites", "6. Vites", "6th Gear");
            AddShared("1. Vites (kPa)", "1. Vites (kPa)", "1st Gear (kPa)");
            AddShared("2. Vites (kPa)", "2. Vites (kPa)", "2nd Gear (kPa)");
            AddShared("3. Vites (kPa)", "3. Vites (kPa)", "3rd Gear (kPa)");
            AddShared("4. Vites (kPa)", "4. Vites (kPa)", "4th Gear (kPa)");
            AddShared("5. Vites (kPa)", "5. Vites (kPa)", "5th Gear (kPa)");
            AddShared("ğŸ•¹ï¸ SÃ¼rÃ¼ÅŸ SimÃ¼latÃ¶r Girdileri", "ğŸ•¹ï¸ SÃ¼rÃ¼ÅŸ SimÃ¼latÃ¶r Girdileri", "ğŸ•¹ï¸ Driving Simulator Inputs");
            AddShared("AraÃ§ HÄ±zÄ± (km/h):", "AraÃ§ HÄ±zÄ± (km/h):", "Vehicle Speed (km/h):");
            AddShared("Aktif Vites (Gear):", "Aktif Vites (Gear):", "Active Gear:");
            AddShared("âš¡ Scramble Boost DÃ¼ÄŸmesi (GeÃ§ici Avans / Boost)", "âš¡ Scramble Boost DÃ¼ÄŸmesi (GeÃ§ici Avans / Boost)", "âš¡ Scramble Boost Button (Temp Advance / Boost)");
            AddShared("âš ï¸ KaÃ§ak / Wastegate Hortum YÄ±rtÄ±lmasÄ± SimÃ¼lasyonu", "âš ï¸ KaÃ§ak / Wastegate Hortum YÄ±rtÄ±lmasÄ± SimÃ¼lasyonu", "âš ï¸ Boost Leak / Wastegate Hose Tear Simulation");
            AddShared("ğŸ•¹ï¸ Solenoid & PID Kontrol Ã‡Ä±ktÄ±larÄ±", "ğŸ•¹ï¸ Solenoid & PID Kontrol Ã‡Ä±ktÄ±larÄ±", "ğŸ•¹ï¸ Solenoid & PID Control Outputs");
            AddShared("Hedef Turbo BasÄ±ncÄ±:", "Hedef Turbo BasÄ±ncÄ±:", "Target Turbo Pressure:");
            AddShared("Aktif Turbo BasÄ±ncÄ±:", "Aktif Turbo BasÄ±ncÄ±:", "Active Turbo Pressure:");
            AddShared("Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:");
            AddShared("VTEC Valf Sinyali (Solenoid):", "VTEC Valf Sinyali (Solenoid):", "VTEC Valve Signal (Solenoid):");
            AddShared("Devir (RPM)", "Devir (RPM)", "Engine RPM");

            // Engine Protection Control
            AddShared("ğŸš¨ Limit & Emniyet AyarlarÄ±", "ğŸš¨ Limit & Emniyet AyarlarÄ±", "ğŸš¨ Limit & Safety Settings");
            AddShared("ğŸŒ¡ï¸ Termal DÃ¼zeltmeler & IAT/EGT", "ğŸŒ¡ï¸ Termal DÃ¼zeltmeler & IAT/EGT", "ğŸŒ¡ï¸ Thermal Corrections & IAT/EGT");
            AddShared("ğŸ® GÃ¼venlik Koruma SimÃ¼latÃ¶rÃ¼", "ğŸ® GÃ¼venlik Koruma SimÃ¼latÃ¶rÃ¼", "ğŸ® Safety Protection Simulator");
            AddShared("ğŸš¨ Genel GÃ¼venlik Limitleri", "ğŸš¨ Genel GÃ¼venlik Limitleri", "ğŸš¨ General Safety Limits");
            AddShared("Max YaÄŸ SÄ±caklÄ±ÄŸÄ± (Â°C):", "Max YaÄŸ SÄ±caklÄ±ÄŸÄ± (Â°C):", "Max Oil Temp (Â°C):");
            AddShared("Min YakÄ±t BasÄ±ncÄ± (Bar):", "Min YakÄ±t BasÄ±ncÄ± (Bar):", "Min Fuel Pressure (Bar):");
            AddShared("RadyatÃ¶r Fan SÄ±caklÄ±ÄŸÄ± (Â°C):", "RadyatÃ¶r Fan SÄ±caklÄ±ÄŸÄ± (Â°C):", "Radiator Fan Target Temp (Â°C):");
            AddShared("Maksimum EGT SÄ±nÄ±rÄ± (Â°C):", "Maksimum EGT SÄ±nÄ±rÄ± (Â°C):", "Maximum EGT Limit (Â°C):");
            AddShared("ğŸ“ˆ RPM vs Min YaÄŸ BasÄ±ncÄ± SÄ±nÄ±r EÄŸrisi", "ğŸ“ˆ RPM vs Min YaÄŸ BasÄ±ncÄ± SÄ±nÄ±r EÄŸrisi", "ğŸ“ˆ RPM vs Min Oil Pressure Curve");
            AddShared("Min BasÄ±nÃ§ (Bar)", "Min BasÄ±nÃ§ (Bar)", "Min Pressure (Bar)");
            AddShared("ğŸŒ¡ï¸ Termal YÃ¶netim & IAT DÃ¼zeltmeleri", "ğŸŒ¡ï¸ Termal YÃ¶netim & IAT DÃ¼zeltmeleri", "ğŸŒ¡ï¸ Thermal Management & IAT Corrections");
            AddShared("IAT Heat Soak EÅŸiÄŸi (Â°C):", "IAT Heat Soak EÅŸiÄŸi (Â°C):", "IAT Heat Soak Threshold (Â°C):");
            AddShared("IAT Avans KÄ±sma Derecesi (Â°):", "IAT Avans KÄ±sma Derecesi (Â°):", "IAT Timing Retard (Â°):");
            AddShared("IAT Boost KÄ±sma Derecesi (kPa):", "IAT Boost KÄ±sma Derecesi (kPa):", "IAT Boost Limit Reduction (kPa):");
            AddShared("EGT Avans Geri Ã‡ekme (Â°):", "EGT Avans Geri Ã‡ekme (Â°):", "EGT Timing Pull (Â°):");
            AddShared("EGT KarÄ±ÅŸÄ±m ZenginleÅŸtirme (%):", "EGT KarÄ±ÅŸÄ±m ZenginleÅŸtirme (%):", "EGT Fuel Enrichment (%):");
            AddShared("Limp RPM Ãœst Devir Limiti:", "Limp RPM Ãœst Devir Limiti:", "Limp Mode RPM Limit:");
            AddShared("Rpm Devir (rpm):", "Rpm Devir (rpm):", "Engine Speed (rpm):");
            AddShared("Su SÄ±caklÄ±ÄŸÄ± ECT (Â°C):", "Su SÄ±caklÄ±ÄŸÄ± ECT (Â°C):", "Coolant Temp ECT (Â°C):");
            AddShared("Emme SÄ±caklÄ±ÄŸÄ± IAT (Â°C):", "Emme SÄ±caklÄ±ÄŸÄ± IAT (Â°C):", "Intake Air Temp IAT (Â°C):");
            AddShared("YaÄŸ SÄ±caklÄ±ÄŸÄ± (Â°C):", "YaÄŸ SÄ±caklÄ±ÄŸÄ± (Â°C):", "Oil Temp (Â°C):");
            AddShared("YaÄŸ BasÄ±ncÄ± (Bar):", "YaÄŸ BasÄ±ncÄ± (Bar):", "Oil Pressure (Bar):");
            AddShared("YakÄ±t BasÄ±ncÄ± (Bar):", "YakÄ±t BasÄ±ncÄ± (Bar):", "Fuel Pressure (Bar):");
            AddShared("Turbo Manifold (kPa):", "Turbo Manifold (kPa):", "Turbo Manifold (kPa):");
            AddShared("Egzoz SÄ±caklÄ±ÄŸÄ± EGT (Â°C):", "Egzoz SÄ±caklÄ±ÄŸÄ± EGT (Â°C):", "Exhaust Gas Temp EGT (Â°C):");
            AddShared("ğŸ›¡ï¸ Koruma Emniyet DurumlarÄ±", "ğŸ›¡ï¸ Koruma Emniyet DurumlarÄ±", "ğŸ›¡ï¸ Protection Safety States");
            AddShared("Aktif Limit Devri:", "Aktif Limit Devri:", "Active RPM Limit:");
            AddShared("Toplam Avans KÄ±sma:", "Toplam Avans KÄ±sma:", "Total Timing Pull:");
            AddShared("EGT YakÄ±t ArtÄ±ÅŸÄ±:", "EGT YakÄ±t ArtÄ±ÅŸÄ±:", "EGT Fuel Enrichment:");
            AddShared("Fan RÃ¶lesi Ã‡Ä±kÄ±ÅŸÄ±:", "Fan RÃ¶lesi Ã‡Ä±kÄ±ÅŸÄ±:", "Fan Relay Output:");
            AddShared("ğŸ”„ AlarmlarÄ± SÄ±fÄ±rla / Koruma Reset", "ğŸ”„ AlarmlarÄ± SÄ±fÄ±rla / Koruma Reset", "ğŸ”„ Reset Alarms / Protection Reset");
            AddShared("Ã–zel koruma eÅŸiklerinde bir problem algÄ±lanmadÄ±.", "Ã–zel koruma eÅŸiklerinde bir problem algÄ±lanmadÄ±.", "No issue detected at the custom protection thresholds.");

            AddShared("msg_high_oil_temp", "ğŸš¨ YÃœKSEK YAÄ SICAKLIÄI ({0}Â°C): Motor koruma modu devrede, RPM limiti {1} RPM.", "ğŸš¨ HIGH OIL TEMP ({0}Â°C): Engine protection active, RPM limit {1} RPM.");
            AddShared("msg_low_oil_press", "ğŸš¨ KRÄ°TÄ°K DÃœÅÃœK YAÄ BASINCI ({0} Bar): Motor hasar mekanizmasÄ± nedeniyle YAKIT KESÄ°LDÄ°!", "ğŸš¨ CRITICAL LOW OIL PRESS ({0} Bar): FUEL CUT to prevent engine damage!");
            AddShared("msg_low_fuel_press", "ğŸš¨ DÃœÅÃœK YAKIT BASINCI ({0} Bar): YaÄŸlama basÄ±ncÄ± yetersiz, avans geriye Ã§ekildi, limitler dÃ¼ÅŸÃ¼rÃ¼ldÃ¼.", "ğŸš¨ LOW FUEL PRESS ({0} Bar): Unsafe pressure, timing retarded, limits reduced.");
            AddShared("msg_iat_heat_soak", "âš ï¸ EMME HAVA ENJEKTÃ–RÃœ SICAK (HEAT SOAK): IAT {0}Â°C. Koruma amaÃ§lÄ± avans kÄ±sÄ±lÄ±yor (-{1}Â°), boost payÄ± kÄ±sÄ±tlanÄ±yor.", "âš ï¸ IAT HEAT SOAK: IAT {0}Â°C. Timing retarded (-{1}Â°), boost limit reduced for protection.");
            AddShared("msg_critical_egt", "â— CRITICAL EGT LIMIT ({0}Â°C): Avans kÄ±sÄ±lÄ±yor (-{1}Â°), enjeksiyon %{2} zenginleÅŸtirilerek yanma Ä±sÄ±sÄ± dÃ¼ÅŸÃ¼rÃ¼lÃ¼yor.", "â— CRITICAL EGT LIMIT ({0}Â°C): Timing reduced (-{1}Â°), fueling enriched by {2}% to lower temps.");
            AddShared("msg_lean_cut", "ğŸš¨ LEAN CUT: RPM={0:0} â€” MAP={1:0} kPa â€” AFR={2:0.00} (eÅŸik: >{3}). Fakir yanma korumasÄ± aktif!", "ğŸš¨ LEAN CUT: RPM={0:0} â€” MAP={1:0} kPa â€” AFR={2:0.00} (threshold: >{3}). Lean protection active!");
            AddShared("msg_overboost", "ğŸš¨ OVERBOOST: MAP={0:0} kPa > Limit={1:0} kPa. Boost kesme korumasÄ± devrede!", "ğŸš¨ OVERBOOST: MAP={0:0} kPa > Limit={1:0} kPa. Boost cut protection active!");
            AddShared("msg_ect_temp", "âš ï¸ ECT AÅIRI SICAKLIK ({0}Â°C): Avans -{1:0.0}Â° kÄ±sÄ±ldÄ± (dinamik retard).", "âš ï¸ ECT OVERTEMP ({0}Â°C): Timing retarded -{1:0.0}Â° (dynamic retard).");
            AddShared("msg_knock", "ğŸ”” KNOCK ALGILANDI: Avans -{0:0.0}Â° geri Ã§ekildi.", "ğŸ”” KNOCK DETECTED: Timing retarded -{0:0.0}Â°.");
            AddShared("vtec_inactive", "VTEC KapalÄ±", "VTEC Inactive");
            AddShared("vtec_active", "VTEC Devrede!", "VTEC Active!");
            AddShared("wg_system_safe", "SÄ°STEM GÃœVENLÄ°: Solenoid ve Turbo stabil.", "SYSTEM SAFE: Solenoid and Turbo stable.");
            AddShared("ep_system_safe", "âœ… SÄ°STEM GÃœVENLÄ°", "âœ… SYSTEM SAFE");
            AddShared("ep_power_reduction", "âš ï¸ KORUMA: AVANS/YAKIT MÃœDAHALESÄ°", "âš ï¸ PROTECTION: TIMING/FUEL PULL");
            AddShared("ep_limp_mode", "âš ï¸ LIMP MODE (DEVÄ°R KESÄ°LÄ°YOR)", "âš ï¸ LIMP MODE (RPM CUT)");
            AddShared("ep_fuel_cut", "â›” KRÄ°TÄ°K ALARM: YAKIT KESÄ°LDÄ°!", "â›” CRITICAL ALARM: FUEL CUT!");
            AddShared("fan_relay_on", "AKTÄ°F (Ã‡ekili)", "ACTIVE (Relay On)");
            AddShared("fan_relay_off", "PASÄ°F", "INACTIVE (Relay Off)");

            // DynoLogsControl UI
            AddShared("ğŸ“Š Virtual Dyno & GÃ¼Ã§ AnalizÃ¶rÃ¼", "ğŸ“Š Virtual Dyno & GÃ¼Ã§ AnalizÃ¶rÃ¼", "ğŸ“Š Virtual Dyno & Power Analyzer");
            AddShared("â±ï¸ Pist SÃ¼rÃ¼ÅŸ & Performans", "â±ï¸ Pist SÃ¼rÃ¼ÅŸ & Performans", "â±ï¸ Track Logs & Performance");
            AddShared("ğŸŒ¿ Versiyon & RAM Watchdog", "ğŸŒ¿ Versiyon & RAM Watchdog", "ğŸŒ¿ Versioning & RAM Watchdog");
            AddShared("ğŸï¸ Virtual Dyno Parametreleri", "ğŸï¸ Virtual Dyno Parametreleri", "ğŸï¸ Virtual Dyno Parameters");
            AddShared("AraÃ§ AÄŸÄ±rlÄ±ÄŸÄ± (Kg):", "AraÃ§ AÄŸÄ±rlÄ±ÄŸÄ± (Kg):", "Vehicle Weight (Kg):");
            AddShared("Aktarma KaybÄ± (%):", "Aktarma KaybÄ± (%):", "Drivetrain Loss (%):");
            AddShared("DÃ¼zeltme StandardÄ±:", "DÃ¼zeltme StandardÄ±:", "Correction Standard:");
            AddShared("SimÃ¼le Manifold BasÄ±ncÄ± (Boost):", "SimÃ¼le Manifold BasÄ±ncÄ± (Boost):", "Simulated Manifold Pressure (Boost):");
            AddShared("âš¡ Sanal Dyno Testini Ã‡alÄ±ÅŸtÄ±r", "âš¡ Sanal Dyno Testini Ã‡alÄ±ÅŸtÄ±r", "âš¡ Run Virtual Dyno Test");
            AddShared("ğŸ“ˆ Sanal GÃ¼Ã§ / Tork Ã‡Ä±ktÄ± Tablosu", "ğŸ“ˆ Sanal GÃ¼Ã§ / Tork Ã‡Ä±ktÄ± Tablosu", "ğŸ“ˆ Virtual Power / Torque Output Table");
            AddShared("WHP (Teker)", "WHP (Teker)", "WHP (Wheel)");
            AddShared("Engine HP", "Engine HP", "Engine HP");
            AddShared("Tork (Nm)", "Tork (Nm)", "Torque (Nm)");
            AddShared("â±ï¸ Pist PerformansÄ± & Vites GeÃ§iÅŸ Ã–lÃ§er", "â±ï¸ Pist PerformansÄ± & Vites GeÃ§iÅŸ Ã–lÃ§er", "â±ï¸ Track Performance & Shift Timer");
            AddShared("Lastik Ã‡apÄ± (Ä°nÃ§):", "Lastik Ã‡apÄ± (Ä°nÃ§):", "Tyre Diameter (Inches):");
            AddShared("ÅanzÄ±man Vites OranÄ±:", "ÅanzÄ±man Vites OranÄ±:", "Gearbox Ratio:");
            AddShared("Ayna Mahruti OranÄ±:", "Ayna Mahruti OranÄ±:", "Final Drive Ratio:");
            AddShared("ğŸš€ 0 - 100 km/h HÄ±zlanma:", "ğŸš€ 0 - 100 km/h HÄ±zlanma:", "ğŸš€ 0 - 100 km/h Acceleration:");
            AddShared("âœˆï¸ 100 - 200 km/h HÄ±zlanma:", "âœˆï¸ 100 - 200 km/h HÄ±zlanma:", "âœˆï¸ 100 - 200 km/h Acceleration:");
            AddShared("ğŸ”Œ Vites GeÃ§iÅŸ YavaÅŸlamasÄ±:", "ğŸ”Œ Vites GeÃ§iÅŸ YavaÅŸlamasÄ±:", "ğŸ”Œ Gear Shift Delay:");
            AddShared("ğŸŒ¿ Kalibrasyon SÃ¼rÃ¼m KontrolÃ¼ (Branching)", "ğŸŒ¿ Kalibrasyon SÃ¼rÃ¼m KontrolÃ¼ (Branching)", "ğŸŒ¿ Calibration Version Control (Branching)");
            AddShared("Aktif Dal (Branch):", "Aktif Dal (Branch):", "Active Branch:");
            AddShared("Yeni Dal OluÅŸtur:", "Yeni Dal OluÅŸtur:", "Create New Branch:");
            AddShared("â• Dal AÃ§", "â• Dal AÃ§", "â• New Branch");
            AddShared("HafÄ±za Commit AÃ§Ä±klamasÄ±:", "HafÄ±za Commit AÃ§Ä±klamasÄ±:", "Memory Commit Description:");
            AddShared("ğŸ’¾ Commit", "ğŸ’¾ Commit", "ğŸ’¾ Commit");
            AddShared("ğŸ” RAM DeÄŸer Watchdog (MCU Mercek)", "ğŸ” RAM DeÄŸer Watchdog (MCU Mercek)", "ğŸ” RAM Value Watchdog (MCU Lens)");
            AddShared("DeÄŸiÅŸken", "DeÄŸiÅŸken", "Variable");
            AddShared("CanlÄ± DeÄŸer", "CanlÄ± DeÄŸer", "Live Value");
            AddShared("diag_sim_mode", "(SimÃ¼lasyon Modu)", "(Simulation Mode)");
            AddShared("diag_live_stream_ok", "[VERÄ° AKIÅI] 9600 bps OBD1 Aktif -> Son Okuma: BAÅARILI (Durum: {0})", "[DATA STREAM] 9600 bps OBD1 Active -> Last Read: SUCCESS (State: {0})");
            AddShared("1 (AKTÄ°F)", "1 (AKTÄ°F)", "1 (ACTIVE)");
            AddShared("0 (PASÄ°F)", "0 (PASÄ°F)", "0 (INACTIVE)");
            AddShared("â— BaÄŸlÄ± DeÄŸil", "â— BaÄŸlÄ± DeÄŸil", "â— Disconnected");
            AddShared("CH341A ProgramlayÄ±cÄ± hazÄ±r. 'BaÄŸlan' butonuna basÄ±n.", "CH341A ProgramlayÄ±cÄ± hazÄ±r. 'BaÄŸlan' butonuna basÄ±n.", "CH341A Programmer ready. Press 'Connect' button.");
            AddShared("Yenile", "Yenile", "Refresh");
            AddShared("KodlarÄ± Temizle", "KodlarÄ± Temizle", "Clear DTCs");
            AddShared("mbt_risk", "âš ï¸ RÄ°SK: Mevcut Avans MBT Ãœzerinde! Vuruntu (Knock) Tehlikesi Var.", "âš ï¸ RISK: Current Advance is above MBT! Knock Danger Active.");
            AddShared("mbt_opt", "â„¹ï¸ OPTÄ°MÄ°ZASYON: Avans MBT'nin Ã‡ok Gerisinde. GÃ¼Ã§ Kazanmak Ä°Ã§in AvansÄ± ArtÄ±rÄ±n.", "â„¹ï¸ OPTIMIZATION: Advance is far behind MBT. Increase advance to gain power.");
            AddShared("mbt_safe", "âœ… GÃœVENLÄ°: AteÅŸleme ZamanlamasÄ± MBT NoktasÄ±na Ã‡ok YakÄ±n.", "âœ… SAFE: Ignition Timing is very close to MBT.");
            AddShared("Kod", "Kod", "Code");
            AddShared("AÃ§Ä±klama", "AÃ§Ä±klama", "Description");
            AddShared("branch_created", "\"{0}\" dalÄ± oluÅŸturuldu ve bu dala geÃ§ildi.", "\"{0}\" branch was created and checked out.");
            AddShared("commit_msg", "[Commit: {0}] \"{1}\" dalÄ±nda: {2}", "[Commit: {0}] on branch \"{1}\": {2}");
            AddShared("branch_merged", "\"{0}\" dalÄ± \"{1}\" dalÄ±yla BÄ°RLEÅTÄ°RÄ°LDÄ° (MERGE).", "Branch \"{0}\" was MERGED into \"{1}\".");
            AddShared("Ä°lk temel kalibrasyon dosyasÄ± hazÄ±rlandÄ±.", "Ä°lk temel kalibrasyon dosyasÄ± hazÄ±rlandÄ±.", "Initial base calibration setup stock");

            AddShared("alarm_injector_saturation", "ALARM: EnjektÃ¶r DoygunluÄŸu! (Duty %{0})", "ALARM: Injector Saturation! (Duty %{0})");
            AddShared("diag_can_error", "HATA: GeÃ§ersiz Format", "ERROR: Invalid Format");
            AddShared("mbt_above", "+{0:0.0}Â° (Erken)", "+{0:0.0}Â° (Advanced)");
            AddShared("mbt_retarded", "{0:0.0}Â° (Gecikmeli)", "{0:0.0}Â° (Retarded)");
            AddShared("mbt_apply_msg_fmt", "AteÅŸleme avansÄ± haritada {0:F1}Â° olarak gÃ¼ncellendi.", "Ignition advance on map updated to {0:F1}Â°.");
            AddShared("mbt_apply_title", "Harita GÃ¼ncellendi", "Map Updated");
            AddShared("perf_default_sec", "-- saniye", "-- seconds");
            AddShared("dyno_shift_delay", "{0} ms (Kavrama bÄ±rakma gecikmesi)", "{0} ms (Clutch drop delay)");

            AddShared("Closed Loop AutoTune Kontrol Paneli", "Closed Loop AutoTune Kontrol Paneli", "Closed Loop AutoTune Control Panel");
            AddShared("Oturum AyarlarÄ±", "Oturum AyarlarÄ±", "Session Settings");

            AddShared("chart_3d_title", "3D Harita", "3D Map");
            AddShared("chart_waiting_data", "Harita verisi bekleniyor...", "Waiting for map data...");
            AddShared("chart_drag_rotate", "âŸ³ SÃ¼rÃ¼kle: dÃ¶ndÃ¼r", "âŸ³ Drag: rotate");

            LoadVehicleSelectionDefaults();
        }

        private static void AddShared(string key, string tr, string en)
        {
            _translations[key] = CurrentLanguage == "en" ? en : tr;
        }

        public static void LoadVehicleSelectionDefaults()
        {
            AddShared("veh_dialog_title", "Honda Tuner â€” AraÃ§ / ECU SeÃ§", "Honda Tuner â€” Select Vehicle / ECU");
            AddShared("veh_dialog_label_title", "AraÃ§ & ECU SeÃ§imi", "Vehicle & ECU Selection");
            AddShared("veh_dialog_label_sub", "Honda Community Verified ECU Database â€” pgmfi.org  |  8 ECU  Â·  14 araÃ§ modeli", "Honda Community Verified ECU Database â€” pgmfi.org  |  8 ECUs  Â·  14 vehicle models");
            AddShared("veh_dialog_count", "Bir ECU seÃ§in â†’", "Select an ECU â†’");
            AddShared("veh_dialog_cancel", "Ä°ptal", "Cancel");
            AddShared("veh_dialog_ok", "âœ“  Bu AraÃ§ ile Devam Et", "âœ“  Continue with this Vehicle");
            AddShared("veh_dialog_make", "Marka", "Make");
            AddShared("veh_dialog_model", "Model", "Model");
            AddShared("veh_dialog_trim", "DonanÄ±m", "Trim");
            AddShared("veh_dialog_engine", "Motor", "Engine");
            AddShared("veh_dialog_year", "YÄ±l", "Year");
            AddShared("veh_dialog_hp", "HP", "HP");
            AddShared("veh_dialog_trans", "ÅanzÄ±man", "Transmission");
            AddShared("veh_dialog_region", "BÃ¶lge", "Region");

            AddShared("veh_dialog_vtec", "âš¡ VTEC", "âš¡ VTEC");
            AddShared("veh_dialog_nonvtec", "â—‹ Non-VTEC", "â—‹ Non-VTEC");
            AddShared("veh_dialog_iab", "  IAB", "  IAB");
            AddShared("veh_dialog_desc_select", "â‘  Listeden bir araÃ§ seÃ§in, ardÄ±ndan \"Bu AraÃ§ ile Devam Et\" butonuna tÄ±klayÄ±n.", "â‘  Select a vehicle from the list, then click \"Continue with this Vehicle\".");
            AddShared("veh_dialog_ecu_count", "{0} araÃ§ modeli  |  {1}", "{0} vehicle models  |  {1}");

            AddShared("ecu_vtece", "VTEC-E (Ekonomi)", "VTEC-E (Economy)");
            AddShared("ecu_p05_desc", "DÃ¼ÅŸÃ¼k emisyon / yakÄ±t tasarrufu odaklÄ± VTEC-E motor. Performans odaklÄ± deÄŸil.", "Low emission / fuel economy focused VTEC-E engine. Not performance oriented.");
            AddShared("ecu_p05_note1", "VTEC-E: dÃ¼ÅŸÃ¼k devirde tek supap Ã§alÄ±ÅŸÄ±r â€” yakÄ±t tasarrufu", "VTEC-E: single intake valve operation at low RPM â€” fuel economy");

            AddShared("ecu_nonvtec", "Non-VTEC", "Non-VTEC");
            AddShared("ecu_p06_desc", "Standart 1.5L motor. VTEC devresi yok. Chipleme ile B-serisi swap'larda popÃ¼ler.", "Standard 1.5L engine. No VTEC circuit. Popular for B-series swaps with chipping.");
            AddShared("ecu_p06_note_auto", "Otomatik vites versiyonu", "Automatic transmission version");

            AddShared("ecu_sohcvtec", "SOHC VTEC", "SOHC VTEC");
            AddShared("ecu_p28_desc", "En popÃ¼ler OBD1 ECU. D16Z6 motor. Swap ve tuning iÃ§in referans platform.", "Most popular OBD1 ECU. D16Z6 engine. Reference platform for swaps and tuning.");
            AddShared("ecu_p28_delsol", "Del Sol Ã§atÄ±sÄ±z 2 kiÅŸilik", "Del Sol targa top 2-seater");
            AddShared("ecu_p28_ies_ek", "EK kasa iES â€” P28 OBD1 dÃ¶nÃ¼ÅŸÃ¼mÃ¼ ile tuning", "EK chassis iES â€” tuning with P28 OBD1 conversion");
            AddShared("ecu_p28_ies_tr", "Yumurta kasa iES: OBD1 P28/P06 dÃ¶nÃ¼ÅŸÃ¼mÃ¼yle basemap ve sokak ayarÄ±", "Egg body iES: basemap and street tune with OBD1 P28/P06 conversion");
            AddShared("ecu_p28_swap", "iES kasaya SOHC VTEC swap veya mini-me kurulumlarÄ± iÃ§in", "For SOHC VTEC swap or mini-me setups on iES chassis");

            AddShared("ecu_p30_desc", "EG kasa 1.5i. Non-VTEC D15B2. DÃ¼ÅŸÃ¼k maliyetli chip platform.", "EG chassis 1.5i. Non-VTEC D15B2. Low-cost chip platform.");

            AddShared("ecu_dohcvtec", "DOHC VTEC", "DOHC VTEC");
            AddShared("ecu_p61_desc", "1.7L B17A1 DOHC VTEC. Ä°lk Integra GS-R nesli. 8200 RPM sÄ±nÄ±r.", "1.7L B17A1 DOHC VTEC. First Integra GS-R generation. 8200 RPM limit.");
            AddShared("ecu_p61_note", "Ä°lk DOHC VTEC Integra â€” B17A1", "First DOHC VTEC Integra â€” B17A1");

            AddShared("ecu_dohciab", "DOHC VTEC + IAB", "DOHC VTEC + IAB");
            AddShared("ecu_p72_desc", "B18C1 DOHC VTEC + Intake Air Bypass. 170HP stock. Efsanevi tuning platformu.", "B18C1 DOHC VTEC + Intake Air Bypass. 170HP stock. Legendary tuning platform.");
            AddShared("ecu_p72_iab", "IAB (Intake Air Bypass) solenoidi mevcut â€” P72'ye Ã¶zel", "Features IAB (Intake Air Bypass) solenoid â€” specific to P72");
            AddShared("ecu_p72_itr", "ITR â€” P73 ECU; P72 swap'la uyumlu", "ITR â€” P73 ECU; compatible with P72 swap");

            AddShared("ecu_p74_desc", "B18B1 DOHC Non-VTEC. LS Vtec swap iÃ§in temel ECU.", "B18B1 DOHC Non-VTEC. Base ECU for LS Vtec swap.");

            AddShared("ecu_p13_desc", "H22A 2.2L DOHC VTEC. Prelude serisinin gÃ¼Ã§lÃ¼ kalbi. 190HP JDM.", "H22A 2.2L DOHC VTEC. The powerful heart of the Prelude series. 190HP JDM.");
            AddShared("ecu_p13_jdm_190", "JDM versiyonu 190HP", "JDM version 190HP");
            AddShared("ecu_p13_accord", "JDM/EDM Accord SiR â€” aynÄ± motor, farklÄ± kamera", "JDM/EDM Accord SiR â€” same engine, different cams");

            AddShared("trans_mt", "Manuel", "Manual");
            AddShared("trans_at", "Otomatik", "Automatic");
            AddShared("trans_mt_at", "Manuel/Otomatik", "Manual/Automatic");

            // Appended keys
            AddShared("rom_not_loaded_status", "ROM yÃ¼klenmedi. Dosya â†’ AÃ§ ile baÅŸlayÄ±n.", "ROM not loaded. Start with File â†’ Open.");
            AddShared("commit_msg_init_base", "Ä°lk temel kalibrasyon dosyasÄ± hazÄ±rlandÄ±.", "Initial base calibration file prepared.");
            AddShared("prog_init_ready", "CH341A ProgramlayÄ±cÄ± hazÄ±r. 'BaÄŸlan' butonuna basÄ±n.", "CH341A Programmer ready. Press 'Connect'.");
        }
    }
}
