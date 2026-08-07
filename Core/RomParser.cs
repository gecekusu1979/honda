using System;
using System.IO;

namespace HondaTuner.Core
{
    public class RomParser
    {
        private byte[] _rom;
        private EcuProfile _profile;

        public string FilePath { get; private set; }
        public bool IsLoaded => _rom != null;
        public EcuProfile Profile => _profile;

        // ── Yükle ───────────────────────────────────────────────

        /// <summary>ROM'u yükle. Profil belirtilmezse P28 varsayılan.</summary>
        public void Load(string path, EcuProfile profile = null)
        {
            _profile = profile ?? EcuProfiles.P28;

            byte[] data = File.ReadAllBytes(path);

            if (data.Length != _profile.RomSize)
                throw new InvalidDataException(
                    $"Hatalı ROM boyutu: {data.Length} byte. " +
                    $"{_profile.EcuCode} = {_profile.RomSize} byte olmalı.");

            _rom = data;
            FilePath = path;

            // Checksum uyarı olarak göster, hata fırlatma
            // (Demo ROM'lar ve swap ECU'lar farklı checksum değeri taşıyabilir)
            if (!VerifyChecksum())
                System.Diagnostics.Debug.WriteLine(
                    $"[UYARI] Checksum doğrulanamadı: {_profile.Name}");
        }

        // ── Fuel Map ─────────────────────────────────────────────

        public byte[,] ReadFuelMap()
        {
            AssertLoaded();
            return ReadMap(_profile.FuelMapOffset,
                           _profile.FuelMapRows,
                           _profile.FuelMapCols);
        }

        public void WriteFuelMap(byte[,] map)
        {
            AssertLoaded();
            WriteMap(_profile.FuelMapOffset, map);
            UpdateChecksum();
        }

        // ── Ignition Map ─────────────────────────────────────────

        public byte[,] ReadIgnitionMap()
        {
            AssertLoaded();
            return ReadMap(_profile.IgnMapOffset,
                           _profile.IgnMapRows,
                           _profile.IgnMapCols);
        }

        public void WriteIgnitionMap(byte[,] map)
        {
            AssertLoaded();
            WriteMap(_profile.IgnMapOffset, map);
            UpdateChecksum();
        }

        // ── VTEC ─────────────────────────────────────────────────

        public int ReadVtecRpm()
        {
            AssertLoaded();
            if (!_profile.HasVtec)
                return 0;
            return (_rom[_profile.VtecRpmOffset] << 8)
                  | _rom[_profile.VtecRpmOffset + 1];
        }

        public void WriteVtecRpm(int rpm)
        {
            AssertLoaded();
            if (!_profile.HasVtec) return; // Non-VTEC ECU, yoksay

            if (rpm < _profile.VtecRpmMin || rpm > _profile.VtecRpmMax)
                throw new ArgumentOutOfRangeException(
                    nameof(rpm),
                    $"VTEC RPM {_profile.VtecRpmMin}-{_profile.VtecRpmMax} arasında olmalı.");

            _rom[_profile.VtecRpmOffset] = (byte)(rpm >> 8);
            _rom[_profile.VtecRpmOffset + 1] = (byte)(rpm & 0xFF);
            UpdateChecksum();
        }

        // ── Rev Limit ────────────────────────────────────────────

        public int ReadRevLimit()
        {
            AssertLoaded();
            return (_rom[_profile.RevLimitOffset] << 8)
                  | _rom[_profile.RevLimitOffset + 1];
        }

        public void WriteRevLimit(int rpm)
        {
            AssertLoaded();
            if (rpm < _profile.RevLimitMin || rpm > _profile.RevLimitMax)
                throw new ArgumentOutOfRangeException(
                    nameof(rpm),
                    $"Rev limit {_profile.RevLimitMin}-{_profile.RevLimitMax} arasında olmalı.");

            _rom[_profile.RevLimitOffset] = (byte)(rpm >> 8);
            _rom[_profile.RevLimitOffset + 1] = (byte)(rpm & 0xFF);
            UpdateChecksum();
        }

        // ── Speed Limiter ─────────────────────────────────────────

