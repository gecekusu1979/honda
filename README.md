# <HondaTuner V2

HondaTuner V2, Honda OBD1 ECU ROM dosyalarını incelemek, düzenlemek, doğrulamak ve geliştirme ortamında test etmek için hazırlanmış Windows Forms tabanlı modern bir tuning aracıdır. Proje P28 odaklı başlamış olup; güncel kod tabanı P05, P06, P28, P30, P61, P72, P74 ve P13 gibi farklı Honda ECU profillerini de kapsayan genişletilebilir bir mimariye sahiptir.

> **Yasal Uyarı:** Bu yazılım yalnızca araştırma, eğitim ve kapalı pist/yarış geliştirme amaçlıdır. Gerçek araç, ECU, EEPROM programlayıcı veya emülatör üzerinde işlem yapmadan önce mutlaka orijinal ROM yedeği alınmalı ve yapılan değişiklikler uzman kontrolünden geçirilmelidir.

---

## Öne Çıkan Özellikler

* **ROM Yönetimi:** ROM açma, kaydetme, binary karşılaştırma (Diff) ve otomatik yedekleme akışları.
* **Harita Düzenleme:** Fuel (VE) ve Ignition (Ateşleme) 2D/3D tablo düzenleme arayüzleri.
* **Kalibrasyon Limitleri:** VTEC devreye girme devri, Rev Limiter ve Hız Limiti düzenleyicileri.
* **Bütünlük & Güvenlik:** Otomatik Honda Checksum doğrulama ve güncelleme motoru.
* **Patch Motoru:** Patch tanımları, preview, validation ve rollback altyapısı.
* **AutoTune Motoru:** Gerçek zamanlı VE düzeltme kararları, güvenlik kontrolleri, snapshot ve recovery servisleri.
* **Canlı Telemetri & Koruma Barı:** OBD1 akışı sırasında tetiklenen motor koruma limitlerini (`LeanCut`, `OverboostCut`, `ECTRetard`, `KnockRetard`) üst barda gösteren **Canlı Koruma Uyarı Banneri**.
* **AutoTune Öneri Tablosu:** Sürüş esnasında AutoTune motoru tarafından üretilen düzeltmeleri gösteren, UI blokajını engellemek için 50 satırla sınırlı **Öneri Tablosu (Suggestions Grid)**.
* **Gelişmiş OBD1 Protokol Parser'ı:** Parazitli/hatalı seri port paketlerini filtrelemek için `0xFF 0xFE` sync baytları ve checksum kontrolünü barındıran 32-byte ring-buffer tabanlı parser.
* **Multi-Language (TR/EN) Desteği:** `Database/` klasöründeki `.resx` XML dosyaları üzerinden çalışan, menü barından anında dil değiştirmeyi sağlayan Türkçe ve İngilizce arayüz desteği.
* **Dahili Test Altyapısı:** Dahili test harness ve sentetik ROM doğrulama suite'i (8 yeni koruma testi dahil **123 unit test**).

---

## Gereksinimler

