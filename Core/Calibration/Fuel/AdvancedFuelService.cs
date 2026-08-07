using System;

namespace HondaTuner.Calibration.Fuel
{
    public class AdvancedFuelService
    {
        public AdvancedFuelTables Tables { get; }

        // Görev döngüsü doyum alarmı olay dinleyicisi
        public event EventHandler<double> InjectorSaturationAlarm;

        public AdvancedFuelService()
        {
            Tables = new AdvancedFuelTables();
        }

        // 1. Bilinear Enterpolasyon — 2D Harita Okuma (Alpha-N Haritası için)
        public double InterpolateAlphaN(double tps, double rpm)
        {
            return Interpolate2D(
                Tables.AlphaNTpsBins,
                Tables.AlphaNRpmBins,
                Tables.AlphaNVolumetricEfficiency,
                tps,
                rpm
            );
        }

        // 2. MAF Akış Okuma (Volt -> g/s)
        public double CalculateMafFlow(double voltage)
        {
            return Interpolate1D(Tables.MafVoltages, Tables.MafFlowRates, voltage);
        }

        // 3. Cold Start Çarpanı
        public double CalculateColdStartMultiplier(double ect)
        {
            return Interpolate1D(Tables.ColdStartEctBins, Tables.ColdStartMultipliers, ect);
        }

        // 4. Kısa Enjeksiyon Düzeltmesi (Short Pulse Adder)
        public double CalculateShortPulseAdder(double pulseWidthMs)
        {
            return Interpolate1D(Tables.ShortPulseBaseBins, Tables.ShortPulseAdders, pulseWidthMs);
        }

        // 5. Gaz Değişim Hızı Dengelemesi (Transient Acceleration Fuel / Wall Wetting)
        public double CalculateTransientFuel(double dTPS, double ect)
        {
            // dTPS/dt değerine göre ham yakıt eklemesini bul
            double baseEnrichment = Interpolate1D(Tables.TransientDtpsBins, Tables.TransientEnrichments, Math.Abs(dTPS));

            // Soğuk motorlarda daha yüksek duvar ıslanması (wall wetting) kompanzasyonu gerekir.
            double ectScale = 1.0;
            if (ect < 60)
            {
                ectScale = 1.0 + (60.0 - ect) * 0.02; // ECT = -40 için 3.0x, ECT = 20 için 1.8x çarpanı
            }

            return baseEnrichment * ectScale;
        }

        // 6. Yakıt Basıncı Düzeltmesi
        public double CalculateFuelPressureCorrection(double actualPressure, double targetPressure)
        {
            if (actualPressure <= 5.0) return 2.0; // Basınç kritik düzeyde düşükse maksimum kompanse uygula
            return Math.Sqrt(targetPressure / actualPressure);
        }

        // 7. Nihai Pulse Width Hesaplama Zinciri
        public double CalculatePulseWidth(
            double basePulseWidth,
            double rpm,
            double tps,
            double ect,
            double map,
            double fuelPressureActual,
            double fuelPressureTarget,
            double dTPS,
            bool useAlphaN,
            ref double transientEnrichmentAccumulator)
        {
            double pulseWidth = basePulseWidth;

            // Step A: Alpha-N Modu
            if (useAlphaN)
            {
                // Alpha-N VE haritasından doğrudan baz süreyi saptar (VE * sabiti şeklinde basitleştirelim)
                double ve = InterpolateAlphaN(tps, rpm);
                pulseWidth = ve * 0.15; // 80 VE = 12 ms enjeksiyon
            }

            // Step B: Soğuk Motor Çarpanı
            double coldMult = CalculateColdStartMultiplier(ect);
            pulseWidth *= coldMult;

            // Step C: Yakıt Basıncı Kompanzasyonu
            double pressureCorr = CalculateFuelPressureCorrection(fuelPressureActual, fuelPressureTarget);
            pulseWidth *= pressureCorr;

            // Step D: Geçici Rejim Hızlanma Zenginleştirmesi (Wall Wetting Decay)
            if (dTPS > 5.0)
            {
                double extraTransient = CalculateTransientFuel(dTPS, ect);
                transientEnrichmentAccumulator += extraTransient; // Akümülatöre yakıt briketi ekle
            }

            // Her motor döngüsünde veya hesaplama adımında yakıt sönümlenir
            pulseWidth += transientEnrichmentAccumulator;
            transientEnrichmentAccumulator *= 0.70; // %30 sönümleme katsayısı

            // Step E: Kısa Enjeksiyon Genişletme (Non-linear Short Pulse Adder)
            double shortPulseAdder = CalculateShortPulseAdder(pulseWidth);
            pulseWidth += shortPulseAdder;

            // Step F: Güvenlik Denetimi & Doyum Alarmı
            double dutyCycle = (rpm * pulseWidth) / 1200.0;
            if (dutyCycle >= 85.0)
            {
                InjectorSaturationAlarm?.Invoke(this, dutyCycle);
            }

            return Math.Max(0.0, pulseWidth);
        }

        // Doğrusal enterpolasyon yardımcısı (1D)
        private static double Interpolate1D(double[] xBins, double[] yValues, double targetX)
        {
            if (targetX <= xBins[0]) return yValues[0];
            if (targetX >= xBins[xBins.Length - 1]) return yValues[yValues.Length - 1];

            for (int i = 0; i < xBins.Length - 1; i++)
            {
                if (targetX >= xBins[i] && targetX <= xBins[i + 1])
                {
                    double pct = (targetX - xBins[i]) / (xBins[i + 1] - xBins[i]);
                    return yValues[i] + pct * (yValues[i + 1] - yValues[i]);
                }
            }
            return yValues[0];
        }

        // Bilinear enterpolasyon yardımcısı (2D)
        private static double Interpolate2D(double[] xBins, double[] yBins, double[,] zGrid, double targetX, double targetY)
        {
            double x = Math.Max(xBins[0], Math.Min(xBins[xBins.Length - 1], targetX));
            double y = Math.Max(yBins[0], Math.Min(yBins[yBins.Length - 1], targetY));

            int xi = 0;
            for (int i = 0; i < xBins.Length - 1; i++)
            {
                if (x >= xBins[i] && x <= xBins[i + 1])
                {
                    xi = i;
                    break;
                }
            }

            int yi = 0;
            for (int i = 0; i < yBins.Length - 1; i++)
            {
                if (y >= yBins[i] && y <= yBins[i + 1])
                {
                    yi = i;
                    break;
                }
            }

            double x0 = xBins[xi];
            double x1 = xBins[xi + 1];
            double y0 = yBins[yi];
            double y1 = yBins[yi + 1];

            double z00 = zGrid[xi, yi];
            double z01 = zGrid[xi, yi + 1];
            double z10 = zGrid[xi + 1, yi];
            double z11 = zGrid[xi + 1, yi + 1];

            double t = (x - x0) / (x1 - x0);
            double u = (y - y0) / (y1 - y0);

            return (1 - t) * (1 - u) * z00 + t * (1 - u) * z10 + (1 - t) * u * z01 + t * u * z11;
        }
    }
}
