# HondaTuner V2

HondaTuner V2, Honda OBD1 ECU ROM dosyalarını incelemek, düzenlemek, doğrulamak ve geliştirme ortamında test etmek için hazırlanmış Windows Forms tabanlı bir tuning aracıdır. Proje P28 odaklı başladı; güncel kod tabanı P05, P06, P28, P30, P61, P72, P74 ve P13 gibi farklı Honda ECU profillerini de kapsayan daha geniş bir mimariye taşındı.

Bu yazılım araştırma, eğitim, kalibrasyon geliştirme ve test amaçlıdır. Gerçek araç, ECU, EEPROM programlayıcı veya emulator üzerinde işlem yapmadan önce mutlaka orijinal ROM yedeği alınmalı ve yapılan değişiklikler uzman kontrolünden geçirilmelidir.

## Öne Çıkanlar

- ROM açma, kaydetme, karşılaştırma ve yedekleme akışları
- Fuel ve ignition map düzenleme arayüzleri
- VTEC RPM, rev limit ve hız limiti düzenleme
- Checksum doğrulama ve güncelleme altyapısı
- Patch tanımları, patch preview, validation ve rollback modeli
- AutoTune karar motoru, güvenlik kontrolleri, snapshot ve recovery servisleri
- RTP kalibrasyon altyapısı ve domain event akışı
- Telemetry pipeline, buffer, dispatcher, channel ve provider yapısı
- Datalog manager ve seri port tabanlı Honda OBD1 bağlantı denemeleri
- DTC okuma/temizleme için canlı OBD1 kontrol katmanı
- CH341A ve TL866 EEPROM programlayıcı wrapper sınıfları
- Ostrich emulator bağlantı sınıfı
- Reverse engineering yardımcıları, map arama ve axis çıkarma araçları
- Dyno/log analiz servisleri
- Engine protection, diagnostics, VTEC/boost, advanced fuel ve advanced ignition ekranları
- 3D parça/engine görüntüleme bileşenleri
- Test ROM üretimi ve uygulama başlangıcında çalışan test harness

## Gereksinimler

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 veya VS Code + C# eklentisi
- Seri port / OBD1 / EEPROM işlemleri için ilgili donanım sürücüleri

Donanım entegrasyonları gerçek cihaz gerektirir. CH341A, TL866, Ostrich veya Honda OBD1 seri bağlantı akışları simülasyon yerine fiziksel bağlantı istediğinde hata verebilir; bu beklenen bir durumdur.

## Kurulum

```powershell
git clone https://github.com/gecekusu1979/honda.git
cd honda
dotnet restore
dotnet build --nologo
dotnet run
```

Sadece terminal test akışını çalıştırmak için:

```powershell
dotnet run -- --test-only
```

## Proje Yapısı

```text
HondaTuner/
├── Core/
│   ├── AutoTune/              AutoTune karar, güvenlik, snapshot ve recovery servisleri
│   ├── Calibration/           Yakıt, ateşleme, diagnostics, dyno/log ve koruma tabloları
│   ├── Rom/                   ROM servisleri, checksum, patch engine ve backup yönetimi
│   ├── ReverseEngineering/    ROM analiz, map search ve axis extraction yardımcıları
│   ├── Rtp/                   RTP kalibrasyon motoru ve event modelleri
│   ├── Telemetry/             Telemetry bus, provider, dispatcher, frame ve channel altyapısı
│   ├── EcuProfiles.cs         Desteklenen ECU profil tanımları
│   ├── EcuConstants.cs        Ortak ROM boyutu ve OBD1 baud rate sabitleri
│   └── RomParser.cs           ROM buffer okuma/yazma ve temel parser işlemleri
├── UI/
│   ├── MainForm.cs            Ana Windows Forms arayüzü
│   ├── MapGridControl.cs      Harita düzenleme grid bileşeni
│   ├── TelemetryDashboard.cs  Canlı telemetri ekranı
│   ├── ReverseControl.cs      ROM inceleme ekranı
│   └── *Control.cs            Kalibrasyon, diagnostics, dyno ve donanım ekranları
├── Hardware/
│   ├── EEPROM/                CH341A ve TL866 programlayıcı sınıfları
│   ├── Emulator/              Ostrich emulator entegrasyonu
│   ├── OBD/                   Honda OBD1 seri bağlantı ve DTC yönetimi
│   └── Discovery/             Donanım keşif yardımcıları
├── Calibration/               Eski/yardımcı kalibrasyon modelleri
├── Database/                  ECU, telemetry, safety, patch ve AutoTune JSON verileri
├── Telemetry/                 Üst seviye telemetry manager
├── Tests/                     Tuning test harness ve sample ROM testleri
├── Tools/                     ROM üretim ve model sadeleştirme araçları
├── test_roms/                 Demo ve örnek ROM dosyaları
└── HondaTuner.csproj
```

