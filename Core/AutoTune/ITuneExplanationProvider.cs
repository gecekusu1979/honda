namespace HondaTuner.Core.AutoTune
{
    public interface ITuneExplanationProvider
    {
        string GenerateExplanation(TuneDecision decision, string userRole);
    }
}
