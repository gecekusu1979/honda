using System;
using HondaTuner.Core.Logging;

namespace HondaTuner.Calibration.Injector
{
    /// <summary>
    /// Enjektör Kalibrasyon Yöneticisi.
    /// Enjektör boyutu değişikliğinde fuel haritası ölçekleme,
    /// dead time düzeltmesi ve batarya voltajı kompanzasyonu sağlar.
    /// </summary>
    public class InjectorManager
    {
        // ── Enjektör Ayarları ──────────────────────────────────────
        public class InjectorSettings
        {
            public double SizeCc { get; set; }
            public double DeadTimeMs { get; set; }
            // 8V-16V arası batarya voltajı kompanzasyon tablosu (9 nokta)
            public double[] BatteryCompensationMs { get; set; } =
                { 4.0, 3.2, 2.6, 2.1, 1.8, 1.5, 1.3, 1.1, 1.0 };
        }

        /// <summary>
        /// Fuel haritasını enjektör boyutu oranına göre ölçekler.
        /// Formül: newValue = oldValue * (oldCc / newCc)
        /// </summary>
        public static byte[,] ScaleFuelTable(byte[,] sourceMap, double oldCc, double newCc)
        {
            if (sourceMap == null) throw new ArgumentNullException(nameof(sourceMap));
            if (newCc <= 0) throw new ArgumentException("Yeni enjektör boyutu sıfırdan büyük olmalı.");

            double ratio = oldCc / newCc;
            int rows = sourceMap.GetLength(0);
            int cols = sourceMap.GetLength(1);
            var result = new byte[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double scaled = sourceMap[r, c] * ratio;
                    result[r, c] = (byte)Math.Max(0, Math.Min(255, Math.Round(scaled)));
                }
            }

            ApplicationLogger.Info("InjectorManager",
                $"Fuel haritası ölçeklendi: {oldCc}cc → {newCc}cc (oran: {ratio:F3})");

            return result;
        }

        /// <summary>
        /// Dead time düzeltmesi hesaplar.
        /// Yeni enjektörlerin açılma gecikmesi farkı ms cinsinden döner.
        /// </summary>
        public static double CalculateDeadTimeCorrection(InjectorSettings oldSet, InjectorSettings newSet)
        {
            double correction = newSet.DeadTimeMs - oldSet.DeadTimeMs;
            ApplicationLogger.Info("InjectorManager",
                $"Dead time düzeltmesi: {oldSet.DeadTimeMs}ms → {newSet.DeadTimeMs}ms (fark: {correction:+0.00;-0.00}ms)");
            return correction;
        }

        /// <summary>
        /// Belirli batarya voltajı için kompanzasyon değeri döner.
        /// Voltaj 8V-16V aralığında indekslenir.
        /// </summary>
        public static double GetBatteryCompensation(InjectorSettings settings, double batteryVolts)
        {
            int index = (int)Math.Round(batteryVolts - 8.0);
            index = Math.Max(0, Math.Min(settings.BatteryCompensationMs.Length - 1, index));
            return settings.BatteryCompensationMs[index];
        }
    }
}
