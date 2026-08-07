namespace HondaTuner.Core.AutoTune
{
    public interface ICalibrationCellLockManager
    {
        bool TryLockCell(string mapName, int row, int col, string ownerId);
        void ReleaseCell(string mapName, int row, int col, string ownerId);
        bool IsCellLocked(string mapName, int row, int col, out string ownerId);
        void ReleaseAllLocks(string ownerId);
    }
}
