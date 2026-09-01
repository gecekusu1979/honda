# <HondaTuner V2

HondaTuner V2, Honda OBD1 ECU ROM dosyalarÄ±nÄ± incelemek, dÃ¼zenlemek, doÄŸrulamak ve geliÅŸtirme ortamÄ±nda test etmek iÃ§in hazÄ±rlanmÄ±ÅŸ Windows Forms tabanlÄ± modern bir tuning aracÄ±dÄ±r. Proje P28 odaklÄ± baÅŸlamÄ±ÅŸ olup; gÃ¼ncel kod tabanÄ± P05, P06, P28, P30, P61, P72, P74 ve P13 gibi farklÄ± Honda ECU profillerini de kapsayan geniÅŸletilebilir bir mimariye sahiptir.

> **Yasal UyarÄ±:** Bu yazÄ±lÄ±m yalnÄ±zca araÅŸtÄ±rma, eÄŸitim ve kapalÄ± pist/yarÄ±ÅŸ geliÅŸtirme amaÃ§lÄ±dÄ±r. GerÃ§ek araÃ§, ECU, EEPROM programlayÄ±cÄ± veya emÃ¼latÃ¶r Ã¼zerinde iÅŸlem yapmadan Ã¶nce mutlaka orijinal ROM yedeÄŸi alÄ±nmalÄ± ve yapÄ±lan deÄŸiÅŸiklikler uzman kontrolÃ¼nden geÃ§irilmelidir.

---

## Ã–ne Ã‡Ä±kan Ã–zellikler

* **ROM YÃ¶netimi:** ROM aÃ§ma, kaydetme, binary karÅŸÄ±laÅŸtÄ±rma (Diff) ve otomatik yedekleme akÄ±ÅŸlarÄ±.
* **Harita DÃ¼zenleme:** Fuel (VE) ve Ignition (AteÅŸleme) 2D/3D tablo dÃ¼zenleme arayÃ¼zleri.
* **Kalibrasyon Limitleri:** VTEC devreye girme devri, Rev Limiter ve HÄ±z Limiti dÃ¼zenleyicileri.
* **BÃ¼tÃ¼nlÃ¼k & GÃ¼venlik:** Otomatik Honda Checksum doÄŸrulama ve gÃ¼ncelleme motoru.
* **Patch Motoru:** Patch tanÄ±mlarÄ±, preview, validation ve rollback altyapÄ±sÄ±.
* **AutoTune Motoru:** GerÃ§ek zamanlÄ± VE dÃ¼zeltme kararlarÄ±, gÃ¼venlik kontrolleri, snapshot ve recovery servisleri.
* **CanlÄ± Telemetri & Koruma BarÄ±:** OBD1 akÄ±ÅŸÄ± sÄ±rasÄ±nda tetiklenen motor koruma limitlerini (`LeanCut`, `OverboostCut`, `ECTRetard`, `KnockRetard`) Ã¼st barda gÃ¶steren **CanlÄ± Koruma UyarÄ± Banneri**.
* **AutoTune Ã–neri Tablosu:** SÃ¼rÃ¼ÅŸ esnasÄ±nda AutoTune motoru tarafÄ±ndan Ã¼retilen dÃ¼zeltmeleri gÃ¶steren, UI blokajÄ±nÄ± engellemek iÃ§in 50 satÄ±rla sÄ±nÄ±rlÄ± **Ã–neri Tablosu (Suggestions Grid)**.
* **GeliÅŸmiÅŸ OBD1 Protokol Parser'Ä±:** Parazitli/hatalÄ± seri port paketlerini filtrelemek iÃ§in `0xFF 0xFE` sync baytlarÄ± ve checksum kontrolÃ¼nÃ¼ barÄ±ndÄ±ran 32-byte ring-buffer tabanlÄ± parser.
* **Multi-Language (TR/EN) DesteÄŸi:** `Database/` klasÃ¶rÃ¼ndeki `.resx` XML dosyalarÄ± Ã¼zerinden Ã§alÄ±ÅŸan, menÃ¼ barÄ±ndan anÄ±nda dil deÄŸiÅŸtirmeyi saÄŸlayan TÃ¼rkÃ§e ve Ä°ngilizce arayÃ¼z desteÄŸi.
* **Dahili Test AltyapÄ±sÄ±:** Dahili test harness ve sentetik ROM doÄŸrulama suite'i (8 yeni koruma testi dahil **123 unit test**).

---

## Gereksinimler

