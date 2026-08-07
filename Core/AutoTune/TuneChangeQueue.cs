using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HondaTuner.Core.AutoTune
{
    public class TuneChangeQueue : ITuneChangeQueue
    {
        private readonly List<TuneDecision> _queue = new List<TuneDecision>();
        private readonly object _lockObj = new object();
        private const int MaxCapacity = 1000;

        public int Count
        {
            get
            {
                lock (_lockObj)
                {
                    return _queue.Count;
                }
            }
        }

        public void Enqueue(TuneDecision decision)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            lock (_lockObj)
            {
                if (_queue.Count >= MaxCapacity)
                {
                    // Discard oldest if overflows
                    _queue.RemoveAt(0);
                }

                _queue.Add(decision);
                RemoveDuplicatesAndConflictsInternal();
            }
        }

        public bool TryDequeue(out TuneDecision decision)
        {
            decision = null;
            lock (_lockObj)
            {
                if (_queue.Count > 0)
                {
                    decision = _queue[0];
                    _queue.RemoveAt(0);
                    return true;
                }
            }
            return false;
        }

        public void RemoveDuplicatesAndConflicts()
        {
            lock (_lockObj)
            {
                RemoveDuplicatesAndConflictsInternal();
            }
        }

        private void RemoveDuplicatesAndConflictsInternal()
        {
            // Sorting strategy: Priority sorting.
            // 1. Group by cell target (MapName, CellRow, CellCol).
            // 2. In each cell group, sort by ConfidenceScore descending, keeping only the highest confidence recommendation.
            // 3. Then sort the overall queue by ConfidenceScore descending.

            var groups = _queue
                .GroupBy(d => new { d.MapName, d.CellRow, d.CellCol })
                .Select(g => g.OrderByDescending(d => d.ConfidenceScore).ThenByDescending(d => d.Timestamp).First())
                .OrderByDescending(d => d.ConfidenceScore)
                .ToList();

            _queue.Clear();
            _queue.AddRange(groups);
        }

        public List<TuneDecision> GetSnapshot()
        {
            lock (_lockObj)
            {
                return new List<TuneDecision>(_queue);
            }
        }

        public void Clear()
        {
            lock (_lockObj)
            {
                _queue.Clear();
            }
        }
    }
}
