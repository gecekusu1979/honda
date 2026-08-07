namespace HondaTuner.Workflow
{
    /// <summary>
    /// Tuning iş akışı durumları ve adımları.
    /// </summary>
    public enum TuningStep
    {
        LoadRom,
        SelectEcu,
        SelectEngine,
        ReadDatalog,
        AnalyzeAfr,
        SuggestChanges,
        ApplyChanges,
        SaveRom,
        WriteChip
    }

    /// <summary>
    /// Adım adım tuning iş akışı yöneticisi.
    /// </summary>
    public class TuningWorkflow
    {
        public TuningStep CurrentStep { get; private set; } = TuningStep.LoadRom;
        public bool IsComplete => CurrentStep == TuningStep.WriteChip;

        private static readonly TuningStep[] StepOrder =
        {
            TuningStep.LoadRom,
            TuningStep.SelectEcu,
            TuningStep.SelectEngine,
            TuningStep.ReadDatalog,
            TuningStep.AnalyzeAfr,
            TuningStep.SuggestChanges,
            TuningStep.ApplyChanges,
            TuningStep.SaveRom,
            TuningStep.WriteChip
        };

        public bool AdvanceStep()
        {
            int idx = System.Array.IndexOf(StepOrder, CurrentStep);
            if (idx < StepOrder.Length - 1)
            {
                CurrentStep = StepOrder[idx + 1];
                return true;
            }
            return false;
        }

        public bool GoBackStep()
        {
            int idx = System.Array.IndexOf(StepOrder, CurrentStep);
            if (idx > 0)
            {
                CurrentStep = StepOrder[idx - 1];
                return true;
            }
            return false;
        }

        public void Reset() => CurrentStep = TuningStep.LoadRom;

        public string GetStepDescription()
        {
            switch (CurrentStep)
            {
                case TuningStep.LoadRom: return "ROM dosyasını yükleyin";
                case TuningStep.SelectEcu: return "ECU profili seçin (veya otomatik tanıma bekleyin)";
                case TuningStep.SelectEngine: return "Motor tipini onaylayın";
                case TuningStep.ReadDatalog: return "Datalog verisini okuyun";
                case TuningStep.AnalyzeAfr: return "AFR analizi yapın";
                case TuningStep.SuggestChanges: return "Düzeltme önerilerini inceleyin";
                case TuningStep.ApplyChanges: return "Değişiklikleri uygulayın";
                case TuningStep.SaveRom: return "ROM'u kaydedin";
                case TuningStep.WriteChip: return "Chip'e yazın";
                default: return "Bilinmeyen adım";
            }
        }

        public int GetProgress()
        {
            int idx = System.Array.IndexOf(StepOrder, CurrentStep);
            return (int)(((idx + 1) / (double)StepOrder.Length) * 100);
        }
    }
}
