using System;

namespace HondaTuner.Core.AutoTune
{
    public class CorrectionDecayManager
    {
        public double DecayFactorTime { get; set; } = 0.01; // Per second decay rate
        public double DecayFactorTemp { get; set; } = 0.05; // Extra decay under extreme conditions

        public double ApplyDecay(ParameterType parameter, double currentCorrection, double timeDeltaSeconds, EnvironmentalContext env)
        {
            if (timeDeltaSeconds <= 0) return currentCorrection;

            // Decay brings it closer to 0 (which means no correction)
            double decayRate = DecayFactorTime;

            if (env != null && (env.Temperature > 95.0 || string.Equals(env.OperatingConditions, "Extreme", StringComparison.OrdinalIgnoreCase)))
            {
                // Extreme temperature / conditions decays correction faster
                decayRate += DecayFactorTemp;
            }

            double multiplier = Math.Pow(1.0 - decayRate, timeDeltaSeconds);
            if (multiplier < 0) multiplier = 0;

            return currentCorrection * multiplier;
        }
    }
}
