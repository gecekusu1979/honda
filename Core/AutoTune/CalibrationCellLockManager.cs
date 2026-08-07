using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public class CalibrationCellLockManager : ICalibrationCellLockManager
    {
        private class CellKey
        {
            public string MapName { get; }
            public int Row { get; }
            public int Col { get; }

            public CellKey(string mapName, int row, int col)
            {
                MapName = mapName;
                Row = row;
                Col = col;
            }

            public override bool Equals(object obj)
            {
                return obj is CellKey other &&
                       MapName == other.MapName &&
                       Row == other.Row &&
                       Col == other.Col;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(MapName, Row, Col);
            }
        }

        private readonly ConcurrentDictionary<CellKey, string> _locks = new ConcurrentDictionary<CellKey, string>();

        public bool TryLockCell(string mapName, int row, int col, string ownerId)
        {
            if (string.IsNullOrEmpty(mapName)) return false;
            if (string.IsNullOrEmpty(ownerId)) return false;

            var key = new CellKey(mapName, row, col);

            // Add or retrieve. If it matches ownerId, return true.
            string existingOwner = _locks.GetOrAdd(key, ownerId);
            return existingOwner == ownerId;
        }

        public void ReleaseCell(string mapName, int row, int col, string ownerId)
        {
            if (string.IsNullOrEmpty(mapName) || string.IsNullOrEmpty(ownerId)) return;
            var key = new CellKey(mapName, row, col);

            if (_locks.TryGetValue(key, out string currentOwner) && currentOwner == ownerId)
            {
                _locks.TryRemove(key, out _);
            }
        }

        public bool IsCellLocked(string mapName, int row, int col, out string ownerId)
        {
            ownerId = null;
            if (string.IsNullOrEmpty(mapName)) return false;
            var key = new CellKey(mapName, row, col);

            if (_locks.TryGetValue(key, out string currentOwner))
            {
                ownerId = currentOwner;
                return true;
            }
            return false;
        }

        public void ReleaseAllLocks(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId)) return;

            foreach (var kvp in _locks)
            {
                if (kvp.Value == ownerId)
                {
                    _locks.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
