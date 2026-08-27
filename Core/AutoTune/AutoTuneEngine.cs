using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Telemetry;
using HondaTuner.Core.Logging;
using HondaTuner.Core.AutoTune.Safety;
using HondaTuner.Core.AutoTune.Analyzers;
using HondaTuner.Calibration.Maps;
using HondaTuner.Core.Rom.Checksum;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneEngine : IAutoTuneEngine
    {
        private readonly object _lockObj = new object();
        private readonly List<ITuneAnalyzer> _analyzers = new List<ITuneAnalyzer>();
        private readonly List<CalibrationSnapshot> _snapshotsList = new List<CalibrationSnapshot>();
        private readonly StableWindowFilter _stableFilter = new StableWindowFilter();
        private readonly CorrectionDecayManager _decayManager = new CorrectionDecayManager();

        // Dependencies
        private readonly ICalibrationCellLockManager _cellLockManager;
        private readonly ICalibrationSnapshotManager _snapshotManager;
        private readonly ICalibrationRecoveryManager _recoveryManager;
        private readonly ITuneConfidenceEngine _confidenceEngine;
        private readonly ICalibrationDiffEngine _diffEngine;
        private readonly ITuneExplanationProvider _explanationProvider;
        private readonly IAutoTuneSafetyManager _safetyManager;
        private readonly ICalibrationSecurityManager _securityManager;
        private readonly IAutoTuneSessionManager _sessionManager;
        private readonly ITuneChangeQueue _changeQueue;
        private readonly ICalibrationService _calibrationService;
        private readonly MapManager _mapManager;
        private readonly IChecksumEngine _checksumEngine;
        private readonly IAutoTuneEventPublisher _eventPublisher;
        private readonly ICalibrationStreamPublisher _streamPublisher;

        public bool IsRunning { get; private set; }
        public AutoTuneSession ActiveSession { get; private set; }
        public AdaptiveMemory Memory { get; } = new AdaptiveMemory();
        public CalibrationJournal Journal { get; } = new CalibrationJournal();
        public IReadOnlyList<CalibrationSnapshot> Snapshots => _snapshotsList.AsReadOnly();
        public TargetMapProvider TargetMapProvider { get; } = new TargetMapProvider();
        public ICalibrationRecoveryManager RecoveryManager => _recoveryManager;
        public ICalibrationSnapshotManager SnapshotManager => _snapshotManager;

        public event Action<IAutoTuneDomainEvent> OnDomainEvent;
        public event Action<CalibrationStreamPayload> OnCalibrationStream;

        public AutoTuneEngine(
            ICalibrationCellLockManager cellLockManager,
            ICalibrationSnapshotManager snapshotManager,
            ICalibrationRecoveryManager recoveryManager,
            ITuneConfidenceEngine confidenceEngine,
            ICalibrationDiffEngine diffEngine,
            ITuneExplanationProvider explanationProvider,
            IAutoTuneSafetyManager safetyManager,
            ICalibrationSecurityManager securityManager,
            IAutoTuneSessionManager sessionManager,
            ITuneChangeQueue changeQueue,
            ICalibrationService calibrationService,
            MapManager mapManager,
            IChecksumEngine checksumEngine,
            IAutoTuneEventPublisher eventPublisher,
            ICalibrationStreamPublisher streamPublisher)
        {
            _cellLockManager = cellLockManager ?? throw new ArgumentNullException(nameof(cellLockManager));
            _snapshotManager = snapshotManager ?? throw new ArgumentNullException(nameof(snapshotManager));
            _recoveryManager = recoveryManager ?? throw new ArgumentNullException(nameof(recoveryManager));
            _confidenceEngine = confidenceEngine ?? throw new ArgumentNullException(nameof(confidenceEngine));
            _diffEngine = diffEngine ?? throw new ArgumentNullException(nameof(diffEngine));
            _explanationProvider = explanationProvider ?? throw new ArgumentNullException(nameof(explanationProvider));
            _safetyManager = safetyManager ?? throw new ArgumentNullException(nameof(safetyManager));
            _securityManager = securityManager ?? throw new ArgumentNullException(nameof(securityManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _changeQueue = changeQueue ?? throw new ArgumentNullException(nameof(changeQueue));
            _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
            _mapManager = mapManager ?? throw new ArgumentNullException(nameof(mapManager));
            _checksumEngine = checksumEngine ?? throw new ArgumentNullException(nameof(checksumEngine));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _streamPublisher = streamPublisher ?? throw new ArgumentNullException(nameof(streamPublisher));

            // Load sequential analyzers
            _analyzers.Add(new FuelTrimAnalyzer());
            _analyzers.Add(new AFRAnalyzer());
            _analyzers.Add(new KnockAnalyzer());
            _analyzers.Add(new IgnitionAnalyzer());
            _analyzers.Add(new TemperatureAnalyzer());
            _analyzers.Add(new LoadAnalyzer());

            // Forward events
            _eventPublisher.OnEventPublished += e => OnDomainEvent?.Invoke(e);
            _streamPublisher.OnMessagePublished += p => OnCalibrationStream?.Invoke(p);
        }

        public bool StartSession(string ecuid, string userId, AutoTuneOperatingMode mode, string profile)
        {
            lock (_lockObj)
            {
                if (ActiveSession != null)
                {
                    ApplicationLogger.Warn("AutoTuneEngine", "Zaten aktif bir oturum var.");
                    return false;
                }

                // Check session lock validation
                if (!_sessionManager.AcquireSessionLock(ecuid, userId, mode, "Initializing", out string owner))
                {
                    ApplicationLogger.Warn("AutoTuneEngine", $"ECU '{ecuid}' başka bir kullanıcı '{owner}' tarafından kilitlenmiş.");
                    return false;
                }

                // Profile and security permissions checks
                if (!_securityManager.ValidateProfilePermissions(profile, mode.ToString(), out string securityReason))
                {
                    _sessionManager.ReleaseSessionLock(ecuid);
                    ApplicationLogger.Warn("AutoTuneEngine", $"Profil izin engeli: {securityReason}");
                    return false;
                }

                ActiveSession = new AutoTuneSession
                {
                    EcuIdentifier = ecuid,
                    ActiveProfile = profile,
                    UserRole = userId == "AdvancedUser" ? "Advanced" : (userId == "BeginnerUser" ? "Beginner" : "Professional"),
                    OperatingMode = mode,
                    State = "Running"
                };

                // Load target maps & rules
                string dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
                TargetMapProvider.LoadTargets(Path.Combine(dbDir, "autotune_targets.json"));
                _safetyManager.ReloadSafetyLimits(Path.Combine(dbDir, "safety_limits.json"));

                IsRunning = true;

                _eventPublisher.Publish(new AutoTuneDomainEvent
                {
                    SessionId = ActiveSession.SessionId,
                    EcuIdentifier = ecuid,
                    User = ActiveSession.UserRole,
                    OperatingMode = mode,
                    EventType = "SessionCreated",
                    Payload = $"Oturum başlatıldı. Profil: {profile}"
                });

                ApplicationLogger.Info("AutoTuneEngine", $"Oturum başarıyla başlatıldı: {ActiveSession.SessionId}");
                return true;
            }
        }

        public void StopSession()
        {
            lock (_lockObj)
            {
                if (ActiveSession == null) return;

                IsRunning = false;
                ActiveSession.State = "Stopped";
                ActiveSession.EndTime = DateTime.Now;

                _eventPublisher.Publish(new AutoTuneDomainEvent
                {
                    SessionId = ActiveSession.SessionId,
                    EcuIdentifier = ActiveSession.EcuIdentifier,
                    User = ActiveSession.UserRole,
                    OperatingMode = ActiveSession.OperatingMode,
                    EventType = "SessionStopped",
                    Payload = "Oturum sonlandırıldı."
                });

                _sessionManager.ReleaseSessionLock(ActiveSession.EcuIdentifier);
                _cellLockManager.ReleaseAllLocks(ActiveSession.SessionId);
                _stableFilter.Clear();
                _changeQueue.Clear();

                ActiveSession = null;
            }
        }

        public void PauseSession()
        {
            lock (_lockObj)
            {
                if (ActiveSession == null) return;
                IsRunning = false;
                ActiveSession.State = "Paused";
                _sessionManager.UpdateSessionState(ActiveSession.EcuIdentifier, "Paused");

                _eventPublisher.Publish(new AutoTuneDomainEvent
                {
                    SessionId = ActiveSession.SessionId,
                    EcuIdentifier = ActiveSession.EcuIdentifier,
                    User = ActiveSession.UserRole,
                    OperatingMode = ActiveSession.OperatingMode,
                    EventType = "SessionPaused",
                    Payload = "Oturum duraklatıldı."
                });
            }
        }

        public void ResumeSession()
        {
            lock (_lockObj)
            {
                if (ActiveSession == null) return;
                IsRunning = true;
                ActiveSession.State = "Running";
                _sessionManager.UpdateSessionState(ActiveSession.EcuIdentifier, "Running");

                _eventPublisher.Publish(new AutoTuneDomainEvent
                {
                    SessionId = ActiveSession.SessionId,
                    EcuIdentifier = ActiveSession.EcuIdentifier,
                    User = ActiveSession.UserRole,
                    OperatingMode = ActiveSession.OperatingMode,
                    EventType = "SessionResumed",
                    Payload = "Oturum devam ettirildi."
                });
            }
        }

        public void ProcessTelemetry(TelemetrySnapshot telemetry)
        {
            if (telemetry == null || !IsRunning || ActiveSession == null) return;

            // Stable window check
            if (_stableFilter.AddSnapshot(telemetry, out var stableList))
            {
                _eventPublisher.Publish(new AutoTuneDomainEvent
                {
                    SessionId = ActiveSession.SessionId,
                    EcuIdentifier = ActiveSession.EcuIdentifier,
                    User = ActiveSession.UserRole,
                    OperatingMode = ActiveSession.OperatingMode,
                    EventType = "TelemetryWindowAccepted",
                    Payload = $"Stabil telemetri yakalandı. Size: {stableList.Count}"
                });

                // Calculate variations
                double sumRpm = stableList.Sum(s => s.RPM);
                double avgRpm = sumRpm / stableList.Count;
                double sumMap = stableList.Sum(s => s.MAP);
                double avgMap = sumMap / stableList.Count;

                double rpmSd = Math.Sqrt(stableList.Sum(s => Math.Pow(s.RPM - avgRpm, 2)) / (stableList.Count - 1));
                double mapSd = Math.Sqrt(stableList.Sum(s => Math.Pow(s.MAP - avgMap, 2)) / (stableList.Count - 1));

                foreach (var analyzer in _analyzers)
                {
                    var decision = analyzer.Analyze(telemetry, TargetMapProvider);
                    if (decision == null) continue;

                    // Enforce lock manager check
                    if (!_cellLockManager.TryLockCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId))
                    {
                        // Cell is locked by other execution path / thread. Ignore.
                        continue;
                    }

                    // Evaluate confidence
                    double confidence = _confidenceEngine.CalculateConfidence(
                        rpmSd, mapSd, stableList.Count, Memory, telemetry.ECT, telemetry.Battery, out string confReason);
                    decision.ConfidenceScore = confidence;
                    decision.ConfidenceReason = confReason;

                    if (confidence < 50.0)
                    {
                        _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);
                        continue;
                    }

                    // Safety verification
                    var safetyVal = _safetyManager.EvaluateSafety(telemetry, decision);
                    decision.Safety = safetyVal;

                    if (safetyVal.Status == "Reject")
                    {
                        _eventPublisher.Publish(new AutoTuneDomainEvent
                        {
                            SessionId = ActiveSession.SessionId,
                            EcuIdentifier = ActiveSession.EcuIdentifier,
                            User = ActiveSession.UserRole,
                            OperatingMode = ActiveSession.OperatingMode,
                            EventType = "SafetyViolation",
                            Payload = $"Güvenlik İhlali: {safetyVal.Reason}"
                        });

                        _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);
                        continue;
                    }

                    // State machine & Approval evaluation
                    string status = TuneApprovalWorkflow.DetermineInitialStatus(ActiveSession.UserRole, ActiveSession.OperatingMode, out string approvalExp);
                    decision.ApprovalStatus = status;
                    decision.Explanation = _explanationProvider.GenerateExplanation(decision, ActiveSession.UserRole);

                    // Add to Queue and sort priorities
                    _changeQueue.Enqueue(decision);
                    ActiveSession.AddDecision(decision);

                    _eventPublisher.Publish(new AutoTuneDomainEvent
                    {
                        SessionId = ActiveSession.SessionId,
                        EcuIdentifier = ActiveSession.EcuIdentifier,
                        User = ActiveSession.UserRole,
                        OperatingMode = ActiveSession.OperatingMode,
                        EventType = "DecisionCreated",
                        Payload = decision.Explanation
                    });

                    // Auto-apply if immediately approved
                    if (status == "Approved")
                    {
                        ApplyDecisionInternal(decision);
                    }
                }
            }
        }

        public bool ApproveDecision(string decisionId)
        {
            var decision = ActiveSession?.Decisions.FirstOrDefault(d => d.DecisionId == decisionId);
            if (decision == null || ActiveSession == null) return false;

            if (!TuneApprovalWorkflow.CanTransition(decision.ApprovalStatus, "Approved", ActiveSession.UserRole, out string err))
            {
                ApplicationLogger.Warn("AutoTuneEngine", $"Geçiş engeli: {err}");
                return false;
            }

            decision.ApprovalStatus = "Approved";
            _eventPublisher.Publish(new AutoTuneDomainEvent
            {
                SessionId = ActiveSession.SessionId,
                EcuIdentifier = ActiveSession.EcuIdentifier,
                User = ActiveSession.UserRole,
                OperatingMode = ActiveSession.OperatingMode,
                EventType = "DecisionApproved",
                Payload = $"Karar {decisionId} onaylandı."
            });

            return ApplyDecisionInternal(decision);
        }

        public void RejectDecision(string decisionId)
        {
            var decision = ActiveSession?.Decisions.FirstOrDefault(d => d.DecisionId == decisionId);
            if (decision == null || ActiveSession == null) return;

            if (TuneApprovalWorkflow.CanTransition(decision.ApprovalStatus, "Rejected", ActiveSession.UserRole, out _))
            {
                decision.ApprovalStatus = "Rejected";
                _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);

                _eventPublisher.Publish(new AutoTuneDomainEvent
                {
                    SessionId = ActiveSession.SessionId,
                    EcuIdentifier = ActiveSession.EcuIdentifier,
                    User = ActiveSession.UserRole,
                    OperatingMode = ActiveSession.OperatingMode,
                    EventType = "DecisionRejected",
                    Payload = $"Karar {decisionId} reddedildi."
                });
            }
        }

        private bool ApplyDecisionInternal(TuneDecision decision)
        {
            if (ActiveSession == null) return false;

            // Security checks validation
            if (!_securityManager.ValidatePermissions(ActiveSession.UserRole, ActiveSession.OperatingMode, "Apply", out string permReason))
            {
                decision.ApprovalStatus = "Rejected";
                _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);
                ApplicationLogger.Warn("AutoTuneEngine", $"Güvenlik kilidi engeli: {permReason}");
                return false;
            }

            // SafeMode and DryRun simulation
            if (ActiveSession.OperatingMode == AutoTuneOperatingMode.DryRun || ActiveSession.OperatingMode == AutoTuneOperatingMode.Simulation)
            {
                decision.ApprovalStatus = "Applied";
                _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);

                // Stream প্রস্তাব simulation change
                _streamPublisher.PublishApplied(new CalibrationStreamPayload
                {
                    SessionId = ActiveSession.SessionId,
                    Timestamp = DateTime.Now,
                    Parameter = decision.Parameter,
                    MapAddress = $"{decision.MapName}[{decision.CellRow},{decision.CellCol}]",
                    OldValue = decision.OldValue,
                    NewValue = decision.NewValue,
                    Confidence = decision.ConfidenceScore,
                    SafetyStatus = "Allow",
                    ApprovalStatus = "Applied"
                });

                // Write learning to memory
                Memory.Learn(decision.ParameterName, decision.MapName, decision.CellRow, decision.CellCol, decision.ChangePercent, true, null);

                // Log to journal
                Journal.Log(new JournalEntry
                {
                    User = ActiveSession.UserRole,
                    Profile = ActiveSession.ActiveProfile,
                    Parameter = decision.ParameterName,
                    RPM = decision.CellRow, // Row correlates
                    Load = decision.CellCol,
                    BeforeValue = decision.OldValue,
                    AfterValue = decision.NewValue,
                    Confidence = decision.ConfidenceScore,
                    SafetyStatus = "Allow",
                    ApprovalStatus = "Applied",
                    Result = "Accepted"
                });

                return true;
            }

            // Normal mode physical ROM update using rollback transactions
            try
            {
                // Capture Snapshot
                var cells = new List<CellSnapshot>
                {
                    new CellSnapshot { MapName = decision.MapName, Row = decision.CellRow, Col = decision.CellCol, Value = decision.OldValue }
                };

                var snapshot = _snapshotManager.CaptureSnapshot(
                    ActiveSession.EcuIdentifier,
                    ActiveSession.UserRole,
                    ActiveSession.ActiveProfile,
                    0.0, // Checksum placeholder
                    decision.Safety,
                    decision.ConfidenceScore,
                    null,
                    cells);

                lock (_lockObj)
                {
                    _snapshotsList.Add(snapshot);
                }

                // Register pending recovery details
                var recoveryMeta = new RecoveryMetaData
                {
                    TransactionId = decision.DecisionId,
                    SnapshotId = snapshot.SnapshotId,
                    PreviousChecksum = 0.0,
                    ExpectedChecksum = 1.0,
                    RollbackStatus = "Pending",
                    EcuProfile = ActiveSession.ActiveProfile,
                    ActiveUser = ActiveSession.UserRole,
                    Timestamp = DateTime.Now,
                    PreviousCellValues = cells
                };
                _recoveryManager.RegisterPendingTransaction(recoveryMeta);

                // Transaction open
                _calibrationService.BeginTransaction();

                // Define map limits — look up real map offset from active ROM profile
                int resolvedOffset = 0x0278; // P28 fuel map default (safe non-arbitrary fallback)
                try
                {
                    var romSvc = HondaTuner.Core.Container.ServiceContainer.Resolve<IRomService>();
                    var prof = romSvc?.Profile;
                    if (prof != null && decision.Offset <= 0)
                    {
                        bool isIgn = decision.MapName?.IndexOf("ign", StringComparison.OrdinalIgnoreCase) >= 0;
                        resolvedOffset = isIgn ? prof.IgnMapOffset : prof.FuelMapOffset;
                    }
                    else if (decision.Offset > 0)
                        resolvedOffset = decision.Offset;
                }
                catch { /* Profile not loaded yet — use fallback */ }

                var dummyDef = new MapDefinition
                {
                    MapName = decision.MapName,
                    Offset = resolvedOffset,
                    Rows = 8,
                    Columns = 8
                };

                // Map write cell call
                _mapManager.WriteCell(dummyDef, decision.CellRow, decision.CellCol, decision.NewValue);

                // Checksum Engine evaluation validation
                bool isRcValid = true;
                try
                {
                    var romService = HondaTuner.Core.Container.ServiceContainer.Resolve<IRomService>();
                    var checksums = romService?.Profile?.ChecksumDefinitions ?? new List<HondaTuner.Core.Rom.Checksum.ChecksumDefinition>();
                    isRcValid = _checksumEngine.VerifyBeforeSave(romService.GetBuffer(), checksums, out _);
                }
                catch
                {
                    isRcValid = false;
                }

                if (isRcValid)
                {
                    // Success, commit!
                    _calibrationService.CommitTransaction();
                    _recoveryManager.ClearPendingTransaction();

                    decision.ApprovalStatus = "Applied";
                    _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);

                    _streamPublisher.PublishApplied(new CalibrationStreamPayload
                    {
                        SessionId = ActiveSession.SessionId,
                        Timestamp = DateTime.Now,
                        Parameter = decision.Parameter,
                        MapAddress = $"{decision.MapName}[{decision.CellRow},{decision.CellCol}]",
                        OldValue = decision.OldValue,
                        NewValue = decision.NewValue,
                        Confidence = decision.ConfidenceScore,
                        SafetyStatus = "Allow",
                        ApprovalStatus = "Applied"
                    });

                    Memory.Learn(decision.ParameterName, decision.MapName, decision.CellRow, decision.CellCol, decision.ChangePercent, true, null);

                    Journal.Log(new JournalEntry
                    {
                        User = ActiveSession.UserRole,
                        Profile = ActiveSession.ActiveProfile,
                        Parameter = decision.ParameterName,
                        RPM = decision.CellRow,
                        Load = decision.CellCol,
                        BeforeValue = decision.OldValue,
                        AfterValue = decision.NewValue,
                        Confidence = decision.ConfidenceScore,
                        SafetyStatus = "Allow",
                        ApprovalStatus = "Applied",
                        Result = "Accepted"
                    });

                    _eventPublisher.Publish(new AutoTuneDomainEvent
                    {
                        SessionId = ActiveSession.SessionId,
                        EcuIdentifier = ActiveSession.EcuIdentifier,
                        User = ActiveSession.UserRole,
                        OperatingMode = ActiveSession.OperatingMode,
                        EventType = "MapChangeApplied",
                        Payload = $"Applied {decision.ParameterName} cell update ({decision.OldValue:F2} -> {decision.NewValue:F2})"
                    });

                    return true;
                }
                else
                {
                    // Fail validation, rollback transaction
                    _calibrationService.RollbackTransaction();
                    _recoveryManager.ClearPendingTransaction();

                    decision.ApprovalStatus = "Rejected";
                    _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);

                    Journal.Log(new JournalEntry
                    {
                        User = ActiveSession.UserRole,
                        Profile = ActiveSession.ActiveProfile,
                        Parameter = decision.ParameterName,
                        RPM = decision.CellRow,
                        Load = decision.CellCol,
                        BeforeValue = decision.OldValue,
                        AfterValue = decision.NewValue,
                        Confidence = decision.ConfidenceScore,
                        SafetyStatus = "Reject",
                        ApprovalStatus = "Rejected",
                        Result = "RolledBack"
                    });

                    return false;
                }
            }
            catch (Exception ex)
            {
                _calibrationService.RollbackTransaction();
                _recoveryManager.ClearPendingTransaction();
                _cellLockManager.ReleaseCell(decision.MapName, decision.CellRow, decision.CellCol, ActiveSession.SessionId);

                ApplicationLogger.Error("AutoTuneEngine", $"Uygulama hatası: {ex.Message}");
                return false;
            }
        }

        public bool RollbackLastChange(out string resultMessage)
        {
            resultMessage = "";
            lock (_lockObj)
            {
                if (ActiveSession == null)
                {
                    resultMessage = "Aktif oturum yok.";
                    return false;
                }

                if (_snapshotsList.Count == 0)
                {
                    resultMessage = "Geri yükleme için hiçbir snapshot bulunamadı.";
                    return false;
                }

                var last = _snapshotsList.Last(s => !s.IsRestored);
                if (last == null)
                {
                    resultMessage = "Tüm snapshotlar zaten geri yüklendi.";
                    return false;
                }

                try
                {
                    _snapshotManager.RestoreSnapshot(last);

                    // Recover each cell value in snapshots list
                    _calibrationService.BeginTransaction();
                    var dummyDef = new MapDefinition { MapName = last.CellSnapshots[0].MapName };
                    foreach (var cell in last.CellSnapshots)
                    {
                        _mapManager.WriteCell(dummyDef, cell.Row, cell.Col, cell.Value);
                    }
                    _calibrationService.CommitTransaction();

                    Journal.Log(new JournalEntry
                    {
                        User = ActiveSession.UserRole,
                        Profile = ActiveSession.ActiveProfile,
                        Parameter = last.CellSnapshots[0].MapName,
                        RPM = last.CellSnapshots[0].Row,
                        Load = last.CellSnapshots[0].Col,
                        BeforeValue = last.CellSnapshots[0].Value,
                        AfterValue = last.CellSnapshots[0].Value,
                        Confidence = last.ConfidenceScore,
                        SafetyStatus = "Allow",
                        ApprovalStatus = "Applied",
                        Result = "RolledBack"
                    });

                    _streamPublisher.PublishRollback(ActiveSession.SessionId, $"{last.CellSnapshots[0].MapName}[{last.CellSnapshots[0].Row},{last.CellSnapshots[0].Col}]", last.CellSnapshots[0].Value);

                    resultMessage = $"Snapshot {last.SnapshotId} adımı geri yüklenmiştir.";
                    return true;
                }
                catch (Exception ex)
                {
                    _calibrationService.RollbackTransaction();
                    resultMessage = $"Geri yükleme hatası: {ex.Message}";
                    return false;
                }
            }
        }
    }
}
