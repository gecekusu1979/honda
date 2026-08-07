using System;
using System.Collections.Generic;

namespace HondaTuner.Calibration
{
    /// <summary>
    /// Geri Al / İleri Al (Undo / Redo) işlemlerini yöneten sınıf.
    /// Her bir işlem bir CalibrationTransaction bazında saklanır.
    /// </summary>
    public class CalibrationUndoManager
    {
        private readonly Stack<CalibrationTransaction> _undoStack = new Stack<CalibrationTransaction>();
        private readonly Stack<CalibrationTransaction> _redoStack = new Stack<CalibrationTransaction>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void PushTransaction(CalibrationTransaction tx)
        {
            if (tx == null || tx.Changes.Count == 0) return;
            _undoStack.Push(tx);
            _redoStack.Clear(); // Yeni değişiklik yapıldığında redo geçmişi silinir
        }

        public CalibrationTransaction PopUndo()
        {
            if (!CanUndo) return null;
            var tx = _undoStack.Pop();
            _redoStack.Push(tx);
            return tx;
        }

        public CalibrationTransaction PopRedo()
        {
            if (!CanRedo) return null;
            var tx = _redoStack.Pop();
            _undoStack.Push(tx);
            return tx;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
