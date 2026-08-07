using System;

namespace HondaTuner.Calibration.Fuel
{
    public class AdvancedFuelTables
    {
        // 1. Alpha-N Table: Rows = TPS bins (e.g. 0 to 100%), Cols = RPM bins (500 to 9000 RPM)
        public double[] AlphaNTpsBins { get; set; } = { 0, 2, 5, 10, 20, 30, 45, 60, 80, 100 };
        public double[] AlphaNRpmBins { get; set; } = { 500, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000 };
        public double[,] AlphaNVolumetricEfficiency { get; set; }

        // 2. MAF Scale Table: Voltage (0.0V - 5.0V) to Mass Airflow (g/s)
        public double[] MafVoltages { get; set; } = { 0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0 };
        public double[] MafFlowRates { get; set; } = { 0.0, 4.5, 12.0, 28.0, 55.0, 95.0, 150.0, 220.0, 310.0, 420.0, 550.0 };

        // 3. Cold Start Enrichment Table: ECT (Engine Coolant Temp in °C) to Fuel Multiplier
        public double[] ColdStartEctBins { get; set; } = { -40, -20, 0, 20, 40, 60, 80 };
        public double[] ColdStartMultipliers { get; set; } = { 2.00, 1.70, 1.45, 1.25, 1.12, 1.05, 1.00 };

        // 4. Injector Short Pulse Adder Table: Base pulse width (ms) to microsecond adder (ms)
        public double[] ShortPulseBaseBins { get; set; } = { 0.2, 0.5, 0.8, 1.0, 1.2, 1.5, 1.8, 2.0 };
        public double[] ShortPulseAdders { get; set; } = { 0.35, 0.22, 0.14, 0.08, 0.04, 0.01, 0.00, 0.00 };

        // 5. Transient Fuel Table: dTPS/dt (%/sec) to Base Transient Enrichment (ms)
        public double[] TransientDtpsBins { get; set; } = { 0, 10, 20, 40, 70, 110, 160 };
        public double[] TransientEnrichments { get; set; } = { 0.00, 0.18, 0.40, 0.85, 1.40, 2.30, 3.80 };

        public AdvancedFuelTables()
        {
            AlphaNVolumetricEfficiency = new double[10, 10];
            // Temel ve makul VE yüklemeleri
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 10; c++)
                {
                    AlphaNVolumetricEfficiency[r, c] = 35.0 + (r * 4.0) + (c * 2.5); // %35 ile %91.5 arası kademeli
                }
            }
        }
    }
}
