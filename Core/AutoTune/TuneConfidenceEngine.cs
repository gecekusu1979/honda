using System;
using System.Linq;

namespace HondaTuner.Core.AutoTune
{
    public class TuneConfidenceEngine : ITuneConfidenceEngine
    {
        public double CalculateConfidence(
            double stdDevRpm,
            double stdDevMap,
            int sampleCount,
            AdaptiveMemory memory,
            double ectValue,
            double batteryVoltage,
            out string reason)
        {
            // Perfect signal gets 100%. Deductions based on instability and lacking samples.
            double score = 100.0;

            // 1. Telemetry stability deduction
            if (stdDevRpm > 50.0) score -= (stdDevRpm - 50.0) * 0.2; // Rpm jitter
            if (stdDevMap > 1.0) score -= (stdDevMap - 1.0) * 5.0;   // Map jitter

            // 2. Sample size check
            if (sampleCount < 10)
            {
                score -= (10 - sampleCount) * 5.0; // Lack of samples
            }

            // 3. Environmental conditions: Optimal temperatures match ECT in range 80C to 95C
            if (ectValue < 75.0 || ectValue > 98.0)
            {
                score -= 15.0; // Non-optimal operational temperature
            }

            // Voltage check
            if (batteryVoltage < 12.5)
            {
                score -= 10.0;
            }

            // 4. Historical adapter memory matching check
            if (memory != null && memory.Entries.Count > 0)
            {
                int totalSuccess = memory.Entries.Count(e => e.Success);
                double successRate = (double)totalSuccess / memory.Entries.Count;
                if (successRate < 0.8)
                {
                    score -= (0.8 - successRate) * 30.0; // Decay confidence if success rate is poor
                }
            }

            score = Math.Clamp(score, 0.0, 100.0);

            // Construct explanation reason
            if (score >= 85.0)
            {
                reason = "Kararlı telemetri, yüksek örneklem sayısı ve uygun çalışma koşulları.";
            }
            else if (score >= 60.0)
            {
                reason = "Kısmen değişken sensör değerleri veya düşük voltaj/sıcaklık sapması.";
            }
            else
            {
                reason = "Yetersiz kararlı veri penceresi, dengesiz sensor okumaları veya düşük voltaj.";
            }

            return score;
        }
    }
}
