using System;
using System.Diagnostics;
using System.IO;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

/*
 * TL866II Plus Programmer — minipro.exe CLI wrapper
 * minipro: https://gitlab.com/DavidGriffith/minipro (open-source, Windows binary available)
 *
 * Usage: minipro -p "SST27SF512" -r output.bin   (read)
 *        minipro -p "SST27SF512" -w input.bin    (erase + write)
 *        minipro -p "SST27SF512" -E              (erase)
 *
 * For CH341A-based programmers, prefer Ch341aProgrammer.cs which uses
 * the native ch341a.dll for direct USB access without CLI dependency.
 *
 * REQUIRES minipro.exe + TL866II/NUSPI programmer connected via USB.
 */

using HondaTuner.Core;

namespace HondaTuner.Hardware.EEPROM
{
    public class Tl866Programmer : IEepromProgrammer
    {
        private const string MiniproExe = "minipro.exe";
        private const int DefaultSize = EcuConstants.DefaultRomSize;
        private const int CliTimeout = 120_000; // ms

        public string DeviceName => "TL866II Plus Programmer (minipro)";
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;
        public event EventHandler<int> ProgressChanged;
        public event EventHandler<string> OperationCompleted;

        private string _miniproPath;
        private string _chipType = "SST27SF512"; // default Honda OBD1 EEPROM

        public string ChipType
        {
            get => _chipType;
            set => _chipType = value ?? "SST27SF512";
        }

        public void Connect()
        {
            SetState(ConnectionState.Connecting, "TL866 aranıyor...");
            ApplicationLogger.Info("Tl866Programmer", "TL866 programlayıcısı aranıyor...");

            _miniproPath = LocateMinipro();
            if (string.IsNullOrEmpty(_miniproPath))
            {
                string msg = "minipro.exe bulunamadı. Lütfen uygulama klasörüne ya da PATH'e ekleyin. " +
                             "İndir: https://gitlab.com/DavidGriffith/minipro";
                SetState(ConnectionState.Error, msg);
                ApplicationLogger.Error("Tl866Programmer", msg);
                throw new InvalidOperationException(msg);
            }

            // Verify minipro responds (list devices)
            try
            {
                RunMinipro("--version", silentFail: false);
            }
            catch (Exception ex)
            {
                string msg = $"minipro.exe test başarısız: {ex.Message}";
                SetState(ConnectionState.Error, msg);
                throw new InvalidOperationException(msg);
            }

            SetState(ConnectionState.Connected, "TL866 hazır (minipro).");
            ApplicationLogger.Info("Tl866Programmer", $"minipro bulundu: {_miniproPath}");
        }

        public void Disconnect()
        {
            SetState(ConnectionState.Disconnected, "Programlayıcı bağlantısı kesildi.");
            ApplicationLogger.Info("Tl866Programmer", "Bağlantı kapatıldı.");
        }

        public byte[] ReadChip(int romLength)
        {
            EnsureConnected();
            if (romLength <= 0) romLength = DefaultSize;

            ApplicationLogger.Info("Tl866Programmer", $"Chip okunuyor: {_chipType}, {romLength} byte");
            ReportProgress(10);

            string outPath = Path.GetTempFileName();
            try
            {
                RunMinipro($"-p \"{_chipType}\" -r \"{outPath}\"");
                ReportProgress(80);

                if (!File.Exists(outPath))
                    throw new InvalidOperationException("minipro chip okuma çıktısı üretilmedi.");

                byte[] data = File.ReadAllBytes(outPath);
                if (data.Length < romLength)
                    throw new InvalidOperationException($"Chip boyutu küçük: {data.Length} < {romLength}");
                if (data.Length > romLength)
                    Array.Resize(ref data, romLength);

                ReportProgress(100);
                ApplicationLogger.Info("Tl866Programmer", $"Chip okuma başarılı: {data.Length} byte");
                OperationCompleted?.Invoke(this, $"Okuma başarılı ({data.Length} byte)");
                return data;
            }
            finally
            {
                TryDelete(outPath);
            }
        }

