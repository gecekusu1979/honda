using System;
using System.Collections.Generic;
using System.IO;

namespace HondaTuner.Core.Logging
{
    /// <summary>
    /// Uygulama seviyesi loglama sistemi.
    /// ROM yazma, yama, donanım işlemleri kayıt altına alınır.
    /// </summary>
    public static class ApplicationLogger
    {
        public enum LogLevel { DEBUG, INFO, WARNING, ERROR }

        private static readonly List<LogEntry> _logBuffer = new List<LogEntry>();
        private static readonly object _lock = new object();
        private static string _logFilePath;

        public static event EventHandler<LogEntry> LogAdded;

        public static void Initialize(string logDirectory)
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            _logFilePath = Path.Combine(logDirectory,
                $"hondatuner_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        }

        public static void Log(LogLevel level, string source, string message)
        {
            var entry = new LogEntry
            {
                Level = level,
                Source = source,
                Message = message,
                Timestamp = DateTime.Now
            };

            lock (_lock)
            {
                _logBuffer.Add(entry);
            }

            LogAdded?.Invoke(null, entry);

            if (_logFilePath != null)
            {
                try
                {
                    File.AppendAllText(_logFilePath,
                        $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] [{entry.Source}] {entry.Message}{Environment.NewLine}");
                }
                catch { /* Dosya yazılamadıysa sessizce geç */ }
            }
        }

        public static void Info(string source, string message) => Log(LogLevel.INFO, source, message);
        public static void Warn(string source, string message) => Log(LogLevel.WARNING, source, message);
        public static void Error(string source, string message) => Log(LogLevel.ERROR, source, message);
        public static void Debug(string source, string message) => Log(LogLevel.DEBUG, source, message);

        public static IReadOnlyList<LogEntry> GetAllLogs()
        {
            lock (_lock) { return _logBuffer.ToArray(); }
        }
    }

    public class LogEntry
    {
        public ApplicationLogger.LogLevel Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
