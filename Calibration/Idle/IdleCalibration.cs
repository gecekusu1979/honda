using HondaTuner.Core.Logging;

namespace HondaTuner.Calibration.Idle
{
    /// <summary>
    /// Rölanti Kalibrasyon Modülü.
    /// Hedef RPM, IACV (Idle Air Control Valve) duty cycle ve rölanti düzeltme kazancını yönetir.
    /// </summary>
    public class IdleCalibration
    {
        // ── Rölanti Parametreleri ──────────────────────────────────
        public int TargetIdleRpm { get; set; } = 750;
        public double IacvDutyCycle { get; set; } = 35.0;  // % duty cycle
        public double IdleCorrectionGain { get; set; } = 1.0;

        // ECT'ye bağlı soğuk rölanti RPM artışı
        // -40°C'de +400 RPM, 80°C'de +0 RPM
        private static readonly int[] EctTempPoints =
            { -40, -20, -10, 0, 10, 20, 30, 40, 60, 80 };
        private int[] _coldIdleRpmAdder =
            { 400, 350, 300, 250, 200, 150, 100, 60, 20, 0 };

        /// <summary>
        /// Verilen ECT sıcaklığı için soğuk rölanti RPM eklentisini döner.
        /// </summary>
        public int GetColdIdleAdder(double ectCelsius)
        {
            if (ectCelsius <= EctTempPoints[0]) return _coldIdleRpmAdder[0];
            if (ectCelsius >= EctTempPoints[EctTempPoints.Length - 1])
                return _coldIdleRpmAdder[_coldIdleRpmAdder.Length - 1];

            for (int i = 0; i < EctTempPoints.Length - 1; i++)
            {
                if (ectCelsius >= EctTempPoints[i] && ectCelsius <= EctTempPoints[i + 1])
                {
                    double t = (ectCelsius - EctTempPoints[i]) /
                               (double)(EctTempPoints[i + 1] - EctTempPoints[i]);
                    return (int)(_coldIdleRpmAdder[i] +
                        t * (_coldIdleRpmAdder[i + 1] - _coldIdleRpmAdder[i]));
                }
            }
            return 0;
        }

        /// <summary>Efektif rölanti hedef RPM'i (soğuk düzeltme dahil).</summary>
        public int GetEffectiveIdleRpm(double ectCelsius)
        {
            return TargetIdleRpm + GetColdIdleAdder(ectCelsius);
        }

        public void SetColdIdleTable(int[] values)
        {
            if (values != null && values.Length == EctTempPoints.Length)
            {
                _coldIdleRpmAdder = (int[])values.Clone();
                ApplicationLogger.Info("IdleCalibration", "Soğuk rölanti RPM tablosu güncellendi.");
            }
        }
    }
}
