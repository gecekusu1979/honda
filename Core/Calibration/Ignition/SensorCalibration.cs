using System;

namespace HondaTuner.Calibration.Ignition
{
    public class SensorCalibration
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public double[] Voltages { get; set; }
        public double[] PhysicalValues { get; set; }

        public SensorCalibration(string name, string unit, double[] voltages, double[] physicalValues)
        {
            Name = name;
            Unit = unit;
            Voltages = voltages;
            PhysicalValues = physicalValues;
        }

        // Voltajdan Fiziksel Değer Çözümleme (Doğrusal Enterpolasyon)
        public double Linearize(double voltage)
        {
            if (voltage <= Voltages[0]) return PhysicalValues[0];
            if (voltage >= Voltages[Voltages.Length - 1]) return PhysicalValues[PhysicalValues.Length - 1];

            for (int i = 0; i < Voltages.Length - 1; i++)
            {
                if (voltage >= Voltages[i] && voltage <= Voltages[i + 1])
                {
                    double pct = (voltage - Voltages[i]) / (Voltages[i + 1] - Voltages[i]);
                    return PhysicalValues[i] + pct * (PhysicalValues[i + 1] - PhysicalValues[i]);
                }
            }
            return PhysicalValues[0];
        }

        // Hazır Fabrika Kalibrasyonları (B20 / B16 / Custom Sensörler)
        public static SensorCalibration CreateOemMapCalibration()
        {
            return new SensorCalibration(
                "Honda OEM 1.7-Bar MAP",
                "kPa",
                new double[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0 },
                new double[] { 10.0, 45.0, 80.0, 115.0, 150.0, 185.0 }
            );
        }

        public static SensorCalibration CreateOilPressureCalibration()
        {
            return new SensorCalibration(
                "AEM 10-Bar Oil Pressure",
                "Bar",
                new double[] { 0.5, 1.0, 2.0, 3.0, 4.5 },
                new double[] { 0.0, 1.25, 3.75, 6.25, 10.00 }
            );
        }

        public static SensorCalibration CreateOilTempCalibration()
        {
            return new SensorCalibration(
                "GM-style Oil Temp Sensor",
                "°C",
                new double[] { 0.2, 0.8, 1.5, 2.5, 3.5, 4.8 },
                new double[] { 150.0, 120.0, 90.0, 60.0, 30.0, -10.0 }
            );
        }
    }
}