## Test ROM Kullanımı

Temel örnek ROM:

```text
test_roms/p28_d16z6_stock.bin
```

Uygulama açıldığında bazı demo ROM dosyaları `bin/Debug/.../test_roms` altında otomatik üretilebilir. Geliştirme sırasında gerçek araçtan alınmış orijinal dosyalar yerine önce demo ROM dosyalarıyla deneme yapılması önerilir.

## ROM ve ECU Notları

- Varsayılan Honda OBD1 ROM boyutu: `32768` byte
- Genişletilmiş ROM boyutu: `65536` byte
- Honda OBD1 K-Line baud rate: `9600`
- Ortak sabitler: `Core/EcuConstants.cs`
- P28 temel offsetleri: `Core/P28Offsets.cs`

P28 için temel adresler:

| Alan | Offset | Boyut |
| --- | ---: | --- |
| Fuel Map | `0x1D40` | `16x16` |
| Ignition Map | `0x1E40` | `16x16` |
| VTEC RPM | `0x1F40` | `2 byte` |
| Rev Limit | `0x1FAA` | `2 byte` |
| Checksum | `0x7FFF` | `1 byte` |

## Donanım Durumu

Projede donanım sınıfları ve UI bağlantıları bulunur; ancak gerçek cihaz desteği kullanılan adaptöre, sürücüye, ROM çipine ve ECU kablolamasına bağlıdır.

| Modül | Durum |
| --- | --- |
| CH341A EEPROM programmer | Kod altyapısı mevcut, gerçek donanımla doğrulama gerekir |
| TL866 / minipro wrapper | Kod altyapısı mevcut, `minipro.exe` gerekir |
| Ostrich emulator | Bağlantı sınıfı mevcut, gerçek cihazla doğrulama gerekir |
| Honda OBD1 serial | Protokol denemesi mevcut, gerçek araç/ECU ile test gerekir |
| Datalog simulation | Uygulama içinde geliştirme/test amaçlı kullanılabilir |

## Güvenlik Uyarısı

Yanlış yakıt, ateşleme, rev limit, boost veya koruma ayarları motor hasarına yol açabilir. Gerçek ECU'ya yazma işlemlerinde:

- Orijinal ROM yedeğini saklayın.
- Değişiklikleri küçük adımlarla yapın.
- Wideband, knock ve sıcaklık verilerini izleyin.
- Şüpheli patch veya haritaları canlı araçta kullanmayın.
- EEPROM yazma ve silme işlemlerini doğru çip seçimiyle yapın.

## Geliştirme Notları

Build doğrulaması:

```powershell
dotnet build --nologo
```

Beklenen güncel hedef framework:

```xml
<TargetFramework>net8.0-windows</TargetFramework>
```

Repository içinde `bin/`, `obj/`, log dosyaları ve yerel `.dotnet/` klasörü geliştirme çıktısıdır. Yeni değişikliklerde mümkün olduğunca kaynak dosyaları, proje dosyası ve gerekli veritabanı/test varlıkları commit edilmelidir.

## Lisans ve Sorumluluk

Bu proje hobi, eğitim ve geliştirme amaçlıdır. Gerçek araç üzerinde yapılan her değişiklik kullanıcının kendi sorumluluğundadır.