        /// <summary>Hız sınırını km/h olarak okur (Offset 0x1FAC, 2 byte).</summary>
        public int ReadSpeedLimiter()
        {
            AssertLoaded();
            int offset = 0x1FAC;
            if (offset + 1 >= _rom.Length) return 180;
            return (_rom[offset] << 8) | _rom[offset + 1];
        }

        /// <summary>Hız sınırını km/h olarak yazar.</summary>
        public void WriteSpeedLimiter(int kmh)
        {
            AssertLoaded();
            if (kmh < 50 || kmh > 300)
                throw new ArgumentOutOfRangeException(nameof(kmh), "Hız sınırı 50-300 km/h arasında olmalı.");
            int offset = 0x1FAC;
            _rom[offset] = (byte)(kmh >> 8);
            _rom[offset + 1] = (byte)(kmh & 0xFF);
            UpdateChecksum();
        }

        // ── VTEC Yük Eşiği ────────────────────────────────────────

        /// <summary>VTEC devreye giriş yük eşiğini kPa olarak okur (Offset 0x1F42, 2 byte).</summary>
        public int ReadVtecLoadThreshold()
        {
            AssertLoaded();
            if (!_profile.HasVtec) return 0;
            int offset = 0x1F42;
            if (offset + 1 >= _rom.Length) return 60;
            return (_rom[offset] << 8) | _rom[offset + 1];
        }

        /// <summary>VTEC devreye giriş yük eşiğini kPa olarak yazar.</summary>
        public void WriteVtecLoadThreshold(int kpa)
        {
            AssertLoaded();
            if (!_profile.HasVtec) return;
            if (kpa < 10 || kpa > 150)
                throw new ArgumentOutOfRangeException(nameof(kpa), "VTEC yük eşiği 10-150 kPa arasında olmalı.");
            int offset = 0x1F42;
            _rom[offset] = (byte)(kpa >> 8);
            _rom[offset + 1] = (byte)(kpa & 0xFF);
            UpdateChecksum();
        }

        // ── Enjektör Ölü Süresi ───────────────────────────────────

        /// <summary>Enjektör ölü süresini ms olarak okur (Offset 0x1D80, 1 byte, 0.05ms adım).</summary>
        public double ReadInjectorDeadTime()
        {
            AssertLoaded();
            return _rom[0x1D80] * 0.05;
        }

        /// <summary>Enjektör ölü süresini ms olarak yazar (0.05ms adım çözünürlüğü).</summary>
        public void WriteInjectorDeadTime(double ms)
        {
            AssertLoaded();
            int rawVal = (int)Math.Round(ms / 0.05);
            if (rawVal < 0 || rawVal > 255)
                throw new ArgumentOutOfRangeException(nameof(ms), "Enjektör ölü süresi 0-12.75 ms arasında olmalı.");
            _rom[0x1D80] = (byte)rawVal;
            UpdateChecksum();
        }

        // ── Kaydet ───────────────────────────────────────────────

        public void Save() => Save(FilePath);

        public void Save(string path)
        {
            AssertLoaded();
            File.WriteAllBytes(path, _rom);
        }

        public void SaveAs(string path)
        {
            AssertLoaded();
            File.WriteAllBytes(path, _rom);
            FilePath = path;
        }

        // ── Ham Erişim (ileri seviye) ────────────────────────────

        public byte ReadByte(int offset) => _rom[offset];
        public void WriteByte(int offset, byte value)
        {
            _rom[offset] = value;
            UpdateChecksum();
        }

        public byte[] GetRomBuffer() => _rom != null ? (byte[])_rom.Clone() : null;
        public void SetRomBuffer(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            _rom = (byte[])buffer.Clone();
            if (_profile == null)
            {
                _profile = EcuProfiles.P28;
            }
            UpdateChecksum();
        }

        // ── Checksum ─────────────────────────────────────────────

