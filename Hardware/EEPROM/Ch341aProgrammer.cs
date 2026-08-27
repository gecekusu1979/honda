using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

/*
 * CH341A USB EEPROM Programmer
 * Supports: SST27SF512, 27C256, 29C256, AM29F010 (common Honda OBD1 ECU chips)
 *
 * Implementation Strategy:
 *   A) If ch341a.dll is present in app directory → P/Invoke native USB
 *   B) Fallback: uses minipro.exe CLI (open-source TL866/CH341A tool)
 *
 * minipro: https://gitlab.com/DavidGriffith/minipro
 * ch341a.dll: vendor-supplied, or from ch341a-software package
 *
 * REQUIRES REAL CH341A HARDWARE + DRIVER FOR LIVE OPERATION.
 */

using HondaTuner.Core;

namespace HondaTuner.Hardware.EEPROM
{
    public class Ch341aProgrammer : IEepromProgrammer
    {
        private const string DllName = "ch341a.dll";
        private const string Minipro = "minipro.exe";
        private const int DefaultRomSize = EcuConstants.DefaultRomSize; // 32KB Honda P28/P30

        public string DeviceName => "CH341A USB EEPROM Programmer";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;
        public event EventHandler<int> ProgressChanged;   // 0–100
        public event EventHandler<string> OperationCompleted;

        private bool _useDll;
        private int _deviceIndex;
        private string _chipType = "SST27SF512"; // default Honda OBD1 chip

        public string ChipType
        {
            get => _chipType;
            set => _chipType = value ?? "SST27SF512";
        }

