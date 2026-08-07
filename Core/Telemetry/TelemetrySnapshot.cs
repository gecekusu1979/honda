using System;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Canlı ECU verilerinden derlenmiş, salt okunur (immutable), anlık araç durum veri setidir.
    /// AutoTune ve RTP motorları bu sınıfı girdi olarak kullanır.
    /// </summary>
    public class TelemetrySnapshot
    {
        // Sistem İzleme
        public string Version { get; }
        public DateTime Timestamp { get; }
        public long Sequence { get; }

        // Motor Parametreleri
        public double RPM { get; }
        public double TPS { get; }
        public double MAP { get; }
        public double ECT { get; }
        public double IAT { get; }
        public double Battery { get; }
        public double VehicleSpeed { get; }
        public double InjectorDuty { get; }
        public double IgnitionAdvance { get; }
        public double AFR { get; }
        public double Lambda { get; }
        public int KnockCount { get; }
        public double FuelTrimSTFT { get; }
        public double FuelTrimLTFT { get; }
        public bool ClosedLoop { get; }
        public bool OpenLoop { get; }
        public double EngineLoad { get; }

        public TelemetrySnapshot(
            string version,
            DateTime timestamp,
            long sequence,
            double rpm,
            double tps,
            double map,
            double ect,
            double iat,
            double battery,
            double speed,
            double injectorDuty,
            double ignitionAdvance,
            double afr,
            double lambda,
            int knockCount,
            double fuelTrimStft,
            double fuelTrimLtft,
            bool closedLoop,
            bool openLoop,
            double engineLoad)
        {
            Version = version;
            Timestamp = timestamp;
            Sequence = sequence;
            RPM = rpm;
            TPS = tps;
            MAP = map;
            ECT = ect;
            IAT = iat;
            Battery = battery;
            VehicleSpeed = speed;
            InjectorDuty = injectorDuty;
            IgnitionAdvance = ignitionAdvance;
            AFR = afr;
            Lambda = lambda;
            KnockCount = knockCount;
            FuelTrimSTFT = fuelTrimStft;
            FuelTrimLTFT = fuelTrimLtft;
            ClosedLoop = closedLoop;
            OpenLoop = openLoop;
            EngineLoad = engineLoad;
        }
    }
}
