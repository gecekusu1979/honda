using System;
using System.Collections.Generic;
using HondaTuner.Calibration;
using HondaTuner.Calibration.AutoTune;
using HondaTuner.Calibration.Maps;
using HondaTuner.Calibration.Interpolation;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Rom;
using HondaTuner.Hardware.EEPROM;
using HondaTuner.Hardware.Emulator;
using HondaTuner.Hardware.OBD;
using HondaTuner.Report;

namespace HondaTuner.Core.Container
{
    /// <summary>
    /// Basit ve kararlı Dependency Injection Konteyneri (Service Locator).
    /// Dışarıdan paket bağımlılığı olmadan servislerin kaydını ve çözümlemesini yönetir.
    /// </summary>
    public static class ServiceContainer
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, Func<object>> Overrides = new Dictionary<Type, Func<object>>();

        static ServiceContainer()
        {
            // Varsayılan servis kayıtları
            Register<IRomService>(new RomService());
            Register<IRomIdentifier>(new RomIdentifier());
            Register<IRomPatchManager>(new RomPatchManager());
            Register<ICalibrationService>(new CalibrationManager());
            Register<IReportGenerator>(new RawHtmlReportGenerator());
            Register<IInterpolationEngine>(new BilinearInterpolationEngine());
            Register<MapManager>(new MapManager());

            // Checksum Engine
            var algoList = new List<HondaTuner.Core.Rom.Checksum.IChecksumAlgorithm>
            {
                new HondaTuner.Core.Rom.Checksum.Xor8Algorithm(),
                new HondaTuner.Core.Rom.Checksum.Add8Algorithm(),
                new HondaTuner.Core.Rom.Checksum.Sum16Algorithm(),
                new HondaTuner.Core.Rom.Checksum.Xor16Algorithm(),
                new HondaTuner.Core.Rom.Checksum.HondaCustomAlgorithm()
            };
            Register<HondaTuner.Core.Rom.Checksum.IChecksumEngine>(new HondaTuner.Core.Rom.Checksum.ChecksumEngine(algoList));

            // Donanım & Diğer
            Register<IObdConnection>(new RealObd1Connection());
            Register<IEepromProgrammer>(new Tl866Programmer());
            Register<IEmulator>(new OstrichEmulator());

            // Phase 10 — Real hardware additions
            Register<Ch341aProgrammer>(new Ch341aProgrammer());
            Register<DtcManager>(new DtcManager());

            // AutoTune varsayılan olarak P28 ekseniyle başlayabilir,
            // runtime'da güncellenebilir veya yeniden çözümlenebilir.
            Register<IAutoTuneEngine>(new AutoTuneEngine(
                new int[] { 500, 1000, 2000, 3000, 4000, 5000, 6000, 7000 },
                new int[] { 20, 40, 60, 80, 100, 120, 140, 160 }
            ));

            // Closed Loop AutoTune (Phase 8) services mapping
            var cellLocks = new HondaTuner.Core.AutoTune.CalibrationCellLockManager();
            var snapshots = new HondaTuner.Core.AutoTune.CalibrationSnapshotManager();
            var recovery = new HondaTuner.Core.AutoTune.CalibrationRecoveryManager();
            var confEng = new HondaTuner.Core.AutoTune.TuneConfidenceEngine();
            var diffEng = new HondaTuner.Core.AutoTune.CalibrationDiffEngine();
            var explProv = new HondaTuner.Core.AutoTune.TuneExplanationProvider();
            var safetyRules = new HondaTuner.Core.AutoTune.Safety.SafetyRuleProvider();
            var safetyMgr = new HondaTuner.Core.AutoTune.AutoTuneSafetyManager(safetyRules);
            var secMgr = new HondaTuner.Core.AutoTune.CalibrationSecurityManager();
            var sessMgr = new HondaTuner.Core.AutoTune.AutoTuneSessionManager();
            var chgQ = new HondaTuner.Core.AutoTune.TuneChangeQueue();
            var evPub = new HondaTuner.Core.AutoTune.AutoTuneEventPublisher();
            var strPub = new HondaTuner.Core.AutoTune.CalibrationStreamPublisher();
            var mapMgr = Resolve<MapManager>();
            var calSvc = Resolve<ICalibrationService>();
            var chkSvc = Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();

            var closedLoopEngine = new HondaTuner.Core.AutoTune.AutoTuneEngine(
                cellLocks, snapshots, recovery, confEng, diffEng, explProv,
                safetyMgr, secMgr, sessMgr, chgQ, calSvc, mapMgr, chkSvc, evPub, strPub);

            Register<HondaTuner.Core.AutoTune.ICalibrationCellLockManager>(cellLocks);
            Register<HondaTuner.Core.AutoTune.ICalibrationSnapshotManager>(snapshots);
            Register<HondaTuner.Core.AutoTune.ICalibrationRecoveryManager>(recovery);
            Register<HondaTuner.Core.AutoTune.ITuneConfidenceEngine>(confEng);
            Register<HondaTuner.Core.AutoTune.ICalibrationDiffEngine>(diffEng);
            Register<HondaTuner.Core.AutoTune.ITuneExplanationProvider>(explProv);
            Register<HondaTuner.Core.AutoTune.IAutoTuneSafetyManager>(safetyMgr);
            Register<HondaTuner.Core.AutoTune.ICalibrationSecurityManager>(secMgr);
            Register<HondaTuner.Core.AutoTune.IAutoTuneSessionManager>(sessMgr);
            Register<HondaTuner.Core.AutoTune.ITuneChangeQueue>(chgQ);
            Register<HondaTuner.Core.AutoTune.IAutoTuneEventPublisher>(evPub);
            Register<HondaTuner.Core.AutoTune.ICalibrationStreamPublisher>(strPub);
            Register<HondaTuner.Core.AutoTune.IAutoTuneEngine>(closedLoopEngine);

            Register<HondaTuner.Core.AutoTune.Safety.ISafetyRuleProvider>(safetyRules);
            Register<HondaTuner.Core.AutoTune.IReplayDeterministicValidator>(new HondaTuner.Core.AutoTune.ReplayDeterministicValidator());

            var queryService = new HondaTuner.Core.AutoTune.AutoTuneQueryService(closedLoopEngine);
            var commandService = new HondaTuner.Core.AutoTune.AutoTuneCommandService(closedLoopEngine);
            Register<HondaTuner.Core.AutoTune.IAutoTuneQueryService>(queryService);
            Register<HondaTuner.Core.AutoTune.IAutoTuneCommandService>(commandService);


            // Dynamic ROM Patch Management Engine v2
            var checksumEngine = Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
            var calibrationService = Resolve<ICalibrationService>();
            Register<Rom.Patch.IPatchEngine>(new Rom.Patch.PatchEngine(checksumEngine, calibrationService));

            // Telemetry & Live Datalog Bus Engine (Phase 7)
            var timeClock = new Telemetry.HighResolutionClock();
            var telemetryBus = new Telemetry.TelemetryBus();
            var accessCtrl = new Telemetry.AccessControl();
            var providerFac = new Telemetry.TelemetryProviderFactory(timeClock);
            var providerDisc = new Telemetry.TelemetryProviderDiscovery();
            var configWatch = new Telemetry.ConfigurationWatcher();
            var telemetryEngine = new Telemetry.TelemetryEngine(telemetryBus, accessCtrl, timeClock, providerFac, configWatch);

            Register<Telemetry.ITimeProvider>(timeClock);
            Register<Telemetry.ITelemetryBus>(telemetryBus);
            Register<Telemetry.IAccessControl>(accessCtrl);
            Register<Telemetry.ITelemetryProviderFactory>(providerFac);
            Register<Telemetry.ITelemetryProviderDiscovery>(providerDisc);
            Register<Telemetry.IConfigurationWatcher>(configWatch);
            Register<Telemetry.ITelemetryEngine>(telemetryEngine);

            // Real-Time Calibration & RTP Emulator Sync (Phase 9)
            var rtpEngine = new HondaTuner.Core.Rtp.RtpCalibrationEngine();
            Register<HondaTuner.Core.Rtp.IRtpCalibrationEngine>(rtpEngine);
        }

        /// <summary>Bir servis tipini örnek olarak kaydeder.</summary>
        public static void Register<T>(T serviceInstance)
        {
            if (serviceInstance == null) throw new ArgumentNullException(nameof(serviceInstance));
            Services[typeof(T)] = serviceInstance;
        }

        /// <summary>Bir servisi fabrika fonksiyonu (lazy) olarak kaydeder.</summary>
        public static void RegisterLazy<T>(Func<T> factory) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            Overrides[typeof(T)] = () => factory();
        }

        /// <summary>İlgili servisi çözümler.</summary>
        public static T Resolve<T>() where T : class
        {
            var type = typeof(T);
            if (Overrides.TryGetValue(type, out var factory))
            {
                return (T)factory();
            }
            if (Services.TryGetValue(type, out var instance))
            {
                return (T)instance;
            }
            throw new InvalidOperationException($"Servis bulunamadı: {type.FullName}");
        }

        /// <summary>Tüm servisleri sıfırlar (Testlerde mocks enjeksiyonu için kullanışlıdır).</summary>
        public static void Reset()
        {
            Services.Clear();
            Overrides.Clear();
        }
    }
}
