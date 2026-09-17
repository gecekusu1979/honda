// AutoTuneDependencies.cs — Aşama 6: Parameter Object Pattern
//
// AutoTuneEngine'in 15 parametreli constructor'ını tek aggregate record'a indirger.
// Bu sayede:
//   - DI kayıt kodu basitleşir (tek kayıt yeteri)
//   - İleride yeni bağımlılık eklenmesi MainForm/ServiceContainer'ı etkilemez
//   - Unit testlerde partial mock oluşturmak kolaylaşır
//
// Kullanım:
//   var deps = new AutoTuneDependencies { CellLockManager = ..., ... };
//   var engine = new AutoTuneEngine(deps);

using HondaTuner.Calibration.Maps;
using HondaTuner.Core.AutoTune.Safety;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Rom.Checksum;

namespace HondaTuner.Core.AutoTune
{
    /// <summary>
    /// AutoTuneEngine'in tüm bağımlılıklarını saran Aggregate (Parameter Object) kaydı.
    /// 15 parametreli constructor yerine tek nesne geçirilir.
    /// </summary>
    public sealed record AutoTuneDependencies
    {
        /// <summary>Kalibrasyon hücresi kilit yöneticisi.</summary>
        public required ICalibrationCellLockManager CellLockManager { get; init; }

        /// <summary>Kalibrasyon anlık görüntü yöneticisi.</summary>
        public required ICalibrationSnapshotManager SnapshotManager { get; init; }

        /// <summary>Kalibrasyon kurtarma yöneticisi.</summary>
        public required ICalibrationRecoveryManager RecoveryManager { get; init; }

        /// <summary>Güven skoru hesaplama motoru.</summary>
        public required ITuneConfidenceEngine ConfidenceEngine { get; init; }

        /// <summary>Kalibrasyon fark algılama motoru.</summary>
        public required ICalibrationDiffEngine DiffEngine { get; init; }

        /// <summary>İnsan okunabilir açıklama sağlayıcı.</summary>
        public required ITuneExplanationProvider ExplanationProvider { get; init; }

        /// <summary>Güvenlik kural yöneticisi.</summary>
        public required IAutoTuneSafetyManager SafetyManager { get; init; }

        /// <summary>Güvenlik (security) yöneticisi.</summary>
        public required ICalibrationSecurityManager SecurityManager { get; init; }

        /// <summary>Oturum yaşam döngüsü yöneticisi.</summary>
        public required IAutoTuneSessionManager SessionManager { get; init; }

        /// <summary>Ayar değişiklik kuyruğu.</summary>
        public required ITuneChangeQueue ChangeQueue { get; init; }

        /// <summary>Kalibrasyon servisi (ROM okuma/yazma).</summary>
        public required ICalibrationService CalibrationService { get; init; }

        /// <summary>Harita (map table) yöneticisi.</summary>
        public required MapManager MapManager { get; init; }

        /// <summary>Checksum doğrulama ve güncelleme motoru.</summary>
        public required IChecksumEngine ChecksumEngine { get; init; }

        /// <summary>Domain event yayıncısı.</summary>
        public required IAutoTuneEventPublisher EventPublisher { get; init; }

        /// <summary>Gerçek zamanlı kalibrasyon akış yayıncısı.</summary>
        public required ICalibrationStreamPublisher StreamPublisher { get; init; }
    }
}
