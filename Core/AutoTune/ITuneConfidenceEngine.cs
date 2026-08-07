namespace HondaTuner.Core.AutoTune
{
    public interface ITuneConfidenceEngine
    {
        double CalculateConfidence(
            double stdDevRpm,
            double stdDevMap,
            int sampleCount,
            AdaptiveMemory memory,
            double ectValue,
            double batteryVoltage,
            out string reason);
    }
}
