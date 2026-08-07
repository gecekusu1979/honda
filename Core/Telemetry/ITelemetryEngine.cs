using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri akışını başlatan, durduran, profil ve eklentileri (analyzer, computed channels)
    /// koordine eden ana motor arayüzüdür.
    /// </summary>
    public interface ITelemetryEngine : IDisposable
    {
        ITelemetryProvider ActiveProvider { get; }
        ITelemetryBus Bus { get; }
        IAccessControl Access { get; }
        TelemetryAnalyzerPipeline Analyzers { get; }

        event Action OnConfigReloaded;

        void SelectProvider(string providerName);
        void SetActiveProfile(string profileId);
        void StartDatalogging(int intervalMs = 10);
        void StopDatalogging();
        void PauseDatalogging();
        void ResumeDatalogging();

        IReadOnlyDictionary<string, TelemetryChannel> GetChannels();
        IReadOnlyList<string> GetProfiles();
    }
}
