# HondaTuner V2

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
* **Canlı Telemetri:** Datalog manager, seri port tabanlı Honda OBD1 bağlantısı ve DTC (Arıza Kodu) okuma/temizleme katmanı.
* **Donanım Desteği:** CH341A / TL866 (minipro) EEPROM programlayıcı wrapper sınıfları ve Moates Ostrich emülatör entegrasyonu.
* **Motor Güvenliği:** Engine Protection, Diagnostics A2L, VTEC/Boost, Advanced Fuel ve Gelişmiş Ateşleme simülatörleri.
* **Dahili Test Altyapısı:** Dahili test harness ve sentetik ROM doğrulama suite'i (115+ unit test).

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