        // ── P/Invoke declarations for ch341a.dll ────────────────────────
        [System.Runtime.InteropServices.DllImport("ch341a.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, EntryPoint = "CH341OpenDevice")]
        private static extern int CH341OpenDevice(int index);

        [System.Runtime.InteropServices.DllImport("ch341a.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, EntryPoint = "CH341CloseDevice")]
        private static extern void CH341CloseDevice(int index);

        [System.Runtime.InteropServices.DllImport("ch341a.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, EntryPoint = "CH341ReadEEPROM")]
        private static extern int CH341ReadEEPROM(int index, byte[] buffer, int length);

        [System.Runtime.InteropServices.DllImport("ch341a.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, EntryPoint = "CH341WriteEEPROM")]
        private static extern int CH341WriteEEPROM(int index, byte[] buffer, int length);

        // ── IHardwareDevice ─────────────────────────────────────────────

        public void Connect()
        {
            SetState(ConnectionState.Connecting, "CH341A aranıyor...");
            ApplicationLogger.Info("Ch341aProgrammer", "CH341A bağlantısı başlatılıyor...");

            try
            {
                // Prefer native DLL first
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllName);
                if (File.Exists(dllPath))
                {
                    ApplicationLogger.Info("Ch341aProgrammer", $"ch341a.dll bulundu: {dllPath}");
                    int result = CH341OpenDevice(0);
                    if (result >= 0)
                    {
                        _useDll = true;
                        _deviceIndex = 0;
                        SetState(ConnectionState.Connected, "CH341A (DLL) bağlandı.");
                        ApplicationLogger.Info("Ch341aProgrammer", "CH341A DLL bağlantısı başarılı.");
                        return;
                    }
                    ApplicationLogger.Warn("Ch341aProgrammer", "DLL bağlantısı başarısız, minipro.exe deneniyor...");
                }

                // Fallback: minipro CLI
                string miniproPath = LocateMinipro();
                if (!string.IsNullOrEmpty(miniproPath))
                {
                    ApplicationLogger.Info("Ch341aProgrammer", $"minipro.exe bulundu: {miniproPath}");
                    _useDll = false;
                    SetState(ConnectionState.Connected, "CH341A (minipro CLI) bağlandı.");
                    OperationCompleted?.Invoke(this, $"minipro.exe kullanılıyor: {miniproPath}");
                    return;
                }

                throw new InvalidOperationException(
                    "CH341A sürücüsü (ch341a.dll) veya minipro.exe bulunamadı. " +
                    "Lütfen ch341a.dll dosyasını uygulama klasörüne koyun veya minipro aracını kurun.");
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("Ch341aProgrammer", $"Bağlantı hatası: {ex.Message}");
                SetState(ConnectionState.Error, ex.Message);
                throw;
            }
        }

        public void Disconnect()
        {
            if (_useDll && State == ConnectionState.Connected)
            {
                try { CH341CloseDevice(_deviceIndex); } catch { }
            }
            SetState(ConnectionState.Disconnected, "CH341A bağlantısı kesildi.");
            ApplicationLogger.Info("Ch341aProgrammer", "Bağlantı kapatıldı.");
        }

        // ── IEepromProgrammer ───────────────────────────────────────────

        public byte[] ReadChip(int romLength)
        {
            EnsureConnected();
            if (romLength <= 0) romLength = DefaultRomSize;

            ApplicationLogger.Info("Ch341aProgrammer", $"Chip okunuyor: {romLength} byte, Chip: {_chipType}");
            ReportProgress(0);

            try
            {
                if (_useDll)
                    return ReadViaDll(romLength);
                else
                    return ReadViaCli(romLength);
            }
            finally
            {
                ReportProgress(100);
            }
        }

        public void WriteChip(byte[] romData)
        {
            if (romData == null || romData.Length == 0) throw new ArgumentNullException(nameof(romData));
            EnsureConnected();

            // Safety: validate ROM size
            if (romData.Length != DefaultRomSize && romData.Length != EcuConstants.ExtendedRomSize)
                throw new InvalidOperationException($"Geçersiz ROM boyutu: {romData.Length} byte. Honda ECU: {EcuConstants.DefaultRomSize} ya da {EcuConstants.ExtendedRomSize} byte olmalı.");

            // Auto-backup before write
            CreateBackup(romData);

            ApplicationLogger.Info("Ch341aProgrammer", $"Chip yazılıyor: {romData.Length} byte, Chip: {_chipType}");
            ReportProgress(0);

            try
            {
                if (_useDll)
                    WriteViaDll(romData);
                else
                    WriteViaCli(romData);
            }
            finally
            {
                ReportProgress(100);
            }

            ApplicationLogger.Info("Ch341aProgrammer", "Chip yazma tamamlandı.");
            OperationCompleted?.Invoke(this, "Yazma başarılı.");
        }

        public void EraseChip()
        {
            EnsureConnected();
            ApplicationLogger.Info("Ch341aProgrammer", $"Chip siliniyor: {_chipType}");
            ReportProgress(0);

            try
            {
                if (_useDll)
                    EraseViaDll();
                else
                    EraseViaCli();
            }
            finally
            {
                ReportProgress(100);
            }

            ApplicationLogger.Info("Ch341aProgrammer", "Chip silme tamamlandı.");
            OperationCompleted?.Invoke(this, "Silme başarılı.");
        }

        public bool VerifyChip(byte[] expectedData)
        {
            if (expectedData == null) throw new ArgumentNullException(nameof(expectedData));
            EnsureConnected();

            ApplicationLogger.Info("Ch341aProgrammer", "Chip doğrulanıyor...");
            byte[] actual = ReadChip(expectedData.Length);

            for (int i = 0; i < expectedData.Length; i++)
            {
                if (actual[i] != expectedData[i])
                {
                    ApplicationLogger.Error("Ch341aProgrammer",
                        $"Doğrulama hatası → offset 0x{i:X4}: beklenen=0x{expectedData[i]:X2}, okunan=0x{actual[i]:X2}");
                    OperationCompleted?.Invoke(this, $"Doğrulama BAŞARISIZ @ 0x{i:X4}");
                    return false;
                }

                if (i % 1024 == 0)
                    ReportProgress((int)((double)i / expectedData.Length * 100));
            }

            ApplicationLogger.Info("Ch341aProgrammer", "Doğrulama başarılı — tüm baytlar eşleşiyor.");
            OperationCompleted?.Invoke(this, "Doğrulama başarılı.");
            return true;
        }

        // ── DLL implementation ──────────────────────────────────────────

        private byte[] ReadViaDll(int length)
        {
            var buffer = new byte[length];
            int result = CH341ReadEEPROM(_deviceIndex, buffer, length);
            if (result < 0)
                throw new InvalidOperationException($"CH341A DLL okuma başarısız: {result}");
            return buffer;
        }

        private void WriteViaDll(byte[] data)
        {
            int result = CH341WriteEEPROM(_deviceIndex, data, data.Length);
            if (result < 0)
                throw new InvalidOperationException($"CH341A DLL yazma başarısız: {result}");
        }

        private void EraseViaDll()
        {
            // Erase by writing 0xFF everywhere (NOR flash erased state)
            var erased = new byte[DefaultRomSize];
            for (int i = 0; i < erased.Length; i++) erased[i] = 0xFF;
            WriteViaDll(erased);
        }

        // ── minipro CLI implementation ──────────────────────────────────

        private byte[] ReadViaCli(int length)
        {
            string outPath = Path.GetTempFileName();
            try
            {
                RunMinipro($"-p \"{_chipType}\" -r \"{outPath}\"", "Chip okunuyor...");
                if (!File.Exists(outPath))
                    throw new InvalidOperationException("minipro chip okuma çıktısı üretilmedi.");
                byte[] data = File.ReadAllBytes(outPath);
                if (data.Length < length)
                    throw new InvalidOperationException($"Chip boyutu {data.Length} byte — beklenen {length} byte.");
                // Trim or pad to exact length
                if (data.Length > length)
                    Array.Resize(ref data, length);
                return data;
            }
            finally
            {
                try { if (File.Exists(outPath)) File.Delete(outPath); } catch { }
            }
        }

        private void WriteViaCli(byte[] data)
        {
            string tmpPath = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tmpPath, data);
                RunMinipro($"-p \"{_chipType}\" -w \"{tmpPath}\"", "Chip yazılıyor...");
            }
            finally
            {
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            }
        }

        private void EraseViaCli()
        {
            RunMinipro($"-p \"{_chipType}\" -E", "Chip siliniyor...");
        }

        private void RunMinipro(string args, string opDesc)
        {
            string miniproPath = LocateMinipro();
            if (string.IsNullOrEmpty(miniproPath))
                throw new InvalidOperationException("minipro.exe bulunamadı.");

            ApplicationLogger.Info("Ch341aProgrammer", $"minipro çalıştırılıyor: {opDesc}");

            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = miniproPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(60_000);

            if (proc.ExitCode != 0)
            {
                string msg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                throw new InvalidOperationException($"minipro hatası ({proc.ExitCode}): {msg.Trim()}");
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static string LocateMinipro()
        {
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Minipro);
            if (File.Exists(local)) return local;

            // Check common install paths
            foreach (var dir in new[] { @"C:\Program Files\minipro", @"C:\minipro", @"C:\Tools" })
            {
                string p = Path.Combine(dir, Minipro);
                if (File.Exists(p)) return p;
            }

            return null;
        }

        private static void CreateBackup(byte[] romData)
        {
            try
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "HondaTuner", "Backups");
                Directory.CreateDirectory(backupDir);
                string fileName = $"chip_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                File.WriteAllBytes(Path.Combine(backupDir, fileName), romData);
                ApplicationLogger.Info("Ch341aProgrammer", $"Yedek oluşturuldu: {fileName}");
            }
            catch (Exception ex)
            {
                ApplicationLogger.Warn("Ch341aProgrammer", $"Yedek oluşturma hatası: {ex.Message}");
            }
        }

        private void EnsureConnected()
        {
            if (State != ConnectionState.Connected)
                throw new InvalidOperationException("CH341A bağlı değil. Önce Connect() çağrın.");
        }

        private void ReportProgress(int percent)
            => ProgressChanged?.Invoke(this, Math.Min(100, Math.Max(0, percent)));

        private void SetState(ConnectionState newState, string message)
        {
            var old = State;
            State = newState;
            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                OldState = old,
                NewState = newState,
                Message = message
            });
        }
    }
}
