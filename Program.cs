using System;
using System.IO;
using System.Windows.Forms;
using HondaTuner.Core;
using HondaTuner.Tools;
using HondaTuner.UI;

namespace HondaTuner
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Loglamayı başlat
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Core.Logging.ApplicationLogger.Initialize(logDir);

            // Testleri çalıştır ve sakla
            string testLogs = Tests.TuningTestHarness.RunAllTests();
            string sampleRomLogs = Tests.SampleRomTestFramework.RunAllTests();

            Core.Logging.ApplicationLogger.Info("Program", "Test sonuçları:\n" + testLogs);
            Core.Logging.ApplicationLogger.Info("Program", "Örnek ROM test sonuçları:\n" + sampleRomLogs);

            if (args != null && args.Length > 0 && (args[0] == "--test-only" || args[0] == "-t"))
            {
                Console.WriteLine("=== TUNING TEST HARNESS RESULTS ===");
                Console.WriteLine(testLogs);
                Console.WriteLine(sampleRomLogs);
                string testOutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_results.txt");
                File.WriteAllText(testOutPath, testLogs + Environment.NewLine + sampleRomLogs);
                Console.WriteLine($"Test results saved to: {testOutPath}");
                Environment.Exit(0);
                return;
            }

            // ROM demo dosyaları yoksa üret
            EnsureRomFiles();

            Application.Run(new MainForm());
        }

        /// <summary>
        /// test_roms klasörüne her ECU için bir demo .bin üretir.
        /// Zaten varsa atlar.
        /// </summary>
        private static void EnsureRomFiles()
        {
            try
            {
                string dir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "test_roms");
                Directory.CreateDirectory(dir);

                // ECU profillerini ve benzersiz çarpanları tanımla
                var configs = new (EcuProfile Profile, string FileName, float Fuel, float Ign)[]
                {
                    (EcuProfiles.P05, "p05_d15z1_civic_cx_stock.bin",  0.78f, 0.82f),
                    (EcuProfiles.P06, "p06_d15b7_civic_dx_stock.bin",  0.85f, 0.88f),
                    (EcuProfiles.P28, "p28_d16z6_civic_ex_stock.bin",  1.00f, 1.00f),
                    (EcuProfiles.P30, "p30_d15b2_civic_15i_stock.bin", 0.88f, 0.90f),
                    (EcuProfiles.P61, "p61_b17a1_integra_gsr_stock.bin",1.10f,1.12f),
                    (EcuProfiles.P72, "p72_b18c1_integra_gsr_stock.bin",1.15f,1.18f),
                    (EcuProfiles.P74, "p74_b18b1_integra_ls_stock.bin", 1.05f,1.05f),
                    (EcuProfiles.P13, "p13_h22a_prelude_vtec_stock.bin",1.20f,1.10f),
                };

                foreach (var c in configs)
                {
                    string path = Path.Combine(dir, c.FileName);
                    if (!File.Exists(path))
                        RomGenerator.SaveToFile(c.Profile, path, c.Fuel, c.Ign);
                }
            }
            catch
            {
                // ROM üretimi kritik değil; sessizce atla
            }
        }
    }
}
