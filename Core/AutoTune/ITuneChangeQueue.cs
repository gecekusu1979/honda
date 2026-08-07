using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public interface ITuneChangeQueue
    {
        void Enqueue(TuneDecision decision);
        bool TryDequeue(out TuneDecision decision);
        void RemoveDuplicatesAndConflicts();
        List<TuneDecision> GetSnapshot();
        void Clear();
        int Count { get; }
    }
}
