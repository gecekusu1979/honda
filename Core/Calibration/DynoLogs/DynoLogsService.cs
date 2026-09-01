using System;
using System.Collections.Generic;

namespace HondaTuner.Calibration.DynoLogs
{
    public class DynoDataPoint
    {
        public double Rpm { get; set; }
        public double Whp { get; set; }
        public double EngineHp { get; set; }
        public double TorqueNm { get; set; }
    }

    public class PerformanceTimer
    {
        public double Time0To100 { get; set; } // Saniye
        public double Time100To200 { get; set; } // Saniye
        public double ShiftGapMs { get; set; } // Milisaniye
    }

    public class DynoLogsService
    {
        public DynoLogsTables Tables { get; } = new DynoLogsTables();
        public List<DynoDataPoint> CurrentDynoPoints { get; } = new List<DynoDataPoint>();
        public List<string> GitMergeHistory { get; } = new List<string>();
        public string ActiveBranch { get; private set; } = "main";

        private Random _rand = new Random();

        // 1. SAE J1349 / DIN 70020 Çarpan Düzeltme Katsayıları
        public double CalculateCorrectionMultiplier(double intakeTempCelsius, double baroKpa)
        {
            if (baroKpa < 50.0) baroKpa = 101.3; // Güvenli varsayılan değer

            if (Tables.CorrectionFactorType == "SAE")
            {
                // SAE J1349 standardı
                double cf = 1.18 * ((99.0 / baroKpa) * Math.Sqrt((intakeTempCelsius + 273.15) / 298.15)) - 0.18;
                return Math.Clamp(cf, 0.8, 1.2);
            }
            else if (Tables.CorrectionFactorType == "DIN")
            {
                // DIN 70020 standardı
                double cf = (101.3 / baroKpa) * Math.Sqrt((intakeTempCelsius + 273.15) / 293.15);
                return Math.Clamp(cf, 0.8, 1.25);
            }
            return 1.0;
        }

        // 2. Sanal Dyno Eğrisi Koşturucu
        public void RunVirtualDynoSim(double peakBoostKpa, double tempC = 25.0, double baroKpa = 101.3)
        {
            CurrentDynoPoints.Clear();

            double cf = CalculateCorrectionMultiplier(tempC, baroKpa);
            double boostMultiplier = 1.0 + ((peakBoostKpa - 100.0) / 100.0) * 0.95; // Boost gücü artırır

            for (double rpm = 2000; rpm <= 8000; rpm += 500)
            {
                // Standart VTEC Honda B16/D16 tork eğrisi benzetimi
                double baseTorque = 120.0;
                if (rpm >= 5500)
                {
                    // VTEC sonrası tork artışı
                    baseTorque = 150.0 + (rpm - 5500) * 0.005;
                }
                else
                {
                    baseTorque = 110.0 + (rpm - 2000) * 0.012;
                }

                double correctedTorque = baseTorque * boostMultiplier * cf;
                double whp = (correctedTorque * rpm) / 7127.0; // Tekerlek gücü formülü
                double engineHp = whp / (1.0 - (Tables.DrivetrainLossPct / 100.0)); // Krank gücü hesabı

                CurrentDynoPoints.Add(new DynoDataPoint
                {
                    Rpm = rpm,
                    Whp = Math.Round(whp, 1),
                    EngineHp = Math.Round(engineHp, 1),
                    TorqueNm = Math.Round(correctedTorque, 1)
                });
            }
        }

        // 3. Yol Sürüş Performans Analizleri
        public PerformanceTimer EstimatePerformanceTimes(double peakBoostKpa)
        {
            double boostFactor = (peakBoostKpa - 100.0) / 100.0; // 0.0 - 1.5

            double t0to100 = 7.4 - boostFactor * 2.2; // Yüksek basınçta daha hızlı ivmelenir
            double t100to200 = 18.5 - boostFactor * 8.0;
            double shiftGap = 280.0 - boostFactor * 60.0; // Profesyonel geçişler daha hızlıdır

            return new PerformanceTimer
            {
                Time0To100 = Math.Round(Math.Max(3.8, t0to100), 2),
                Time100To200 = Math.Round(Math.Max(7.0, t100to200), 2),
                ShiftGapMs = Math.Round(Math.Max(120.0, shiftGap), 1)
            };
        }

        // 4. Git-Style Dallanma & Commit-Merge Yönetimi
        public void CreateBranch(string name)
        {
            ActiveBranch = name;
            GitMergeHistory.Add($"[{DateTime.Now:HH:mm:ss}] " + string.Format(HondaTuner.Core.Localization.L.Get("branch_created"), name));
        }

        public void CommitChange(string msg)
        {
            string hash = _rand.Next(0x100000, 0xFFFFFF).ToString("X6");
            GitMergeHistory.Add($"[{DateTime.Now:HH:mm:ss}] " + string.Format(HondaTuner.Core.Localization.L.Get("commit_msg"), hash, ActiveBranch, msg));
        }

        public void MergeBranch(string source, string target)
        {
            GitMergeHistory.Add($"[{DateTime.Now:HH:mm:ss}] " + string.Format(HondaTuner.Core.Localization.L.Get("branch_merged"), source, target));
            ActiveBranch = target;
        }

        // 5. MCU / RAM İzleme Alanı Değer Simülasyonu
        public Dictionary<string, string> GetWatchdogValues(double simTime)
        {
            var res = new Dictionary<string, string>();

            // Değerlerin canlı dalgalanması simülasyonu
            bool vtec = simTime % 10.0 > 5.0;
            double afr = 14.7 + Math.Sin(simTime) * 0.4;
            if (vtec) afr = 12.5 + Math.Sin(simTime) * 0.15;

            double advance = 28.5 + Math.Cos(simTime) * 1.5;
            double map = 101.3 + Math.Abs(Math.Sin(simTime)) * 60;
            double ect = 89.0 + Math.Sin(simTime * 0.1) * 2.0;

            res.Add("VTEC_ACTIVE", vtec ? HondaTuner.Core.Localization.L.Get("1 (AKTİF)") : HondaTuner.Core.Localization.L.Get("0 (PASİF)"));
            res.Add("AFR_TARGET", afr.ToString("F2"));
            res.Add("IGN_ADVANCE", $"{advance.ToString("F1")}° BTDC");
            res.Add("MANIFOLD_KPA", $"{map.ToString("F0")} kPa");
            res.Add("ECT_CELSIUS", $"{ect.ToString("F1")}°C");

            return res;
        }
    }
}
