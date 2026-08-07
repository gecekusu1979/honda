using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Rom
{
    public class RomVersion
    {
        public DateTime Timestamp { get; }
        public byte[] RomData { get; }
        public string Description { get; }

        public RomVersion(byte[] romData, string description)
        {
            Timestamp = DateTime.Now;
            RomData = (byte[])romData.Clone();
            Description = description;
        }
    }

    public class RomBackupManager
    {
        private byte[] _originalRom;
        private readonly List<RomVersion> _history = new List<RomVersion>();

        public void InitBackup(byte[] originalData)
        {
            if (originalData == null) throw new ArgumentNullException(nameof(originalData));
            _originalRom = (byte[])originalData.Clone();
            _history.Clear();
            SaveVersion(originalData, "ROM Yuklendi (Orijinal Yedek)");
        }

        public void SaveVersion(byte[] currentData, string changeLog)
        {
            if (currentData == null) throw new ArgumentNullException(nameof(currentData));
            _history.Add(new RomVersion(currentData, changeLog));
        }

        public byte[] Undo()
        {
            if (_history.Count > 1)
            {
                _history.RemoveAt(_history.Count - 1);
                return (byte[])_history[_history.Count - 1].RomData.Clone();
            }
            return GetOriginal();
        }

        public byte[] GetOriginal()
        {
            return _originalRom != null ? (byte[])_originalRom.Clone() : null;
        }

        public List<RomVersion> GetHistory()
        {
            return _history;
        }
    }
}