* **İşletim Sistemi:** Windows 10 / 11 (x64)
* **Çalışma Zamanı / SDK:** .NET 8.0 SDK (LTS)
* **Geliştirme Ortamı:** Visual Studio 2022 veya VS Code (C# Dev Kit eklentisi ile)
* **Sürücüler:** FTDI / Seri Port / CH341A / TL866 donanımları için güncel Windows sürücüleri.

---

## Kurulum ve Çalıştırma

```bash
# Depoyu klonlayın
git clone https://github.com/gecekusu1979/honda.git
cd honda

# Bağımlılıkları geri yükleyin ve derleyin
dotnet restore
dotnet build --nologo

# Uygulamayı başlatın
dotnet run

# Yalnızca otomatik test suite'ini çalıştırmak için:
dotnet run -- --test-only
```

---

## Proje Mimarisi

```plaintext
HondaTuner/
├── Core/
│   ├── AutoTune/              AutoTune karar, güvenlik, snapshot ve recovery servisleri
│   ├── Calibration/           Yakıt, ateşleme, diagnostics, dyno/log ve koruma tabloları
│   ├── Localization/          XML .resx tabanlı TR/EN dil çeviri yardımcısı (L.cs)
│   ├── Protocol/              Ring-buffer tabanlı Honda OBD1 Serial Frame Parser
│   ├── Rom/                   ROM servisleri, checksum, patch engine ve backup yönetimi
│   ├── ReverseEngineering/    ROM analiz, map search ve axis extraction yardımcıları
│   ├── Rtp/                   RTP kalibrasyon motoru ve event modelleri
│   ├── Telemetry/             Telemetry bus, provider, dispatcher, frame ve channel altyapısı
│   ├── EcuProfiles.cs         Desteklenen ECU profil tanımları
│   ├── EcuConstants.cs        Ortak ROM boyutu ve OBD1 baud rate sabitleri
│   └── RomParser.cs           ROM buffer okuma/yazma ve temel parser işlemleri
├── UI/
│   ├── MainForm.cs            Ana Windows Forms arayüzü ve dil ayarları
│   ├── MapGridControl.cs      Harita düzenleme grid bileşeni
│   ├── TelemetryDashboard.cs  Canlı telemetri ekranı
│   ├── ReverseControl.cs      ROM inceleme ekranı
│   └── Controls/              Kalibrasyon, diagnostics, dyno ve donanım ekranları
├── Hardware/
│   ├── EEPROM/                CH341A ve TL866 programlayıcı sınıfları
│   ├── Emulator/              Ostrich emulator entegrasyonu
│   ├── OBD/                   Honda OBD1 seri bağlantı ve DTC yönetimi
│   └── Discovery/             Donanım keşif yardımcıları
├── Tests/                     Tuning test harness ve sample ROM testleri
└── HondaTuner.csproj
```

---

## ROM ve ECU Referans Bilgileri

* **Varsayılan Honda OBD1 ROM Boyutu:** 32768 byte (32 KB - P28/P30)
* **Genişletilmiş ROM Boyutu:** 65536 byte (64 KB - P72/P06 MCU extension)
* **OBD1 K-Line Baud Rate:** 9600 bps
* **Sabitler Sınıfı:** `Core/EcuConstants.cs`

---

## P28 Temel Bellek Haritası

| Parametre / Tablo | Başlangıç Offset | Boyut / Format |
| :--- | :---: | :--- |
| Fuel Map (Düşük Yük) | 0x1D40 | 16x16 Tablo |
| Ignition Map (Düşük Yük) | 0x1E40 | 16x16 Tablo |
| VTEC Devri (RPM) | 0x1F40 | 2 Byte (Word) |
| Rev Limiter (Kesici) | 0x1FAA | 2 Byte (Word) |
| ROM Checksum | 0x7FFF | 1 Byte |

---

## Donanım Kontrol Durumu

| Modül | Durum | Açıklama |
| :--- | :---: | :--- |
| **CH341A EEPROM Programlayıcı** | ✅ Aktif | `ch341a.dll` / `minipro` API entegrasyonu tamamlandı; fiziksel donanım ve sürücü gerektirir. |
| **TL866 / Minipro Wrapper** | ✅ Aktif | `minipro.exe` CLI wrapper entegrasyonu tamamlandı; fiziksel donanım ve CLI aracı gerektirir. |
| **Moates Ostrich 2.0 Emülatör** | ✅ Aktif | Seri haberleşme ve gerçek zamanlı ROM yükleme altyapısı tamam; emülatör donanımı gerektirir. |
| **Honda OBD1 Seri Telemetri** | ✅ Aktif | 32-byte ring-buffer, `0xFF 0xFE` sync ve checksum korumalı parser entegrasyonu tamamlandı. |
| **Simülatör & Datalog** | ✅ Aktif | Dahili telemetri jeneratörü ile donanımsız test edilebilir. |

---

## Yasal Uyarı ve Sorumluluk Reddi

* **Kullanım Amacı:** Bu yazılım eğitim, hobi ve pist/yarış geliştirme amacıyla sunulmuştur. Kamuya açık yollarda kullanılan araçlarda kalibrasyon değişikliği yapılması önerilmez.
* **Sorumluluk:** Yanlış yapılan yakıt, avans veya devir kesici ayarlarından doğabilecek mekanik arızalar, motor hasarları veya maddi/manevi zararlardan yazılım geliştiricileri sorumlu tutulamaz. Detaylı bilgi için `DISCLAIMER.md` dosyasını inceleyiniz.
* **Ticari Markalar:** "Honda", "VTEC", "PGM-FI" ve ilgili araç modelleri Honda Motor Co., Ltd. şirketinin tescilli ticari markalarıdır. Bu projede yalnızca donanım mimarisini ve protokolleri tanımlama amacıyla kullanılmıştır.

---

## Lisans

Bu proje MIT Lisansı altında açık kaynak olarak lisanslanmıştır.

<br/><hr/><br/>

# HondaTuner V2 (Global 🌎)

HondaTuner V2 is a modern, Windows Forms-based tuning laboratory designed for analyzing, editing, validating, and testing Honda OBD1 ECU ROM files. While originally focused on the P28 architecture, the current codebase has an extensible layout natively supporting ECU profiles such as P05, P06, P28, P30, P61, P72, P74, and P13.

> **Legal Disclaimer:** This software is strictly for research, educational, and closed-circuit/racing development purposes only. You must always back up your original ROM and seek expert review before making physical edits to your vehicle's engine management system.

---

## ⚡ Core Features

* **Multi-Language Support (EN/TR):** Fully dynamic bilingual UI utilizing `.resx` localization caching. Toggle instantly between English and Turkish via the overhead menu without restarting the application!
* **ROM Management:** Comprehensive workflow for opening, saving, taking automatic backups, and conducting binary differential comparisons (Diff).
* **Map Tuning:** 2D/3D visual interpolation and editing across Fuel (VE) and Ignition (Spark) tables.
* **Calibration Limits:** Dedicated editors for manipulating VTEC crossover RPM, Rev Limiters, and Speed limiters. 
* **Integrity & Checksums:** Honda Checksum automatic update and verification engine.
* **Live Telemetry & Safety:** Dynamic **Live Protection Banner** that alerts you against runtime limits (`LeanCut`, `OverboostCut`, `ECTRetard`, `KnockRetard`) through the OBD1 data stream.
* **AutoTune Engine:** Real-time VE (Volumetric Efficiency) correction decisions tracking AFR constraints via the Suggestions Grid.
* **Advanced OBD1 Datastream Parser:** Integrated ring-buffer handling `0xFF 0xFE` sync bytes and checksum validation to filter parity errors on live serial links.
* **Robust Test Infrastructure:** Contains a full internal testing harness ensuring system integrity, including **123 unit tests** spanning ROM validation and backpressure modeling.

---

## 💻 Requirements

* **OS:** Windows 10 / 11 (x64)
* **Runtime / SDK:** .NET 8.0 SDK (LTS)
* **Development:** Visual Studio 2022 or VS Code (with C# Dev Kit)
* **Hardware Drivers:** Must have up-to-date Windows drivers for hardware control components (FTDI / Serial Port / CH341A / TL866).

---

## 🚀 Setup & Execution

```bash
# Clone the repository
git clone https://github.com/gecekusu1979/honda.git
cd honda

# Restore dependencies and build the app
dotnet restore
dotnet build --nologo

# Launch Application
dotnet run
```

---

## 🔌 Hardware Control Status

| Module | Status | Description |
| :--- | :---: | :--- |
| **CH341A EEPROM Programmer** | ✅ Active | Native `ch341a.dll` / `minipro` API integration; requires physical hardware. |
| **TL866 / Minipro Wrapper** | ✅ Active | CLI wrapper support implemented. |
| **Moates Ostrich 2.0 Emulator** | ✅ Active | Real-time RAM emulation via serial block pushes; requires hardware. |
| **Honda OBD1 Serial Datalogging** | ✅ Active | Checksum verified, packet loss resilient sync parser. |
| **Simulator & Datalog Playback** | ✅ Active | Test application logic via offline synthetic trace generation. |

---

## ⚖️ License
This project is open-sourced under the **MIT License**.
