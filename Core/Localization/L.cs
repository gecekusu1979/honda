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
                // Alınan hatalarda varsayılanları koru
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

                // Hardware Control Tab
                _translations["CH341A EEPROM Programlayıcı"] = "CH341A EEPROM Programmer";
                _translations["Çip Tipi:"] = "Chip Type:";
                _translations["Bağlan"] = "Connect";
                _translations["Bağlantıyı Kes"] = "Disconnect";
                _translations["Çipten Oku"] = "Read Chip";
                _translations["Çipe Yaz"] = "Write Chip";
                _translations["Çipi Sil"] = "Erase Chip";
                _translations["Çipi Doğrula"] = "Verify Chip";
                _translations["İşlem Kaydı:"] = "Operation Log:";
                _translations["İlerleme:"] = "Progress:";
                _translations["Canlı OBD1 Arıza Kodları (DTC)"] = "Live OBD1 Diagnostic Trouble Codes (DTC)";
                _translations["OBD1 seri portu seçin, bağlantı kurun, sonra kodu okuyun."] = "Select OBD1 serial port, connect, then read codes.";
                _translations["Arıza Kodlarını Oku"] = "Read DTCs";
                _translations["Arıza Kodlarını Temizle"] = "Clear DTCs";
                _translations["Akü Voltajı:"] = "Battery Voltage:";

                // Dynamic Status labels
                _translations["Bağlantı Durumu:"] = "Connection Status:";
                _translations["Kuyruk Derinliği:"] = "Queue Depth:";
                _translations["Ortalama Gecikme:"] = "Average Latency:";
                _translations["Hata / Yeniden Deneme:"] = "Error / Retry:";
                _translations["Düşen Yazmalar:"] = "Dropped Writes:";
                _translations["Tuning Kalite Skoru:"] = "Tuning Quality Score:";
                _translations["Araç seçilmedi"] = "No vehicle selected";
                _translations["Status:"] = "Status:";
                _translations["Durum:"] = "Status:";
                _translations["Güvenlik Durumu:"] = "Safety Status:";
                _translations["Kullanıcı Rolü:"] = "User Role:";
                _translations["ECU Bağlantısı:"] = "ECU Connection:";

                // Nested UserControl Headers & Tabs
                _translations["📶 Protokol & Donanım Arayüzleri"] = "📶 Protocols & Hardware Interfaces";
                _translations["A2L Veri Tabanı / Tanımlamalar (ASAP2)"] = "A2L Database / Definitions (ASAP2)";
                _translations["Freeze Frame Verileri (DTC Hata Anı)"] = "Freeze Frame Data (DTC Freeze)";
                _translations["🏁 VTEC Solenoid Limitleri"] = "🏁 VTEC Solenoid Limits";
                _translations["📈 Target Boost (RPM vs Gear)"] = "📈 Target Boost (RPM vs Gear)";
                _translations["Sensör Limitleri & Güvenlik Kesicileri"] = "Sensor Limits & Safety Cuts";
                _translations["Termal Limitler & Avans Kısma"] = "Thermal Limits & Ignition Pulls";
                _translations["Simülasyon Girişleri"] = "Simulation Inputs";
                _translations["Simülasyon Çıkışları"] = "Simulation Outputs";
                _translations["Knock Koruma"] = "Knock Protection";
                _translations["Donanım Öz-Testi Başlat"] = "Start Hardware Self-Test";
                _translations["Protokol Seçin:"] = "Select Protocol:";
                _translations["A2L Bilgileri"] = "A2L Info";
                _translations["Karşılaştırılan Dosya:"] = "Compared File:";
                _translations["Fark Tablosu"] = "Difference Table";
                _translations["Hedef Karışım (Target AFR):"] = "Target Mixture (Target AFR):";
                _translations["Ölçülen Karışım (Wideband AFR):"] = "Measured Mixture (Wideband AFR):";
                _translations["İşlem Yarıçapı (Radius):"] = "Processing Radius (Radius):";
                _translations["Enjektör Boyutu (cc):"] = "Injector Size (cc):";
                _translations["MAP Sensörü Çözünürlüğü:"] = "MAP Sensor Resolution:";
                _translations["Önerilen Düzeltme Yüzdesi:"] = "Recommended Correction %:";
                _translations["Uygula"] = "Apply";
                _translations["Kılavuzlar & Sihirbazlar"] = "Wizards & Guides";
                _translations["Asistan Notları"] = "Assistant Notes";
                _translations["Enjeksiyon Zamanlaması"] = "Injection Timing";
                _translations["Isınma Zenginleştirmesi"] = "Warm-up Enrichment";
                _translations["Hızlanma Zenginleştirmesi"] = "Acceleration Enrichment";
                _translations["ECT Avans Düzeltmesi"] = "ECT Ignition Retard";
                _translations["IAT Avans Düzeltmesi"] = "IAT Ignition Retard";
                _translations["Dwell Süresi"] = "Dwell Duration";
                _translations["Solenoid Aktifleşme RPM'i:"] = "Solenoid Trigger RPM:";
                _translations["Minimum Hız:"] = "Minimum Speed:";
                _translations["Port:"] = "Port:";
                _translations["Dynamic Dyno Canvas"] = "Dynamic Dyno Canvas";
                _translations["Datalog Playback / Kayıt Günlüğü"] = "Datalog Playback / Recording Log";
                _translations["Sürüm Geçmişi & Dallanma"] = "Version History & Branching";
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
                _translations["Enjektör cc"] = "Injector Size (cc)";
                _translations["MAP sensörü bar"] = "MAP Sensor (bar)";
                _translations["Power AFR hedefi"] = "Power AFR Target";
                _translations["Wideband yakıt düzeltme"] = "Wideband Fuel Correction";
                _translations["Ölçülen AFR"] = "Measured AFR";
                _translations["RPM"] = "RPM";
                _translations["Load kPa"] = "Load kPa";
                _translations["Etki alanı"] = "Processing Radius";
                _translations["AFR Düzelt"] = "Correct AFR";
                _translations["Basemap Uygula"] = "Apply Basemap";
                _translations["Notları Yenile"] = "Refresh Notes";

                // Status Bar & Helper Warns
                _translations["Aktif profil:"] = "Active profile:";
                _translations["Araç:"] = "Vehicle:";
                _translations["disclaimer_notes"] = "ROM file is provided by the user. The application does not download or distribute copyrighted stock ROMs; it generates test maps / basemaps and operates on the ROM you provide.";
                _translations["rom_not_loaded_status"] = "ROM not loaded. Start with File → Open.";
                _translations["checksum_ok"] = "✅ Checksum OK";
                _translations["checksum_error"] = "❌ Checksum ERROR";
                _translations["rom_warn_msg"] = "Please load a ROM file first.";
                _translations["rom_warn_title"] = "No ROM";
                _translations["discard_confirm_msg"] = "There are unsaved changes. Continue?";
                _translations["discard_confirm_title"] = "Warning";
                _translations["aktif"] = "active";
                _translations["aktif değil"] = "inactive";
                _translations["Bağlandı"] = "Connected";
                _translations["Bağlanıyor..."] = "Connecting...";
                _translations["Hata"] = "Error";
                _translations["Bağlı Değil"] = "Disconnected";
                _translations["ONLINE"] = "ONLINE";
                _translations["OFFLINE"] = "OFFLINE";
                _translations["BAĞLANIYOR"] = "CONNECTING";
                _translations["HATA"] = "ERROR";
                _translations["SİMÜLASYON"] = "SIMULATION";
                _translations["CANLI"] = "LIVE";
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
                _translations["tab_adv_fuel"] = "🚀 Gelişmiş Yakıt";
                _translations["tab_adv_ignition"] = "⚡ Gelişmiş Ateşleme";
                _translations["tab_vtec_boost"] = "🏁 VTEC & Boost";
                _translations["tab_engine_protection"] = "🛡️ Motor Koruması";
                _translations["tab_diagnostics_a2l"] = "📶 Diagnostics & A2L";
                _translations["tab_dyno_logs"] = "📊 Dyno & Loglar";
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

                // Hardware Control Tab
                _translations["CH341A EEPROM Programlayıcı"] = "CH341A EEPROM Programlayıcı";
                _translations["Çip Tipi:"] = "Çip Tipi:";
                _translations["Bağlan"] = "Bağlan";
                _translations["Bağlantıyı Kes"] = "Bağlantıyı Kes";
                _translations["Çipten Oku"] = "Çipten Oku";
                _translations["Çipe Yaz"] = "Çipe Yaz";
                _translations["Çipi Sil"] = "Çipi Sil";
                _translations["Çipi Doğrula"] = "Çipi Doğrula";
                _translations["İşlem Kaydı:"] = "İşlem Kaydı:";
                _translations["İlerleme:"] = "İlerleme:";
                _translations["Canlı OBD1 Arıza Kodları (DTC)"] = "Canlı OBD1 Arıza Kodları (DTC)";
                _translations["OBD1 seri portu seçin, bağlantı kurun, sonra kodu okuyun."] = "OBD1 seri portu seçin, bağlantı kurun, sonra kodu okuyun.";
                _translations["Arıza Kodlarını Oku"] = "Arıza Kodlarını Oku";
                _translations["Arıza Kodlarını Temizle"] = "Arıza Kodlarını Temizle";
                _translations["Akü Voltajı:"] = "Akü Voltajı:";

                // Dynamic Status labels
                _translations["Bağlantı Durumu:"] = "Bağlantı Durumu:";
                _translations["Kuyruk Derinliği:"] = "Kuyruk Derinliği:";
                _translations["Ortalama Gecikme:"] = "Ortalama Gecikme:";
                _translations["Hata / Yeniden Deneme:"] = "Hata / Yeniden Deneme:";
                _translations["Düşen Yazmalar:"] = "Düşen Yazmalar:";
                _translations["Tuning Kalite Skoru:"] = "Tuning Kalite Skoru:";
                _translations["Araç seçilmedi"] = "Araç seçilmedi";
                _translations["Status:"] = "Durum:";
                _translations["Durum:"] = "Durum:";
                _translations["Güvenlik Durumu:"] = "Güvenlik Durumu:";
                _translations["Kullanıcı Rolü:"] = "Kullanıcı Rolü:";
                _translations["ECU Bağlantısı:"] = "ECU Bağlantısı:";

                // Nested UserControl Headers & Tabs
                _translations["📶 Protokol & Donanım Arayüzleri"] = "📶 Protokol & Donanım Arayüzleri";
                _translations["A2L Veri Tabanı / Tanımlamalar (ASAP2)"] = "A2L Veri Tabanı / Tanımlamalar (ASAP2)";
                _translations["Freeze Frame Verileri (DTC Hata Anı)"] = "Freeze Frame Verileri (DTC Hata Anı)";
                _translations["🏁 VTEC Solenoid Limitleri"] = "🏁 VTEC Solenoid Limitleri";
                _translations["📈 Target Boost (RPM vs Gear)"] = "📈 Target Boost (RPM vs Gear)";
                _translations["Sensör Limitleri & Güvenlik Kesicileri"] = "Sensör Limitleri & Güvenlik Kesicileri";
                _translations["Termal Limitler & Avans Kısma"] = "Termal Limitler & Avans Kısma";
                _translations["Simülasyon Girişleri"] = "Simülasyon Girişleri";
                _translations["Simülasyon Çıkışları"] = "Simülasyon Çıkışları";
                _translations["Knock Koruma"] = "Knock Koruma";
                _translations["Donanım Öz-Testi Başlat"] = "Donanım Öz-Testi Başlat";
                _translations["Protokol Seçin:"] = "Protokol Seçin:";
                _translations["A2L Bilgileri"] = "A2L Bilgileri";
                _translations["Karşılaştırılan Dosya:"] = "Karşılaştırılan Dosya:";
                _translations["Fark Tablosu"] = "Fark Tablosu";
                _translations["Hedef Karışım (Target AFR):"] = "Hedef Karışım (Target AFR):";
                _translations["Ölçülen Karışım (Wideband AFR):"] = "Ölçülen Karışım (Wideband AFR):";
                _translations["İşlem Yarıçapı (Radius):"] = "İşlem Yarıçapı (Radius):";
                _translations["Enjektör Boyutu (cc):"] = "Enjektör Boyutu (cc):";
                _translations["MAP Sensörü Çözünürlüğü:"] = "MAP Sensörü Çözünürlüğü:";
                _translations["Önerilen Düzeltme Yüzdesi:"] = "Önerilen Düzeltme Yüzdesi:";
                _translations["Uygula"] = "Uygula";
                _translations["Kılavuzlar & Sihirbazlar"] = "Kılavuzlar & Sihirbazlar";
                _translations["Asistan Notları"] = "Asistan Notları";
                _translations["Enjeksiyon Zamanlaması"] = "Enjeksiyon Zamanlaması";
                _translations["Isınma Zenginleştirmesi"] = "Isınma Zenginleştirmesi";
                _translations["Hızlanma Zenginleştirmesi"] = "Hızlanma Zenginleştirmesi";
                _translations["ECT Avans Düzeltmesi"] = "ECT Avans Düzeltmesi";
                _translations["IAT Avans Düzeltmesi"] = "IAT Avans Düzeltmesi";
                _translations["Dwell Süresi"] = "Dwell Süresi";
                _translations["Solenoid Aktifleşme RPM'i:"] = "Solenoid Aktifleşme RPM'i:";
                _translations["Minimum Hız:"] = "Minimum Hız:";
                _translations["Port:"] = "Port:";
                _translations["Dynamic Dyno Canvas"] = "Dynamic Dyno Canvas";
                _translations["Datalog Playback / Kayıt Günlüğü"] = "Datalog Playback / Kayıt Günlüğü";
                _translations["Sürüm Geçmişi & Dallanma"] = "Sürüm Geçmişi & Dallanma";
                _translations["Disconnected"] = "Bağlantı Yok";
                _translations["Connected"] = "Bağlı";
                _translations["Connecting"] = "Bağlanıyor";
                _translations["Synchronizing"] = "Senkronize Ediliyor";
                _translations["Paused"] = "Duraklatıldı";
                _translations["Faulted"] = "Hatalı";

                // ComboBox Items & Descriptions
                _translations["Asistan Kilavuzu"] = "Asistan Kılavuzu";
                _translations["Gelismis Ayarlar"] = "Gelişmiş Ayarlar";
                _translations["Sihirbazlar"] = "Sihirbazlar";
                _translations["Gelismis Patch Merkezi"] = "Gelişmiş Patch Merkezi";
                _translations["Stock / gunluk kullanim"] = "Stock / Günlük Kullanım";
                _translations["iES VTEC yumurta kasa sokak ayari"] = "iES VTEC Sokak Ayarı";
                _translations["Atmosferik performans"] = "Atmosferik Performans";
                _translations["Turbo guvenli basemap"] = "Turbo Güvenli Basemap";
                _translations["Ekonomi / dusuk tuketim"] = "Ekonomi / Düşük Tüketim";

                // Labels in BuildAssistantPage
                _translations["Gorunum Secenegi"] = "Görünüm Seçeneği";
                _translations["Basemap hedefi"] = "Basemap Hedefi";
                _translations["Enjektör cc"] = "Enjektör Boyutu (cc)";
                _translations["MAP sensörü bar"] = "MAP Sensörü (bar)";
                _translations["Power AFR hedefi"] = "Power AFR Hedefi";
                _translations["Wideband yakıt düzeltme"] = "Wideband Yakıt Düzeltme";
                _translations["Ölçülen AFR"] = "Ölçülen AFR";
                _translations["RPM"] = "RPM";
                _translations["Load kPa"] = "Load (kPa)";
                _translations["Etki alanı"] = "Etki Alanı";
                _translations["AFR Düzelt"] = "AFR Düzelt";
                _translations["Basemap Uygula"] = "Basemap Uygula";
                _translations["Notları Yenile"] = "Notları Yenile";

                // Status Bar & Helper Warns
                _translations["Aktif profil:"] = "Aktif profil:";
                _translations["Araç:"] = "Araç:";
                _translations["disclaimer_notes"] = "ROM dosyası kullanıcıdan alınır. Uygulama telifli stock ROM indirmez veya dağıtmaz; test/basemap üretir ve kendi okuduğun ROM üzerinde çalışır.";
                _translations["rom_not_loaded_status"] = "ROM yüklenmedi. Dosya → Aç ile başlayın.";
                _translations["checksum_ok"] = "✅ Checksum OK";
                _translations["checksum_error"] = "❌ Checksum HATA";
                _translations["rom_warn_msg"] = "Önce bir ROM dosyası yükleyin.";
                _translations["rom_warn_title"] = "ROM Yok";
                _translations["discard_confirm_msg"] = "Kaydedilmemiş değişiklikler var. Devam et?";
                _translations["discard_confirm_title"] = "Uyarı";
                _translations["aktif"] = "aktif";
                _translations["aktif değil"] = "aktif değil";
                _translations["Bağlandı"] = "Bağlandı";
                _translations["Bağlanıyor..."] = "Bağlanıyor...";
                _translations["Hata"] = "Hata";
                _translations["Bağlı Değil"] = "Bağlı Değil";
                _translations["ONLINE"] = "ONLINE";
                _translations["OFFLINE"] = "OFFLINE";
                _translations["BAĞLANIYOR"] = "BAĞLANIYOR";
                _translations["HATA"] = "HATA";
                _translations["SİMÜLASYON"] = "SİMÜLASYON";
                _translations["CANLI"] = "CANLI";
            }

            LoadSharedDefaults();
        }

        private static void LoadSharedDefaults()
        {
            AddShared("status_offline", "ÇEVRİMDIŞI", "OFFLINE");
            AddShared("status_online", "ÇEVRİMİÇİ", "ONLINE");
            AddShared("status_connecting", "BAĞLANIYOR", "CONNECTING");
            AddShared("status_error", "HATA", "ERROR");
            AddShared("status_disconnected", "BAĞLI DEĞİL", "DISCONNECTED");

            AddShared("status_prog_offline", "🔌 PROG: ÇEVRİMDIŞI", "🔌 PROG: OFFLINE");
            AddShared("status_prog_online", "🔌 PROG: ÇEVRİMİÇİ", "🔌 PROG: ONLINE");
            AddShared("status_prog_connecting", "🔌 PROG: BAĞLANIYOR", "🔌 PROG: CONNECTING");
            AddShared("status_prog_error", "🔌 PROG: HATA", "🔌 PROG: ERROR");

            AddShared("status_emu_offline", "🎮 EMU: ÇEVRİMDIŞI", "🎮 EMU: OFFLINE");
            AddShared("status_emu_online", "🎮 EMU: ÇEVRİMİÇİ", "🎮 EMU: ONLINE");
            AddShared("status_emu_connecting", "🎮 EMU: BAĞLANIYOR", "🎮 EMU: CONNECTING");
            AddShared("status_emu_error", "🎮 EMU: HATA", "🎮 EMU: ERROR");

            AddShared("status_simulation", "🟢 SİMÜLASYON", "🟢 SIMULATION");
            AddShared("status_live", "🔴 CANLI", "🔴 LIVE");
            AddShared("status_connection_state", "Bağlantı Durumu", "Connection State");
            AddShared("status_queue_depth", "Kuyruk Derinliği", "Queue Depth");
            AddShared("status_average_latency", "Ortalama Gecikme", "Average Latency");
            AddShared("status_error_retry", "Hata / Yeniden Deneme", "Error / Retry");
            AddShared("status_dropped_writes", "Düşen Yazmalar", "Dropped Writes");
            AddShared("status_connected", "● Bağlandı", "● Connected");

            AddShared("status_auto_tune_off", "Durum: OFF", "Status: OFF");
            AddShared("status_auto_tune_running", "Durum: Sürüyor", "Status: Running");
            AddShared("status_auto_tune_paused", "Durum: Askıda", "Status: Paused");
            AddShared("status_auto_tune_stopped", "Durum: Durduruldu", "Status: Stopped");
            AddShared("status_safety_safe", "Güvenlik Durumu: SAFE", "Safety Status: SAFE");
            AddShared("status_safety_unknown", "Güvenlik Durumu: --", "Safety Status: --");
            AddShared("status_safety_violation", "Güvenlik Durumu: VIOLATION", "Safety Status: VIOLATION");
            AddShared("status_user_role", "Kullanıcı Rolü", "User Role");
            AddShared("status_user_role_professional", "Kullanıcı Rolü: Professional", "User Role: Professional");
            AddShared("status_ecu_connection", "ECU Bağlantısı", "ECU Connection");
            AddShared("status_ecu_connection_none", "ECU Bağlantısı: Yok", "ECU Connection: None");
            AddShared("status_tuning_quality", "Tuning Kalite Skoru", "Tuning Quality Score");
            AddShared("status_tuning_quality_value", "Tuning Kalite Skoru: {0:0.0}%", "Tuning Quality Score: {0:0.0}%");
            AddShared("warn_safety_violation", "[WARN] Safety Violation: {0}", "[WARN] Safety Violation: {0}");

            AddShared("autotune_commit", "Commit", "Commit");
            AddShared("autotune_tune", "Tune", "Tune");
            AddShared("autotune_map", "Map", "Map");
            AddShared("autotune_wildcard", "[*,*]", "[*,*]");
            AddShared("autotune_unknown", "--", "--");
            AddShared("autotune_success_rate", "100%", "100%");
            AddShared("autotune_richen", "Zenginleştir", "Richen");
            AddShared("autotune_leanen", "Fakirleştir", "Lean");
            AddShared("autotune_live_status_title", "Canlı Durum ve Güvenlik Limitleri", "Live Status and Safety Limits");
            AddShared("btn_at_start", "▶ Başlat", "▶ Start");
            AddShared("btn_at_pause", "⏸ Duraklat", "⏸ Pause");
            AddShared("btn_at_resume", "▶ Devam Et", "▶ Resume");
            AddShared("btn_at_stop", "⏹ Durdur", "⏹ Stop");
            AddShared("autotune_user_advanced", "Advanced", "Advanced");
            AddShared("autotune_user_beginner", "Beginner", "Beginner");

            AddShared("csv_loaded_status", "CSV yüklendi", "CSV loaded");
            AddShared("autotune_session_started", "AutoTune Oturumu Başlatıldı", "AutoTune Session Started");
            AddShared("autotune_session_paused", "AutoTune Oturumu Duraklatıldı", "AutoTune Session Paused");
            AddShared("autotune_session_resumed", "AutoTune Oturumu Devam Ettiriliyor", "AutoTune Session Resumed");
            AddShared("autotune_session_stopped", "AutoTune Oturumu Durduruldu", "AutoTune Session Stopped");
            AddShared("wideband_correction_applied", "Wideband AFR ölçümüne göre yakıt haritası düzeltildi.", "Fuel map corrected according to wideband AFR measurement.");
            AddShared("stock_restored_status", "↩  Stock ROM'a döndürüldü.", "↩  Restored to stock ROM.");
            AddShared("status_empty", "", "");

            AddShared("CH341A EEPROM Programlayıcı", "CH341A EEPROM Programlayıcı", "CH341A EEPROM Programmer");
            AddShared("Çip Tipi:", "Çip Tipi:", "Chip Type:");
            AddShared("Bağlan", "Bağlan", "Connect");
            AddShared("Bağlantıyı Kes", "Bağlantıyı Kes", "Disconnect");
            AddShared("Çipten Oku", "Çipten Oku", "Read Chip");
            AddShared("Çipe Yaz", "Çipe Yaz", "Write Chip");
            AddShared("Çipi Sil", "Çipi Sil", "Erase Chip");
            AddShared("Çipi Doğrula", "Çipi Doğrula", "Verify Chip");
            AddShared("İşlem Kaydı:", "İşlem Kaydı:", "Operation Log:");
            AddShared("İlerleme:", "İlerleme:", "Progress:");
            AddShared("Canlı OBD1 Arıza Kodları (DTC)", "Canlı OBD1 Arıza Kodları (DTC)", "Live OBD1 Diagnostic Trouble Codes (DTC)");
            AddShared("OBD1 seri portu seçin, bağlantı kurun, sonra kodu okuyun.", "OBD1 seri portu seçin, bağlantı kurun, sonra kodu okuyun.", "Select the OBD1 serial port, connect, then read the code.");
            AddShared("Arıza Kodlarını Oku", "Arıza Kodlarını Oku", "Read DTCs");
            AddShared("Arıza Kodlarını Temizle", "Arıza Kodlarını Temizle", "Clear DTCs");
            AddShared("Akü Voltajı:", "Akü Voltajı:", "Battery Voltage:");
            AddShared("Çalışma Modu:", "Çalışma Modu:", "Working Mode:");
            AddShared("Ayar Profili:", "Ayar Profili:", "Profile:");
            AddShared("Kullanıcı Rolü:", "Kullanıcı Rolü:", "User Role:");
            AddShared("Canlı Durum ve Güvenlik Limitleri", "Canlı Durum ve Güvenlik Limitleri", "Live Status and Safety Limits");
            AddShared("RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator");
            AddShared("Bağlantı Durumu:", "Bağlantı Durumu:", "Connection State:");
            AddShared("Kuyruk Derinliği:", "Kuyruk Derinliği:", "Queue Depth:");
            AddShared("Ortalama Gecikme:", "Ortalama Gecikme:", "Average Latency:");
            AddShared("Hata / Yeniden Deneme:", "Hata / Yeniden Deneme:", "Error / Retry:");
            AddShared("Düşen Yazmalar:", "Düşen Yazmalar:", "Dropped Writes:");
            AddShared("Gorunum Secenegi", "Görünüm Seçeneği", "View Option");
            AddShared("Basemap hedefi", "Basemap Hedefi", "Basemap Target");
            AddShared("Basemap Uygula", "Basemap Uygula", "Apply Basemap");
            AddShared("Notları Yenile", "Notları Yenile", "Refresh Notes");
            AddShared("AFR Düzelt", "AFR Düzelt", "Correct AFR");
            AddShared("Wideband yakıt düzeltme", "Wideband Yakıt Düzeltme", "Wideband fuel correction");
            AddShared("Ölçülen AFR", "Ölçülen AFR", "Measured AFR");
            AddShared("Load kPa", "Load kPa", "Load kPa");
            AddShared("Etki alanı", "Etki Alanı", "Processing radius");

            AddShared("🚨 Limit & Emniyet Ayarları", "🚨 Limit & Emniyet Ayarları", "🚨 Limit & Safety Settings");
            AddShared("🌡️ Termal Düzeltmeler & IAT/EGT", "🌡️ Termal Düzeltmeler & IAT/EGT", "🌡️ Thermal Corrections & IAT/EGT");
            AddShared("🎮 Güvenlik Koruma Simülatörü", "🎮 Güvenlik Koruma Simülatörü", "🎮 Safety Protection Simulator");
            AddShared("🚨 Genel Güvenlik Limitleri", "🚨 Genel Güvenlik Limitleri", "🚨 General Safety Limits");
            AddShared("📈 RPM vs Min Yağ Basıncı Sınır Eğrisi", "📈 RPM vs Min Yağ Basıncı Sınır Eğrisi", "📈 RPM vs Minimum Oil Pressure Curve");
            AddShared("🌡️ Termal Yönetim & IAT Düzeltmeleri", "🌡️ Termal Yönetim & IAT Düzeltmeleri", "🌡️ Thermal Management & IAT Corrections");
            AddShared("🕹️ Simülasyon Sürüş Parametreleri", "🕹️ Simülasyon Sürüş Parametreleri", "🕹️ Simulation Driving Parameters");
            AddShared("🛡️ Koruma Emniyet Durumları", "🛡️ Koruma Emniyet Durumları", "🛡️ Safety Protection States");
            AddShared("🔄 Alarmları Sıfırla / Koruma Reset", "🔄 Alarmları Sıfırla / Koruma Reset", "🔄 Reset Alarms / Protection Reset");
            AddShared("Özel koruma eşiklerinde bir problem algılanmadı.", "Özel koruma eşiklerinde bir problem algılanmadı.", "No issue detected at the custom protection thresholds.");
            AddShared("Aktif Limit Devri:", "Aktif Limit Devri:", "Active Limit RPM:");
            AddShared("Toplam Avans Kısma:", "Toplam Avans Kısma:", "Total Timing Pull:");
            AddShared("EGT Yakıt Artışı:", "EGT Yakıt Artışı:", "EGT Fuel Enrichment:");
            AddShared("Fan Rölesi Çıkışı:", "Fan Rölesi Çıkışı:", "Fan Relay Output:");
            AddShared("Protokol & Donanım Arayüzleri", "Protokol & Donanım Arayüzleri", "Protocols & Hardware Interfaces");
            AddShared("A2L Veri Tabanı / Tanımlamalar (ASAP2)", "A2L Veri Tabanı / Tanımlamalar (ASAP2)", "A2L Database / Definitions (ASAP2)");
            AddShared("Freeze Frame Verileri (DTC Hata Anı)", "Freeze Frame Verileri (DTC Hata Anı)", "Freeze Frame Data (DTC Event)");
            AddShared("📶 Protokol & Donanım Arayüzleri", "📶 Protokol & Donanım Arayüzleri", "📶 Protocol & Hardware Interfaces");
            AddShared("📷 Freeze Frame Günlükleri", "📷 Freeze Frame Günlükleri", "📷 Freeze Frame Logs");
            AddShared("📝 Standartlar & A2L Export", "📝 Standartlar & A2L Export", "📝 Standards & A2L Export");
            AddShared("Donanım Arayüzü:", "Donanım Arayüzü:", "Hardware Interface:");
            AddShared("Datalog Baud Rate:", "Datalog Baud Rate:", "Datalog Baud Rate:");
            AddShared("WiFi IP Address:", "WiFi IP Address:", "WiFi IP Address:");
            AddShared("WiFi Port:", "WiFi Port:", "WiFi Port:");
            AddShared("⚡ ECU Diagnostic Self-Test Başlat", "⚡ ECU Diagnostic Self-Test Başlat", "⚡ Start ECU Diagnostic Self-Test");
            AddShared("🖥️ Canlı İletişim & Hata Tanı Konsolu", "🖥️ Canlı İletişim & Hata Tanı Konsolu", "🖥️ Live Communication & Diagnostics Console");
            AddShared("Arıza Kodu Seç:", "Arıza Kodu Seç:", "Select DTC:");
            AddShared("💥 Hata Tetikle (Freeze Frame)", "💥 Hata Tetikle (Freeze Frame)", "💥 Trigger Fault (Freeze Frame)");
            AddShared("💾 A2L Harita Dosyası (.a2l) İhraç Et", "💾 A2L Harita Dosyası (.a2l) İhraç Et", "💾 Export A2L Map File (.a2l)");
            AddShared("Arıza Kodu", "Arıza Kodu", "Fault Code");
            AddShared("Tetiklenme Saati", "Tetiklenme Saati", "Trigger Time");
            AddShared("Motor Devri (RPM)", "Motor Devri (RPM)", "Engine RPM");
            AddShared("Su Sıcaklığı (°C)", "Su Sıcaklığı (°C)", "Coolant Temp (°C)");
            AddShared("Intake Sıcaklığı (°C)", "Intake Sıcaklığı (°C)", "Intake Temp (°C)");
            AddShared("Hız (km/h)", "Hız (km/h)", "Speed (km/h)");
            AddShared("Boost MAP (kPa)", "Boost MAP (kPa)", "Boost MAP (kPa)");
            AddShared("📝 PROJE VE MOTOR META VERİLERİ", "📝 PROJE VE MOTOR META VERİLERİ", "📝 PROJECT AND ENGINE METADATA");
            AddShared("ECU Seri Numarası:", "ECU Seri Numarası:", "ECU Serial Number:");
            AddShared("Hardware Revizyonu:", "Hardware Revizyonu:", "Hardware Revision:");
            AddShared("Şasi Numarası (VIN):", "Şasi Numarası (VIN):", "VIN Number:");
            AddShared("Kasa Kodu (Örn: EG6, EK4):", "Kasa Kodu (Örn: EG6, EK4):", "Chassis Code (e.g. EG6, EK4):");
            AddShared("Sıkıştırma Oranı (:1):", "Sıkıştırma Oranı (:1):", "Compression Ratio (:1):");
            AddShared("Eksantrik Profili:", "Eksantrik Profili:", "Camshaft Profile:");
            AddShared("Şanzıman Tipi:", "Şanzıman Tipi:", "Gearbox Type:");
            AddShared("İndüksiyon Türü:", "İndüksiyon Türü:", "Induction Type:");
            AddShared("💾 Değişiklikleri Kaydet", "💾 Değişiklikleri Kaydet", "💾 Save Changes");
            AddShared("⚡ Canlı Analiz", "⚡ Canlı Analiz", "⚡ Live Analysis");
            AddShared("🔌 OBD1 ECU Pinout", "🔌 OBD1 ECU Pinout", "🔌 OBD1 ECU Pinout");
            AddShared("Ara:", "Ara:", "Search:");
            AddShared("Soket:", "Soket:", "Connector:");
            AddShared("Hepsi", "Hepsi", "All");
            AddShared("Soket A", "Soket A", "Socket A");
            AddShared("Soket B", "Soket B", "Socket B");
            AddShared("Soket D", "Soket D", "Socket D");
            AddShared("Pin", "Pin", "Pin");
            AddShared("Sembol", "Sembol", "Symbol");
            AddShared("Sinyal Türü", "Sinyal Türü", "Signal Type");
            AddShared("Kablo Rengi", "Kablo Rengi", "Wire Color");
            AddShared("Açıklama", "Açıklama", "Description");
            AddShared("🏁 VTEC Geçiş Koşulları", "🏁 VTEC Geçiş Koşulları", "🏁 VTEC Transition Conditions");
            AddShared("VTEC Minimum Devir (RPM):", "VTEC Minimum Devir (RPM):", "VTEC Minimum RPM:");
            AddShared("VTEC Minimum Hız (km/h):", "VTEC Minimum Hız (km/h):", "VTEC Minimum Speed (km/h):");
            AddShared("VTEC Engellenen Vites Seçenekleri (Gear Lockout out):", "VTEC Engellenen Vites Seçenekleri (Gear Lockout):", "VTEC Locked Gear Options (Gear Lockout):");
            AddShared("1. Vites", "1. Vites", "1st Gear");
            AddShared("2. Vites", "2. Vites", "2nd Gear");
            AddShared("3. Vites", "3. Vites", "3rd Gear");
            AddShared("4. Vites", "4. Vites", "4th Gear");
            AddShared("5. Vites", "5. Vites", "5th Gear");
            AddShared("6. Vites", "6. Vites", "6th Gear");
            AddShared("🕹️ Sürüş Simülatör Girdileri", "🕹️ Sürüş Simülatör Girdileri", "🕹️ Driving Simulator Inputs");
            AddShared("Motor Devri (RPM):", "Motor Devri (RPM):", "Engine RPM:");
            AddShared("Araç Hızı (km/h):", "Araç Hızı (km/h):", "Vehicle Speed (km/h):");
            AddShared("Aktif Vites (Gear):", "Aktif Vites (Gear):", "Current Gear:");
            AddShared("⚡ Scramble Boost Düğmesi (Geçici Avans / Boost)", "⚡ Scramble Boost Düğmesi (Geçici Avans / Boost)", "⚡ Scramble Boost Button (Temporary Advance / Boost)");
            AddShared("⚠️ Kaçak / Wastegate Hortum Yırtılması Simülasyonu", "⚠️ Kaçak / Wastegate Hortum Yırtılması Simülasyonu", "⚠️ Leak / Wastegate Hose Tear Simulation");
            AddShared("🕹️ Solenoid & PID Kontrol Çıktıları", "🕹️ Solenoid & PID Kontrol Çıktıları", "🕹️ Solenoid & PID Control Outputs");
            AddShared("Hedef Turbo Basıncı:", "Hedef Turbo Basıncı:", "Target Boost:");
            AddShared("Aktif Turbo Basıncı:", "Aktif Turbo Basıncı:", "Actual Boost:");
            AddShared("Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:");
            AddShared("VTEC Valf Sinyali (Solenoid):", "VTEC Valf Sinyali (Solenoid):", "VTEC Valve Signal (Solenoid):");
            AddShared("⚡ PASİF (VTEC LOCK)", "⚡ PASİF (VTEC LOCK)", "⚡ INACTIVE (VTEC LOCK)");
            AddShared("✅ Wastegate Sistemi Güvenli Aralıkta Çalışıyor", "✅ Wastegate Sistemi Güvenli Aralıkta Çalışıyor", "✅ Wastegate System Operating Within Safe Range");
            AddShared("🏎️ Virtual Dyno Parametreleri", "🏎️ Virtual Dyno Parametreleri", "🏎️ Virtual Dyno Parameters");
            AddShared("Araç Ağırlığı (Kg):", "Araç Ağırlığı (Kg):", "Vehicle Weight (Kg):");
            AddShared("Aktarma Kaybı (%):", "Aktarma Kaybı (%):", "Drivetrain Loss (%):");
            AddShared("Düzeltme Standardı:", "Düzeltme Standardı:", "Correction Standard:");
            AddShared("Simüle Manifold Basıncı (Boost):", "Simüle Manifold Basıncı (Boost):", "Simulated Manifold Pressure (Boost):");
            AddShared("⚡ Sanal Dyno Testini Çalıştır", "⚡ Sanal Dyno Testini Çalıştır", "⚡ Run Virtual Dyno Test");
            AddShared("Azami Güç: -- HP @ -- RPM | Azami Tork: -- Nm", "Azami Güç: -- HP @ -- RPM | Azami Tork: -- Nm", "Peak Power: -- HP @ -- RPM | Peak Torque: -- Nm");
            AddShared("📈 Sanal Güç / Tork Çıktı Tablosu", "📈 Sanal Güç / Tork Çıktı Tablosu", "📈 Virtual Power / Torque Output Table");
            AddShared("⏱️ Pist Performansı & Vites Geçiş Ölçer", "⏱️ Pist Performansı & Vites Geçiş Ölçer", "⏱️ Track Performance & Shift Timer");
            AddShared("Lastik Çapı (İnç):", "Lastik Çapı (İnç):", "Tyre Diameter (Inch):");
            AddShared("Şanzıman Vites Oranı:", "Şanzıman Vites Oranı:", "Gear Ratio:");
            AddShared("Ayna Mahruti Oranı:", "Ayna Mahruti Oranı:", "Final Drive Ratio:");
            AddShared("🚀 0 - 100 km/h Hızlanma:", "🚀 0 - 100 km/h Hızlanma:", "🚀 0 - 100 km/h Acceleration:");
            AddShared("✈️ 100 - 200 km/h Hızlanma:", "✈️ 100 - 200 km/h Hızlanma:", "✈️ 100 - 200 km/h Acceleration:");
            AddShared("🔌 Vites Geçiş Yavaşlaması:", "🔌 Vites Geçiş Yavaşlaması:", "🔌 Shift Delay:");
            AddShared("🌿 Kalibrasyon Sürüm Kontrolü (Branching)", "🌿 Kalibrasyon Sürüm Kontrolü (Branching)", "🌿 Calibration Version Control (Branching)");
            AddShared("Aktif Dal (Branch):", "Aktif Dal (Branch):", "Active Branch:");
            AddShared("Yeni Dal Oluştur:", "Yeni Dal Oluştur:", "Create New Branch:");
            AddShared("➕ Dal Aç", "➕ Dal Aç", "➕ Open Branch");
            AddShared("Hafıza Commit Açıklaması:", "Hafıza Commit Açıklaması:", "Memory Commit Description:");
            AddShared("💾 Commit", "💾 Commit", "💾 Commit");
            AddShared("🔎 RAM Değer Watchdog (MCU Mercek)", "🔎 RAM Değer Watchdog (MCU Mercek)", "🔎 RAM Value Watchdog (MCU Lens)");
            AddShared("Canlı OBD1 Arıza Kodları (DTC)", "Canlı OBD1 Arıza Kodları (DTC)", "Live OBD1 Diagnostic Trouble Codes (DTC)");
            AddShared("PCB / Map", "PCB / Map", "PCB / Map");

            // --- USER INTERFACE APP ADDITIONS ---
            // Main Tab Buttons
            AddShared("tab_fuel", "⛽ Yakıt", "⛽ Fuel");
            AddShared("tab_ignition", "⚡ Ateşleme", "⚡ Ignition");
            AddShared("tab_tuning_assistant", "🧠 Tuning Asistanı", "🧠 Tuning Assistant");
            AddShared("tab_diff", "🔍 Diff", "🔍 Diff");
            AddShared("tab_telemetry", "📊 Telemetri", "📊 Telemetry");
            AddShared("tab_part_viewer", "🔩 3D Parça", "🔩 3D Part");
            AddShared("tab_autotune", "🚀 AutoTune", "🚀 AutoTune");
            AddShared("tab_project_pinout", "✏️ Proje & Pinout", "✏️ Project & Pinout");
            AddShared("tab_analysis_decompiler", "🔍 Analiz & Decompiler", "🔍 Analysis & Decompiler");
            AddShared("tab_adv_fuel", "🚀 Advanced Fuel", "🚀 Advanced Fuel");
            AddShared("tab_adv_ignition", "⚡ Advanced Ignition", "⚡ Advanced Ignition");
            AddShared("tab_vtec_boost", "🏁 VTEC & Boost", "🏁 VTEC & Boost");
            AddShared("tab_engine_protection", "🛡️ Engine Protection", "🛡️ Engine Protection");
            AddShared("tab_diagnostics_a2l", "📶 Diagnostics & A2L", "📶 Diagnostics & A2L");
            AddShared("tab_dyno_logs", "📊 Dyno, Logs & Branching", "📊 Dyno & Logs");
            AddShared("tab_hardware_control", "🔌 Donanım Kontrol", "🔌 Hardware Control");

            // Sub Tab pages in Advanced Fuel
            AddShared("⛽ Alpha-N VE", "⛽ Alpha-N VE", "⛽ Alpha-N VE");
            AddShared("🔌 MAF Ölçeği", "🔌 MAF Ölçeği", "🔌 MAF Scale");
            AddShared("🌡️ Soğuk Çalışma & Düzeltmeler", "🌡️ Soğuk Çalışma & Düzeltmeler", "🌡️ Cold Start & Corrections");
            AddShared("⚡ Canlı Enjektör & Düzeltme Simülatörü", "⚡ Canlı Enjektör & Düzeltme Simülatörü", "⚡ Live Injector Simulator");
            AddShared("Taban Yakıt Süresi (ms):", "Taban Yakıt Süresi (ms):", "Base Fuel Duration (ms):");
            AddShared("Motor Sıcaklık (°C ECT):", "Motor Sıcaklık (°C ECT):", "Engine Temp (°C ECT):");
            AddShared("Yakıt Basıncı (psi - Aktif):", "Yakıt Basıncı (psi - Aktif):", "Fuel Pressure (psi - Active):");
            AddShared("Hedef Yakıt Basıncı (psi):", "Hedef Yakıt Basıncı (psi):", "Target Fuel Pressure (psi):");
            AddShared("Gaz Değişim Hızı (dTPS %/s):", "Gaz Değişim Hızı (dTPS %/s):", "Throttle Change Rate (dTPS %/s):");
            AddShared("Alpha-N Yakıt Modunu Kullan (TPS vs RPM)", "Alpha-N Yakıt Modunu Kullan (TPS vs RPM)", "Use Alpha-N Fuel Mode (TPS vs RPM)");
            AddShared("💥 Gaz Pedalına Hızlıca Bas (Throttle Step Sim)", "💥 Gaz Pedalına Hızlıca Bas (Throttle Step Sim)", "💥 Blast Throttle (Throttle Step Sim)");
            AddShared("Kısa Enjeksiyon Eklemesi (adder):", "Kısa Enjeksiyon Eklemesi (adder):", "Short Pulse Adder (adder):");
            AddShared("Geçici Yakıt Havuzu (acc):", "Geçici Yakıt Havuzu (acc):", "Transient Fuel Pool (acc):");
            AddShared("Nihai Enjeksiyon Süresi (PW):", "Nihai Enjeksiyon Süresi (PW):", "Final Pulse Width (PW):");
            AddShared("Enjektör Görev Döngüsü (Duty):", "Enjektör Görev Döngüsü (Duty):", "Injector Duty Cycle (Duty):");
            AddShared("✅ Görev Döngüsü Güvenli Limit Aralığında", "✅ Görev Döngüsü Güvenli Limit Aralığında", "✅ Duty Cycle Within Safe Limit Range");
            AddShared("alarm_injector_saturation", "🚨 KRİTİK: ENJEKTÖR DOYUMA ULAŞTI (%{0})!", "🚨 CRITICAL: INJECTOR SATURATED (%{0})!");

            // Sub Tab pages in Advanced Ignition
            AddShared("⚡ Çalıştırma & Silindir Düzeltmeleri", "⚡ Çalıştırma & Silindir Düzeltmeleri", "⚡ Cranking & Cylinder Offsets");
            AddShared("🔌 Sensör Kalibrasyon Eğrisi", "🔌 Sensör Kalibrasyon Eğrisi", "🔌 Sensor Calibration Curve");
            AddShared("📡 CAN Bus Kod Çözücü", "📡 CAN Bus Kod Çözücü", "📡 CAN Bus Decoder");
            AddShared("🧠 MBT Avans Önerici", "🧠 MBT Avans Önerici", "🧠 MBT Advance Advisor");
            AddShared("🔑 Çalıştırma Anı Avans Haritası", "🔑 Çalıştırma Anı Avans Haritası", "🔑 Cranking Ignition Map");
            AddShared("🔥 Bireysel Silindir Avans Düzeltmeleri", "🔥 Bireysel Silindir Avans Düzeltmeleri", "🔥 Individual Cylinder Ignition Trims");
            AddShared("Silindir 1:", "Silindir 1:", "Cylinder 1:");
            AddShared("Silindir 2:", "Silindir 2:", "Cylinder 2:");
            AddShared("Silindir 3:", "Silindir 3:", "Cylinder 3:");
            AddShared("Silindir 4:", "Silindir 4:", "Cylinder 4:");
            AddShared("Sensör Tipi Kalibrasyon Eğrisi Seçin:", "Sensör Tipi Kalibrasyon Eğrisi Seçin:", "Select Sensor Type Calibration Curve:");
            AddShared("🔌 Sinyal Linearizasyon Simülasyonu", "🔌 Sinyal Linearizasyon Simülasyonu", "🔌 Signal Linearization Simulation");
            AddShared("Analog Voltaj Girişi (0.0V - 5.0V):", "Analog Voltaj Girişi (0.0V - 5.0V):", "Analog Voltage Input (0.0V - 5.0V):");
            AddShared("Okunan Fiziksel Değer:", "Okunan Fiziksel Değer:", "Read Physical Value:");
            AddShared("📡 CAN Bus Çerçeve Çözümleme Tanımları", "📡 CAN Bus Çerçeve Çözümleme Tanımları", "📡 CAN Bus Frame Decode Definitions");
            AddShared("Frame ID (HEX):", "Frame ID (HEX):", "Frame ID (HEX):");
            AddShared("Başlangıç Biti (Start Bit):", "Başlangıç Biti (Start Bit):", "Start Bit:");
            AddShared("Bit Uzunluğu (Bit Len):", "Bit Uzunluğu (Bit Len):", "Bit Length:");
            AddShared("Çarpan Katsayı (Scale):", "Çarpan Katsayı (Scale):", "Scale Factor:");
            AddShared("Kayma Katsayı (Offset):", "Kayma Katsayı (Offset):", "Offset:");
            AddShared("Is Motorla Format (Big Endian)", "Is Motorla Format (Big Endian)", "Is Motorola Format (Big Endian)");
            AddShared("📡 CAN Mesaj Paketi Canlı Simülasyonu", "📡 CAN Mesaj Paketi Canlı Simülasyonu", "📡 CAN Message Packet Live Simulation");
            AddShared("Simüle Edilen 8-Byte Çerçeve Mesaj (Hex):", "Simüle Edilen 8-Byte Çerçeve Mesaj (Hex):", "Simulated 8-Byte Frame Message (Hex):");
            AddShared("Çözümlenen Sensör Çıktısı (EGT):", "Çözümlenen Sensör Çıktısı (EGT):", "Decoded Sensor Output (EGT):");
            AddShared("🧠 MBT Ateşleme Simülasyon Girdileri", "🧠 MBT Ateşleme Simülasyon Girdileri", "🧠 MBT Ignition Simulation Inputs");
            AddShared("Emme Manifold Yükü (kPa):", "Emme Manifold Yükü (kPa):", "Intake Manifold Load (kPa):");
            AddShared("Yakıt Oktan Oranı (RON):", "Yakıt Oktan Oranı (RON):", "Fuel Octane Rating (RON):");
            AddShared("Mevcut Avans Değeri (°):", "Mevcut Avans Değeri (°):", "Current Advance Value (°):");
            AddShared("🧠 Ateşleme Optimizasyon Kararı", "🧠 Ateşleme Optimizasyon Kararı", "🧠 Ignition Optimization Decision");
            AddShared("Modellenen Teorik MBT Avansı:", "Modellenen Teorik MBT Avansı:", "Modeled Theoretical MBT Advance:");
            AddShared("Sapma (Current - MBT):", "Sapma (Current - MBT):", "Deviation (Current - MBT):");
            AddShared("🔧 Avans Düzeltmesini Haritada Otomatik Ayarla", "🔧 Avans Düzeltmesini Haritada Otomatik Ayarla", "🔧 Auto Adjust Advance Trim on Map");

            // MetadataControl
            AddShared("📝 PROJE VE MOTOR META VERİLERİ", "📝 PROJE VE MOTOR META VERİLERİ", "📝 PROJECT AND ENGINE METADATA");
            AddShared("ECU Seri Numarası:", "ECU Seri Numarası:", "ECU Serial Number:");
            AddShared("Hardware Revizyonu:", "Hardware Revizyonu:", "Hardware Revision:");
            AddShared("Şasi Numarası (VIN):", "Şasi Numarası (VIN):", "Chassis Number (VIN):");
            AddShared("Kasa Kodu (Örn: EG6, EK4):", "Kasa Kodu (Örn: EG6, EK4):", "Chassis Code (e.g. EG6, EK4):");
            AddShared("Sıkıştırma Oranı (:1):", "Sıkıştırma Oranı (:1):", "Compression Ratio (:1):");
            AddShared("Eksantrik Profili:", "Eksantrik Profili:", "Camshaft Profile:");
            AddShared("Şanzıman Tipi:", "Şanzıman Tipi:", "Gearbox Type:");
            AddShared("İndüksiyon Türü:", "İndüksiyon Türü:", "Induction Type:");
            AddShared("💾 Değişiklikleri Kaydet", "💾 Değişiklikleri Kaydet", "💾 Save Changes");
            AddShared("⚡ Canlı Analiz", "⚡ Canlı Analiz", "⚡ Live Analysis");
            AddShared("🔌 OBD1 ECU Pinout", "🔌 OBD1 ECU Pinout", "🔌 OBD1 ECU Pinout");
            AddShared("Ara:", "Ara:", "Search:");
            AddShared("Soket:", "Soket:", "Connector:");

            // DiagnosticsControl
            AddShared("📶 Protokol & Donanım Arayüzleri", "📶 Protokol & Donanım Arayüzleri", "📶 Protocol & Hardware Interfaces");
            AddShared("📷 Freeze Frame Günlükleri", "📷 Freeze Frame Günlükleri", "📷 Freeze Frame Logs");
            AddShared("📝 Standartlar & A2L Export", "📝 Standartlar & A2L Export", "📝 Calibration Standards & A2L Export");
            AddShared("📡 Dönüştürücü & Protokol Arayüzü", "📡 Dönüştürücü & Protokol Arayüzü", "📡 Converter & Protocol Interface");
            AddShared("WiFi IP Address:", "WiFi IP Address:", "WiFi IP Address:");
            AddShared("WiFi Port:", "WiFi Port:", "WiFi Port:");
            AddShared("⚡ ECU Diagnostic Self-Test Başlat", "⚡ ECU Diagnostic Self-Test Başlat", "⚡ Start ECU Diagnostic Self-Test");
            AddShared("🖥️ Canlı İletişim & Hata Tanı Konsolu", "🖥️ Canlı İletişim & Hata Tanı Konsolu", "🖥️ Live Communication & Diagnostics Console");
            AddShared("📷 Hata Kodu Dondurulmuş Veri Çerçeveleri (Freeze Frames)", "📷 Hata Kodu Dondurulmuş Veri Çerçeveleri (Freeze Frames)", "📷 Freeze Frame Diagnostic Data Logs");
            AddShared("Arıza Kodu Seç:", "Arıza Kodu Seç:", "Select Trouble Code:");
            AddShared("💥 Hata Tetikle (Freeze Frame)", "💥 Hata Tetikle (Freeze Frame)", "💥 Trigger Fault (Freeze Frame)");
            AddShared("📝 ASAM MCD-2 MC (A2L) Kalibrasyon Standart Tanımları", "📝 ASAM MCD-2 MC (A2L) Kalibrasyon Standart Tanımları", "📝 ASAM MCD-2 MC (A2L) Calibration Standards");
            AddShared("💾 A2L Harita Dosyası (.a2l) İhraç Et", "💾 A2L Harita Dosyası (.a2l) İhraç Et", "💾 Export A2L Map File (.a2l)");

            // DynoLogsControl & Versioning
            AddShared("📊 Virtual Dyno & Güç Analizörü", "📊 Virtual Dyno & Güç Analizörü", "📊 Virtual Dyno & Power Analyzer");
            AddShared("⏱️ Pist Sürüş & Performans", "⏱️ Pist Sürüş & Performans", "⏱️ Track Stats & Performance");
            AddShared("🌿 Versiyon & RAM Watchdog", "🌿 Versiyon & RAM Watchdog", "🌿 Versioning & RAM Watchdog");
            AddShared("Lastik Çapı (İnç):", "Lastik Çapı (İnç):", "Tyre Diameter (Inches):");
            AddShared("Şanzıman Vites Oranı:", "Şanzıman Vites Oranı:", "Gearbox Ratio:");
            AddShared("Ayna Mahruti Oranı:", "Ayna Mahruti Oranı:", "Final Drive Ratio:");
            AddShared("🚀 0 - 100 km/h Hızlanma:", "🚀 0 - 100 km/h Hızlanma:", "🚀 0 - 100 km/h Acceleration:");
            AddShared("✈️ 100 - 200 km/h Hızlanma:", "✈️ 100 - 200 km/h Hızlanma:", "✈️ 100 - 200 km/h Acceleration:");
            AddShared("🔌 Vites Geçiş Yavaşlaması:", "🔌 Vites Geçiş Yavaşlaması:", "🔌 Gear Shift Delay:");
            AddShared("Azami Krank Gücü:", "Azami Krank Gücü:", "Max Crank Power:");
            AddShared("Azami Krank Torku:", "Azami Krank Torku:", "Max Crank Torque:");

            // Grids & ListViews Columns
            AddShared("Zaman", "Zaman", "Time");
            AddShared("Tip", "Tip", "Type");
            AddShared("Harita", "Harita", "Map");
            AddShared("Hücre [R, C]", "Hücre [R, C]", "Cell [R, C]");
            AddShared("Sapma", "Sapma", "Dev");
            AddShared("Düzeltme", "Düzeltme", "Corr");
            AddShared("Güven Skoru", "Güven Skoru", "Confidence");
            AddShared("Durum", "Durum", "Status");
            AddShared("Yük (kPa)", "Yük (kPa)", "Load (kPa)");
            AddShared("Hedef AFR", "Hedef AFR", "Target AFR");
            AddShared("Ölçülen AFR", "Ölçülen AFR", "Measured AFR");
            AddShared("Öneri", "Öneri", "Suggestion");
            AddShared("Düzeltme %", "Düzeltme %", "Correction %");
            AddShared("Arıza Kodu", "Arıza Kodu", "Fault Code");
            AddShared("Tetiklenme Saati", "Tetiklenme Saati", "Trigger Time");
            AddShared("Su Sıcaklığı (°C)", "Su Sıcaklığı (°C)", "Coolant Temp (°C)");
            AddShared("Intake Sıcaklığı (°C)", "Intake Sıcaklığı (°C)", "Intake Temp (°C)");
            AddShared("Hız (km/h)", "Hız (km/h)", "Speed (km/h)");
            AddShared("Sembol", "Sembol", "Symbol");
            AddShared("Sinyal Türü", "Sinyal Türü", "Signal Type");
            AddShared("Kablo Rengi", "Kablo Rengi", "Wire Color");
            AddShared("Devir (RPM)", "Devir (RPM)", "Engine RPM");
            AddShared("WHP (Teker)", "WHP (Teker)", "WHP (Wheel)");
            AddShared("Değişken", "Değişken", "Variable");
            AddShared("Canlı Değer", "Canlı Değer", "Live Value");

            // Dynamic runtime status strings (set in timer ticks, cannot use Tag mechanism)
            AddShared("duty_cycle_safe", "✅ Görev Döngüsü Güvenli Limit Aralığında", "✅ Duty Cycle Within Safe Limit Range");
            AddShared("vtec_active", "🔥 AKTİF VTEC (12V)", "🔥 VTEC ACTIVE (12V)");
            AddShared("vtec_inactive", "⚡ PASİF (VTEC LOCK)", "⚡ INACTIVE (VTEC LOCK)");
            AddShared("wg_system_safe", "✅ Wastegate Sistemi Güvenli Aralıkta Çalışıyor", "✅ Wastegate System Within Safe Range");
            AddShared("fan_relay_on", "🔥 ETKİN (Röle ON)", "🔥 ACTIVE (Relay ON)");
            AddShared("fan_relay_off", "PASİF (Röle OFF)", "INACTIVE (Relay OFF)");

            // AdvancedFuel simulator labels (static init label)
            AddShared("fuel_safe_status_init", "✅ Görev Döngüsü Güvenli Limit Aralığında", "✅ Duty Cycle Within Safe Limit Range");

            // EngineProtection safety status labels
            AddShared("ep_system_safe", "✅ SYSTEM SAFE", "✅ SYSTEM SAFE");
            AddShared("ep_fuel_cut", "🚨 FUEL CUT / ACİL DURUM!", "🚨 FUEL CUT / EMERGENCY ALERT!");
            AddShared("ep_limp_mode", "⚠️ MOTOR LİMP MODDA", "⚠️ ENGINE LIMP MODE ACTIVE");
            AddShared("ep_power_reduction", "⚠️ GÜÇ AZALTILIYOR", "⚠️ POWER REDUCTION ACTIVE");

            // MBT Optimizer deviation labels
            AddShared("mbt_above", "{0:F1}° (MBT Üzeri)", "+{0:F1}° (Above MBT)");
            AddShared("mbt_retarded", "{0:F1}° (Gecikmeli)", "{0:F1}° (Retarded)");

            // DynoLogs performance labels
            AddShared("dyno_max_power_fmt", "Azami Krank Gücü: {0} HP @ {1} RPM\nAzami Krank Torku: {2} Nm", "Peak Crank Power: {0} HP @ {1} RPM\nPeak Crank Torque: {2} Nm");
            AddShared("dyno_time_seconds", "{0} saniye", "{0} seconds");
            AddShared("dyno_shift_ms", "{0} ms (Clutch drop delay)", "{0} ms (Clutch drop delay)");

            // VtecBoost / Alarm
            AddShared("wg_failure_alarm_prefix", "🚨 ŞARJ ALARMI:", "🚨 WG ALARM:");

            // Diagnostics console messages (runtime)
            AddShared("diag_protocol_changed", "[SYSTEM] İletişim protokolü değiştirildi: ", "[SYSTEM] Protocol changed: ");
            AddShared("diag_selftest_start", "[SYSTEM] Cihaz öz-teşhis taraması başlatılıyor...", "[SYSTEM] Starting device self-test scan...");
            AddShared("diag_a2l_saved", "A2L dosyası başarıyla kaydedildi!", "A2L file saved successfully!");
            AddShared("diag_a2l_saved_title", "Bilgi", "Info");
            AddShared("diag_can_error", "Hata (Geçersiz veri)", "Error (Invalid data)");

            // MBT apply button message
            AddShared("mbt_apply_msg_fmt", "Zamanlama Başarıyla Kararlaştırıldı: {0:F1}° avans aktif tabloya referans atandı ve patch edildi.", "Timing locked: {0:F1}° advance applied to active map.");
            AddShared("mbt_apply_title", "Flaş Avans Düzeltmesi", "Flash Timing Correction");

            // ─── ReverseControl (Analysis & Decompiler tab) ───────────────
            AddShared("rc_scan_btn", "🔍 ROM'u Analiz Et & Tara", "🔍 Scan & Analyze ROM");
            AddShared("rc_filter_lbl", "Filtrele:", "Filter:");
            AddShared("rc_col_maptype", "Harita Tipi", "Map Type");
            AddShared("rc_col_dims", "Boyutlar", "Dimensions");
            AddShared("rc_col_confidence", "Güvenilirlik", "Confidence");
            AddShared("rc_col_desc", "Açıklama", "Description");
            AddShared("rc_axes_title", "Eksen Analiz Motoru", "Axis Analysis Engine");
            AddShared("rc_rpm_axis_init", "RPM Ekseni: Seçilmedi", "RPM Axis: Not selected");
            AddShared("rc_load_axis_init", "Load Ekseni: Seçilmedi", "Load Axis: Not selected");
            AddShared("rc_rpm_axis_unsearched", "RPM Ekseni: Aranmadı", "RPM Axis: Not searched");
            AddShared("rc_load_axis_unsearched", "Load Ekseni: Aranmadı", "Load Axis: Not searched");
            AddShared("rc_rpm_axis_notfound", "RPM Ekseni: Bulunamadı", "RPM Axis: Not found");
            AddShared("rc_load_axis_notfound", "Load Ekseni: Bulunamadı", "Load Axis: Not found");
            AddShared("rc_axis_scan_btn", "Eksen Taraması Yap", "Scan Axes");
            AddShared("rc_decompiler_title", "Decompiler & Register Trace Akışı", "Decompiler & Register Trace");
            AddShared("rc_routine_lbl", "Rutin:", "Routine:");
            AddShared("rc_routine_vtec", "VTEC Yönetimi", "VTEC Management");
            AddShared("rc_routine_revcut", "Devir Kesici", "Rev Limiter");
            AddShared("rc_routine_checksum", "Checksum Kontrolü", "Checksum Verification");
            AddShared("rc_address_lbl", "Adres (Hex):", "Address (Hex):");
            AddShared("rc_adopt_btn", "📥 Seçilen Haritayı ECU Profiline Entegre Et", "📥 Integrate Selected Map to ECU Profile");
            AddShared("rc_no_rom_msg", "Öncelikle bir ROM dosyası yüklemelisiniz!", "Please load a ROM file first!");
            AddShared("rc_no_rom_title", "Hata", "Error");
            AddShared("rc_scan_done", "\n\nHarita taraması tamamlandı! Soldaki listeden incelemek istediğiniz aday haritayı seçin.", "\n\nMap scan complete! Select a candidate map from the list on the left to inspect.");
            AddShared("rc_axis_results_header", "=== EKSEN HARİTALAMA SONUÇLARI ===", "=== AXIS MAPPING RESULTS ===");
            AddShared("rc_axis_notfound_msg", "Monoton olarak artış gösteren uygun eksen adresleri tespit edilemedi.", "No monotonically increasing axis addresses could be detected.");
            AddShared("rc_axis_notfound_title", "Bilgi", "Info");
            AddShared("rc_addr_invalid_msg", "Adres geçersiz! Lütfen 16'lık (Hex) formatta girin (örn: 1FC0).", "Invalid address! Please enter in hex format (e.g. 1FC0).");
            AddShared("rc_addr_invalid_title", "Hata", "Error");
            AddShared("rc_adopt_already_msg", "Bu adresteki harita zaten ECU profiline eklenmiş durumda!", "A map at this address is already in the ECU profile!");
            AddShared("rc_adopt_already_title", "Bilgi", "Info");
            AddShared("rc_adopt_success_title", "Profil Entegrasyonu Başarılı", "Profile Integration Successful");

            // ─── MainForm BuildPatchCenter ────────────────────────────────
            AddShared("pc_available_patches", "Kullanılabilir Yamalar", "Available Patches");
            AddShared("pc_patch_details", "Yama Detayları ve Önizleme", "Patch Details & Preview");
            AddShared("pc_apply_patch_btn", "Yamayı Uygula", "Apply Patch");
            AddShared("pc_rollback_patch_btn", "Geri Al (Rollback)", "Rollback");
            AddShared("pc_audit_log_lbl", "Yama Log Kayıtları", "Patch Audit Log");
            AddShared("pc_col_time", "Zaman", "Time");
            AddShared("pc_col_patch_id", "Yama ID", "Patch ID");
            AddShared("pc_col_result", "Sonuç", "Result");
            AddShared("pc_no_patches", "Bu ECU profili için kullanılabilir yama bulunamadı.", "No patches available for this ECU profile.");

            // ─── MainForm BuildCalibrationWizards ────────────────────────
            AddShared("wiz_inj_header", "🧪 Enjektör Ölçekleme Sihirbazı", "🧪 Injector Scaling Wizard");
            AddShared("wiz_old_inj", "Eski Enjektör Boyutu:", "Old Injector Size:");
            AddShared("wiz_new_inj", "Yeni Enjektör Boyutu:", "New Injector Size:");
            AddShared("wiz_scale_inj_btn", "Enjektörleri Ölçekle", "Scale Injectors");
            AddShared("wiz_map_header", "🔌 MAP Sensörü Kalibrasyon Sihirbazı", "🔌 MAP Sensor Calibration Wizard");
            AddShared("wiz_new_map_lbl", "Yeni MAP Sensörü Seçin:", "Select New MAP Sensor:");
            AddShared("wiz_calibrate_map_btn", "Yük Eksenini Kalibre Et", "Calibrate Load Axis");
            AddShared("wiz_inj_ok_msg", "Enjektör ölçekleme başarıyla uygulandı!\nÖlçek oranı: {0:F3}\nYakıt haritası güncellendi.", "Injector scaling applied!\nScale ratio: {0:F3}\nFuel map updated.");
            AddShared("wiz_inj_ok_title", "Başarılı", "Success");
            AddShared("wiz_map_ok_msg", "MAP sensörü başarıyla kalibre edildi!\nYeni Yük ekseni (kPa) 20 - {0} aralığına ölçeklendi.", "MAP sensor calibrated!\nNew load axis (kPa) scaled 20 - {0}.");
            AddShared("wiz_map_ok_title", "Başarılı", "Success");

            // ─── MainForm BuildVtecPanel ──────────────────────────────────
            AddShared("vtec_panel_load_lbl", "Yük Eşiği:", "Load Threshold:");
            AddShared("vtec_panel_speed_lbl", "Hız Sınırı:", "Speed Limit:");

            // ─── BuildAdvancedTuningControls / Launch Control ────────────
            AddShared("adv_launch_control", "Launch Control (2-Step) Aktif", "Launch Control (2-Step) Active");
            AddShared("adv_limit_rpm_lbl", "Sınır Devri:", "Limit RPM:");
            AddShared("adv_speed_thresh_lbl", "Hız Eşiği:", "Speed Threshold:");
            AddShared("adv_dtc_header", "🔧 DTC (Arıza Işığı) Devre Dışı Bırakma", "🔧 DTC (CEL) Disable");
            AddShared("adv_bypass_knock", "Vuruntu Sensörünü Bypass Et (Knock Sensor CEL 23)", "Bypass Knock Sensor (CEL 23)");
            AddShared("adv_bypass_vtec_sw", "VTEC Yağ Basıncı Müşürünü Bypass Et (VTEC Switch CEL 22)", "Bypass VTEC Oil Pressure Switch (CEL 22)");
            AddShared("adv_bypass_o2_heater", "Oksijen Sensörü Isıtıcısını Bypass Et (O2 Heater CEL 41)", "Bypass O2 Sensor Heater (CEL 41)");
            AddShared("adv_bypass_eld", "ELD - Elektriksel Yük Dedektörünü Bypass Et (ELD CEL 20)", "Bypass ELD Electrical Load Detector (CEL 20)");

            // ─── Ignition grid / map loading placeholder ──────────────────
            AddShared("map_waiting", "Harita verisi bekleniyor...", "Waiting for map data...");

            // ─── Direct Turkish Strings to English Translations ───────────
            AddShared("Harita verisi bekleniyor...", "Harita verisi bekleniyor...", "Waiting for map data...");
            AddShared("Launch Control (2-Step) Aktif", "Launch Control (2-Step) Aktif", "Launch Control (2-Step) Active");
            AddShared("Sınır Devri:", "Sınır Devri:", "Limit RPM:");
            AddShared("Hız Eşiği:", "Hız Eşiği:", "Speed Threshold:");
            AddShared("🔧 DTC (Arıza Işığı) Devre Dışı Bırakma", "🔧 DTC (Arıza Işığı) Devre Dışı Bırakma", "🔧 DTC (CEL) Disable");
            AddShared("Vuruntu Sensörünü Bypass Et (Knock Sensor CEL 23)", "Vuruntu Sensörünü Bypass Et (Knock Sensor CEL 23)", "Bypass Knock Sensor (CEL 23)");
            AddShared("VTEC Yağ Basınç Müşürünü Bypass Et (VTEC Switch CEL 22)", "VTEC Yağ Basınç Müşürünü Bypass Et (VTEC Switch CEL 22)", "Bypass VTEC Oil Pressure Switch (CEL 22)");
            AddShared("VTEC Yağ Basıncı Müşürünü Bypass Et (VTEC Switch CEL 22)", "VTEC Yağ Basıncı Müşürünü Bypass Et (VTEC Switch CEL 22)", "Bypass VTEC Oil Pressure Switch (CEL 22)");
            AddShared("Oksijen Sensörü Isıtıcısını Bypass Et (O2 Heater CEL 41)", "Oksijen Sensörü Isıtıcısını Bypass Et (O2 Heater CEL 41)", "Bypass O2 Sensor Heater (CEL 41)");
            AddShared("ELD - Elektriksel Yük Dedektörünü Bypass Et (ELD CEL 20)", "ELD - Elektriksel Yük Dedektörünü Bypass Et (ELD CEL 20)", "Bypass ELD Electrical Load Detector (CEL 20)");

            AddShared("🧪 Enjektör Ölçekleme Sihirbazı", "🧪 Enjektör Ölçekleme Sihirbazı", "🧪 Injector Scaling Wizard");
            AddShared("Eski Enjektör Boyutu:", "Eski Enjektör Boyutu:", "Old Injector Size:");
            AddShared("Yeni Enjektör Boyutu:", "Yeni Enjektör Boyutu:", "New Injector Size:");
            AddShared("Enjektörleri Ölçekle", "Enjektörleri Ölçekle", "Scale Injectors");
            AddShared("🔌 MAP Sensörü Kalibrasyon Sihirbazı", "🔌 MAP Sensörü Kalibrasyon Sihirbazı", "🔌 MAP Sensor Calibration Wizard");
            AddShared("Yeni MAP Sensörü Seçin:", "Yeni MAP Sensörü Seçin:", "Select New MAP Sensor:");
            AddShared("Yük Eksenini Kalibre Et", "Yük Eksenini Kalibre Et", "Calibrate Load Axis");

            AddShared("Stok 1-Bar (20 - 105 kPa)", "Stok 1-Bar (20 - 105 kPa)", "Stock 1-Bar (20 - 105 kPa)");
            AddShared("Motorola 2.5-Bar (20 - 250 kPa)", "Motorola 2.5-Bar (20 - 250 kPa)", "Motorola 2.5-Bar (20 - 250 kPa)");
            AddShared("Omnipower 3-Bar (20 - 300 kPa)", "Omnipower 3-Bar (20 - 300 kPa)", "Omnipower 3-Bar (20 - 300 kPa)");
            AddShared("Omnipower 4-Bar (20 - 400 kPa)", "Omnipower 4-Bar (20 - 400 kPa)", "Omnipower 4-Bar (20 - 400 kPa)");

            AddShared("Kullanılabilir Yamalar", "Kullanılabilir Yamalar", "Available Patches");
            AddShared("Yama Detayları ve Önizleme", "Yama Detayları ve Önizleme", "Patch Details & Preview");
            AddShared("Yamayı Uygula", "Yamayı Uygula", "Apply Patch");
            AddShared("Geri Al (Rollback)", "Geri Al (Rollback)", "Rollback");
            AddShared("Yama Log Kayıtları", "Yama Log Kayıtları", "Patch Audit Log");
            AddShared("Zaman", "Zaman", "Time");
            AddShared("Yama ID", "Yama ID", "Patch ID");
            AddShared("Sonuç", "Sonuç", "Result");

            AddShared("⚡ VTEC RPM:", "⚡ VTEC RPM:", "⚡ VTEC RPM:");
            AddShared("Yük Eşiği:", "Yük Eşiği:", "Load Threshold:");
            AddShared("Rev Limit:", "Rev Limit:", "Rev Limit:");
            AddShared("Hız Sınırı:", "Hız Sınırı:", "Speed Limit:");
            AddShared("Inj. Dead:", "Inj. Dead:", "Inj. Dead:");

            AddShared("🚗  Araç Seç", "🚗  Araç Seç", "🚗  Select Vehicle");
            AddShared("Araç Seç", "Araç Seç", "Select Vehicle");

            AddShared("Harita Tipi", "Harita Tipi", "Map Type");
            AddShared("Boyutlar", "Boyutlar", "Dimensions");
            AddShared("Güvenilirlik", "Güvenilirlik", "Confidence");
            AddShared("Açıklama", "Açıklama", "Description");

            AddShared("RPM Ekseni: Seçilmedi", "RPM Ekseni: Seçilmedi", "RPM Axis: Not Selected");
            AddShared("Load Ekseni: Seçilmedi", "Load Ekseni: Seçilmedi", "Load Axis: Not Selected");
            AddShared("RPM Ekseni: Aranmadı", "RPM Ekseni: Aranmadı", "RPM Axis: Not Searched");
            AddShared("Load Ekseni: Aranmadı", "Load Ekseni: Aranmadı", "Load Axis: Not Searched");
            AddShared("RPM Ekseni: Bulunamadı", "RPM Ekseni: Bulunamadı", "RPM Axis: Not Found");
            AddShared("Load Ekseni: Bulunamadı", "Load Ekseni: Bulunamadı", "Load Axis: Not Found");
            AddShared("RPM Ekseni", "RPM Ekseni", "RPM Axis");
            AddShared("Load Ekseni", "Load Ekseni", "Load Axis");

            AddShared("VTEC Yönetimi", "VTEC Yönetimi", "VTEC Management");
            AddShared("Devir Kesici", "Devir Kesici", "Rev Limiter");
            AddShared("Checksum Kontrolü", "Checksum Kontrolü", "Checksum Verification");

            // ─── ECU Profile CasaTags ─────────────────────────────────────
            AddShared("EG kasa 1.5L VTEC-E (Yüksek Verimli)", "EG kasa 1.5L VTEC-E (Yüksek Verimli)", "EG chassis 1.5L VTEC-E (High Efficiency)");
            AddShared("EG kasa 1.5L Non-VTEC", "EG kasa 1.5L Non-VTEC", "EG chassis 1.5L Non-VTEC");
            AddShared("EG/EK kasa 1.6L SOHC VTEC", "EG/EK kasa 1.6L SOHC VTEC", "EG/EK chassis 1.6L SOHC VTEC");
            AddShared("DC2 Integra GS-R 1.7L DOHC VTEC (1992-93)", "DC2 Integra GS-R 1.7L DOHC VTEC (1992-93)", "DC2 Integra GS-R 1.7L DOHC VTEC (1992-93)");
            AddShared("DC2 Integra GSR 1.8L DOHC VTEC + IAB (1994-95)", "DC2 Integra GSR 1.8L DOHC VTEC + IAB (1994-95)", "DC2 Integra GSR 1.8L DOHC VTEC + IAB (1994-95)");
            AddShared("DC2 Integra LS/GS 1.8L DOHC Non-VTEC", "DC2 Integra LS/GS 1.8L DOHC Non-VTEC", "DC2 Integra LS/GS 1.8L DOHC Non-VTEC");
            AddShared("BB Prelude 2.2L DOHC VTEC (1993-95)", "BB Prelude 2.2L DOHC VTEC (1993-95)", "BB Prelude 2.2L DOHC VTEC (1993-95)");

            // ─── 3D Parts & Notes ──────────────────────────────────────────
            AddShared("3D MODEL SEÇİMİ", "3D MODEL SEÇİMİ", "3D MODEL SELECTION");
            AddShared("🧠  ECU Ana Kartı", "🧠  ECU Ana Kartı", "🧠  ECU Motherboard");
            AddShared("🧠  ECU (Geri)", "🧠  ECU (Geri)", "🧠  ECU (Back)");
            AddShared("💾  EEPROM Çip", "💾  EEPROM Çip", "💾  EEPROM Chip");
            AddShared("🔌  OBD1 Konnektör", "🔌  OBD1 Konnektör", "🔌  OBD1 Connector");
            AddShared("🌡️  MAP Sensörü", "🌡️  MAP Sensörü", "🌡️  MAP Sensor");
            AddShared("⛽  Enjektör", "⛽  Enjektör", "⛽  Injector");
            AddShared("⚙️  Distribütör", "⚙️  Distribütör", "⚙️  Distributor");
            AddShared("🔩  B16 FWD Motor", "🔩  B16 FWD Motor", "🔩  B16 FWD Engine");
            AddShared("📁  PROJE PARÇALARI", "📁  PROJE PARÇALARI", "📁  PROJECT PARTS");
            AddShared("Sol: döndür  |  Sağ: kaydır  |  Tekerlek: zoom", "Sol: döndür  |  Sağ: kaydır  |  Tekerlek: zoom", "Left: rotate  |  Right: pan  |  Wheel: zoom");

            // 3D Parts list descriptions (rendered in GDI+)
            AddShared("ECU Ana Kartı (Honda P28)", "ECU Ana Kartı (Honda P28)", "ECU Motherboard (Honda P28)");
            AddShared("EEPROM Çip (28C256)", "EEPROM Çip (28C256)", "EEPROM Chip (28C256)");
            AddShared("OBD1 Konnektör (3-Plug)", "OBD1 Konnektör (3-Plug)", "OBD1 Connector (3-Plug)");
            AddShared("MAP Sensörü (1 bar)", "MAP Sensörü (1 bar)", "MAP Sensor (1 bar)");
            AddShared("Enjektör (240cc EV1)", "Enjektör (240cc EV1)", "Injector (240cc EV1)");
            AddShared("Distribütör (TDC Sensörlü)", "Distribütör (TDC Sensörlü)", "Distributor (with TDC)");
            AddShared("Motor (B16 FWD)", "Motor (B16 FWD)", "Engine (B16 FWD)");

            // 3D Parts Tech notes
            AddShared("16 MHz NEC V25 · 32KB · OBD1", "16 MHz NEC V25 · 32KB · OBD1", "16 MHz NEC V25 · 32KB · OBD1");
            AddShared("DIP-28 · 32KB · 5V · In-circuit", "DIP-28 · 32KB · 5V · In-circuit", "DIP-28 · 32KB · 5V · In-circuit");
            AddShared("3-plug A/B/C · Jumper hafıza", "3-plug A/B/C · Jumper hafıza", "3-plug A/B/C · Jumper memory");
            AddShared("0–200 kPa · 5V · Barometrik", "0–200 kPa · 5V · Barometrik", "0–200 kPa · 5V · Barometric");
            AddShared("EV1 · 12Ω · 240cc · 4/6 adet", "EV1 · 12Ω · 240cc · 4/6 adet", "EV1 · 12Ω · 240cc · 4/6 pcs");
            AddShared("TDC · CYP/CKP dahili · Bobin", "TDC · CYP/CKP dahili · Bobin", "TDC · CYP/CKP internal · Coil");
            AddShared("1.6L DOHC VTEC · B16A · FWD", "1.6L DOHC VTEC · B16A · FWD", "1.6L DOHC VTEC · B16A · FWD");

            // Project Parts (3D Part Tab)
            AddShared("🔧 PROJE PARÇALARI", "🔧 PROJE PARÇALARI", "🔧 PROJECT PARTS");
            AddShared("ECU", "ECU", "ECU");
            AddShared("Hondata S300, Neptune RTP, Crome Pro, HTS", "Hondata S300, Neptune RTP, Crome Pro, HTS", "Hondata S300, Neptune RTP, Crome Pro, HTS");
            AddShared("Emme", "Emme", "Intake");
            AddShared("Cold Air Intake, Skunk2 Pro, K&N filtre", "Cold Air Intake, Skunk2 Pro, K&N filtre", "Cold Air Intake, Skunk2 Pro, K&N filter");
            AddShared("Gaz Kelebeği", "Gaz Kelebeği", "Throttle Body");
            AddShared("B16/B18 62 mm throttle body", "B16/B18 62 mm throttle body", "B16/B18 62mm throttle body");
            AddShared("Emme Manifoldu", "Emme Manifoldu", "Intake Manifold");
            AddShared("D16Y8 veya Skunk2", "D16Y8 veya Skunk2", "D16Y8 or Skunk2");
            AddShared("Egzoz", "Egzoz", "Exhaust");
            AddShared("4-2-1 Header, 2.25\" düz hat", "4-2-1 Header, 2.25\" düz hat", "4-2-1 Header, 2.25\" straight pipe");
            AddShared("Egzantrik", "Egzantrik", "Camshaft");
            AddShared("Delta Cam, Bisimoto Stage 1", "Delta Cam, Bisimoto Stage 1", "Delta Cam, Bisimoto Stage 1");
            AddShared("Yakıt", "Yakıt", "Fuel");
            AddShared("Walbro 255, büyük enjektör", "Walbro 255, büyük enjektör", "Walbro 255, larger injectors");
            AddShared("Ateşleme", "Ateşleme", "Ignition");
            AddShared("NGK Iridium, MSD", "NGK Iridium, MSD", "NGK Iridium, MSD");
            AddShared("Volan", "Volan", "Flywheel");
            AddShared("Hafifletilmiş Volan", "Hafifletilmiş Volan", "Lightweight Flywheel");
            AddShared("Debriyaj", "Debriyaj", "Clutch");
            AddShared("Exedy Stage 1", "Exedy Stage 1", "Exedy Stage 1");
            AddShared("Süspansiyon", "Süspansiyon", "Suspension");
            AddShared("BC Racing, Tein, D2", "BC Racing, Tein, D2", "BC Racing, Tein, D2");
            AddShared("Fren", "Fren", "Brakes");
            AddShared("Integra DC2 veya Civic VTi disk", "Integra DC2 veya Civic VTi disk", "Integra DC2 or Civic VTi discs");
            AddShared("Turbo", "Turbo", "Turbo");
            AddShared("TD04, GT2554R, GT2860", "TD04, GT2554R, GT2860", "TD04, GT2554R, GT2860");

            // Telemetry Sensors & Units
            AddShared("🌡 ECT (Motor Sıcaklığı):", "🌡 ECT (Motor Sıcaklığı):", "🌡 ECT (Engine Temp):");
            AddShared("💨 IAT (Emme Havası Sıcaklığı):", "💨 IAT (Emme Havası Sıcaklığı):", "💨 IAT (Intake Air Temp):");

            // Diff View
            AddShared("Diff görünümü — önce bir ROM yükleyin", "Diff görünümü — önce bir ROM yükleyin", "Diff view — load a ROM first");
            AddShared("📋 STOCK", "📋 STOCK", "📋 STOCK");
            AddShared("⚡ DELTA (Δ)", "⚡ DELTA (Δ)", "⚡ DELTA (Δ)");
            AddShared("✏️ MODIFIED", "✏️ MODIFIED", "✏️ MODIFIED");
            AddShared("Değişen", "Değişen", "Changed");
            AddShared("Ort. Δ", "Ort. Δ", "Avg. Δ");
            AddShared("Maks. Δ", "Maks. Δ", "Max. Δ");
            AddShared("Stock vs Modified", "Stock vs Modified", "Stock vs Modified");

            // AutoTune
            AddShared("Önerilen ve Uygulanan Kararlar", "Önerilen ve Uygulanan Kararlar", "Recommended & Applied Decisions");
            AddShared("Canlı AutoTune Düzeltme Önerileri (Son 50 Öneri)", "Canlı AutoTune Düzeltme Önerileri (Son 50 Öneri)", "Live AutoTune Correction Suggestions (Last 50)");
            AddShared("Yük (kPa)", "Yük (kPa)", "Load (kPa)");
            AddShared("Hedef AFR", "Hedef AFR", "Target AFR");
            AddShared("Ölçülen AFR", "Ölçülen AFR", "Measured AFR");
            AddShared("Öneri", "Öneri", "Suggestion");
            AddShared("Düzeltme %", "Düzeltme %", "Correction %");
            AddShared("Bağlantı Durumu: Disconnected", "Bağlantı Durumu: Disconnected", "Connection Status: Disconnected");
            AddShared("Kuyruk Derinliği: 0 eleman", "Kuyruk Derinliği: 0 eleman", "Queue Depth: 0 elements");
            AddShared("Ortalama Gecikme: 0.0 ms", "Ortalama Gecikme: 0.0 ms", "Average Latency: 0.0 ms");
            AddShared("Hata / Yeniden Deneme: 0 / 0", "Hata / Yeniden Deneme: 0 / 0", "Errors / Retries: 0 / 0");
            AddShared("Düşen Yazmalar: 0", "Düşen Yazmalar: 0", "Dropped Writes: 0");
            AddShared("COM Port:", "COM Port:", "COM Port:");
            AddShared("DOSYA BULUNAMADI", "DOSYA BULUNAMADI", "FILE NOT FOUND");
            AddShared("HATA:", "HATA:", "ERROR:");
            AddShared("Disconnected", "Bağlantı Kesildi", "Disconnected");
            AddShared("Connecting", "Bağlanıyor", "Connecting");
            AddShared("Connected", "Bağlandı", "Connected");
            AddShared("Synchronizing", "Senkronize Ediliyor", "Synchronizing");
            AddShared("Paused", "Duraklatıldı", "Paused");
            AddShared("Faulted", "Hata Durumu", "Faulted");
            AddShared("eleman", "eleman", "elements");
            AddShared("Zaman", "Zaman", "Time");
            AddShared("Tip", "Tip", "Type");
            AddShared("Harita", "Harita", "Map");
            AddShared("Hücre [R, C]", "Hücre [R, C]", "Cell [R, C]");
            AddShared("Sapma", "Sapma", "Deviation");
            AddShared("Düzeltme", "Düzeltme", "Correction");
            AddShared("Güven Skoru", "Güven Skoru", "Confidence");
            AddShared("Durum", "Durum", "Status");
            AddShared("RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator", "RTP Real-Time Calibration & Emulator");
            AddShared("Real-Time Calibration Sync Etkin", "Real-Time Calibration Sync Etkin", "Real-Time Calibration Sync Active");
            AddShared("▶ Emulator Bağlan", "▶ Emulator Bağlan", "▶ Connect Emulator");
            AddShared("🔄 Tüm ROM'u Senkronize Et (Upload)", "🔄 Tüm ROM'u Senkronize Et (Upload)", "🔄 Sync Full ROM (Upload)");
            AddShared("⏹ Bağlantıyı Kes", "⏹ Bağlantıyı Kes", "⏹ Disconnect");

            // Advanced Fuel UI 
            AddShared("Kademe", "Kademe", "Index");
            AddShared("MAF Sensör (Volt)", "MAF Sensör (Volt)", "MAF Sensor (Volt)");
            AddShared("Hava Debisi (g/s)", "Hava Debisi (g/s)", "Air Flow (g/s)");
            AddShared("Hararet (°C ECT)", "Hararet (°C ECT)", "Coolant Temp (°C ECT)");
            AddShared("Sıcaklık Yakıt Çarpanı", "Sıcaklık Yakıt Çarpanı", "ECT Fuel Multiplier");
            AddShared("TPS %", "TPS %", "TPS (%)");
            AddShared("⚡ Canlı Enjektör & Düzeltme Simülatörü", "⚡ Canlı Enjektör & Düzeltme Simülatörü", "⚡ Live Injector & Correction Simulator");
            AddShared("Motor Devri (RPM):", "Motor Devri (RPM):", "Engine Speed (RPM):");
            AddShared("Taban Yakıt Süresi (ms):", "Taban Yakıt Süresi (ms):", "Base Fuel Pulse (ms):");
            AddShared("Motor Sıcaklık (°C ECT):", "Motor Sıcaklık (°C ECT):", "Coolant Temp (°C ECT):");
            AddShared("Yakıt Basıncı (psi - Aktif):", "Yakıt Basıncı (psi - Aktif):", "Fuel Pressure (psi - Active):");
            AddShared("Hedef Yakıt Basıncı (psi):", "Hedef Yakıt Basıncı (psi):", "Target Fuel Pressure (psi):");
            AddShared("Gaz Değişim Hızı (dTPS %/s):", "Gaz Değişim Hızı (dTPS %/s):", "Throttle Change Rate (dTPS %/s):");
            AddShared("Alpha-N Yakıt Modunu Kullan (TPS vs RPM)", "Alpha-N Yakıt Modunu Kullan (TPS vs RPM)", "Use Alpha-N Fuel Mode (TPS vs RPM)");
            AddShared("💥 Gaz Pedalına Hızlıca Bas (Throttle Step Sim)", "💥 Gaz Pedalına Hızlıca Bas (Throttle Step Sim)", "💥 Blast Throttle (Throttle Step Sim)");
            AddShared("Kısa Enjeksiyon Eklemesi (adder):", "Kısa Enjeksiyon Eklemesi (adder):", "Short Pulse Adder:");
            AddShared("Geçici Yakıt Havuzu (acc):", "Geçici Yakıt Havuzu (acc):", "Transient Fuel Pool (acc):");
            AddShared("Nihai Enjeksiyon Süresi (PW):", "Nihai Enjeksiyon Süresi (PW):", "Final Pulse Width (PW):");
            AddShared("Enjektör Görev Döngüsü (Duty):", "Enjektör Görev Döngüsü (Duty):", "Injector Duty Cycle (Duty):");

            // Advanced Ignition UI
            AddShared("⚡ Çalıştırma & Silindir Düzeltmeleri", "⚡ Çalıştırma & Silindir Düzeltmeleri", "⚡ Cranking & Cylinder Offsets");
            AddShared("🔌 Sensör Kalibrasyon Eğrisi", "🔌 Sensör Kalibrasyon Eğrisi", "🔌 Sensor Calibration Curve");
            AddShared("📡 CAN Bus Kod Çözücü", "📡 CAN Bus Kod Çözücü", "📡 CAN Bus Decoder");
            AddShared("🧠 MBT Avans Önerici", "🧠 MBT Avans Önerici", "🧠 MBT Advance Recommender");
            AddShared("🔑 Çalıştırma Anı Avans Haritası", "🔑 Çalıştırma Anı Avans Haritası", "🔑 Cranking Timing Map");
            AddShared("ECT Hararet (°C)", "ECT Hararet (°C)", "ECT Temp (°C)");
            AddShared("Ateşleme Avansı (°)", "Ateşleme Avansı (°)", "Ignition Advance (°)");
            AddShared("🔥 Bireysel Silindir Avans Düzeltmeleri", "🔥 Bireysel Silindir Avans Düzeltmeleri", "🔥 Individual Cylinder Advance Corrections");
            AddShared("Silindir 1:", "Silindir 1:", "Cylinder 1:");
            AddShared("Silindir 2:", "Silindir 2:", "Cylinder 2:");
            AddShared("Silindir 3:", "Silindir 3:", "Cylinder 3:");
            AddShared("Silindir 4:", "Silindir 4:", "Cylinder 4:");
            AddShared("Sensör Tipi Kalibrasyon Eğrisi Seçin:", "Sensör Tipi Kalibrasyon Eğrisi Seçin:", "Select Sensor Calibration Curve Preset:");
            AddShared("🔌 Sinyal Linearizasyon Simülasyonu", "🔌 Sinyal Linearizasyon Simülasyonu", "🔌 Signal Linearization Simulation");
            AddShared("Analog Voltaj Girişi (0.0V - 5.0V):", "Analog Voltaj Girişi (0.0V - 5.0V):", "Analog Voltage Input (0.0V - 5.0V):");
            AddShared("Okunan Fiziksel Değer:", "Okunan Fiziksel Değer:", "Physical Value Read:");
            AddShared("📡 CAN Bus Çerçeve Çözümleme Tanımları", "📡 CAN Bus Çerçeve Çözümleme Tanımları", "📡 CAN Bus Frame Parsing Definitions");
            AddShared("Frame ID (HEX):", "Frame ID (HEX):", "Frame ID (HEX):");
            AddShared("Başlangıç Biti (Start Bit):", "Başlangıç Biti (Start Bit):", "Start Bit:");
            AddShared("Bit Uzunluğu (Bit Len):", "Bit Uzunluğu (Bit Len):", "Bit Length:");
            AddShared("Çarpan Katsayı (Scale):", "Çarpan Katsayı (Scale):", "Scaling Factor:");
            AddShared("Kayma Katsayı (Offset):", "Kayma Katsayı (Offset):", "Offset:");
            AddShared("Is Motorla Format (Big Endian)", "Is Motorla Format (Big Endian)", "Is Motorola Format (Big Endian)");
            AddShared("📡 CAN Mesaj Paketi Canlı Simülasyonu", "📡 CAN Mesaj Paketi Canlı Simülasyonu", "📡 CAN Message Packet Live Simulation");
            AddShared("Simüle Edilen 8-Byte Çerçeve Mesaj (Hex):", "Simüle Edilen 8-Byte Çerçeve Mesaj (Hex):", "Simulated 8-Byte Frame Message (Hex):");
            AddShared("Çözümlenen Sensör Çıktısı (EGT):", "Çözümlenen Sensör Çıktısı (EGT):", "Decoded Sensor Output (EGT):");
            AddShared("🧠 MBT Ateşleme Simülasyon Girdileri", "🧠 MBT Ateşleme Simülasyon Girdileri", "🧠 MBT Ignition Simulation Inputs");
            AddShared("Emme Manifold Yükü (kPa):", "Emme Manifold Yükü (kPa):", "Intake Manifold Load (kPa):");
            AddShared("Yakıt Oktan Oranı (RON):", "Yakıt Oktan Oranı (RON):", "Fuel Octane Rating (RON):");
            AddShared("Mevcut Avans Değeri (°):", "Mevcut Avans Değeri (°):", "Current Advance Value (°):");
            AddShared("🧠 Ateşleme Optimizasyon Kararı", "🧠 Ateşleme Optimizasyon Kararı", "🧠 Ignition Optimization Decision");
            AddShared("Modellenen Teorik MBT Avansı:", "Modellenen Teorik MBT Avansı:", "Modeled Theoretical MBT Advance:");
            AddShared("Sapma (Current - MBT):", "Sapma (Current - MBT):", "Deviation (Current - MBT):");
            AddShared("ℹ️ OPTİMİZASYON: Avans MBT'nin Çok Gerisinde. Güç Kazanmak İçin Avansı Artırın.", "ℹ️ OPTİMİZASYON: Avans MBT'nin Çok Gerisinde. Güç Kazanmak İçin Avansı Artırın.", "ℹ️ OPTIMIZATION: Advance is far behind MBT. Increase advance to gain power.");
            AddShared("🔧 Avans Düzeltmesini Haritada Otomatik Ayarla", "🔧 Avans Düzeltmesini Haritada Otomatik Ayarla", "🔧 Auto-Adjust Advance Correction on Map");
            AddShared("Sensör Sinyali (Volt)", "Sensör Sinyali (Volt)", "Sensor Signal (Volt)");

            // VTEC Boost Control
            AddShared("🏁 VTEC Solenoid Limitleri", "🏁 VTEC Solenoid Limitleri", "🏁 VTEC Solenoid Limits");
            AddShared("📈 Target Boost (RPM vs Gear)", "📈 Target Boost (RPM vs Gear)", "📈 Target Boost (RPM vs Gear)");
            AddShared("🔌 Base WG Solenoid Duty", "🔌 Base WG Solenoid Duty", "🔌 Base WG Solenoid Duty");
            AddShared("🕹️ Dynamic Solenoid Simülatör", "🕹️ Dynamic Solenoid Simülatör", "🕹️ Dynamic Solenoid Simulator");
            AddShared("🏁 VTEC Geçiş Koşulları", "🏁 VTEC Geçiş Koşulları", "🏁 VTEC Transition Conditions");
            AddShared("VTEC Minimum Devir (RPM):", "VTEC Minimum Devir (RPM):", "VTEC Minimum RPM:");
            AddShared("VTEC Minimum Hız (km/h):", "VTEC Minimum Hız (km/h):", "VTEC Minimum Speed (km/h):");
            AddShared("VTEC Engellenen Vites Seçenekleri (Gear Lockout out):", "VTEC Engellenen Vites Seçenekleri (Gear Lockout out):", "VTEC Gear Lockout:");
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
            AddShared("🕹️ Sürüş Simülatör Girdileri", "🕹️ Sürüş Simülatör Girdileri", "🕹️ Driving Simulator Inputs");
            AddShared("Araç Hızı (km/h):", "Araç Hızı (km/h):", "Vehicle Speed (km/h):");
            AddShared("Aktif Vites (Gear):", "Aktif Vites (Gear):", "Active Gear:");
            AddShared("⚡ Scramble Boost Düğmesi (Geçici Avans / Boost)", "⚡ Scramble Boost Düğmesi (Geçici Avans / Boost)", "⚡ Scramble Boost Button (Temp Advance / Boost)");
            AddShared("⚠️ Kaçak / Wastegate Hortum Yırtılması Simülasyonu", "⚠️ Kaçak / Wastegate Hortum Yırtılması Simülasyonu", "⚠️ Boost Leak / Wastegate Hose Tear Simulation");
            AddShared("🕹️ Solenoid & PID Kontrol Çıktıları", "🕹️ Solenoid & PID Kontrol Çıktıları", "🕹️ Solenoid & PID Control Outputs");
            AddShared("Hedef Turbo Basıncı:", "Hedef Turbo Basıncı:", "Target Turbo Pressure:");
            AddShared("Aktif Turbo Basıncı:", "Aktif Turbo Basıncı:", "Active Turbo Pressure:");
            AddShared("Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:", "Wastegate Solenoid Duty:");
            AddShared("VTEC Valf Sinyali (Solenoid):", "VTEC Valf Sinyali (Solenoid):", "VTEC Valve Signal (Solenoid):");
            AddShared("Devir (RPM)", "Devir (RPM)", "Engine RPM");

            // Engine Protection Control
            AddShared("🚨 Limit & Emniyet Ayarları", "🚨 Limit & Emniyet Ayarları", "🚨 Limit & Safety Settings");
            AddShared("🌡️ Termal Düzeltmeler & IAT/EGT", "🌡️ Termal Düzeltmeler & IAT/EGT", "🌡️ Thermal Corrections & IAT/EGT");
            AddShared("🎮 Güvenlik Koruma Simülatörü", "🎮 Güvenlik Koruma Simülatörü", "🎮 Safety Protection Simulator");
            AddShared("🚨 Genel Güvenlik Limitleri", "🚨 Genel Güvenlik Limitleri", "🚨 General Safety Limits");
            AddShared("Max Yağ Sıcaklığı (°C):", "Max Yağ Sıcaklığı (°C):", "Max Oil Temp (°C):");
            AddShared("Min Yakıt Basıncı (Bar):", "Min Yakıt Basıncı (Bar):", "Min Fuel Pressure (Bar):");
            AddShared("Radyatör Fan Sıcaklığı (°C):", "Radyatör Fan Sıcaklığı (°C):", "Radiator Fan Target Temp (°C):");
            AddShared("Maksimum EGT Sınırı (°C):", "Maksimum EGT Sınırı (°C):", "Maximum EGT Limit (°C):");
            AddShared("📈 RPM vs Min Yağ Basıncı Sınır Eğrisi", "📈 RPM vs Min Yağ Basıncı Sınır Eğrisi", "📈 RPM vs Min Oil Pressure Curve");
            AddShared("Min Basınç (Bar)", "Min Basınç (Bar)", "Min Pressure (Bar)");
            AddShared("🌡️ Termal Yönetim & IAT Düzeltmeleri", "🌡️ Termal Yönetim & IAT Düzeltmeleri", "🌡️ Thermal Management & IAT Corrections");
            AddShared("IAT Heat Soak Eşiği (°C):", "IAT Heat Soak Eşiği (°C):", "IAT Heat Soak Threshold (°C):");
            AddShared("IAT Avans Kısma Derecesi (°):", "IAT Avans Kısma Derecesi (°):", "IAT Timing Retard (°):");
            AddShared("IAT Boost Kısma Derecesi (kPa):", "IAT Boost Kısma Derecesi (kPa):", "IAT Boost Limit Reduction (kPa):");
            AddShared("EGT Avans Geri Çekme (°):", "EGT Avans Geri Çekme (°):", "EGT Timing Pull (°):");
            AddShared("EGT Karışım Zenginleştirme (%):", "EGT Karışım Zenginleştirme (%):", "EGT Fuel Enrichment (%):");
            AddShared("Limp RPM Üst Devir Limiti:", "Limp RPM Üst Devir Limiti:", "Limp Mode RPM Limit:");
            AddShared("Rpm Devir (rpm):", "Rpm Devir (rpm):", "Engine Speed (rpm):");
            AddShared("Su Sıcaklığı ECT (°C):", "Su Sıcaklığı ECT (°C):", "Coolant Temp ECT (°C):");
            AddShared("Emme Sıcaklığı IAT (°C):", "Emme Sıcaklığı IAT (°C):", "Intake Air Temp IAT (°C):");
            AddShared("Yağ Sıcaklığı (°C):", "Yağ Sıcaklığı (°C):", "Oil Temp (°C):");
            AddShared("Yağ Basıncı (Bar):", "Yağ Basıncı (Bar):", "Oil Pressure (Bar):");
            AddShared("Yakıt Basıncı (Bar):", "Yakıt Basıncı (Bar):", "Fuel Pressure (Bar):");
            AddShared("Turbo Manifold (kPa):", "Turbo Manifold (kPa):", "Turbo Manifold (kPa):");
            AddShared("Egzoz Sıcaklığı EGT (°C):", "Egzoz Sıcaklığı EGT (°C):", "Exhaust Gas Temp EGT (°C):");
            AddShared("🛡️ Koruma Emniyet Durumları", "🛡️ Koruma Emniyet Durumları", "🛡️ Protection Safety States");
            AddShared("Aktif Limit Devri:", "Aktif Limit Devri:", "Active RPM Limit:");
            AddShared("Toplam Avans Kısma:", "Toplam Avans Kısma:", "Total Timing Pull:");
            AddShared("EGT Yakıt Artışı:", "EGT Yakıt Artışı:", "EGT Fuel Enrichment:");
            AddShared("Fan Rölesi Çıkışı:", "Fan Rölesi Çıkışı:", "Fan Relay Output:");
            AddShared("🔄 Alarmları Sıfırla / Koruma Reset", "🔄 Alarmları Sıfırla / Koruma Reset", "🔄 Reset Alarms / Protection Reset");
            AddShared("Özel koruma eşiklerinde bir problem algılanmadı.", "Özel koruma eşiklerinde bir problem algılanmadı.", "No issue detected at the custom protection thresholds.");

            AddShared("msg_high_oil_temp", "🚨 YÜKSEK YAĞ SICAKLIĞI ({0}°C): Motor koruma modu devrede, RPM limiti {1} RPM.", "🚨 HIGH OIL TEMP ({0}°C): Engine protection active, RPM limit {1} RPM.");
            AddShared("msg_low_oil_press", "🚨 KRİTİK DÜŞÜK YAĞ BASINCI ({0} Bar): Motor hasar mekanizması nedeniyle YAKIT KESİLDİ!", "🚨 CRITICAL LOW OIL PRESS ({0} Bar): FUEL CUT to prevent engine damage!");
            AddShared("msg_low_fuel_press", "🚨 DÜŞÜK YAKIT BASINCI ({0} Bar): Yağlama basıncı yetersiz, avans geriye çekildi, limitler düşürüldü.", "🚨 LOW FUEL PRESS ({0} Bar): Unsafe pressure, timing retarded, limits reduced.");
            AddShared("msg_iat_heat_soak", "⚠️ EMME HAVA ENJEKTÖRÜ SICAK (HEAT SOAK): IAT {0}°C. Koruma amaçlı avans kısılıyor (-{1}°), boost payı kısıtlanıyor.", "⚠️ IAT HEAT SOAK: IAT {0}°C. Timing retarded (-{1}°), boost limit reduced for protection.");
            AddShared("msg_critical_egt", "❗ CRITICAL EGT LIMIT ({0}°C): Avans kısılıyor (-{1}°), enjeksiyon %{2} zenginleştirilerek yanma ısısı düşürülüyor.", "❗ CRITICAL EGT LIMIT ({0}°C): Timing reduced (-{1}°), fueling enriched by {2}% to lower temps.");
            AddShared("msg_lean_cut", "🚨 LEAN CUT: RPM={0:0} — MAP={1:0} kPa — AFR={2:0.00} (eşik: >{3}). Fakir yanma koruması aktif!", "🚨 LEAN CUT: RPM={0:0} — MAP={1:0} kPa — AFR={2:0.00} (threshold: >{3}). Lean protection active!");
            AddShared("msg_overboost", "🚨 OVERBOOST: MAP={0:0} kPa > Limit={1:0} kPa. Boost kesme koruması devrede!", "🚨 OVERBOOST: MAP={0:0} kPa > Limit={1:0} kPa. Boost cut protection active!");
            AddShared("msg_ect_temp", "⚠️ ECT AŞIRI SICAKLIK ({0}°C): Avans -{1:0.0}° kısıldı (dinamik retard).", "⚠️ ECT OVERTEMP ({0}°C): Timing retarded -{1:0.0}° (dynamic retard).");
            AddShared("msg_knock", "🔔 KNOCK ALGILANDI: Avans -{0:0.0}° geri çekildi.", "🔔 KNOCK DETECTED: Timing retarded -{0:0.0}°.");
            AddShared("vtec_inactive", "VTEC Kapalı", "VTEC Inactive");
            AddShared("vtec_active", "VTEC Devrede!", "VTEC Active!");
            AddShared("wg_system_safe", "SİSTEM GÜVENLİ: Solenoid ve Turbo stabil.", "SYSTEM SAFE: Solenoid and Turbo stable.");
            AddShared("ep_system_safe", "✅ SİSTEM GÜVENLİ", "✅ SYSTEM SAFE");
            AddShared("ep_power_reduction", "⚠️ KORUMA: AVANS/YAKIT MÜDAHALESİ", "⚠️ PROTECTION: TIMING/FUEL PULL");
            AddShared("ep_limp_mode", "⚠️ LIMP MODE (DEVİR KESİLİYOR)", "⚠️ LIMP MODE (RPM CUT)");
            AddShared("ep_fuel_cut", "⛔ KRİTİK ALARM: YAKIT KESİLDİ!", "⛔ CRITICAL ALARM: FUEL CUT!");
            AddShared("fan_relay_on", "AKTİF (Çekili)", "ACTIVE (Relay On)");
            AddShared("fan_relay_off", "PASİF", "INACTIVE (Relay Off)");

            // DynoLogsControl UI
            AddShared("📊 Virtual Dyno & Güç Analizörü", "📊 Virtual Dyno & Güç Analizörü", "📊 Virtual Dyno & Power Analyzer");
            AddShared("⏱️ Pist Sürüş & Performans", "⏱️ Pist Sürüş & Performans", "⏱️ Track Logs & Performance");
            AddShared("🌿 Versiyon & RAM Watchdog", "🌿 Versiyon & RAM Watchdog", "🌿 Versioning & RAM Watchdog");
            AddShared("🏎️ Virtual Dyno Parametreleri", "🏎️ Virtual Dyno Parametreleri", "🏎️ Virtual Dyno Parameters");
            AddShared("Araç Ağırlığı (Kg):", "Araç Ağırlığı (Kg):", "Vehicle Weight (Kg):");
            AddShared("Aktarma Kaybı (%):", "Aktarma Kaybı (%):", "Drivetrain Loss (%):");
            AddShared("Düzeltme Standardı:", "Düzeltme Standardı:", "Correction Standard:");
            AddShared("Simüle Manifold Basıncı (Boost):", "Simüle Manifold Basıncı (Boost):", "Simulated Manifold Pressure (Boost):");
            AddShared("⚡ Sanal Dyno Testini Çalıştır", "⚡ Sanal Dyno Testini Çalıştır", "⚡ Run Virtual Dyno Test");
            AddShared("📈 Sanal Güç / Tork Çıktı Tablosu", "📈 Sanal Güç / Tork Çıktı Tablosu", "📈 Virtual Power / Torque Output Table");
            AddShared("WHP (Teker)", "WHP (Teker)", "WHP (Wheel)");
            AddShared("Engine HP", "Engine HP", "Engine HP");
            AddShared("Tork (Nm)", "Tork (Nm)", "Torque (Nm)");
            AddShared("⏱️ Pist Performansı & Vites Geçiş Ölçer", "⏱️ Pist Performansı & Vites Geçiş Ölçer", "⏱️ Track Performance & Shift Timer");
            AddShared("Lastik Çapı (İnç):", "Lastik Çapı (İnç):", "Tyre Diameter (Inches):");
            AddShared("Şanzıman Vites Oranı:", "Şanzıman Vites Oranı:", "Gearbox Ratio:");
            AddShared("Ayna Mahruti Oranı:", "Ayna Mahruti Oranı:", "Final Drive Ratio:");
            AddShared("🚀 0 - 100 km/h Hızlanma:", "🚀 0 - 100 km/h Hızlanma:", "🚀 0 - 100 km/h Acceleration:");
            AddShared("✈️ 100 - 200 km/h Hızlanma:", "✈️ 100 - 200 km/h Hızlanma:", "✈️ 100 - 200 km/h Acceleration:");
            AddShared("🔌 Vites Geçiş Yavaşlaması:", "🔌 Vites Geçiş Yavaşlaması:", "🔌 Gear Shift Delay:");
            AddShared("🌿 Kalibrasyon Sürüm Kontrolü (Branching)", "🌿 Kalibrasyon Sürüm Kontrolü (Branching)", "🌿 Calibration Version Control (Branching)");
            AddShared("Aktif Dal (Branch):", "Aktif Dal (Branch):", "Active Branch:");
            AddShared("Yeni Dal Oluştur:", "Yeni Dal Oluştur:", "Create New Branch:");
            AddShared("➕ Dal Aç", "➕ Dal Aç", "➕ New Branch");
            AddShared("Hafıza Commit Açıklaması:", "Hafıza Commit Açıklaması:", "Memory Commit Description:");
            AddShared("💾 Commit", "💾 Commit", "💾 Commit");
            AddShared("🔎 RAM Değer Watchdog (MCU Mercek)", "🔎 RAM Değer Watchdog (MCU Mercek)", "🔎 RAM Value Watchdog (MCU Lens)");
            AddShared("Değişken", "Değişken", "Variable");
            AddShared("Canlı Değer", "Canlı Değer", "Live Value");
            AddShared("diag_sim_mode", "(Simülasyon Modu)", "(Simulation Mode)");
            AddShared("diag_live_stream_ok", "[VERİ AKIŞI] 9600 bps OBD1 Aktif -> Son Okuma: BAŞARILI (Durum: {0})", "[DATA STREAM] 9600 bps OBD1 Active -> Last Read: SUCCESS (State: {0})");
            AddShared("1 (AKTİF)", "1 (AKTİF)", "1 (ACTIVE)");
            AddShared("0 (PASİF)", "0 (PASİF)", "0 (INACTIVE)");
            AddShared("● Bağlı Değil", "● Bağlı Değil", "● Disconnected");
            AddShared("CH341A Programlayıcı hazır. 'Bağlan' butonuna basın.", "CH341A Programlayıcı hazır. 'Bağlan' butonuna basın.", "CH341A Programmer ready. Press 'Connect' button.");
            AddShared("Yenile", "Yenile", "Refresh");
            AddShared("Kodları Temizle", "Kodları Temizle", "Clear DTCs");
            AddShared("mbt_risk", "⚠️ RİSK: Mevcut Avans MBT Üzerinde! Vuruntu (Knock) Tehlikesi Var.", "⚠️ RISK: Current Advance is above MBT! Knock Danger Active.");
            AddShared("mbt_opt", "ℹ️ OPTİMİZASYON: Avans MBT'nin Çok Gerisinde. Güç Kazanmak İçin Avansı Artırın.", "ℹ️ OPTIMIZATION: Advance is far behind MBT. Increase advance to gain power.");
            AddShared("mbt_safe", "✅ GÜVENLİ: Ateşleme Zamanlaması MBT Noktasına Çok Yakın.", "✅ SAFE: Ignition Timing is very close to MBT.");
            AddShared("Kod", "Kod", "Code");
            AddShared("Açıklama", "Açıklama", "Description");
            AddShared("branch_created", "\"{0}\" dalı oluşturuldu ve bu dala geçildi.", "\"{0}\" branch was created and checked out.");
            AddShared("commit_msg", "[Commit: {0}] \"{1}\" dalında: {2}", "[Commit: {0}] on branch \"{1}\": {2}");
            AddShared("branch_merged", "\"{0}\" dalı \"{1}\" dalıyla BİRLEŞTİRİLDİ (MERGE).", "Branch \"{0}\" was MERGED into \"{1}\".");
            AddShared("İlk temel kalibrasyon dosyası hazırlandı.", "İlk temel kalibrasyon dosyası hazırlandı.", "Initial base calibration setup stock");

            AddShared("alarm_injector_saturation", "ALARM: Enjektör Doygunluğu! (Duty %{0})", "ALARM: Injector Saturation! (Duty %{0})");
            AddShared("diag_can_error", "HATA: Geçersiz Format", "ERROR: Invalid Format");
            AddShared("mbt_above", "+{0:0.0}° (Erken)", "+{0:0.0}° (Advanced)");
            AddShared("mbt_retarded", "{0:0.0}° (Gecikmeli)", "{0:0.0}° (Retarded)");
            AddShared("mbt_apply_msg_fmt", "Ateşleme avansı haritada {0:F1}° olarak güncellendi.", "Ignition advance on map updated to {0:F1}°.");
            AddShared("mbt_apply_title", "Harita Güncellendi", "Map Updated");
            AddShared("perf_default_sec", "-- saniye", "-- seconds");
            AddShared("dyno_shift_delay", "{0} ms (Kavrama bırakma gecikmesi)", "{0} ms (Clutch drop delay)");

            AddShared("Closed Loop AutoTune Kontrol Paneli", "Closed Loop AutoTune Kontrol Paneli", "Closed Loop AutoTune Control Panel");
            AddShared("Oturum Ayarları", "Oturum Ayarları", "Session Settings");

            AddShared("chart_3d_title", "3D Harita", "3D Map");
            AddShared("chart_waiting_data", "Harita verisi bekleniyor...", "Waiting for map data...");
            AddShared("chart_drag_rotate", "⟳ Sürükle: döndür", "⟳ Drag: rotate");

            LoadVehicleSelectionDefaults();
        }

        private static void AddShared(string key, string tr, string en)
        {
            _translations[key] = CurrentLanguage == "en" ? en : tr;
        }

        public static void LoadVehicleSelectionDefaults()
        {
            AddShared("veh_dialog_title", "Honda Tuner — Araç / ECU Seç", "Honda Tuner — Select Vehicle / ECU");
            AddShared("veh_dialog_label_title", "Araç & ECU Seçimi", "Vehicle & ECU Selection");
            AddShared("veh_dialog_label_sub", "Honda Community Verified ECU Database — pgmfi.org  |  8 ECU  ·  14 araç modeli", "Honda Community Verified ECU Database — pgmfi.org  |  8 ECUs  ·  14 vehicle models");
            AddShared("veh_dialog_count", "Bir ECU seçin →", "Select an ECU →");
            AddShared("veh_dialog_cancel", "İptal", "Cancel");
            AddShared("veh_dialog_ok", "✓  Bu Araç ile Devam Et", "✓  Continue with this Vehicle");
            AddShared("veh_dialog_make", "Marka", "Make");
            AddShared("veh_dialog_model", "Model", "Model");
            AddShared("veh_dialog_trim", "Donanım", "Trim");
            AddShared("veh_dialog_engine", "Motor", "Engine");
            AddShared("veh_dialog_year", "Yıl", "Year");
            AddShared("veh_dialog_hp", "HP", "HP");
            AddShared("veh_dialog_trans", "Şanzıman", "Transmission");
            AddShared("veh_dialog_region", "Bölge", "Region");

            AddShared("veh_dialog_vtec", "⚡ VTEC", "⚡ VTEC");
            AddShared("veh_dialog_nonvtec", "○ Non-VTEC", "○ Non-VTEC");
            AddShared("veh_dialog_iab", "  IAB", "  IAB");
            AddShared("veh_dialog_desc_select", "① Listeden bir araç seçin, ardından \"Bu Araç ile Devam Et\" butonuna tıklayın.", "① Select a vehicle from the list, then click \"Continue with this Vehicle\".");
            AddShared("veh_dialog_ecu_count", "{0} araç modeli  |  {1}", "{0} vehicle models  |  {1}");

            AddShared("ecu_vtece", "VTEC-E (Ekonomi)", "VTEC-E (Economy)");
            AddShared("ecu_p05_desc", "Düşük emisyon / yakıt tasarrufu odaklı VTEC-E motor. Performans odaklı değil.", "Low emission / fuel economy focused VTEC-E engine. Not performance oriented.");
            AddShared("ecu_p05_note1", "VTEC-E: düşük devirde tek supap çalışır — yakıt tasarrufu", "VTEC-E: single intake valve operation at low RPM — fuel economy");

            AddShared("ecu_nonvtec", "Non-VTEC", "Non-VTEC");
            AddShared("ecu_p06_desc", "Standart 1.5L motor. VTEC devresi yok. Chipleme ile B-serisi swap'larda popüler.", "Standard 1.5L engine. No VTEC circuit. Popular for B-series swaps with chipping.");
            AddShared("ecu_p06_note_auto", "Otomatik vites versiyonu", "Automatic transmission version");

            AddShared("ecu_sohcvtec", "SOHC VTEC", "SOHC VTEC");
            AddShared("ecu_p28_desc", "En popüler OBD1 ECU. D16Z6 motor. Swap ve tuning için referans platform.", "Most popular OBD1 ECU. D16Z6 engine. Reference platform for swaps and tuning.");
            AddShared("ecu_p28_delsol", "Del Sol çatısız 2 kişilik", "Del Sol targa top 2-seater");
            AddShared("ecu_p28_ies_ek", "EK kasa iES — P28 OBD1 dönüşümü ile tuning", "EK chassis iES — tuning with P28 OBD1 conversion");
            AddShared("ecu_p28_ies_tr", "Yumurta kasa iES: OBD1 P28/P06 dönüşümüyle basemap ve sokak ayarı", "Egg body iES: basemap and street tune with OBD1 P28/P06 conversion");
            AddShared("ecu_p28_swap", "iES kasaya SOHC VTEC swap veya mini-me kurulumları için", "For SOHC VTEC swap or mini-me setups on iES chassis");

            AddShared("ecu_p30_desc", "EG kasa 1.5i. Non-VTEC D15B2. Düşük maliyetli chip platform.", "EG chassis 1.5i. Non-VTEC D15B2. Low-cost chip platform.");

            AddShared("ecu_dohcvtec", "DOHC VTEC", "DOHC VTEC");
            AddShared("ecu_p61_desc", "1.7L B17A1 DOHC VTEC. İlk Integra GS-R nesli. 8200 RPM sınır.", "1.7L B17A1 DOHC VTEC. First Integra GS-R generation. 8200 RPM limit.");
            AddShared("ecu_p61_note", "İlk DOHC VTEC Integra — B17A1", "First DOHC VTEC Integra — B17A1");

            AddShared("ecu_dohciab", "DOHC VTEC + IAB", "DOHC VTEC + IAB");
            AddShared("ecu_p72_desc", "B18C1 DOHC VTEC + Intake Air Bypass. 170HP stock. Efsanevi tuning platformu.", "B18C1 DOHC VTEC + Intake Air Bypass. 170HP stock. Legendary tuning platform.");
            AddShared("ecu_p72_iab", "IAB (Intake Air Bypass) solenoidi mevcut — P72'ye özel", "Features IAB (Intake Air Bypass) solenoid — specific to P72");
            AddShared("ecu_p72_itr", "ITR — P73 ECU; P72 swap'la uyumlu", "ITR — P73 ECU; compatible with P72 swap");

            AddShared("ecu_p74_desc", "B18B1 DOHC Non-VTEC. LS Vtec swap için temel ECU.", "B18B1 DOHC Non-VTEC. Base ECU for LS Vtec swap.");

            AddShared("ecu_p13_desc", "H22A 2.2L DOHC VTEC. Prelude serisinin güçlü kalbi. 190HP JDM.", "H22A 2.2L DOHC VTEC. The powerful heart of the Prelude series. 190HP JDM.");
            AddShared("ecu_p13_jdm_190", "JDM versiyonu 190HP", "JDM version 190HP");
            AddShared("ecu_p13_accord", "JDM/EDM Accord SiR — aynı motor, farklı kamera", "JDM/EDM Accord SiR — same engine, different cams");

            AddShared("trans_mt", "Manuel", "Manual");
            AddShared("trans_at", "Otomatik", "Automatic");
            AddShared("trans_mt_at", "Manuel/Otomatik", "Manual/Automatic");

            // Appended keys
            AddShared("rom_not_loaded_status", "ROM yüklenmedi. Dosya → Aç ile başlayın.", "ROM not loaded. Start with File → Open.");
            AddShared("commit_msg_init_base", "İlk temel kalibrasyon dosyası hazırlandı.", "Initial base calibration file prepared.");
            AddShared("prog_init_ready", "CH341A Programlayıcı hazır. 'Bağlan' butonuna basın.", "CH341A Programmer ready. Press 'Connect'.");
        }
    }
}
