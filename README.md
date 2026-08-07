# HondaTuner — P28 / D16Z6 ECU ROM Editörü

Honda EG Civic, D16Z6 motoru için P28-A01 ECU yazılım editörü.

## Gereksinimler

- Windows 10/11
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Visual Studio 2022 (önerilen) veya VS Code + C# eklentisi

## Başlatma

```bash
cd HondaTuner
dotnet run
```

## Test ROM

`test_roms/p28_d16z6_stock.bin` → D16Z6 stock değerlerine yakın 32KB test ROM'u.
Gerçek araba olmadan editörü test etmek için kullan.

Dosya → Aç → `test_roms/p28_d16z6_stock.bin` seç.

## Dosya Yapısı

```
HondaTuner/
├── Core/
│   ├── P28Offsets.cs       ← Tüm ROM adresleri
│   └── RomParser.cs        ← ROM okuma/yazma/checksum
├── UI/
│   ├── MapGridControl.cs   ← Renkli 16x16 harita editörü
│   ├── DiffView.cs         ← Stock vs Modified diff paneli
│   └── MainForm.cs         ← Ana pencere
├── Tools/
│   └── FakeRomGenerator.cs ← C# ile test ROM üretici
├── test_roms/
│   └── p28_d16z6_stock.bin ← Hazır test ROM'u
└── HondaTuner.csproj
```

## Özellikler

- ✅ P28 ROM aç / kaydet
- ✅ Checksum doğrulama (XOR)
- ✅ Fuel Map editörü (ısı haritası renklendirme)
- ✅ Ignition Map editörü
- ✅ VTEC devreye giriş RPM ayarı
- ✅ Rev limit ayarı
- ✅ Diff görünümü (Stock vs Modified, 3 panel)
- ⬜ Datalog (serial port)
- ⬜ Wideband AFR entegrasyonu
- ⬜ Chip burner (Moates/FTDI)

## P28 ROM Harita Koordinatları

| Harita | Offset | Boyut |
|--------|--------|-------|
| Fuel Map | 0x1D40 | 16x16 |
| Ignition Map | 0x1E40 | 16x16 |
| VTEC RPM | 0x1F40 | 2 byte |
| Rev Limit | 0x1FAA | 2 byte |
| Checksum | 0x7FFF | 1 byte |

## Notlar

- ROM boyutu kesinlikle 32768 byte (32KB) olmalı
- Checksum = tüm byte'ların XOR'u (son byte hariç)
- Gerçek ROM ECU'dan chip burner ile okunur