        public void EraseChip()
        {
            EnsureConnected();
            ApplicationLogger.Info("Tl866Programmer", $"Chip siliniyor: {_chipType}");
            ReportProgress(0);
            RunMinipro($"-p \"{_chipType}\" -E");
            ReportProgress(100);
            ApplicationLogger.Info("Tl866Programmer", "Chip silme tamamlandı.");
            OperationCompleted?.Invoke(this, "Silme başarılı.");
        }

        public void WriteChip(byte[] romData)
        {
            if (romData == null || romData.Length == 0) throw new ArgumentNullException(nameof(romData));
            EnsureConnected();

            // Safety: ROM size check
            if (romData.Length != DefaultSize && romData.Length != EcuConstants.ExtendedRomSize)
                throw new InvalidOperationException($"Geçersiz ROM boyutu: {romData.Length} byte.");

            // Auto-backup before write
            CreateBackup(romData);

            string tmpPath = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tmpPath, romData);
                ApplicationLogger.Info("Tl866Programmer", $"Chip yazılıyor: {_chipType}, {romData.Length} byte");
                ReportProgress(10);

                // minipro -w performs erase + write + internal verify
                RunMinipro($"-p \"{_chipType}\" -w \"{tmpPath}\"");
                ReportProgress(100);

                ApplicationLogger.Info("Tl866Programmer", "Chip yazma tamamlandı.");
                OperationCompleted?.Invoke(this, "Yazma başarılı.");
            }
            finally
            {
                TryDelete(tmpPath);
            }
        }

        public bool VerifyChip(byte[] expectedData)
        {
            if (expectedData == null) throw new ArgumentNullException(nameof(expectedData));
            EnsureConnected();

            ApplicationLogger.Info("Tl866Programmer", "Chip doğrulanıyor...");
            byte[] actual = ReadChip(expectedData.Length);

            for (int i = 0; i < expectedData.Length; i++)
            {
                if (actual[i] != expectedData[i])
                {
                    ApplicationLogger.Error("Tl866Programmer",
                        $"Doğrulama hatası @ 0x{i:X4}: beklenen=0x{expectedData[i]:X2}, okunan=0x{actual[i]:X2}");
                    OperationCompleted?.Invoke(this, $"Doğrulama BAŞARISIZ @ 0x{i:X4}");
                    return false;
                }
                if (i % 1024 == 0)
                    ReportProgress((int)((double)i / expectedData.Length * 100));
            }

            ReportProgress(100);
            ApplicationLogger.Info("Tl866Programmer", "Doğrulama başarılı.");
            OperationCompleted?.Invoke(this, "Doğrulama başarılı.");
            return true;
        }

        // ── Private Helpers ─────────────────────────────────────────────

        private void RunMinipro(string args, bool silentFail = false)
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _miniproPath,
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
            proc.WaitForExit(CliTimeout);

            if (proc.ExitCode != 0 && !silentFail)
            {
                string error = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                throw new InvalidOperationException($"minipro hata ({proc.ExitCode}): {error.Trim()}");
            }
        }

        private static string LocateMinipro()
        {
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MiniproExe);
            if (File.Exists(local)) return local;
            foreach (var dir in new[] { @"C:\Program Files\minipro", @"C:\minipro", @"C:\Tools" })
            {
                string p = Path.Combine(dir, MiniproExe);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static void CreateBackup(byte[] data)
        {
            try
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "HondaTuner", "Backups");
                Directory.CreateDirectory(backupDir);
                string fn = $"tl866_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                File.WriteAllBytes(Path.Combine(backupDir, fn), data);
                ApplicationLogger.Info("Tl866Programmer", $"Yedek oluşturuldu: {fn}");
            }
            catch (Exception ex)
            {
                ApplicationLogger.Warn("Tl866Programmer", $"Yedek hatası: {ex.Message}");
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch (Exception ex) { Debug.WriteLine($"[Tl866Programmer] Geçici dosya silinemedi ({path}): {ex.Message}"); }
        }

        private void EnsureConnected()
        {
            if (State != ConnectionState.Connected)
                throw new InvalidOperationException("Programlayıcı bağlı değil. Önce Connect() çağrın.");
        }

        private void ReportProgress(int pct)
            => ProgressChanged?.Invoke(this, Math.Max(0, Math.Min(100, pct)));

        private void SetState(ConnectionState newState, string msg)
        {
            var old = State;
            State = newState;
            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            { OldState = old, NewState = newState, Message = msg });
        }
    }
}
