// AppServiceCollection.cs — Aşama 2: Gerçek DI Migrasyonu
// Mevcut ServiceContainer manuel kayıtlarını Microsoft.Extensions.DependencyInjection
// IServiceCollection formatına taşır. Tüm servisler Singleton olarak kaydedilir
// çünkü uygulama tek ECU oturumu üzerine kurulmuştur.
//
// NOT: Tüm tipler tam qualified isimle yazılmıştır; HondaTuner.Core.AutoTune ve
// HondaTuner.Calibration.AutoTune arasındaki belirsizliği önlemek için.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace HondaTuner.Core.Container
{
    /// <summary>
    /// Microsoft.Extensions.DependencyInjection tabanlı servis kaydı.
    /// Kullanım:
    ///   var services = new ServiceCollection();
    ///   AppServiceCollection.ConfigureServices(services);
    ///   var provider = services.BuildServiceProvider();
    /// </summary>
    public static class AppServiceCollection
    {
        /// <summary>Tüm uygulama servislerini IServiceCollection'a kaydeder.</summary>
        public static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            // ── ROM Katmanı ───────────────────────────────────────────────────
            services.AddSingleton<Interfaces.IRomService, Rom.RomService>();
            services.AddSingleton<Interfaces.IRomIdentifier, Rom.RomIdentifier>();
            services.AddSingleton<Interfaces.IRomPatchManager, Rom.RomPatchManager>();
            services.AddSingleton<Rom.RomLayoutValidator>();

            // ── Kalibrasyon ───────────────────────────────────────────────────
            services.AddSingleton<Interfaces.ICalibrationService, Calibration.CalibrationManager>();
            services.AddSingleton<Calibration.Interpolation.IInterpolationEngine, Calibration.Interpolation.BilinearInterpolationEngine>();
            services.AddSingleton<Calibration.Maps.MapManager>();

            // ── Checksum Engine ───────────────────────────────────────────────
            services.AddSingleton<Rom.Checksum.IChecksumEngine>(sp =>
            {
                var algorithms = new List<Rom.Checksum.IChecksumAlgorithm>
                {
                    new Rom.Checksum.Xor8Algorithm(),
                    new Rom.Checksum.Add8Algorithm(),
                    new Rom.Checksum.Sum16Algorithm(),
                    new Rom.Checksum.Xor16Algorithm(),
                    new Rom.Checksum.HondaCustomAlgorithm()
                };
                return new Rom.Checksum.ChecksumEngine(algorithms);
            });

            // ── Raporlama ─────────────────────────────────────────────────────
            services.AddSingleton<Interfaces.IReportGenerator, Report.RawHtmlReportGenerator>();

            // ── Donanım ──────────────────────────────────────────────────────
            services.AddSingleton<Hardware.OBD.IObdConnection, Hardware.OBD.RealObd1Connection>();
            services.AddSingleton<Hardware.EEPROM.IEepromProgrammer, Hardware.EEPROM.Tl866Programmer>();
            services.AddSingleton<Hardware.Emulator.IEmulator, Hardware.Emulator.OstrichEmulator>();
            services.AddSingleton<Hardware.EEPROM.Ch341aProgrammer>();
            services.AddSingleton<Hardware.OBD.DtcManager>();

            // ── Patch Engine ──────────────────────────────────────────────────
            services.AddSingleton<Rom.Patch.IPatchEngine>(sp =>
                new Rom.Patch.PatchEngine(
                    sp.GetRequiredService<Rom.Checksum.IChecksumEngine>(),
                    sp.GetRequiredService<Interfaces.ICalibrationService>()));

            // ── Telemetry Katmanı ─────────────────────────────────────────────
            services.AddSingleton<Telemetry.ITimeProvider, Telemetry.HighResolutionClock>();
            services.AddSingleton<Telemetry.ITelemetryBus, Telemetry.TelemetryBus>();
            services.AddSingleton<Telemetry.IAccessControl, Telemetry.AccessControl>();
            services.AddSingleton<Telemetry.ITelemetryProviderFactory, Telemetry.TelemetryProviderFactory>();
            services.AddSingleton<Telemetry.ITelemetryProviderDiscovery, Telemetry.TelemetryProviderDiscovery>();
            services.AddSingleton<Telemetry.IConfigurationWatcher, Telemetry.ConfigurationWatcher>();
            services.AddSingleton<Telemetry.ITelemetryEngine>(sp =>
                new Telemetry.TelemetryEngine(
                    sp.GetRequiredService<Telemetry.ITelemetryBus>(),
                    sp.GetRequiredService<Telemetry.IAccessControl>(),
                    sp.GetRequiredService<Telemetry.ITimeProvider>(),
                    sp.GetRequiredService<Telemetry.ITelemetryProviderFactory>(),
                    sp.GetRequiredService<Telemetry.IConfigurationWatcher>()));

            // ── AutoTune Bileşenleri ──────────────────────────────────────────
            services.AddSingleton<AutoTune.ICalibrationCellLockManager, AutoTune.CalibrationCellLockManager>();
            services.AddSingleton<AutoTune.ICalibrationSnapshotManager, AutoTune.CalibrationSnapshotManager>();
            services.AddSingleton<AutoTune.ICalibrationRecoveryManager, AutoTune.CalibrationRecoveryManager>();
            services.AddSingleton<AutoTune.ITuneConfidenceEngine, AutoTune.TuneConfidenceEngine>();
            services.AddSingleton<AutoTune.ICalibrationDiffEngine, AutoTune.CalibrationDiffEngine>();
            services.AddSingleton<AutoTune.ITuneExplanationProvider, AutoTune.TuneExplanationProvider>();
            services.AddSingleton<AutoTune.Safety.ISafetyRuleProvider, AutoTune.Safety.SafetyRuleProvider>();
            services.AddSingleton<AutoTune.IAutoTuneSafetyManager>(sp =>
                new AutoTune.AutoTuneSafetyManager(
                    sp.GetRequiredService<AutoTune.Safety.ISafetyRuleProvider>()));
            services.AddSingleton<AutoTune.ICalibrationSecurityManager, AutoTune.CalibrationSecurityManager>();
            services.AddSingleton<AutoTune.IAutoTuneSessionManager, AutoTune.AutoTuneSessionManager>();
            services.AddSingleton<AutoTune.ITuneChangeQueue, AutoTune.TuneChangeQueue>();
            services.AddSingleton<AutoTune.IAutoTuneEventPublisher, AutoTune.AutoTuneEventPublisher>();
            services.AddSingleton<AutoTune.ICalibrationStreamPublisher, AutoTune.CalibrationStreamPublisher>();
            services.AddSingleton<AutoTune.IReplayDeterministicValidator, AutoTune.ReplayDeterministicValidator>();

            // ── AutoTuneEngine (15 bağımlılık → DI otomatik çözer) ───────────
            // Core.AutoTune.AutoTuneEngine: Closed-Loop tam versiyon (15 parametre)
            services.AddSingleton<AutoTune.IAutoTuneEngine>(sp =>
                new AutoTune.AutoTuneEngine(
                    sp.GetRequiredService<AutoTune.ICalibrationCellLockManager>(),
                    sp.GetRequiredService<AutoTune.ICalibrationSnapshotManager>(),
                    sp.GetRequiredService<AutoTune.ICalibrationRecoveryManager>(),
                    sp.GetRequiredService<AutoTune.ITuneConfidenceEngine>(),
                    sp.GetRequiredService<AutoTune.ICalibrationDiffEngine>(),
                    sp.GetRequiredService<AutoTune.ITuneExplanationProvider>(),
                    sp.GetRequiredService<AutoTune.IAutoTuneSafetyManager>(),
                    sp.GetRequiredService<AutoTune.ICalibrationSecurityManager>(),
                    sp.GetRequiredService<AutoTune.IAutoTuneSessionManager>(),
                    sp.GetRequiredService<AutoTune.ITuneChangeQueue>(),
                    sp.GetRequiredService<Interfaces.ICalibrationService>(),
                    sp.GetRequiredService<Calibration.Maps.MapManager>(),
                    sp.GetRequiredService<Rom.Checksum.IChecksumEngine>(),
                    sp.GetRequiredService<AutoTune.IAutoTuneEventPublisher>(),
                    sp.GetRequiredService<AutoTune.ICalibrationStreamPublisher>()
                ));

            // ── AutoTune Query/Command Services ───────────────────────────────
            services.AddSingleton<AutoTune.IAutoTuneQueryService>(sp =>
                new AutoTune.AutoTuneQueryService(
                    (AutoTune.AutoTuneEngine)sp.GetRequiredService<AutoTune.IAutoTuneEngine>()));
            services.AddSingleton<AutoTune.IAutoTuneCommandService>(sp =>
                new AutoTune.AutoTuneCommandService(
                    (AutoTune.AutoTuneEngine)sp.GetRequiredService<AutoTune.IAutoTuneEngine>()));

            // ── RTP Engine ────────────────────────────────────────────────────
            services.AddSingleton<Rtp.IRtpCalibrationEngine, Rtp.RtpCalibrationEngine>();

            return services;
        }
    }
}
