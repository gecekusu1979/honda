using System;
using System.Threading;
using System.Threading.Tasks;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri işlemlerinin (paket işleme, analiz, disk yazma vb.) 
    /// UI thread'i (arayüz arabelleğini) kilitlemeden arka planda güvenle 
    /// çalıştırılmasını ve gerektiğinde ana thread'e senkronize edilmesini sağlayan yardımcı sınıftır.
    /// </summary>
    public static class TelemetryDispatcher
    {
        private static SynchronizationContext _uiContext;

        /// <summary>
        /// Arayüz (UI) thread'ine ait SynchronizationContext bilgisini kaydeder.
        /// </summary>
        public static void Initialize(SynchronizationContext uiContext)
        {
            _uiContext = uiContext;
        }

        /// <summary>
        /// Belirtilen eylemi arayüz thread'i üzerinde asenkron olarak güvenlice çalıştırır.
        /// </summary>
        public static void RunOnUI(Action action)
        {
            if (action == null) return;
            if (_uiContext != null)
            {
                _uiContext.Post(_ => action(), null);
            }
            else
            {
                action(); // Arayüz context'i tanımlanmamışsa doğrudan çalıştır
            }
        }

        /// <summary>
        /// Bir iş parçacığını iptal desteği ile arka planda çalıştırır.
        /// </summary>
        public static Task RunBackgroundTask(Action action, CancellationToken token)
        {
            return Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;
                try
                {
                    action();
                }
                catch (OperationCanceledException)
                {
                    // Graceful exit
                }
            }, token);
        }
    }
}