* **Ä°ÅŸletim Sistemi:** Windows 10 / 11 (x64)
* **Ã‡alÄ±ÅŸma ZamanÄ± / SDK:** .NET 8.0 SDK (LTS)
* **GeliÅŸtirme OrtamÄ±:** Visual Studio 2022 veya VS Code (C# Dev Kit eklentisi ile)
* **SÃ¼rÃ¼cÃ¼ler:** FTDI / Seri Port / CH341A / TL866 donanÄ±mlarÄ± iÃ§in gÃ¼ncel Windows sÃ¼rÃ¼cÃ¼leri.

---

## Kurulum ve Ã‡alÄ±ÅŸtÄ±rma

```bash
# Depoyu klonlayÄ±n
git clone https://github.com/gecekusu1979/honda.git
cd honda

# BaÄŸÄ±mlÄ±lÄ±klarÄ± geri yÃ¼kleyin ve derleyin
dotnet restore
dotnet build --nologo

# UygulamayÄ± baÅŸlatÄ±n
dotnet run

# YalnÄ±zca otomatik test suite'ini Ã§alÄ±ÅŸtÄ±rmak iÃ§in:
dotnet run -- --test-only
```

---

## Proje Mimarisi

```plaintext
HondaTuner/
â”œâ”€â”€ Core/
â”‚   â”œâ”€â”€ AutoTune/              AutoTune karar, gÃ¼venlik, snapshot ve recovery servisleri
â”‚   â”œâ”€â”€ Calibration/           YakÄ±t, ateÅŸleme, diagnostics, dyno/log ve koruma tablolarÄ±
â”‚   â”œâ”€â”€ Localization/          XML .resx tabanlÄ± TR/EN dil Ã§eviri yardÄ±mcÄ±sÄ± (L.cs)
â”‚   â”œâ”€â”€ Protocol/              Ring-buffer tabanlÄ± Honda OBD1 Serial Frame Parser
â”‚   â”œâ”€â”€ Rom/                   ROM servisleri, checksum, patch engine ve backup yÃ¶netimi
â”‚   â”œâ”€â”€ ReverseEngineering/    ROM analiz, map search ve axis extraction yardÄ±mcÄ±larÄ±
â”‚   â”œâ”€â”€ Rtp/                   RTP kalibrasyon motoru ve event modelleri
â”‚   â”œâ”€â”€ Telemetry/             Telemetry bus, provider, dispatcher, frame ve channel altyapÄ±sÄ±
â”‚   â”œâ”€â”€ EcuProfiles.cs         Desteklenen ECU profil tanÄ±mlarÄ±
â”‚   â”œâ”€â”€ EcuConstants.cs        Ortak ROM boyutu ve OBD1 baud rate sabitleri
â”‚   â””â”€â”€ RomParser.cs           ROM buffer okuma/yazma ve temel parser iÅŸlemleri
â”œâ”€â”€ UI/
â”‚   â”œâ”€â”€ MainForm.cs            Ana Windows Forms arayÃ¼zÃ¼ ve dil ayarlarÄ±
â”‚   â”œâ”€â”€ MapGridControl.cs      Harita dÃ¼zenleme grid bileÅŸeni
â”‚   â”œâ”€â”€ TelemetryDashboard.cs  CanlÄ± telemetri ekranÄ±
â”‚   â”œâ”€â”€ ReverseControl.cs      ROM inceleme ekranÄ±
â”‚   â””â”€â”€ Controls/              Kalibrasyon, diagnostics, dyno ve donanÄ±m ekranlarÄ±
â”œâ”€â”€ Hardware/
â”‚   â”œâ”€â”€ EEPROM/                CH341A ve TL866 programlayÄ±cÄ± sÄ±nÄ±flarÄ±
â”‚   â”œâ”€â”€ Emulator/              Ostrich emulator entegrasyonu
â”‚   â”œâ”€â”€ OBD/                   Honda OBD1 seri baÄŸlantÄ± ve DTC yÃ¶netimi
â”‚   â””â”€â”€ Discovery/             DonanÄ±m keÅŸif yardÄ±mcÄ±larÄ±
â”œâ”€â”€ Tests/                     Tuning test harness ve sample ROM testleri
â””â”€â”€ HondaTuner.csproj
```

---

## ROM ve ECU Referans Bilgileri

* **VarsayÄ±lan Honda OBD1 ROM Boyutu:** 32768 byte (32 KB - P28/P30)
* **GeniÅŸletilmiÅŸ ROM Boyutu:** 65536 byte (64 KB - P72/P06 MCU extension)
* **OBD1 K-Line Baud Rate:** 9600 bps
* **Sabitler SÄ±nÄ±fÄ±:** `Core/EcuConstants.cs`

---

## P28 Temel Bellek HaritasÄ±

| Parametre / Tablo | BaÅŸlangÄ±Ã§ Offset | Boyut / Format |
| :--- | :---: | :--- |
| Fuel Map (DÃ¼ÅŸÃ¼k YÃ¼k) | 0x1D40 | 16x16 Tablo |
| Ignition Map (DÃ¼ÅŸÃ¼k YÃ¼k) | 0x1E40 | 16x16 Tablo |
| VTEC Devri (RPM) | 0x1F40 | 2 Byte (Word) |
| Rev Limiter (Kesici) | 0x1FAA | 2 Byte (Word) |
| ROM Checksum | 0x7FFF | 1 Byte |

---

## DonanÄ±m Kontrol Durumu

| ModÃ¼l | Durum | AÃ§Ä±klama |
| :--- | :---: | :--- |
| **CH341A EEPROM ProgramlayÄ±cÄ±** | âœ… Aktif | `ch341a.dll` / `minipro` API entegrasyonu tamamlandÄ±; fiziksel donanÄ±m ve sÃ¼rÃ¼cÃ¼ gerektirir. |
| **TL866 / Minipro Wrapper** | âœ… Aktif | `minipro.exe` CLI wrapper entegrasyonu tamamlandÄ±; fiziksel donanÄ±m ve CLI aracÄ± gerektirir. |
| **Moates Ostrich 2.0 EmÃ¼latÃ¶r** | âœ… Aktif | Seri haberleÅŸme ve gerÃ§ek zamanlÄ± ROM yÃ¼kleme altyapÄ±sÄ± tamam; emÃ¼latÃ¶r donanÄ±mÄ± gerektirir. |
| **Honda OBD1 Seri Telemetri** | âœ… Aktif | 32-byte ring-buffer, `0xFF 0xFE` sync ve checksum korumalÄ± parser entegrasyonu tamamlandÄ±. |
| **SimÃ¼latÃ¶r & Datalog** | âœ… Aktif | Dahili telemetri jeneratÃ¶rÃ¼ ile donanÄ±msÄ±z test edilebilir. |

---

## Yasal UyarÄ± ve Sorumluluk Reddi

* **KullanÄ±m AmacÄ±:** Bu yazÄ±lÄ±m eÄŸitim, hobi ve pist/yarÄ±ÅŸ geliÅŸtirme amacÄ±yla sunulmuÅŸtur. Kamuya aÃ§Ä±k yollarda kullanÄ±lan araÃ§larda kalibrasyon deÄŸiÅŸikliÄŸi yapÄ±lmasÄ± Ã¶nerilmez.
* **Sorumluluk:** YanlÄ±ÅŸ yapÄ±lan yakÄ±t, avans veya devir kesici ayarlarÄ±ndan doÄŸabilecek mekanik arÄ±zalar, motor hasarlarÄ± veya maddi/manevi zararlardan yazÄ±lÄ±m geliÅŸtiricileri sorumlu tutulamaz. DetaylÄ± bilgi iÃ§in `DISCLAIMER.md` dosyasÄ±nÄ± inceleyiniz.
* **Ticari Markalar:** "Honda", "VTEC", "PGM-FI" ve ilgili araÃ§ modelleri Honda Motor Co., Ltd. ÅŸirketinin tescilli ticari markalarÄ±dÄ±r. Bu projede yalnÄ±zca donanÄ±m mimarisini ve protokolleri tanÄ±mlama amacÄ±yla kullanÄ±lmÄ±ÅŸtÄ±r.

---

## Lisans

Bu proje MIT LisansÄ± altÄ±nda aÃ§Ä±k kaynak olarak lisanslanmÄ±ÅŸtÄ±r.

<br/><hr/><br/>

# HondaTuner V2 (Global ğŸŒ)

HondaTuner V2 is a modern, Windows Forms-based tuning laboratory designed for analyzing, editing, validating, and testing Honda OBD1 ECU ROM files. While originally focused on the P28 architecture, the current codebase has an extensible layout natively supporting ECU profiles such as P05, P06, P28, P30, P61, P72, P74, and P13.

> **Legal Disclaimer:** This software is strictly for research, educational, and closed-circuit/racing development purposes only. You must always back up your original ROM and seek expert review before making physical edits to your vehicle's engine management system.

---

## âš¡ Core Features

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

## ğŸ’» Requirements

* **OS:** Windows 10 / 11 (x64)
* **Runtime / SDK:** .NET 8.0 SDK (LTS)
* **Development:** Visual Studio 2022 or VS Code (with C# Dev Kit)
* **Hardware Drivers:** Must have up-to-date Windows drivers for hardware control components (FTDI / Serial Port / CH341A / TL866).

---

## ğŸš€ Setup & Execution

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

## ğŸ”Œ Hardware Control Status

| Module | Status | Description |
| :--- | :---: | :--- |
| **CH341A EEPROM Programmer** | âœ… Active | Native `ch341a.dll` / `minipro` API integration; requires physical hardware. |
| **TL866 / Minipro Wrapper** | âœ… Active | CLI wrapper support implemented. |
| **Moates Ostrich 2.0 Emulator** | âœ… Active | Real-time RAM emulation via serial block pushes; requires hardware. |
| **Honda OBD1 Serial Datalogging** | âœ… Active | Checksum verified, packet loss resilient sync parser. |
| **Simulator & Datalog Playback** | âœ… Active | Test application logic via offline synthetic trace generation. |

---

## âš–ï¸ License
This project is open-sourced under the **MIT License**.