        public bool VerifyChecksum()
        {
            if (_rom == null || _profile == null) return true;

            try
            {
                var checksumEngine = Container.ServiceContainer.Resolve<Rom.Checksum.IChecksumEngine>();
                if (checksumEngine != null && _profile.ChecksumDefinitions != null && _profile.ChecksumDefinitions.Count > 0)
                {
                    return checksumEngine.VerifyBeforeSave(_rom, _profile.ChecksumDefinitions, out _);
                }
            }
            catch
            {
                // Fallback to legacy
            }

            // Legacy Fallback
            byte xor = 0;
            for (int i = 0; i < _rom.Length - 1; i++)
                xor ^= _rom[i];

            if (_profile.ChecksumOffset >= 0 && _profile.ChecksumOffset < _rom.Length)
            {
                return xor == _rom[_profile.ChecksumOffset];
            }
            return true;
        }

        private void UpdateChecksum()
        {
            if (_rom == null || _profile == null) return;

            try
            {
                var checksumEngine = Container.ServiceContainer.Resolve<Rom.Checksum.IChecksumEngine>();
                if (checksumEngine != null && _profile.ChecksumDefinitions != null && _profile.ChecksumDefinitions.Count > 0)
                {
                    foreach (var def in _profile.ChecksumDefinitions)
                    {
                        checksumEngine.Update(_rom, def);
                    }
                    return;
                }
            }
            catch
            {
                // Fallback to legacy
            }

            // Legacy Fallback
            byte xor = 0;
            for (int i = 0; i < _rom.Length - 1; i++)
                xor ^= _rom[i];

            if (_profile.ChecksumOffset >= 0 && _profile.ChecksumOffset < _rom.Length)
            {
                _rom[_profile.ChecksumOffset] = xor;
            }
        }

        // ── Yardımcılar ──────────────────────────────────────────

        private byte[,] ReadMap(int offset, int rows, int cols)
        {
            var map = new byte[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    map[r, c] = _rom[offset + (r * cols) + c];
            return map;
        }

        private void WriteMap(int offset, byte[,] map)
        {
            int rows = map.GetLength(0);
            int cols = map.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    _rom[offset + (r * cols) + c] = map[r, c];
        }

        // ── Launch Control ───────────────
        public bool ReadLaunchControlActive()
        {
            AssertLoaded();
            if (0x1FB0 >= _rom.Length) return false;
            return _rom[0x1FB0] == 1;
        }
        public void WriteLaunchControlActive(bool active)
        {
            AssertLoaded();
            _rom[0x1FB0] = (byte)(active ? 1 : 0);
            UpdateChecksum();
        }

        public int ReadLaunchControlRpm()
        {
            AssertLoaded();
            if (0x1FB2 + 1 >= _rom.Length) return 3500;
            int v = (_rom[0x1FB2] << 8) | _rom[0x1FB2 + 1];
            return v == 0 ? 3500 : v;
        }
        public void WriteLaunchControlRpm(int rpm)
        {
            AssertLoaded();
            _rom[0x1FB2] = (byte)(rpm >> 8);
            _rom[0x1FB2 + 1] = (byte)(rpm & 0xFF);
            UpdateChecksum();
        }

        public int ReadLaunchControlSpeed()
        {
            AssertLoaded();
            if (0x1FB4 + 1 >= _rom.Length) return 8;
            return (_rom[0x1FB4] << 8) | _rom[0x1FB4 + 1];
        }
        public void WriteLaunchControlSpeed(int speed)
        {
            AssertLoaded();
            _rom[0x1FB4] = (byte)(speed >> 8);
            _rom[0x1FB4 + 1] = (byte)(speed & 0xFF);
            UpdateChecksum();
        }

        // ── DTC Bypass ───────────────
        public bool ReadDtcBypass(int offset)
        {
            AssertLoaded();
            if (offset >= _rom.Length) return false;
            return _rom[offset] == 1;
        }
        public void WriteDtcBypass(int offset, bool bypass)
        {
            AssertLoaded();
            _rom[offset] = (byte)(bypass ? 1 : 0);
            UpdateChecksum();
        }

        private void AssertLoaded()
        {
            if (!IsLoaded)
                throw new InvalidOperationException(
                    "Önce bir ROM dosyası yükleyin.");
        }
    }
}
