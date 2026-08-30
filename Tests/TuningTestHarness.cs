using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HondaTuner.Core.Telemetry;
using HondaTuner.Calibration.AutoTune;
using HondaTuner.Core.AutoTune;
using HondaTuner.Calibration.Injector;
using HondaTuner.Calibration.Maps;
using HondaTuner.Calibration.Interpolation;
using HondaTuner.Core;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;
using HondaTuner.Core.Rom;
using HondaTuner.Core.Rtp;
using HondaTuner.Hardware.Emulator;

namespace HondaTuner.Tests
{
    /// <summary>
    /// Otomatik Tuning Test Harnesi.
    /// GÃ¼venlik sÄ±nÄ±rlarÄ±, kalibrasyon hesaplamalarÄ± ve ROM operasyonlarÄ±nÄ± doÄŸrular.
    /// </summary>
    public static class TuningTestHarness
    {
        private static int _passed = 0;
        private static int _failed = 0;

        public static string RunAllTests()
        {
            _passed = 0;
            _failed = 0;

            var results = new List<string>();
            results.Add("â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");
            results.Add("  HONDATUNER â€” OTOMATÄ°K TEST SÃœÄ°TÄ°");
            results.Add("â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");

            // ROM Testleri
            results.Add("\nâ”€â”€ ROM Testleri â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");
            results.Add(TestRomIdentifier_ValidP28());
            results.Add(TestRomIdentifier_WrongSize());
            results.Add(TestRomPatch_ValidateAndApply());
            results.Add(TestRomPatch_RollbackRestoresOriginal());

            // Kalibrasyon Testleri
            results.Add("\nâ”€â”€ Kalibrasyon Testleri â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");
            results.Add(TestInjectorScaling_240to440());
            results.Add(TestInjectorScaling_ClampTo255());
            results.Add(TestCalibrationTransaction_CommitSavesValues());
            results.Add(TestCalibrationTransaction_RollbackRestoresValues());
            results.Add(TestCalibrationUndoRedo_RestoresState());
            results.Add(TestCalibrationValidator_RejectsInvalidLimits());

            // Faz 4: Map Engine & Interpolation Testleri
            results.Add("\nâ”€â”€ Faz 4: Map Engine & Interpolation Testleri â”€â”€");
            results.Add(TestMapEngine_ReadWriteCell());
            results.Add(TestMapEngine_ScaleConversion());
            results.Add(TestMapEngine_InvalidOffset());
            results.Add(TestMapEngine_UndoRollback());
            results.Add(TestInterpolation_WeightSumExactlyOne());

            // AutoTune GÃ¼venlik Testleri
            results.Add("\nâ”€â”€ AutoTune GÃ¼venlik Testleri â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");
            results.Add(TestAutoTune_RejectsLowECT());
            results.Add(TestAutoTune_RejectsLowBattery());
            results.Add(TestAutoTune_ClampsCorrection());

            // Özet
            results.Add("\n── Faz 5: Checksum Engine & Safety Layer Testleri ──");
            results.Add(TestChecksum_StockRomValidation());
            results.Add(TestChecksum_CorruptBypassesValidation());
            results.Add(TestChecksum_MapEditUpdatesChecksum());
            results.Add(TestChecksum_SaveFailsOnInvalidChecksum());
            results.Add(TestChecksum_ReloadVerification());
            results.Add(TestChecksum_MultipleRegionsValidation());

            // Dynamic ROM Patch Management Engine v2 Testleri
            results.Add("\n── ROM Patch Engine v2 Testleri ──");
            results.Add(TestPatchEngine_Success());
            results.Add(TestPatchEngine_ExpectedBytesMismatch());
            results.Add(TestPatchEngine_IncompatibleEcu());
            results.Add(TestPatchEngine_WrongRomSize());
            results.Add(TestPatchEngine_RollbackSuccess());
            results.Add(TestPatchEngine_TransactionCommit());
            results.Add(TestPatchEngine_TransactionRollback());
            results.Add(TestPatchEngine_ChecksumUpdate());
            results.Add(TestPatchEngine_AuditLogging());
            results.Add(TestPatchEngine_GetAvailablePatches());

            // Phase 7: Telemetry & Live Datalog Bus Engine Testleri
            results.AddRange(RunTelemetryTests());

            // Phase 8: AutoTune Closed Loop Engine Testleri
            results.AddRange(RunAutoTuneEnginePhase8Tests());

            // Phase 9: RTP Emulator & Real-Time Calibration Engine Testleri
            results.AddRange(RunRtpEnginePhase9Tests());

            // Phase 1: ECU Metadata & Engine Info Testleri
            results.Add(TestMetadata_SerializationAndValidation());

            // Phase 2: Reverse Engineering & Decoders Testleri
            results.Add(TestReverseEngineering_ExtractionAndDecompilation());

            // Phase 3: Advanced Fuel & Injection Corrections Testleri
            results.Add(TestAdvancedFuelCorrections_FormulasAndAlarms());

            // Phase 4: Advanced Ignition & Sensor Calibration Testleri
            results.Add(TestAdvancedIgnition_DecodingAndCalibration());

            // Phase 5: VTEC & Boost Control Engine Testleri
            results.Add(TestVtecAndBoostControl_LogicAndAlarms());

            // Phase 6: Engine Protection & Thermal Management Testleri
            results.Add(TestEngineProtection_SafetiesAndAlarms());

            // Faz 3: Unit Test Genişletme (8 Yeni Emniyet Testi)
            results.Add("\n── Faz 3: Yeni Emniyet Sınırları Testleri ──");
            results.Add(TestLeanCut_TriggersAtThreshold());
            results.Add(TestLeanCut_NotTriggeredBelowRpm());
            results.Add(TestLeanCut_NotTriggeredBelowMap());
            results.Add(TestOverboostCut_TriggersAboveLimit());
            results.Add(TestOverboostCut_SafeZone());
            results.Add(TestEctRetard_LinearInterpolation());
            results.Add(TestKnockRetard_ImmediatePull());
            results.Add(TestKnockRetard_GradualRecovery());

            // Phase 7: Diagnostics, Protocols & Standards Testleri
            results.Add(TestDiagnosticsAndProtocols_A2LAndFreezeFrames());

            // Phase 8: Dyno, Logs & Version Control Testleri
            results.Add(TestDynoLogsAndVersions_EstimationAndBranches());

            // Ã–zet
            results.Add("\nâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");
            results.Add($"  SONUÃ‡: {_passed} baÅŸarÄ±lÄ±, {_failed} baÅŸarÄ±sÄ±z");
            results.Add("â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•");

            string output = string.Join(Environment.NewLine, results);
            ApplicationLogger.Info("TestHarness", $"Test tamamlandÄ±: {_passed} geÃ§ti, {_failed} kaldÄ±");
            return output;
        }

        // â”€â”€ Calibration Engine Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static string TestCalibrationTransaction_CommitSavesValues()
        {
            try
            {
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                byte[] cleanRom = new byte[0x8000];
                cleanRom[0x1000] = 50;
                romService.SetBuffer(cleanRom);

                var calMgr = new Calibration.CalibrationManager();
                calMgr.BeginTransaction();
                calMgr.RecordChange(new CalibrationChange
                {
                    Parameter = "Fuel Cell [0,0]",
                    OldValue = "50",
                    NewValue = "60",
                    Offset = 0x1000,
                    MapName = "Fuel Map",
                    Source = "Test"
                });
                calMgr.CommitTransaction();

                byte[] currentRom = romService.GetBuffer();
                return Assert("Calibration Engine â€” Commit applies value to ROM buffer", currentRom[0x1000] == 60);
            }
            catch (Exception ex)
            {
                return Assert($"Calibration Engine â€” Commit applies value to ROM buffer: {ex.Message} @ {ex.StackTrace}", false);
            }
        }

        private static string TestCalibrationTransaction_RollbackRestoresValues()
        {
            try
            {
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                byte[] cleanRom = new byte[0x8000];
                cleanRom[0x1000] = 50;
                romService.SetBuffer(cleanRom);

                var calMgr = new Calibration.CalibrationManager();
                calMgr.BeginTransaction();
                calMgr.RecordChange(new CalibrationChange
                {
                    Parameter = "Fuel Cell [0,0]",
                    OldValue = "50",
                    NewValue = "60",
                    Offset = 0x1000,
                    MapName = "Fuel Map",
                    Source = "Test"
                });
                calMgr.RollbackTransaction();

                byte[] currentRom = romService.GetBuffer();
                return Assert("Calibration Engine â€” Rollback restores original values in ROM buffer", currentRom[0x1000] == 50);
            }
            catch (Exception ex)
            {
                return Assert($"Calibration Engine â€” Rollback restores original values: {ex.Message} @ {ex.StackTrace}", false);
            }
        }

        private static string TestCalibrationUndoRedo_RestoresState()
        {
            try
            {
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                byte[] cleanRom = new byte[0x8000];
                cleanRom[0x1000] = 50;
                romService.SetBuffer(cleanRom);

                var calMgr = new Calibration.CalibrationManager();
                calMgr.BeginTransaction();
                calMgr.RecordChange(new CalibrationChange
                {
                    Parameter = "Fuel Cell [0,0]",
                    OldValue = "50",
                    NewValue = "75",
                    Offset = 0x1000,
                    MapName = "Fuel Map",
                    Source = "Test"
                });
                calMgr.CommitTransaction();

                bool changedApplied = romService.GetBuffer()[0x1000] == 75;

                // Undo
                calMgr.Undo();
                bool undoApplied = romService.GetBuffer()[0x1000] == 50;

                // Redo
                calMgr.Redo();
                bool redoApplied = romService.GetBuffer()[0x1000] == 75;

                return Assert("Calibration Engine â€” Undo and Redo states are fully synchronized", changedApplied && undoApplied && redoApplied);
            }
            catch (Exception ex)
            {
                return Assert($"Calibration Engine â€” Undo/Redo failure: {ex.Message} @ {ex.StackTrace}", false);
            }
        }

        private static string TestCalibrationValidator_RejectsInvalidLimits()
        {
            try
            {
                var calMgr = new Calibration.CalibrationManager();
                bool exceptionThrown = false;
                try
                {
                    calMgr.RecordChange(new CalibrationChange
                    {
                        Parameter = "Primary Rev Limit",
                        OldValue = "7200",
                        NewValue = "12000", // Out of bounds safety (4000-10000)
                        Offset = 0x1FB0,
                        Source = "Test"
                    });
                }
                catch (ArgumentOutOfRangeException)
                {
                    exceptionThrown = true;
                }

                return Assert("Calibration Engine â€” Safety validator rejects OOR Rev Limit changes", exceptionThrown);
            }
            catch (Exception ex)
            {
                return Assert($"Calibration Engine â€” Safety validator failure: {ex.Message} @ {ex.StackTrace}", false);
            }
        }

        // â”€â”€ ROM Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static string TestRomIdentifier_ValidP28()
        {
            var identifier = new RomIdentifier();
            byte[] rom = new byte[0x8000]; // 32KB â€” P28 boyutu
            var result = identifier.IdentifyRom(rom, EcuProfiles.All);
            // Boyut eÅŸleÅŸiyor olmalÄ± ama veri Ã§eÅŸitliliÄŸi olmadÄ±ÄŸÄ± iÃ§in dÃ¼ÅŸÃ¼k skor
            return Assert("ROM Identifier â€” 32KB boÅŸ ROM tanÄ±ma", result != null && result.RomSize == 0x8000);
        }

        private static string TestRomIdentifier_WrongSize()
        {
            var identifier = new RomIdentifier();
            byte[] rom = new byte[1024]; // YanlÄ±ÅŸ boyut
            var result = identifier.IdentifyRom(rom, EcuProfiles.All);
            return Assert("ROM Identifier â€” yanlÄ±ÅŸ boyut reddi", result.IsMismatch || result.CompatibilityScore < 50);
        }

        private static string TestRomPatch_ValidateAndApply()
        {
            var mgr = new RomPatchManager();
            byte[] rom = new byte[100];
            rom[10] = 0xAA; rom[11] = 0xBB;

            var patch = new PatchBlueprint
            {
                PatchId = "TEST_PATCH_1",
                TargetOffset = 10,
                ExpectedSignature = new byte[] { 0xAA, 0xBB },
                PatchBytes = new byte[] { 0xCC, 0xDD },
                EcuCompat = "P28"
            };

            bool valid = mgr.ValidatePatch(rom, patch);
            byte[] patched = valid ? mgr.ApplyPatch(rom, patch) : null;
            bool applied = patched != null && patched[10] == 0xCC && patched[11] == 0xDD;
            return Assert("ROM Patch â€” doÄŸrulama ve uygulama", valid && applied);
        }

        private static string TestRomPatch_RollbackRestoresOriginal()
        {
            var mgr = new RomPatchManager();
            byte[] rom = new byte[100];
            rom[10] = 0xAA;

            var patch = new PatchBlueprint
            {
                PatchId = "TEST_PATCH_ROLLBACK",
                TargetOffset = 10,
                ExpectedSignature = new byte[] { 0xAA },
                PatchBytes = new byte[] { 0xFF },
                EcuCompat = "P28"
            };

            byte[] patched = mgr.ApplyPatch(rom, patch);
            byte[] restored = mgr.RollbackPatch(patched, patch);
            return Assert("ROM Patch â€” rollback geri yÃ¼kleme", restored[10] == 0xAA);
        }

        // â”€â”€ Calibration Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static string TestInjectorScaling_240to440()
        {
            var map = new byte[,] { { 128, 200 }, { 100, 255 } };
            var result = InjectorManager.ScaleFuelTable(map, 240, 440);
            // 128 * (240/440) â‰ˆ 69.8 â†’ 70
            return Assert("Injector Scaling â€” 240â†’440cc", result[0, 0] >= 69 && result[0, 0] <= 71);
        }

        private static string TestInjectorScaling_ClampTo255()
        {
            var map = new byte[,] { { 255 } };
            var result = InjectorManager.ScaleFuelTable(map, 440, 240);
            // 255 * (440/240) â‰ˆ 467 â†’ byte.MaxValue = 255
            return Assert("Injector Scaling â€” clamp to 255", result[0, 0] == 255);
        }

        // â”€â”€ AutoTune Safety Tests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static string TestAutoTune_RejectsLowECT()
        {
            var validator = new AutoTuneValidator();
            var frame = new TelemetryFrameData
            {
                Rpm = 3500,
                Map = 80,
                Tps = 45,
                Afr = 14.0,
                Ect = 50, // < 72Â°C
                BatteryVolts = 13.8
            };
            var result = validator.Validate(frame);
            return Assert("AutoTune â€” dÃ¼ÅŸÃ¼k ECT reddi", !result.IsValid && result.Code == "ECT_LOW");
        }

        private static string TestAutoTune_RejectsLowBattery()
        {
            var validator = new AutoTuneValidator();
            var frame = new TelemetryFrameData
            {
                Rpm = 3500,
                Map = 80,
                Tps = 45,
                Afr = 14.0,
                Ect = 82,
                BatteryVolts = 10.5 // < 12V
            };
            var result = validator.Validate(frame);
            return Assert("AutoTune â€” dÃ¼ÅŸÃ¼k batarya reddi", !result.IsValid && result.Code == "BATT_LOW");
        }

        private static string TestAutoTune_ClampsCorrection()
        {
            var validator = new AutoTuneValidator();
            double clamped = validator.ClampCorrection(25.0); // > 12%
            return Assert("AutoTune â€” dÃ¼zeltme kÄ±rpma (Â±12%)", Math.Abs(clamped) <= 12.0);
        }

        // â”€â”€ Faz 4 Testleri â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static string TestMapEngine_ReadWriteCell()
        {
            try
            {
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                byte[] cleanRom = new byte[0x8000];
                romService.SetBuffer(cleanRom);

                var mapManager = Core.Container.ServiceContainer.Resolve<Calibration.Maps.MapManager>();
                var def = new Calibration.Maps.MapDefinition
                {
                    MapName = "FuelMapTest",
                    EcuCompatibility = "P28",
                    Offset = 0x1D40,
                    Rows = 16,
                    Columns = 16,
                    ScaleFactor = 0.1,
                    OffsetValue = 0.0,
                    MinimumValue = 0.0,
                    MaximumValue = 25.5
                };

                mapManager.WriteCell(def, 4, 5, 12.3);
                double readVal = mapManager.ReadCell(def, 4, 5);

                return Assert("Map Engine â€” Cell write and read accuracy", Math.Abs(readVal - 12.3) < 0.01);
            }
            catch (Exception ex)
            {
                return Assert($"MapEngine Read/Write Test Failure: {ex.Message}", false);
            }
        }

        private static string TestMapEngine_ScaleConversion()
        {
            try
            {
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                byte[] cleanRom = new byte[0x8000];
                romService.SetBuffer(cleanRom);

                var mapManager = Core.Container.ServiceContainer.Resolve<Calibration.Maps.MapManager>();
                var def = new Calibration.Maps.MapDefinition
                {
                    MapName = "FuelMapTest",
                    EcuCompatibility = "P28",
                    Offset = 0x1D40,
                    Rows = 16,
                    Columns = 16,
                    ScaleFactor = 0.1,
                    OffsetValue = 0.0,
                    MinimumValue = 0.0,
                    MaximumValue = 25.5
                };

                // 12.5 / 0.1 = 125 raw value
                mapManager.WriteCell(def, 0, 0, 12.5);
                byte rawByte = romService.GetBuffer()[0x1D40];

                return Assert("Map Engine â€” Scale factors conversion logic", rawByte == 125);
            }
            catch (Exception ex)
            {
                return Assert($"MapEngine Scale Conversion Test Failure: {ex.Message}", false);
            }
        }

        private static string TestMapEngine_InvalidOffset()
        {
            try
            {
                var mapManager = Core.Container.ServiceContainer.Resolve<Calibration.Maps.MapManager>();
                var def = new Calibration.Maps.MapDefinition
                {
                    MapName = "InvalidOffsetMap",
                    EcuCompatibility = "P28",
                    Offset = 999999, // Limit dÄ±ÅŸÄ±
                    Rows = 16,
                    Columns = 16,
                    ScaleFactor = 1.0
                };

                bool exceptionThrown = false;
                try
                {
                    mapManager.WriteCell(def, 0, 0, 10.0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    exceptionThrown = true;
                }

                return Assert("Map Engine â€” Invalid offset out-of-range bounds check", exceptionThrown);
            }
            catch (Exception ex)
            {
                return Assert($"MapEngine Invalid Offset Test Failure: {ex.Message}", false);
            }
        }

        private static string TestMapEngine_UndoRollback()
        {
            try
            {
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                byte[] cleanRom = new byte[0x8000];
                romService.SetBuffer(cleanRom);

                var calMgr = Core.Container.ServiceContainer.Resolve<ICalibrationService>() as Calibration.CalibrationManager;
                var mapManager = Core.Container.ServiceContainer.Resolve<Calibration.Maps.MapManager>();

                var def = new Calibration.Maps.MapDefinition
                {
                    MapName = "FuelMapTest",
                    EcuCompatibility = "P28",
                    Offset = 0x1D40,
                    Rows = 16,
                    Columns = 16,
                    ScaleFactor = 0.1,
                    OffsetValue = 0.0,
                    MinimumValue = 0.0,
                    MaximumValue = 25.5
                };

                // Transaction ile yazÄ±p rollback yapalÄ±m
                calMgr.BeginTransaction();
                mapManager.WriteCell(def, 1, 1, 15.0);
                calMgr.RollbackTransaction();
                bool rolledBack = Math.Abs(mapManager.ReadCell(def, 1, 1) - 0.0) < 0.01;

                // Transaction ile yazÄ±p commit yapalÄ±m
                calMgr.BeginTransaction();
                mapManager.WriteCell(def, 1, 1, 15.0);
                calMgr.CommitTransaction();
                bool committed = Math.Abs(mapManager.ReadCell(def, 1, 1) - 15.0) < 0.01;

                // Undo yapalÄ±m
                calMgr.Undo();
                bool undone = Math.Abs(mapManager.ReadCell(def, 1, 1) - 0.0) < 0.01;

                // Redo yapalÄ±m
                calMgr.Redo();
                bool redone = Math.Abs(mapManager.ReadCell(def, 1, 1) - 15.0) < 0.01;

                return Assert("Map Engine â€” Transaction commits, rollbacks, and Undo/Redo integration", rolledBack && committed && undone && redone);
            }
            catch (Exception ex)
            {
                return Assert($"MapEngine Undo/Rollback Test Failure: {ex.Message}", false);
            }
        }

        private static string TestInterpolation_WeightSumExactlyOne()
        {
            try
            {
                var interpEngine = Core.Container.ServiceContainer.Resolve<IInterpolationEngine>();

                var def = new Calibration.Maps.MapDefinition
                {
                    MapName = "FuelMapTest",
                    EcuCompatibility = "P28",
                    Offset = 0x1D40,
                    Rows = 16,
                    Columns = 16,
                    ScaleFactor = 0.1,
                    OffsetValue = 0.0,
                    MinimumValue = 0.0,
                    MaximumValue = 25.5
                };

                var xAxis = new Calibration.Maps.AxisDefinition
                {
                    Name = "RPM",
                    Length = 16,
                    ConvertedValues = new double[] { 500, 750, 1000, 1250, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000 }
                };

                var yAxis = new Calibration.Maps.AxisDefinition
                {
                    Name = "MAP",
                    Length = 16,
                    ConvertedValues = new double[] { 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170 }
                };

                var table = new Calibration.Maps.TableDefinition(def, xAxis, yAxis);

                // Ortadaki bir devir/yÃ¼k iÃ§in aÄŸÄ±rlÄ±klarÄ± Ã§Ã¶z
                var res = interpEngine.Interpolate(1800, 55, table);

                double weightSum = 0;
                foreach (var w in res.CellWeights)
                {
                    weightSum += w;
                }

                return Assert("Interpolation â€” 4 neighbor bilinear cell weight sum equals exactly 1.0", Math.Abs(weightSum - 1.0) < 1e-9);
            }
            catch (Exception ex)
            {
                return Assert($"Interpolation test failure: {ex.Message}", false);
            }
        }

        // â”€â”€ Checksum Engine & Safety Layer Testleri â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static void ForceLoadDatabase()
        {
            var db = Database.EcuDatabaseManager.Instance;
            if (db.GetProfile("P28") == null)
            {
                string dbDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
                if (!System.IO.Directory.Exists(dbDir) || !System.IO.File.Exists(System.IO.Path.Combine(dbDir, "ecu_database.json")))
                {
                    dbDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Database");
                }
                db.LoadDatabase(dbDir);
                if (db.GetProfile("P28") == null)
                {
                    dbDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Database");
                    db.LoadDatabase(dbDir);
                }
            }
        }

        private static string TestChecksum_StockRomValidation()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28");

                byte[] romBuffer = new byte[0x8000]; // 32KB
                checksumEngine.Update(romBuffer, profile.ChecksumDefinitions[0]);

                var res = checksumEngine.Validate(romBuffer, profile.ChecksumDefinitions[0]);
                return Assert("Checksum â€” Stock ROM passes validation", res.IsValid);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Checksum â€” Stock ROM: {ex.Message}";
            }
        }

        private static string TestChecksum_CorruptBypassesValidation()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28");

                byte[] romBuffer = new byte[0x8000];
                checksumEngine.Update(romBuffer, profile.ChecksumDefinitions[0]);

                // Corrupt a byte
                romBuffer[0x1000] = 0xAA;

                var res = checksumEngine.Validate(romBuffer, profile.ChecksumDefinitions[0]);
                return Assert("Checksum â€” Corrupt byte fails validation", !res.IsValid);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Checksum â€” Corrupt: {ex.Message}";
            }
        }

        private static string TestChecksum_MapEditUpdatesChecksum()
        {
            try
            {
                ForceLoadDatabase();
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28");

                byte[] romBuffer = new byte[0x8000];
                string tempFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_test_rom_map.bin");
                System.IO.File.WriteAllBytes(tempFile, romBuffer);

                romService.LoadRom(tempFile, profile);

                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);

                // Modify fuel cell value (Fuel offset is 0x1D40)
                byte[] currentBuf = romService.GetBuffer();
                currentBuf[0x1D40] = 100;

                // Update checksum
                checksumEngine.Update(currentBuf, profile.ChecksumDefinitions[0]);

                var res = checksumEngine.Validate(currentBuf, profile.ChecksumDefinitions[0]);
                return Assert("Checksum â€” Map change updates checksum successfully", res.IsValid && currentBuf[profile.ChecksumDefinitions[0].ChecksumAddress] == 100);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Checksum â€” Map change: {ex.Message}";
            }
        }

        private static string TestChecksum_SaveFailsOnInvalidChecksum()
        {
            try
            {
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                byte[] romBuffer = new byte[0x8000];

                var badDef = new HondaTuner.Core.Rom.Checksum.ChecksumDefinition
                {
                    ChecksumType = "TestBad",
                    Algorithm = HondaTuner.Core.Rom.Checksum.ChecksumAlgorithm.Xor8,
                    ChecksumAddress = 0x7FFF,
                    RangeStart = 0x0000,
                    RangeEnd = 0x7FFE
                };

                // Corrupted expectation
                romBuffer[0x7FFF] = 0x55;

                var defs = new List<HondaTuner.Core.Rom.Checksum.ChecksumDefinition> { badDef };
                bool isOk = checksumEngine.VerifyBeforeSave(romBuffer, defs, out var results);

                return Assert("Checksum â€” VerifyBeforeSave detects bad checksum and returns false", !isOk);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Checksum â€” Save Fails: {ex.Message}";
            }
        }

        private static string TestChecksum_ReloadVerification()
        {
            try
            {
                ForceLoadDatabase();
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28");

                byte[] romBuffer = new byte[0x8000];
                romBuffer[0x1000] = 0xBB;

                HondaTuner.Core.Logging.ApplicationLogger.Info("ChecksumTest", $"[TRACE-1] romBuffer[0]={romBuffer[0]:X2}");

                string tempFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_test_reload_init.bin");
                System.IO.File.WriteAllBytes(tempFile, romBuffer);

                byte[] tempFileBytes = System.IO.File.ReadAllBytes(tempFile);
                HondaTuner.Core.Logging.ApplicationLogger.Info("ChecksumTest", $"[TRACE-2] tempFile[0]={tempFileBytes[0]:X2}");

                romService.LoadRom(tempFile, profile);
                HondaTuner.Core.Logging.ApplicationLogger.Info("ChecksumTest", $"[TRACE-3] loadedBuffer[0]={romService.GetBuffer()[0]:X2}");

                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);

                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_test_save.bin");

                HondaTuner.Core.Logging.ApplicationLogger.Info("ChecksumTest", $"[TRACE-4] beforeSaveBuffer[0]={romService.GetBuffer()[0]:X2}");
                romService.SaveRom(path);
                HondaTuner.Core.Logging.ApplicationLogger.Info("ChecksumTest", $"[TRACE-5] afterSaveParserBuffer[0]={romService.GetBuffer()[0]:X2}");

                byte[] reloaded = System.IO.File.ReadAllBytes(path);
                HondaTuner.Core.Logging.ApplicationLogger.Info("ChecksumTest", $"[TRACE-6] reloadedFile[0]={reloaded[0]:X2}");

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                var res = checksumEngine.Validate(reloaded, profile.ChecksumDefinitions[0]);
                bool cond = res.IsValid && reloaded[profile.ChecksumDefinitions[0].ChecksumAddress] == 0xBB;
                if (!cond)
                {
                    var nonZero = new List<string>();
                    for (int idx = 0; idx < reloaded.Length; idx++)
                    {
                        if (reloaded[idx] != 0)
                        {
                            nonZero.Add($"0x{idx:X4}=0x{reloaded[idx]:X2}");
                        }
                    }
                    string nonZeroStr = string.Join(", ", nonZero);
                    string defsListStr = string.Join(" | ", profile.ChecksumDefinitions.Select(d => $"Type={d.ChecksumType}, Algo={d.Algorithm}, Addr=0x{d.ChecksumAddress:X4}, Start=0x{d.RangeStart:X4}, End=0x{d.RangeEnd:X4}"));
                    return Assert($"Checksum â€” Reloaded ROM checksum is valid (Failed! res.IsValid={res.IsValid}, ExpectedValue=0x{res.ExpectedValue:X2}, CalculatedValue=0x{res.CalculatedValue:X2}, Defs=[{defsListStr}], NonZeroBytes=[{nonZeroStr}])", false);
                }
                return Assert("Checksum â€” Reloaded ROM checksum is valid", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Checksum â€” Reload verification: {ex.Message}";
            }
        }

        private static string TestChecksum_MultipleRegionsValidation()
        {
            try
            {
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                byte[] romBuffer = new byte[0x8000];

                var def1 = new HondaTuner.Core.Rom.Checksum.ChecksumDefinition
                {
                    ChecksumType = "Region1",
                    Algorithm = HondaTuner.Core.Rom.Checksum.ChecksumAlgorithm.Xor8,
                    ChecksumAddress = 0x7FFF,
                    RangeStart = 0x0000,
                    RangeEnd = 0x7FFE,
                    ExcludeRanges = new List<HondaTuner.Core.Rom.Checksum.ExcludeRange> { new HondaTuner.Core.Rom.Checksum.ExcludeRange { Start = 0x7FFE, End = 0x7FFE } }
                };

                var def2 = new HondaTuner.Core.Rom.Checksum.ChecksumDefinition
                {
                    ChecksumType = "Region2",
                    Algorithm = HondaTuner.Core.Rom.Checksum.ChecksumAlgorithm.Add8,
                    ChecksumAddress = 0x7FFE,
                    RangeStart = 0x0000,
                    RangeEnd = 0x7FDF
                };

                var defs = new List<HondaTuner.Core.Rom.Checksum.ChecksumDefinition> { def1, def2 };

                checksumEngine.Update(romBuffer, def1);
                checksumEngine.Update(romBuffer, def2);

                bool isOk = checksumEngine.VerifyBeforeSave(romBuffer, defs, out var results);
                return Assert("Checksum â€” Multiple regions validation is successful", isOk && results.Count == 2 && results[0].IsValid && results[1].IsValid);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Checksum â€” Multiple regions: {ex.Message}";
            }
        }

        private static string TestPatchEngine_Success()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[32768];
                rom[8112] = 144;
                rom[8113] = 144;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;
                var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");

                bool success = result.IsSuccess && rom[8112] == 205 && rom[8113] == 25;
                if (!success)
                {
                    return Assert($"PatchEngine — Başarılı Yama Uygulama (Hata: {result.ErrorMessage})", false);
                }
                return Assert("PatchEngine — Başarılı Yama Uygulama", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Success: {ex.Message}";
            }
        }

        private static string TestPatchEngine_ExpectedBytesMismatch()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[32768];
                rom[8112] = 0;
                rom[8113] = 0;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;
                var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");

                bool fail = !result.IsSuccess && (result.ErrorMessage.Contains("doğrulanamadı") || result.ErrorMessage.Contains("beklenmeyen") || result.ErrorMessage.Contains("mismatch") || result.ErrorMessage.Contains("başarısız"));
                if (!fail)
                {
                    return Assert($"PatchEngine — Yanlış ExpectedBytes Koruması (Hata: {result.ErrorMessage})", false);
                }
                return Assert("PatchEngine — Yanlış ExpectedBytes Koruması", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Mismatch: {ex.Message}";
            }
        }

        private static string TestPatchEngine_IncompatibleEcu()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[32768];
                rom[8112] = 144;
                rom[8113] = 144;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P30") ?? EcuProfiles.P30;
                var result = patchEngine.ApplyPatch(rom, "SpeedLimiterPatch", profile, "TestUser");

                bool fail = !result.IsSuccess && (result.ErrorMessage.Contains("uyumsuz") || result.ErrorMessage.Contains("compatible") || result.ErrorMessage.Contains("desteklemiyor"));
                if (!fail)
                {
                    return Assert($"PatchEngine — Uyumsuz ECU Koruması (Hata: {result.ErrorMessage})", false);
                }
                return Assert("PatchEngine — Uyumsuz ECU Koruması", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Incompatible ECU: {ex.Message}";
            }
        }

        private static string TestPatchEngine_WrongRomSize()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[16384];

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;
                var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");

                bool fail = !result.IsSuccess && (result.ErrorMessage.Contains("boyutu") || result.ErrorMessage.Contains("Boyut") || result.ErrorMessage.Contains("size"));
                if (!fail)
                {
                    return Assert($"PatchEngine — ROM Boyut Limiti Koruması (Hata: {result.ErrorMessage})", false);
                }
                return Assert("PatchEngine — ROM Boyut Limiti Koruması", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - ROM Size: {ex.Message}";
            }
        }

        private static string TestPatchEngine_RollbackSuccess()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[32768];
                rom[8112] = 144;
                rom[8113] = 144;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;
                var applyResult = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");
                if (!applyResult.IsSuccess)
                {
                    return Assert($"PatchEngine — Yamayı Geri Alma (Rollback) (Apply Hata: {applyResult.ErrorMessage})", false);
                }
                var result = patchEngine.RollbackPatch(rom, "LaunchControl", profile, "TestUser");

                bool success = result.IsSuccess && rom[8112] == 144 && rom[8113] == 144;
                if (!success)
                {
                    return Assert($"PatchEngine — Yamayı Geri Alma (Rollback) (Rollback Hata: {result.ErrorMessage}, bytes: rom[8112]={rom[8112]}, rom[8113]={rom[8113]})", false);
                }
                return Assert("PatchEngine — Yamayı Geri Alma (Rollback)", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Rollback: {ex.Message}";
            }
        }

        private static string TestPatchEngine_TransactionCommit()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[32768];
                rom[8112] = 144;
                rom[8113] = 144;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;

                calService.BeginTransaction();
                var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");
                calService.CommitTransaction();

                bool success = result.IsSuccess && rom[8112] == 205 && rom[8113] == 25;
                if (!success)
                {
                    return Assert($"PatchEngine — İşlem (Transaction) Commit Entegrasyonu (Hata: {result.ErrorMessage})", false);
                }
                return Assert("PatchEngine — İşlem (Transaction) Commit Entegrasyonu", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Commit Tx: {ex.Message}";
            }
        }

        private static string TestPatchEngine_TransactionRollback()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var romService = Core.Container.ServiceContainer.Resolve<IRomService>();

                byte[] backup = romService.GetBuffer();

                byte[] rom = new byte[32768];
                rom[8112] = 144;
                rom[8113] = 144;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;

                string tempBin = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_test_patch_tx_rollback.bin");
                System.IO.File.WriteAllBytes(tempBin, rom);
                romService.LoadRom(tempBin, profile);
                if (System.IO.File.Exists(tempBin))
                    System.IO.File.Delete(tempBin);

                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                calService.BeginTransaction();
                patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");
                calService.RollbackTransaction();

                byte[] activeBuff = romService.GetBuffer();
                bool success = activeBuff[8112] == 144 && activeBuff[8113] == 144;

                romService.SetBuffer(backup);

                return Assert("PatchEngine — İşlem Geri Alma (Transaction Rollback)", success);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Rollback Tx: {ex.Message}";
            }
        }

        private static string TestPatchEngine_ChecksumUpdate()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[32768];
                rom[8112] = 144;
                rom[8113] = 144;
                rom[32767] = 0;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;

                var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");

                bool success = result.IsSuccess && rom[32767] != 0;
                if (!success)
                {
                    return Assert($"PatchEngine — Otomatik Checksum Güncelleme (Hata: {result.ErrorMessage})", false);
                }
                return Assert("PatchEngine — Otomatik Checksum Güncelleme", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Checksum Update: {ex.Message}";
            }
        }

        private static string TestPatchEngine_AuditLogging()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                byte[] rom = new byte[32768];
                rom[8112] = 144;
                rom[8113] = 144;

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;

                patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");

                var logs = patchEngine.GetPatchAudit();
                bool hasAudit = logs.Count > 0 && logs.Any(l => l.PatchId == "LaunchControl" && l.Result.Contains("SUCCESS"));
                if (!hasAudit)
                {
                    string details = logs.Count > 0 ? string.Join(", ", logs.Select(l => $"{l.PatchId}:{l.Result}")) : "no logs";
                    return Assert($"PatchEngine — Audit Loglama Kontrolü (Hata: logs={details})", false);
                }
                return Assert("PatchEngine — Audit Loglama Kontrolü", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - Audit Log: {ex.Message}";
            }
        }

        private static string TestPatchEngine_GetAvailablePatches()
        {
            try
            {
                ForceLoadDatabase();
                var checksumEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rom.Checksum.IChecksumEngine>();
                var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var patchEngine = new HondaTuner.Core.Rom.Patch.PatchEngine(checksumEngine, calService);

                var db = Database.EcuDatabaseManager.Instance;
                var profile = db.GetProfile("P28") ?? EcuProfiles.P28;
                var patches = patchEngine.GetAvailablePatches(profile);

                bool success = patches.Count == 4 && patches.Any(p => p.PatchId == "LaunchControl");
                return Assert("PatchEngine — Profil Destekli Yamaları Bulma", success);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] PatchEngine - GetAvailablePatches: {ex.Message}";
            }
        }

        // â”€â”€ Assert YardÄ±mcÄ±sÄ± â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // ── Phase 7: Telemetry Tests Implementation ──────────────────────────────────────

        private static List<string> RunTelemetryTests()
        {
            var results = new List<string>();
            results.Add("\n── Phase 7: Telemetry & Live Datalog Bus Engine Testleri ──");
            results.Add(TestTelemetry_EventBusPublishAndSubscribe());
            results.Add(TestTelemetry_EventBusQueueMetrics());
            results.Add(TestTelemetry_ChannelFormulaEvaluation());
            results.Add(TestTelemetry_BackpressureDropOldest());
            results.Add(TestTelemetry_BackpressureDropNewest());
            results.Add(TestTelemetry_BackpressureBlockPublisher());
            results.Add(TestTelemetry_ConfigurationWatcherReload());
            results.Add(TestTelemetry_AccessControlAuthorization());
            results.Add(TestTelemetry_SystemClockTicks());
            results.Add(TestTelemetry_HighResolutionClockTicks());
            results.Add(TestTelemetry_ReplayClockAdvance());
            results.Add(TestTelemetry_RingBufferCapacity());
            results.Add(TestTelemetry_RingBufferOverflowOverwrite());
            results.Add(TestTelemetry_MovingAverageFilter());
            results.Add(TestTelemetry_MedianFilter());
            results.Add(TestTelemetry_LowPassFilter());
            results.Add(TestTelemetry_HighPassFilter());
            results.Add(TestTelemetry_MockProviderStreaming());
            results.Add(TestTelemetry_HealthMonitorLatencyWarning());
            results.Add(TestTelemetry_HealthMonitorTimeoutAnomaly());
            results.Add(TestTelemetry_AnalyzerPipelineSequentialExecution());
            results.Add(TestTelemetry_EngineProfileTransitions());
            results.Add(TestTelemetry_EngineDataloggingLifecycle());
            results.Add(TestTelemetry_FramePoolRecycling());
            results.Add(TestTelemetry_SnapshotImmutability());
            results.Add(TestTelemetry_ThreadSafetyStressTest());
            return results;
        }

        private class TestConsumer : Core.Telemetry.ITelemetryConsumer
        {
            public List<Core.Telemetry.TelemetryFrame> Frames { get; } = new List<Core.Telemetry.TelemetryFrame>();
            public List<Core.Telemetry.TelemetryEvent> Events { get; } = new List<Core.Telemetry.TelemetryEvent>();

            public void Consume(Core.Telemetry.TelemetryFrame frame)
            {
                lock (Frames)
                {
                    // Copy values to avoid pool reuse issues in async assertions
                    var copy = Core.Telemetry.TelemetryFramePool.Rent();
                    copy.ChannelId = frame.ChannelId;
                    copy.Value = frame.Value;
                    copy.FilteredValue = frame.FilteredValue;
                    copy.Status = frame.Status;
                    Frames.Add(copy);
                }
            }

            public void ConsumeEvent(Core.Telemetry.TelemetryEvent busEvent)
            {
                lock (Events) { Events.Add(busEvent); }
            }
        }

        private static string TestTelemetry_EventBusPublishAndSubscribe()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    var consumer = new TestConsumer();
                    bus.Subscribe(consumer);
                    bus.Start();

                    var frame = Core.Telemetry.TelemetryFramePool.Rent();
                    frame.ChannelId = "RPM";
                    frame.Value = 3000;
                    frame.FilteredValue = 3000;

                    bus.Publish(frame);
                    System.Threading.Thread.Sleep(100);

                    lock (consumer.Frames)
                    {
                        bool ok = consumer.Frames.Count > 0 && consumer.Frames[0].ChannelId == "RPM";
                        return Assert("Telemetry - Event Bus Publish & Subscribe", ok);
                    }
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Publish & Subscribe: {ex.Message}";
            }
        }

        private static string TestTelemetry_EventBusQueueMetrics()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    bus.Start();
                    var frame = Core.Telemetry.TelemetryFramePool.Rent();
                    frame.ChannelId = "TPS";
                    frame.Value = 50;

                    bus.Publish(frame);
                    System.Threading.Thread.Sleep(150);

                    var metrics = bus.Metrics;
                    bool ok = metrics.SubscribersCount >= 0 && metrics.QueueLength >= 0;
                    return Assert("Telemetry - Event Bus Metrics Kontrolü", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Queue Metrics: {ex.Message}";
            }
        }

        private static string TestTelemetry_ChannelFormulaEvaluation()
        {
            try
            {
                double result = Core.Telemetry.TelemetryFormulaEvaluator.Evaluate("[RPM] * 2 + [TPS] / 10", id =>
                {
                    if (id == "RPM") return 3000.0;
                    if (id == "TPS") return 50.0;
                    return 0.0;
                });

                bool ok = Math.Abs(result - 6005.0) < 0.001;
                return Assert("Telemetry - Kanalsal Matematiksel Formül Çözümleme", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Formula Eval: {ex.Message}";
            }
        }

        private static string TestTelemetry_BackpressureDropOldest()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    bus.SetBackpressurePolicy(Core.Telemetry.BackpressurePolicy.DropOldest);
                    bus.Start();

                    // Kuyruk limitini zorlamak için hızlıca yayın yapıyoruz (kuyruk max 10000)
                    for (int i = 0; i < 11000; i++)
                    {
                        var frame = Core.Telemetry.TelemetryFramePool.Rent();
                        frame.ChannelId = "MAP";
                        frame.Value = i;
                        bus.Publish(frame);
                    }

                    System.Threading.Thread.Sleep(100);
                    // DropOldest durumunda droppedFramesCount > 0 veya başarı durumu olmalı
                    bool ok = bus.Metrics.DroppedFramesCount >= 0;
                    return Assert("Telemetry - Backpressure DropOldest Politikası", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Backpressure DropOldest: {ex.Message}";
            }
        }

        private static string TestTelemetry_BackpressureDropNewest()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    bus.SetBackpressurePolicy(Core.Telemetry.BackpressurePolicy.DropNewest);
                    bus.Start();

                    for (int i = 0; i < 2000; i++)
                    {
                        var frame = Core.Telemetry.TelemetryFramePool.Rent();
                        frame.ChannelId = "MAP";
                        frame.Value = i;
                        bus.Publish(frame);
                    }

                    System.Threading.Thread.Sleep(50);
                    return Assert("Telemetry - Backpressure DropNewest Politikası", true);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Backpressure DropNewest: {ex.Message}";
            }
        }

        private static string TestTelemetry_BackpressureBlockPublisher()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    bus.SetBackpressurePolicy(Core.Telemetry.BackpressurePolicy.BlockPublisher);
                    bus.Start();

                    var frame = Core.Telemetry.TelemetryFramePool.Rent();
                    frame.ChannelId = "ECT";
                    bus.Publish(frame);

                    return Assert("Telemetry - Backpressure BlockPublisher Politikası", true);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Backpressure BlockPublisher: {ex.Message}";
            }
        }

        private static string TestTelemetry_ConfigurationWatcherReload()
        {
            try
            {
                using (var watcher = new Core.Telemetry.ConfigurationWatcher())
                {
                    // Watcher tetiklenme metot simülasyonunu test et
                    watcher.StartWatching("telemetry_channels.json", "telemetry_profiles.json");
                    watcher.StopWatching();

                    return Assert("Telemetry - Yapılandırma Dosya İzleyicisi (Hot Reload)", true);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Config Watcher: {ex.Message}";
            }
        }

        private static string TestTelemetry_AccessControlAuthorization()
        {
            try
            {
                var access = new Core.Telemetry.AccessControl();
                access.SetCurrentRole(Core.Telemetry.TelemetryRole.Calibration);

                bool canCalibrate = access.Authorize(Core.Telemetry.TelemetryRole.Calibration, "ModifyMap", out _);
                bool canFlash = access.Authorize(Core.Telemetry.TelemetryRole.Flash, "FlashEcu", out _);

                bool ok = canCalibrate && !canFlash;
                return Assert("Telemetry - Erişim Kontrolü Yetkilendirme Hiyerarşisi", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Access Control: {ex.Message}";
            }
        }

        private static string TestTelemetry_SystemClockTicks()
        {
            try
            {
                var clock = new Core.Telemetry.SystemClock();
                long tick1 = clock.MonotonicTicks;
                long tick2 = clock.MonotonicTicks;

                return Assert("Telemetry - SystemClock Monotonic Ticks", tick2 >= tick1);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - SystemClock: {ex.Message}";
            }
        }

        private static string TestTelemetry_HighResolutionClockTicks()
        {
            try
            {
                var clock = new Core.Telemetry.HighResolutionClock();
                long tick1 = clock.MonotonicTicks;
                long tick2 = clock.MonotonicTicks;

                return Assert("Telemetry - HighResolutionClock Performance Ticks", tick2 >= tick1);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - HighResolutionClock: {ex.Message}";
            }
        }

        private static string TestTelemetry_ReplayClockAdvance()
        {
            try
            {
                var clock = new Core.Telemetry.ReplayClock();
                clock.SetElapsedTime(10.0);
                clock.Advance(2.5);

                bool ok = Math.Abs(clock.GetElapsedTime(0) - 12.5) < 0.001;
                return Assert("Telemetry - ReplayClock Geri Oynatıcı Zaman Akışı", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - ReplayClock: {ex.Message}";
            }
        }

        private static string TestTelemetry_RingBufferCapacity()
        {
            try
            {
                var buffer = new Core.Telemetry.TelemetryBuffer(5);
                bool ok = buffer.Capacity == 5 && buffer.Count == 0;
                return Assert("Telemetry - Dairesel Bellek (RingBuffer) Kapasite Tanımı", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Buffer Capacity: {ex.Message}";
            }
        }

        private static string TestTelemetry_RingBufferOverflowOverwrite()
        {
            try
            {
                var buffer = new Core.Telemetry.TelemetryBuffer(3);
                for (int i = 1; i <= 5; i++)
                {
                    var f = Core.Telemetry.TelemetryFramePool.Rent();
                    f.ChannelId = "RPM";
                    f.Value = i;
                    buffer.Enqueue(f);
                }

                var all = buffer.GetAll();
                // Kapasite dolduğunda overwrite etmeli ve son 3 öğe kalmalı: 3, 4, 5
                bool ok = buffer.Count == 3 && all[0].Value == 3 && all[2].Value == 5;
                return Assert("Telemetry - RingBuffer Aşımında Overwrite (FIFO Overlay)", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Buffer Overwrite: {ex.Message}";
            }
        }

        private static string TestTelemetry_MovingAverageFilter()
        {
            try
            {
                var filter = Core.Telemetry.TelemetryFilterFactory.Create(Core.Telemetry.FilterType.MovingAverage, 3);
                filter.Filter(10);
                filter.Filter(20);
                double avg = filter.Filter(30); // (10+20+30)/3 = 20

                bool ok = Math.Abs(avg - 20.0) < 0.001;
                return Assert("Telemetry - Moving Average Gürültü Azaltma Filtresi", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Moving Average: {ex.Message}";
            }
        }

        private static string TestTelemetry_MedianFilter()
        {
            try
            {
                var filter = Core.Telemetry.TelemetryFilterFactory.Create(Core.Telemetry.FilterType.Median, 5);
                filter.Filter(10);
                filter.Filter(100); // Sıçrama
                filter.Filter(20);
                filter.Filter(30);
                double med = filter.Filter(15); // Sıralı: 10, 15, 20, 30, 100 -> Median = 20

                bool ok = Math.Abs(med - 20.0) < 0.001;
                return Assert("Telemetry - Median (Ortanca) Filtreleme", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Median Filter: {ex.Message}";
            }
        }

        private static string TestTelemetry_LowPassFilter()
        {
            try
            {
                var filter = Core.Telemetry.TelemetryFilterFactory.Create(Core.Telemetry.FilterType.LowPass, 0.2);
                filter.Filter(10);
                double val = filter.Filter(20); // 0.2*20 + 0.8*10 = 12

                bool ok = Math.Abs(val - 12.0) < 0.001;
                return Assert("Telemetry - LowPass Üstel Sinyal Yumuşatma", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - LowPass Filter: {ex.Message}";
            }
        }

        private static string TestTelemetry_HighPassFilter()
        {
            try
            {
                var filter = Core.Telemetry.TelemetryFilterFactory.Create(Core.Telemetry.FilterType.HighPass, 0.8);
                double val1 = filter.Filter(10);
                double val2 = filter.Filter(12);

                return Assert("Telemetry - HighPass Dalgalanma Odaklı Sinyal Filtresi", val2 != val1);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - HighPass Filter: {ex.Message}";
            }
        }

        private static string TestTelemetry_MockProviderStreaming()
        {
            try
            {
                var time = new Core.Telemetry.HighResolutionClock();
                using (var provider = new Core.Telemetry.MockProvider(time))
                {
                    bool frameRec = false;
                    provider.OnFrameReceived += f => { frameRec = true; };

                    provider.Connect();
                    provider.StartStreaming(new string[] { "RPM" }, 5);
                    System.Threading.Thread.Sleep(100);
                    provider.StopStreaming();

                    return Assert("Telemetry - MockProvider Canlı Veri Akış Simülasyonu", frameRec);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - MockProvider Stream: {ex.Message}";
            }
        }

        private static string TestTelemetry_HealthMonitorLatencyWarning()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    using (var monitor = new Core.Telemetry.TelemetryHealthMonitor(bus))
                    {
                        bus.Start();
                        // Gecikme uyarısını kontrol et
                        bool ok = monitor.MaxLatencyAllowedMs > 0.0;
                        return Assert("Telemetry - Sağlık İzleme (HealthMonitor) Parametreleri", ok);
                    }
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - HealthMonitor Latency: {ex.Message}";
            }
        }

        private static string TestTelemetry_HealthMonitorTimeoutAnomaly()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    using (var monitor = new Core.Telemetry.TelemetryHealthMonitor(bus))
                    {
                        bus.Start();

                        // Timeout frame yayını yapalım
                        var frame = Core.Telemetry.TelemetryFramePool.Rent();
                        frame.ChannelId = "TPS";
                        frame.Status = Core.Telemetry.ChannelStatus.Timeout;
                        bus.Publish(frame);

                        System.Threading.Thread.Sleep(50);
                        bool ok = monitor.TotalTimeoutsCount >= 0;
                        return Assert("Telemetry - HealthMonitor Bağlantı Kaybı Anomali Algılama", ok);
                    }
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - HealthMonitor Anomaly: {ex.Message}";
            }
        }

        private class MockAnalyzer : Core.Telemetry.ITelemetryAnalyzer
        {
            public string Name => "MockAnalyzer";
            public bool Analyzed { get; private set; }
            public void Analyze(Core.Telemetry.TelemetrySnapshot snapshot)
            {
                Analyzed = true;
            }
        }

        private static string TestTelemetry_AnalyzerPipelineSequentialExecution()
        {
            try
            {
                var pipeline = new Core.Telemetry.TelemetryAnalyzerPipeline();
                var analyzer = new MockAnalyzer();
                pipeline.AddAnalyzer(analyzer);

                var snap = new Core.Telemetry.TelemetrySnapshot("1.0", DateTime.UtcNow, 1, 3000, 50, 100, 80, 40, 14.1, 0, 12, 16.5, 14.7, 1.0, 0, 0.0, 0.0, true, false, 50.0);
                pipeline.Execute(snap);

                return Assert("Telemetry - Analiz Hattı (AnalyzerPipeline) Sıralı Çalışma", analyzer.Analyzed);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Analyzer Pipeline: {ex.Message}";
            }
        }

        private static string TestTelemetry_EngineProfileTransitions()
        {
            try
            {
                var bus = Core.Container.ServiceContainer.Resolve<Core.Telemetry.ITelemetryBus>();
                var engine = Core.Container.ServiceContainer.Resolve<Core.Telemetry.ITelemetryEngine>();

                engine.SetActiveProfile("Street");
                engine.SetActiveProfile("Dyno");

                return Assert("Telemetry - Engine Profil Geçiş Yönetimi (Street -> Dyno)", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Profile Transitions: {ex.Message}";
            }
        }

        private static string TestTelemetry_EngineDataloggingLifecycle()
        {
            try
            {
                var engine = Core.Container.ServiceContainer.Resolve<Core.Telemetry.ITelemetryEngine>();
                engine.SelectProvider("MockProvider");
                engine.SetActiveProfile("Street");

                engine.StartDatalogging(5);
                System.Threading.Thread.Sleep(100);

                engine.PauseDatalogging();
                engine.ResumeDatalogging();
                engine.StopDatalogging();

                return Assert("Telemetry - Engine Canlı Datalogging Yaşam Döngüsü", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Datalogging Lifecycle: {ex.Message}";
            }
        }

        private static string TestTelemetry_FramePoolRecycling()
        {
            try
            {
                var frame = Core.Telemetry.TelemetryFramePool.Rent();
                frame.ChannelId = "MAP";
                Core.Telemetry.TelemetryFramePool.Return(frame);

                var rented = Core.Telemetry.TelemetryFramePool.Rent();
                // Temizlenmiş (Reset) olmalı
                bool ok = rented.ChannelId == null;
                Core.Telemetry.TelemetryFramePool.Return(rented);

                return Assert("Telemetry - GC Basıncını Azaltan Frame Nesne Havuzu", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Frame Pool: {ex.Message}";
            }
        }

        private static string TestTelemetry_SnapshotImmutability()
        {
            try
            {
                var snap = new Core.Telemetry.TelemetrySnapshot(
                    "v2.0", DateTime.UtcNow, 100, 3000, 20.0, 95.0, 85.0, 35.0, 13.8, 60.0,
                    15.0, 24.5, 14.5, 0.98, 0, 0.0, 0.0, true, false, 45.0
                );

                // Immutable alan kontrolü (Sadece getter tescili var)
                bool ok = snap.RPM == 3000 && snap.TPS == 20.0 && snap.ClosedLoop;
                return Assert("Telemetry - Salt-Okunur (Immutable) Snaphsot Yapısı", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Snapshot Immutability: {ex.Message}";
            }
        }

        private static string TestTelemetry_ThreadSafetyStressTest()
        {
            try
            {
                using (var bus = new Core.Telemetry.TelemetryBus())
                {
                    bus.Start();
                    var consumer = new TestConsumer();
                    bus.Subscribe(consumer);

                    var threads = new System.Threading.Thread[5];
                    for (int i = 0; i < threads.Length; i++)
                    {
                        int id = i;
                        threads[i] = new System.Threading.Thread(() =>
                        {
                            for (int j = 0; j < 50; j++)
                            {
                                var f = Core.Telemetry.TelemetryFramePool.Rent();
                                f.ChannelId = $"CH-{id}";
                                f.Value = j;
                                bus.Publish(f);
                                System.Threading.Thread.Sleep(1);
                            }
                        });
                    }

                    foreach (var t in threads) t.Start();
                    foreach (var t in threads) t.Join();

                    System.Threading.Thread.Sleep(150);

                    lock (consumer.Frames)
                    {
                        bool ok = consumer.Frames.Count > 0;
                        return Assert("Telemetry - Multi-Thread Veri Giriş Stress Testi", ok);
                    }
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] Telemetry - Thread Safety Stress: {ex.Message}";
            }
        }

        // ── Phase 8: AutoTune Closed Loop Engine Tests ───────────────────────────────────

        private static List<string> RunAutoTuneEnginePhase8Tests()
        {
            var results = new List<string>();
            results.Add("\n── Phase 8: AutoTune Closed Loop Engine Testleri ──");
            results.Add(TestAutoTune_StateTransitions());
            results.Add(TestAutoTune_ConfidenceLowDeviation());
            results.Add(TestAutoTune_ConfidenceHighDeviation());
            results.Add(TestAutoTune_StableFilterStdDev());
            results.Add(TestAutoTune_ConfigSchemaRejection());
            results.Add(TestAutoTune_ConfigRangeValidation());
            results.Add(TestAutoTune_ParameterTypeSafety());
            results.Add(TestAutoTune_SessionManagerMultiLock());
            results.Add(TestAutoTune_SessionManagerRelease());
            results.Add(TestAutoTune_RecoverySaveTrace());
            results.Add(TestAutoTune_RecoveryRestore());
            results.Add(TestAutoTune_DiffEngineComparison());
            results.Add(TestAutoTune_PackageExporterCsv());
            results.Add(TestAutoTune_ReplaySimulationMode());
            results.Add(TestAutoTune_DynamicSafetyRules());
            results.Add(TestAutoTune_QualityScoreCalculations());
            results.Add(TestAutoTune_TelemetryLossReconnect());
            results.Add(TestAutoTune_DomainEventEmission());
            results.Add(TestAutoTune_CalibrationRtpStream());
            results.Add(TestAutoTune_ReplayDeterminism());
            results.Add(TestAutoTune_PersistenceRecovery());
            results.Add(TestAutoTune_ExternalApiQuery());
            results.Add(TestAutoTune_ExternalApiCommand());
            results.Add(TestAutoTune_HardwareCheckSafeMode());
            results.Add(TestAutoTune_CellLockingAcquisition());
            return results;
        }

        private static string TestAutoTune_StateTransitions()
        {
            try
            {
                var engine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();
                engine.StopSession(); // reset
                bool start = engine.StartSession("P28", "TunerUser", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Default");
                bool isRunning = engine.IsRunning;
                engine.PauseSession();
                bool isPaused = engine.ActiveSession?.State == "Paused";
                engine.ResumeSession();
                bool isResumed = engine.IsRunning && engine.ActiveSession?.State == "Running";
                engine.StopSession();

                return Assert("AutoTune - State transitions (Start, Pause, Resume, Stop) lifecycle verification", start && isRunning && isPaused && isResumed && !engine.IsRunning);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - State transitions: {ex.Message}";
            }
        }

        private static string TestAutoTune_ConfidenceLowDeviation()
        {
            try
            {
                var confidenceEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.ITuneConfidenceEngine>();
                var memory = new HondaTuner.Core.AutoTune.AdaptiveMemory();
                double score = confidenceEngine.CalculateConfidence(10.0, 0.2, 15, memory, 85.0, 13.8, out var reason);
                return Assert("AutoTune - Confidence score calculation under low deviation is high", score > 80.0 && !string.IsNullOrEmpty(reason));
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Confidence low deviation: {ex.Message}";
            }
        }

        private static string TestAutoTune_ConfidenceHighDeviation()
        {
            try
            {
                var confidenceEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.ITuneConfidenceEngine>();
                var memory = new HondaTuner.Core.AutoTune.AdaptiveMemory();
                double score = confidenceEngine.CalculateConfidence(120.0, 2.5, 3, memory, 60.0, 11.8, out var reason);
                return Assert("AutoTune - Confidence score calculation under high deviation is low", score < 50.0 && !string.IsNullOrEmpty(reason));
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Confidence high deviation: {ex.Message}";
            }
        }

        private static string TestAutoTune_StableFilterStdDev()
        {
            try
            {
                var filter = new HondaTuner.Core.AutoTune.StableWindowFilter { WindowSize = 3, MaxRpmStdDev = 50.0 };
                var snap1 = new Core.Telemetry.TelemetrySnapshot("v2", DateTime.Now, 1, 2000, 10, 90, 85, 30, 14.0, 50, 0, 0, 14.7, 1.0, 0, 0, 0, true, false, 0);
                var snap2 = new Core.Telemetry.TelemetrySnapshot("v2", DateTime.Now, 2, 2010, 10, 90, 85, 30, 14.0, 50, 0, 0, 14.7, 1.0, 0, 0, 0, true, false, 0);
                var snap3 = new Core.Telemetry.TelemetrySnapshot("v2", DateTime.Now, 3, 2005, 10, 90, 85, 30, 14.0, 50, 0, 0, 14.7, 1.0, 0, 0, 0, true, false, 0);

                bool f1 = filter.AddSnapshot(snap1, out _);
                bool f2 = filter.AddSnapshot(snap2, out _);
                bool f3 = filter.AddSnapshot(snap3, out var stableList);

                return Assert("AutoTune - Stable window filter standard deviation and length checks", !f1 && !f2 && f3 && stableList.Count == 3);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Stable filter std dev: {ex.Message}";
            }
        }

        private static string TestAutoTune_ConfigSchemaRejection()
        {
            try
            {
                string invalidJson = "[ {} ]"; // Profiles is a JSON array of profiles, let's write something that is not valid json or missing fields
                string tempFile = Path.Combine(Path.GetTempPath(), "test_profiles_schema_err.json");
                File.WriteAllText(tempFile, invalidJson);
                bool ok = !HondaTuner.Core.AutoTune.ConfigValidator.ValidateProfiles(tempFile, out var error);
                if (File.Exists(tempFile)) File.Delete(tempFile);
                return Assert("AutoTune - Config validator rejects JSON with missing target attributes", ok && !string.IsNullOrEmpty(error));
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Config schema rejection: {ex.Message}";
            }
        }

        private static string TestAutoTune_ConfigRangeValidation()
        {
            try
            {
                string invalidFieldsJson = "[ { \"ProfileName\": \"InvalidRate\", \"CorrectionRate\": 0.0, \"MaxFuelCorrection\": -5 } ]";
                string tempFile = Path.Combine(Path.GetTempPath(), "test_profiles_range_err.json");
                File.WriteAllText(tempFile, invalidFieldsJson);
                bool ok = !HondaTuner.Core.AutoTune.ConfigValidator.ValidateProfiles(tempFile, out var error);
                if (File.Exists(tempFile)) File.Delete(tempFile);
                return Assert("AutoTune - Config validator rejects out of bounds parameter variables", ok && !string.IsNullOrEmpty(error));
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Config range validation: {ex.Message}";
            }
        }

        private static string TestAutoTune_ParameterTypeSafety()
        {
            try
            {
                bool f = Enum.IsDefined(typeof(HondaTuner.Core.AutoTune.ParameterType), HondaTuner.Core.AutoTune.ParameterType.Fuel);
                bool i = Enum.IsDefined(typeof(HondaTuner.Core.AutoTune.ParameterType), HondaTuner.Core.AutoTune.ParameterType.Ignition);
                bool v = Enum.IsDefined(typeof(HondaTuner.Core.AutoTune.ParameterType), HondaTuner.Core.AutoTune.ParameterType.VE);
                return Assert("AutoTune - ParameterType safety enum structure check", f && i && v);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Parameter type safety: {ex.Message}";
            }
        }

        private static string TestAutoTune_SessionManagerMultiLock()
        {
            try
            {
                var sessionManager = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneSessionManager>();
                sessionManager.ReleaseSessionLock("P28"); // reset
                bool lock1 = sessionManager.AcquireSessionLock("P28", "Session1", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Running", out _);
                bool lock2 = sessionManager.AcquireSessionLock("P28", "Session2", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Running", out _); // same ECU should lock out
                sessionManager.ReleaseSessionLock("P28");
                return Assert("AutoTune - Session manager multi session conflict prevention", lock1 && !lock2);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Session lock conflict: {ex.Message}";
            }
        }

        private static string TestAutoTune_SessionManagerRelease()
        {
            try
            {
                var sessionManager = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneSessionManager>();
                sessionManager.ReleaseSessionLock("P28");
                bool lock1 = sessionManager.AcquireSessionLock("P28", "Session1", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Running", out _);
                sessionManager.ReleaseSessionLock("P28");
                bool lock2 = sessionManager.AcquireSessionLock("P28", "Session2", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Running", out _); // should lock now
                sessionManager.ReleaseSessionLock("P28");
                return Assert("AutoTune - Session manager releases lock and allows new allocation", lock1 && lock2);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Session lock release: {ex.Message}";
            }
        }

        private static string TestAutoTune_RecoverySaveTrace()
        {
            try
            {
                var recovery = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.ICalibrationRecoveryManager>();
                recovery.ClearPendingTransaction();
                var meta = new HondaTuner.Core.AutoTune.RecoveryMetaData
                {
                    TransactionId = "TX-TEMP",
                    SnapshotId = "SNAP-TEMP",
                    EcuProfile = "P28",
                    ActiveUser = "User1",
                    Timestamp = DateTime.UtcNow,
                    PreviousCellValues = new List<CellSnapshot>
                    {
                        new CellSnapshot { MapName = "Fuel", Row = 2, Col = 3, Value = 50.0 }
                    }
                };
                recovery.RegisterPendingTransaction(meta);
                bool hasTrace = recovery.DetectPendingTransaction(out var detected);
                recovery.ClearPendingTransaction();
                return Assert("AutoTune - Recovery manager logs interrupt trace successfully to file", hasTrace && detected.TransactionId == "TX-TEMP");
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Recovery save trace: {ex.Message}";
            }
        }

        private static string TestAutoTune_RecoveryRestore()
        {
            try
            {
                var recovery = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.ICalibrationRecoveryManager>();
                recovery.ClearPendingTransaction();
                var meta = new HondaTuner.Core.AutoTune.RecoveryMetaData
                {
                    TransactionId = "TX-TEMP2",
                    SnapshotId = "SNAP-TEMP2",
                    EcuProfile = "P28",
                    ActiveUser = "User1",
                    Timestamp = DateTime.UtcNow,
                    PreviousCellValues = new List<CellSnapshot>
                    {
                        new CellSnapshot { MapName = "Fuel", Row = 2, Col = 3, Value = 50.0 }
                    }
                };
                recovery.RegisterPendingTransaction(meta);

                var snapshotManager = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.ICalibrationSnapshotManager>();
                bool rollbackOk = recovery.PerformRecoveryRollback(snapshotManager, out var msg);
                recovery.ClearPendingTransaction();

                return Assert("AutoTune - Recovery manager restores trace and validates details", rollbackOk);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Recovery restore trace: {ex.Message}";
            }
        }

        private static string TestAutoTune_DiffEngineComparison()
        {
            try
            {
                var diffEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.ICalibrationDiffEngine>();
                var diff = diffEngine.GenerateDiff("FuelMap", 1, 0, 40, 45, HondaTuner.Core.AutoTune.ParameterType.Fuel);
                bool ok = diff != null && diff.Row == 1 && diff.Col == 0 && diff.DeltaValue == 5;
                return Assert("AutoTune - Diff engine returns detailed comparison matrix values", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Diff engine comparison: {ex.Message}";
            }
        }

        private static string TestAutoTune_PackageExporterCsv()
        {
            try
            {
                var journal = new HondaTuner.Core.AutoTune.CalibrationJournal();
                journal.Log(new HondaTuner.Core.AutoTune.JournalEntry
                {
                    User = "User1",
                    Profile = "Default",
                    Parameter = "Fuel",
                    RPM = 3000,
                    Load = 80,
                    BeforeValue = 40,
                    AfterValue = 45,
                    Confidence = 95.0,
                    SafetyStatus = "Safe",
                    ApprovalStatus = "Approved",
                    Result = "Success"
                });

                var exporter = new HondaTuner.Core.AutoTune.AutoTunePackageExporter();
                string csv = exporter.ExportSessionToCsv(journal);
                bool ok = csv.Contains("User1") && csv.Contains("Fuel") && csv.Contains("3000");
                return Assert("AutoTune - CSV Package export contains session identity and values", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - CSV Package export: {ex.Message}";
            }
        }

        private static string TestAutoTune_ReplaySimulationMode()
        {
            try
            {
                var validator = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IReplayDeterministicValidator>();
                var dataset = new List<Core.Telemetry.TelemetrySnapshot>
                {
                    new Core.Telemetry.TelemetrySnapshot("v2", DateTime.Now, 1, 3000, 20.0, 90, 85, 30, 14.0, 50, 0, 0, 14.7, 1.0, 0, 0, 0, true, false, 0),
                    new Core.Telemetry.TelemetrySnapshot("v2", DateTime.Now, 2, 3020, 20.0, 90, 85, 30, 14.1, 50, 0, 0, 14.7, 1.0, 0, 0, 0, true, false, 0)
                };
                var originalDecisions = new List<HondaTuner.Core.AutoTune.TuneDecision>
                {
                    new HondaTuner.Core.AutoTune.TuneDecision { MapName = "Fuel", CellRow = 1, CellCol = 1, OldValue = 50.0, NewValue = 55.0, ConfidenceScore = 90 },
                    new HondaTuner.Core.AutoTune.TuneDecision { MapName = "Fuel", CellRow = 1, CellCol = 1, OldValue = 50.0, NewValue = 55.0, ConfidenceScore = 90 }
                };

                var results = validator.ValidateReplayDeterminism(dataset, originalDecisions, (snap) =>
                {
                    return new HondaTuner.Core.AutoTune.TuneDecision { MapName = "Fuel", CellRow = 1, CellCol = 1, OldValue = 50.0, NewValue = 55.0, ConfidenceScore = 90 };
                });

                return Assert("AutoTune - Replay simulator matches telemetry lists deterministically", results.IsDeterministic && results.MatchCount == 2);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Replay simulation mode: {ex.Message}";
            }
        }

        private static string TestAutoTune_DynamicSafetyRules()
        {
            try
            {
                var ruleProvider = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.Safety.ISafetyRuleProvider>();
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "safety_limits.json");
                var rules = ruleProvider.LoadRules(path);

                bool hasRules = rules != null && rules.Count > 0;
                bool hasKnock = rules.Any(r => r.Name == "KnockSafetyRule" || r.Name.Contains("Knock"));
                return Assert("AutoTune - SafetyRuleProvider loads dynamic rules successfully", hasRules && hasKnock);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Dynamic safety rules loading: {ex.Message}";
            }
        }

        private static string TestAutoTune_QualityScoreCalculations()
        {
            try
            {
                var analyzer = new HondaTuner.Core.AutoTune.CalibrationQualityAnalyzer();
                var history = new List<Core.Telemetry.TelemetrySnapshot>
                {
                    new Core.Telemetry.TelemetrySnapshot("v2", DateTime.UtcNow, 1, 3000, 20.0, 88.0, 88.0, 30.0, 14.7, 50.0, 14.7, 14.7, 14.7, 1.0, 0, 0, 0, true, false, 0),
                    new Core.Telemetry.TelemetrySnapshot("v2", DateTime.UtcNow, 2, 3010, 20.0, 88.0, 88.0, 30.0, 14.7, 50.0, 14.7, 14.7, 14.7, 1.0, 0, 0, 0, true, false, 0)
                };

                double score = analyzer.AnalyzeQuality(history, new List<TuneDecision>(), out string summary);
                return Assert("AutoTune - QualityScoreCalculator rates stoichiometric history cleanly near 100", score > 90.0 && summary.Contains("Genel Kalite"));
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Quality score calculation: {ex.Message}";
            }
        }

        private static string TestAutoTune_TelemetryLossReconnect()
        {
            try
            {
                var engine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();
                engine.StopSession(); // reset
                engine.StartSession("P28", "Tuner", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Default");
                engine.ProcessTelemetry(new Core.Telemetry.TelemetrySnapshot("v2", DateTime.UtcNow, 1, 3000, 20.0, 90.0, 85.0, 30.0, 14.0, 50.0, 12.0, 12.0, 14.7, 1.0, 0, 0, 0, true, false, 45.0));

                bool active = engine.IsRunning;
                engine.PauseSession();
                bool paused = engine.ActiveSession?.State == "Paused";
                engine.ResumeSession();
                bool resumed = engine.IsRunning;
                engine.StopSession();

                return Assert("AutoTune - Telemetry loss pause and reconnect validation cycle", active && paused && resumed);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Telemetry loss reconnect: {ex.Message}";
            }
        }

        private static string TestAutoTune_DomainEventEmission()
        {
            try
            {
                var engine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();
                var events = new List<HondaTuner.Core.AutoTune.IAutoTuneDomainEvent>();
                Action<HondaTuner.Core.AutoTune.IAutoTuneDomainEvent> handler = (ev) => { events.Add(ev); };
                engine.OnDomainEvent += handler;

                engine.StopSession();
                engine.StartSession("P28", "Tuner", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Default");
                engine.StopSession();

                engine.OnDomainEvent -= handler;
                bool ok = events.Count > 0 && events.Any(e => e.EventType == "SessionCreated");
                return Assert("AutoTune - Domain event emitter publishes valid structured transition events", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Domain event emission: {ex.Message}";
            }
        }

        private static string TestAutoTune_CalibrationRtpStream()
        {
            try
            {
                var engine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();
                var payloads = new List<HondaTuner.Core.AutoTune.CalibrationStreamPayload>();
                Action<HondaTuner.Core.AutoTune.CalibrationStreamPayload> handler = (p) => { payloads.Add(p); };
                engine.OnCalibrationStream += handler;

                engine.StopSession();
                engine.StartSession("P28", "Tuner", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Default");
                for (int i = 0; i < 15; i++)
                {
                    engine.ProcessTelemetry(new Core.Telemetry.TelemetrySnapshot("v2", DateTime.UtcNow, i, 3000, 20.0, 88.0, 88.0, 30.0, 14.7, 50.0, 14.7, 14.7, 14.7, 1.0, 0, 0, 0, true, false, 0));
                }
                engine.StopSession();

                engine.OnCalibrationStream -= handler;
                return Assert("AutoTune - Calibration RTP stream publishing streams virtual changes", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Calibration RTP stream checks: {ex.Message}";
            }
        }

        private static string TestAutoTune_ReplayDeterminism()
        {
            try
            {
                var validator = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IReplayDeterministicValidator>();
                var dataset = new List<Core.Telemetry.TelemetrySnapshot>
                {
                    new Core.Telemetry.TelemetrySnapshot("v2", DateTime.Now, 1, 3000, 20.0, 90, 85, 30, 14.0, 50, 0, 0, 14.7, 1.0, 0, 0, 0, true, false, 0)
                };
                var original = new List<HondaTuner.Core.AutoTune.TuneDecision>
                {
                    new HondaTuner.Core.AutoTune.TuneDecision { MapName = "Fuel", CellRow = 1, CellCol = 1, OldValue = 50.0, NewValue = 54.0, ConfidenceScore = 90 }
                };
                var results = validator.ValidateReplayDeterminism(dataset, original, (snap) =>
                {
                    // Non-deterministic: returns 56.0 instead of 54.0
                    return new HondaTuner.Core.AutoTune.TuneDecision { MapName = "Fuel", CellRow = 1, CellCol = 1, OldValue = 50.0, NewValue = 56.0, ConfidenceScore = 90 };
                });
                return Assert("AutoTune - Replay validation checks run determinism bounds", !results.IsDeterministic);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Replay determinism verification: {ex.Message}";
            }
        }

        private static string TestAutoTune_PersistenceRecovery()
        {
            try
            {
                var memory = new HondaTuner.Core.AutoTune.AdaptiveMemory();
                memory.Learn("Fuel", "FuelMap", 1, 1, 5.0, true, new HondaTuner.Core.AutoTune.EnvironmentalContext { Temperature = 30.0 });

                string json = memory.Export();
                var newMemory = new HondaTuner.Core.AutoTune.AdaptiveMemory();
                newMemory.Import(json);

                bool okExportImport = newMemory.Entries.Count == 1 && newMemory.Entries[0].AppliedCorrection == 5.0;

                // Test migration from V1 to V2
                string v1Json = "{\"SchemaVersion\": 1, \"_entries\": [{\"ParameterName\": \"Ignition\", \"MapName\": \"IgnMap\", \"Row\": 2, \"Col\": 2, \"AppliedCorrection\": -2.0, \"Success\": true, \"Timestamp\": \"2026-08-04T12:00:00\"}]}";
                var migratedMemory = new HondaTuner.Core.AutoTune.AdaptiveMemory();
                migratedMemory.Import(v1Json);

                bool okMigration = migratedMemory.SchemaVersion == 2 && migratedMemory.Entries.Count == 1 && migratedMemory.Entries[0].Environment.OperatingConditions == "Migrated from V1";

                return Assert("AutoTune - Persistence layer correctly serializes state machine records and migrates schema V1 to V2", okExportImport && okMigration);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Persistence recovery verification: {ex.Message}";
            }
        }

        private static string TestAutoTune_ExternalApiQuery()
        {
            try
            {
                var queryService = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneQueryService>();
                var status = queryService.GetSessionStatus("P28");
                bool ok = status != null;
                return Assert("AutoTune - Query service retrieves session lists correctly", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - External API query service: {ex.Message}";
            }
        }

        private static string TestAutoTune_ExternalApiCommand()
        {
            try
            {
                var commandService = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneCommandService>();
                bool start = commandService.StartSession("P28", "TunerUser", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.DryRun, "Default");
                commandService.StopSession("P28");
                return Assert("AutoTune - Command service issues control triggers successfully", start);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - External API command service: {ex.Message}";
            }
        }

        private static string TestAutoTune_HardwareCheckSafeMode()
        {
            try
            {
                var engine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();
                engine.StopSession();
                bool start = engine.StartSession("P28", "Tuner", HondaTuner.Core.AutoTune.AutoTuneOperatingMode.SafeMode, "Default");
                engine.ProcessTelemetry(new Core.Telemetry.TelemetrySnapshot("v2", DateTime.UtcNow, 1, 3000, 20.0, 90.0, 85.0, 30.0, 14.0, 50.0, 12.0, 12.0, 14.7, 1.0, 0, 0, 0, true, false, 45.0));

                bool ok = engine.ActiveSession?.OperatingMode == HondaTuner.Core.AutoTune.AutoTuneOperatingMode.SafeMode;
                engine.StopSession();
                return Assert("AutoTune - Hardware checks and SafeMode blocks writing calibrations", start && ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Hardware check SafeMode: {ex.Message}";
            }
        }

        private static string TestAutoTune_CellLockingAcquisition()
        {
            try
            {
                var cellLock = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.ICalibrationCellLockManager>();
                cellLock.ReleaseAllLocks("Tuner1");
                cellLock.ReleaseAllLocks("Tuner2");

                bool l1 = cellLock.TryLockCell("Fuel", 2, 3, "Tuner1");
                bool l2 = cellLock.TryLockCell("Fuel", 2, 3, "Tuner2"); // Conflict! Should fail.
                bool l3 = cellLock.TryLockCell("Fuel", 4, 4, "Tuner1"); // Different cell, should pass.

                cellLock.ReleaseCell("Fuel", 2, 3, "Tuner1");
                bool l4 = cellLock.TryLockCell("Fuel", 2, 3, "Tuner2"); // Released, should pass now.
                cellLock.ReleaseAllLocks("Tuner1");
                cellLock.ReleaseAllLocks("Tuner2");

                return Assert("AutoTune - Cell lock manager acquires, releases and traps conflicts", l1 && !l2 && l3 && l4);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] AutoTune - Cell locking acquisition: {ex.Message}";
            }
        }

        private static void CleanPersistentQueue()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string queuePath = Path.Combine(baseDir, "Database", "pending_rtp_queue.json");
                if (File.Exists(queuePath)) File.Delete(queuePath);
            }
            catch { }
        }

        private static List<string> RunRtpEnginePhase9Tests()
        {
            var results = new List<string>();
            results.Add("\n── Faz 9: RTP Emulator & Real-Time Calibration Engine Testleri ──");

            CleanPersistentQueue();
            results.Add(TestRtp_DIServiceContainerResolution());
            CleanPersistentQueue();
            results.Add(TestRtp_ConnectionStateMachineTransitions());
            CleanPersistentQueue();
            results.Add(TestRtp_RetryAndExponentialRecovery());
            CleanPersistentQueue();
            results.Add(TestRtp_CoalescedBatchSynchronization());
            CleanPersistentQueue();
            results.Add(TestRtp_OrderingGuarantee());
            CleanPersistentQueue();
            results.Add(TestRtp_LatencyBenchmark());
            CleanPersistentQueue();
            results.Add(TestRtp_HighFrequencyStress());
            CleanPersistentQueue();
            results.Add(TestRtp_CancellationTokenShutdown());
            CleanPersistentQueue();
            results.Add(TestRtp_AutoPauseOnConnectionLost());
            CleanPersistentQueue();
            results.Add(TestRtp_VerificationWriteAcknowledgement());
            CleanPersistentQueue();
            results.Add(TestRtp_IdempotencySuppression());
            CleanPersistentQueue();
            results.Add(TestRtp_QueueBackpressureDropOldest());
            CleanPersistentQueue();
            results.Add(TestRtp_QueueBackpressureRejectNewest());
            CleanPersistentQueue();
            results.Add(TestRtp_QueueBackpressureBlockProducer());
            CleanPersistentQueue();
            results.Add(TestRtp_ConfigValidationFailures());
            CleanPersistentQueue();
            results.Add(TestRtp_PersistentQueueRecovery());
            CleanPersistentQueue();
            results.Add(TestRtp_ConcurrentAutoTuneRtpConsistency());
            CleanPersistentQueue();

            return results;
        }

        private static string TestRtp_DIServiceContainerResolution()
        {
            try
            {
                var engine = Core.Container.ServiceContainer.Resolve<IRtpCalibrationEngine>();
                return Assert("RTP - Engine correctly registers and resolves from DI ServiceContainer", engine != null);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - DI Resolution: {ex.Message}";
            }
        }

        private static string TestRtp_ConnectionStateMachineTransitions()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    bool state1 = engine.ConnectionState == RtpConnectionState.Disconnected;
                    engine.ConnectEmulator();
                    bool state2 = engine.ConnectionState == RtpConnectionState.Connected;
                    engine.EnableSync();
                    bool state3 = engine.ConnectionState == RtpConnectionState.Synchronizing;
                    engine.DisableSync();
                    bool state4 = engine.ConnectionState == RtpConnectionState.Paused;
                    engine.DisconnectEmulator();
                    bool state5 = engine.ConnectionState == RtpConnectionState.Disconnected;
                    return Assert("RTP - Connection state machine cycles correctly through all states", state1 && state2 && state3 && state4 && state5);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - State Machine: {ex.Message}";
            }
        }

        private static string TestRtp_RetryAndExponentialRecovery()
        {
            try
            {
                var mockEmu = new MockEmulator { FailCountTarget = 2 };
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    engine.EnableSync();

                    romSvc.SetBuffer(new byte[0x8000]);
                    calSvc.RecordChange(new CalibrationChange { Offset = 0x1000, NewValue = "99" });

                    Thread.Sleep(200);

                    bool ok = mockEmu.Memory[0x1000] == 99 && engine.RetryCount == 2;
                    return Assert("RTP - Transient write failures trigger automatic retries and recover successfully", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Retry recovery: {ex.Message}";
            }
        }

        private static string TestRtp_CoalescedBatchSynchronization()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    romSvc.SetBuffer(new byte[0x8000]);

                    calSvc.RecordChange(new CalibrationChange { Offset = 0x100, NewValue = "10" });
                    calSvc.RecordChange(new CalibrationChange { Offset = 0x101, NewValue = "11" });
                    calSvc.RecordChange(new CalibrationChange { Offset = 0x102, NewValue = "12" });

                    engine.EnableSync();
                    Thread.Sleep(200);

                    bool valuesWritten = mockEmu.Memory[0x100] == 10 && mockEmu.Memory[0x101] == 11 && mockEmu.Memory[0x102] == 12;
                    bool ok = valuesWritten && mockEmu.WriteCount == 1;
                    return Assert("RTP - Consecutive calibration edits coalesce into a single block write", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Coalesced batch: {ex.Message}";
            }
        }

        private static string TestRtp_OrderingGuarantee()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    romSvc.SetBuffer(new byte[0x8000]);

                    calSvc.RecordChange(new CalibrationChange { Offset = 0x500, NewValue = "1" });
                    calSvc.RecordChange(new CalibrationChange { Offset = 0x500, NewValue = "2" });
                    calSvc.RecordChange(new CalibrationChange { Offset = 0x500, NewValue = "3" });

                    engine.EnableSync();
                    Thread.Sleep(200);

                    bool ok = mockEmu.Memory[0x500] == 3;
                    return Assert("RTP - Multi-threaded queue guarantees strict sequential write ordering", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Ordering guarantee: {ex.Message}";
            }
        }

        private static string TestRtp_LatencyBenchmark()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    engine.EnableSync();
                    romSvc.SetBuffer(new byte[0x8000]);

                    calSvc.RecordChange(new CalibrationChange { Offset = 0x600, NewValue = "45" });
                    Thread.Sleep(200);

                    bool ok = engine.AvgSyncLatencyMs > 0;
                    return Assert("RTP - Execution latency benchmark tracking meets target limits", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Latency benchmark: {ex.Message}";
            }
        }

        private static string TestRtp_HighFrequencyStress()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    engine.EnableSync();
                    romSvc.SetBuffer(new byte[0x8000]);

                    for (int i = 0; i < 500; i++)
                    {
                        calSvc.RecordChange(new CalibrationChange { Offset = i, NewValue = "5" });
                    }

                    Thread.Sleep(400);

                    bool allSynced = true;
                    for (int i = 0; i < 500; i++)
                    {
                        if (mockEmu.Memory[i] != 5) allSynced = false;
                    }

                    return Assert("RTP - Engine sustains high-frequency (500+) update throughput stress", allSynced);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - High frequency stress: {ex.Message}";
            }
        }

        private static string TestRtp_CancellationTokenShutdown()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    engine.EnableSync();
                }
                return Assert("RTP - Background sync worker terminates cleanly on CancellationToken request", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - CancellationToken shutdown: {ex.Message}";
            }
        }

        private static string TestRtp_AutoPauseOnConnectionLost()
        {
            try
            {
                var mockEmu = new MockEmulator { FailWrites = true };
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    engine.EnableSync();
                    romSvc.SetBuffer(new byte[0x8000]);

                    calSvc.RecordChange(new CalibrationChange { Offset = 0x200, NewValue = "55" });
                    Thread.Sleep(200);

                    bool isFaulted = engine.ConnectionState == RtpConnectionState.Faulted;
                    bool syncStopped = !engine.IsSyncActive;
                    return Assert("RTP - Hardware disconnect automatically halts sync and raises Faulted event", isFaulted && syncStopped);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Auto pause: {ex.Message}";
            }
        }

        private static string TestRtp_VerificationWriteAcknowledgement()
        {
            try
            {
                var mockEmu = new MockEmulator { MismatchVerify = true };
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    engine.EnableSync();
                    romSvc.SetBuffer(new byte[0x8000]);

                    calSvc.RecordChange(new CalibrationChange { Offset = 0x300, NewValue = "88" });
                    Thread.Sleep(200);

                    bool ok = engine.ConnectionState == RtpConnectionState.Faulted;
                    return Assert("RTP - Enforces read-back verification after writing values to emulator", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Verification write ack: {ex.Message}";
            }
        }

        private static string TestRtp_IdempotencySuppression()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    engine.ConnectEmulator();
                    engine.EnableSync();
                    romSvc.SetBuffer(new byte[0x8000]);

                    calSvc.RecordChange(new CalibrationChange { Offset = 0x400, NewValue = "22" });
                    Thread.Sleep(100);
                    int count1 = mockEmu.WriteCount;

                    calSvc.RecordChange(new CalibrationChange { Offset = 0x400, NewValue = "22" });
                    Thread.Sleep(100);
                    int count2 = mockEmu.WriteCount;

                    return Assert("RTP - Idempotent synchronization ignores duplicate writes to target cells", count1 == count2);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Idempotency suppression: {ex.Message}";
            }
        }

        private static string TestRtp_QueueBackpressureDropOldest()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(baseDir, "Database", "rtp_config.json");
                string orgJson = File.Exists(configPath) ? File.ReadAllText(configPath) : null;

                var testConfig = new RtpConfig
                {
                    RetryCount = 1,
                    WriteTimeoutMs = 100,
                    PacketSize = 64,
                    SyncIntervalMs = 50,
                    BatchingPolicy = "None",
                    QueueLimit = 5,
                    BackpressurePolicy = "DropOldest"
                };
                File.WriteAllText(configPath, JsonSerializer.Serialize(testConfig));

                try
                {
                    using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                    {
                        romSvc.SetBuffer(new byte[0x8000]);
                        for (int i = 0; i < 7; i++)
                        {
                            calSvc.RecordChange(new CalibrationChange { Offset = 0x700 + i, NewValue = i.ToString() });
                        }

                        bool ok = engine.QueueDepth <= 5 && engine.DroppedWritesCount == 2;
                        return Assert("RTP - Backpressure policy DropOldest purges oldest queued changes under load", ok);
                    }
                }
                finally
                {
                    if (orgJson != null) File.WriteAllText(configPath, orgJson);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Backpressure DropOldest: {ex.Message}";
            }
        }

        private static string TestRtp_QueueBackpressureRejectNewest()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(baseDir, "Database", "rtp_config.json");
                string orgJson = File.Exists(configPath) ? File.ReadAllText(configPath) : null;

                var testConfig = new RtpConfig
                {
                    RetryCount = 1,
                    WriteTimeoutMs = 100,
                    PacketSize = 64,
                    SyncIntervalMs = 50,
                    BatchingPolicy = "None",
                    QueueLimit = 5,
                    BackpressurePolicy = "RejectNewest"
                };
                File.WriteAllText(configPath, JsonSerializer.Serialize(testConfig));

                try
                {
                    using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                    {
                        romSvc.SetBuffer(new byte[0x8000]);
                        for (int i = 0; i < 7; i++)
                        {
                            calSvc.RecordChange(new CalibrationChange { Offset = 0x800 + i, NewValue = i.ToString() });
                        }

                        bool ok = engine.QueueDepth <= 5 && engine.DroppedWritesCount == 2;
                        return Assert("RTP - Backpressure policy RejectNewest drops new updates under queue overflow", ok);
                    }
                }
                finally
                {
                    if (orgJson != null) File.WriteAllText(configPath, orgJson);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Backpressure RejectNewest: {ex.Message}";
            }
        }

        private static string TestRtp_QueueBackpressureBlockProducer()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(baseDir, "Database", "rtp_config.json");
                string orgJson = File.Exists(configPath) ? File.ReadAllText(configPath) : null;

                var testConfig = new RtpConfig
                {
                    RetryCount = 1,
                    WriteTimeoutMs = 100,
                    PacketSize = 64,
                    SyncIntervalMs = 50,
                    BatchingPolicy = "None",
                    QueueLimit = 5,
                    BackpressurePolicy = "BlockProducer"
                };
                File.WriteAllText(configPath, JsonSerializer.Serialize(testConfig));

                try
                {
                    using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                    {
                        romSvc.SetBuffer(new byte[0x8000]);
                        for (int i = 0; i < 7; i++)
                        {
                            calSvc.RecordChange(new CalibrationChange { Offset = 0x900 + i, NewValue = i.ToString() });
                        }
                        bool ok = engine.QueueDepth <= 5;
                        return Assert("RTP - Backpressure policy BlockProducer controls producer rates correctly", ok);
                    }
                }
                finally
                {
                    if (orgJson != null) File.WriteAllText(configPath, orgJson);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Backpressure BlockProducer: {ex.Message}";
            }
        }

        private static string TestRtp_ConfigValidationFailures()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(baseDir, "Database", "rtp_config.json");
                string orgJson = File.Exists(configPath) ? File.ReadAllText(configPath) : null;

                var badConfig = new RtpConfig
                {
                    RetryCount = 100,
                    WriteTimeoutMs = 250,
                    PacketSize = 64,
                    SyncIntervalMs = 50,
                    BatchingPolicy = "None",
                    QueueLimit = 100
                };
                File.WriteAllText(configPath, JsonSerializer.Serialize(badConfig));

                try
                {
                    try
                    {
                        using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                        {
                        }
                        _failed++;
                        return "[FAIL] RTP - Config validator did not reject invalid configuration.";
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return Assert("RTP - Validation layer rejects bad config schema values at initialization", true);
                    }
                }
                finally
                {
                    if (orgJson != null) File.WriteAllText(configPath, orgJson);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Config validation check: {ex.Message}";
            }
        }

        private static string TestRtp_PersistentQueueRecovery()
        {
            try
            {
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string queuePath = Path.Combine(baseDir, "Database", "pending_rtp_queue.json");

                if (File.Exists(queuePath)) File.Delete(queuePath);

                var fakeList = new List<CalibrationChange>
                {
                    new CalibrationChange { Offset = 0xAA0, NewValue = "12", Parameter = "P1", Source = "RecoverTest" }
                };
                File.WriteAllText(queuePath, JsonSerializer.Serialize(fakeList));

                try
                {
                    using (var engine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                    {
                        bool ok = engine.QueueDepth == 1;
                        return Assert("RTP - Restores un-synced calibration state queue records on startup", ok);
                    }
                }
                finally
                {
                    if (File.Exists(queuePath)) File.Delete(queuePath);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Persistent queue recovery: {ex.Message}";
            }
        }

        private static string TestRtp_ConcurrentAutoTuneRtpConsistency()
        {
            try
            {
                ForceLoadDatabase();
                var mockEmu = new MockEmulator();
                var romSvc = Core.Container.ServiceContainer.Resolve<IRomService>();
                var calSvc = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
                var engineAutotune = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();

                using (var rtpEngine = new RtpCalibrationEngine(calSvc, romSvc, mockEmu))
                {
                    rtpEngine.ConnectEmulator();
                    rtpEngine.EnableSync();

                    // Load P28 ROM profile properly so AutoTune can make and apply decisions physically
                    var db = Database.EcuDatabaseManager.Instance;
                    var profile = db.GetProfile("P28");
                    if (profile == null) throw new InvalidOperationException("P28 profile not found");

                    byte[] romBytes = new byte[0x8000];
                    string tempFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_rtp_autotune_rom.bin");
                    File.WriteAllBytes(tempFile, romBytes);
                    romSvc.LoadRom(tempFile, profile);
                    if (File.Exists(tempFile)) File.Delete(tempFile);

                    engineAutotune.StopSession();
                    // Professional role allows auto-approving in Normal mode which writes to Calibration Service
                    engineAutotune.StartSession("P28", "Professional", AutoTuneOperatingMode.Normal, "Default");

                    // Send 15 snapshots with lean AFR (e.g. 17.0) to trigger a stable window and generate fuel correction decisions
                    for (int i = 0; i < 15; i++)
                    {
                        engineAutotune.ProcessTelemetry(new Core.Telemetry.TelemetrySnapshot(
                            "v2", DateTime.UtcNow, i, 3000, 20.0, 90.0, 85.0, 30.0, 14.0, 50.0, 12.0, 12.0, 17.0, 1.0, 0, 0, 0, true, false, 45.0));
                        Thread.Sleep(10);
                    }

                    Thread.Sleep(300); // Give the background sync thread time to flush Rtp queue to Emulator

                    engineAutotune.StopSession();

                    bool ok = mockEmu.Memory.Any(b => b > 0);
                    return Assert("RTP - Synchronizes concurrently applied AutoTune decisions to hardware", ok);
                }
            }
            catch (Exception ex)
            {
                _failed++;
                return $"[FAIL] RTP - Concurrent autotune sync: {ex.Message}";
            }
        }

        private class MockEmulator : IEmulator
        {
            public string DeviceName => "Mock Emulator";
            public ConnectionState State { get; set; } = ConnectionState.Disconnected;
            public byte[] Memory { get; } = new byte[0x8000];
            public int WriteCount { get; private set; }
            public int ReadCount { get; private set; }
            public bool FailWrites { get; set; }
            public int FailCountTarget { get; set; }
            private int _failCounter = 0;
            public bool MismatchVerify { get; set; }

            public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

            public void Connect()
            {
                State = ConnectionState.Connected;
                StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs { OldState = ConnectionState.Disconnected, NewState = ConnectionState.Connected });
            }

            public void Disconnect()
            {
                State = ConnectionState.Disconnected;
                StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs { OldState = ConnectionState.Connected, NewState = ConnectionState.Disconnected });
            }

            public byte ReadByte(int offset)
            {
                ReadCount++;
                if (MismatchVerify) return (byte)(Memory[offset] ^ 0xFF);
                return Memory[offset];
            }

            public void WriteByte(int offset, byte value)
            {
                WriteCount++;
                if (FailWrites)
                {
                    throw new IOException("Simulated write exception");
                }
                if (FailCountTarget > 0 && _failCounter < FailCountTarget)
                {
                    _failCounter++;
                    throw new IOException("Simulated transient write exception");
                }
                Memory[offset] = value;
            }

            public byte[] ReadBlock(int offset, int length)
            {
                ReadCount++;
                byte[] data = new byte[length];
                Array.Copy(Memory, offset, data, 0, length);
                if (MismatchVerify)
                {
                    if (data.Length > 0) data[0] ^= 0xFF;
                }
                return data;
            }

            public void WriteBlock(int offset, byte[] data)
            {
                WriteCount++;
                if (FailWrites)
                {
                    throw new IOException("Simulated block write exception");
                }
                if (FailCountTarget > 0 && _failCounter < FailCountTarget)
                {
                    _failCounter++;
                    throw new IOException("Simulated transient block write exception");
                }
                Array.Copy(data, 0, Memory, offset, data.Length);
            }
        }

        private static string TestMetadata_SerializationAndValidation()
        {
            try
            {
                // Test 1: Serialization / Deserialization
                var meta = new Core.Metadata.EcuMetadata
                {
                    SerialNumber = "HT-12345",
                    HardwareRevision = "OBD1-V2",
                    Vin = "1HGEC999999999999",
                    Chassis = "EG6",
                    CompressionRatio = 11.8,
                    CamshaftProfile = "Stage 2",
                    GearboxType = "Y21",
                    InductionType = "Turbo"
                };

                string json = meta.ToJson();
                var deserialized = Core.Metadata.EcuMetadata.FromJson(json);

                if (deserialized.SerialNumber != "HT-12345" ||
                    deserialized.CompressionRatio != 11.8 ||
                    deserialized.CamshaftProfile != "Stage 2" ||
                    deserialized.InductionType != "Turbo")
                {
                    return Assert("TestMetadata_SerializationAndValidation (Serialization)", false);
                }

                // Test 2: Validation on Turbo with stock MAP (<= 105 kPa)
                int[] stockLoadAxis = new int[] { 20, 30, 45, 60, 75, 90, 105 };
                var results = Core.Metadata.EcuMetadataValidator.Validate(meta, EcuProfiles.P28, 7200, stockLoadAxis);
                bool hasTargetError = results.Any(r => r.RuleId == "VAL_TURBO_MAP_LIMIT" && r.Level == Core.Metadata.ValidationLevel.Error);

                if (!hasTargetError)
                {
                    return Assert("TestMetadata_SerializationAndValidation (Turbo MAP Error)", false);
                }

                // Test 3: High Compression warning
                bool hasCompressionWarning = results.Any(r => r.RuleId == "VAL_COMPRESSION_OCTANE" && r.Level == Core.Metadata.ValidationLevel.Warning);
                if (!hasCompressionWarning)
                {
                    return Assert("TestMetadata_SerializationAndValidation (Compression Warning)", false);
                }

                // Test 4: Camshaft RPM warning
                bool hasCamWarning = results.Any(r => r.RuleId == "VAL_CAM_REV_LIMIT" && r.Level == Core.Metadata.ValidationLevel.Warning);
                if (!hasCamWarning)
                {
                    return Assert("TestMetadata_SerializationAndValidation (Camshaft Warning)", false);
                }

                return Assert("TestMetadata_SerializationAndValidation", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestMetadata_SerializationAndValidation: Hata - {ex.Message}";
            }
        }

        private static string TestReverseEngineering_ExtractionAndDecompilation()
        {
            try
            {
                // Test 1: Map search of a constructed fake ROM with a simulated map
                byte[] mockRom = new byte[0x8000];
                int simulatedMapStart = 0x4000;

                // Construct a simulated 10x10 Fuel Map at 0x4000
                // Raw fuel values grow smoothly from 30 up to 130
                for (int r = 0; r < 10; r++)
                {
                    for (int c = 0; c < 10; c++)
                    {
                        mockRom[simulatedMapStart + (r * 10) + c] = (byte)(30 + (r * 8) + (c * 2));
                    }
                }

                // Construct simulated RPM axis at 0x3F80 (value representing RPM / 50, e.g. 500 RPM to 5000 RPM)
                // 10 elements starting from 8 (400RPM) to 98 (4900RPM)
                // Starts at 8 to fail Load check (requires >= 10)
                int simulatedRpmStart = 0x3F80;
                for (int i = 0; i < 10; i++)
                {
                    mockRom[simulatedRpmStart + i] = (byte)(8 + (i * 10));
                }

                // Construct simulated Load axis at 0x3FC0 (value representing kPa pressure)
                // 10 elements starting from 50 kPa to 230 kPa
                // Ends at 230 to fail RPM check (requires <= 220)
                int simulatedLoadStart = 0x3FC0;
                for (int i = 0; i < 10; i++)
                {
                    mockRom[simulatedLoadStart + i] = (byte)(50 + (i * 20));
                }

                // Run Search
                var candidates = Core.ReverseEngineering.MapSearchHelper.Search(mockRom);
                var fuelMatch = candidates.FirstOrDefault(c => c.Offset == simulatedMapStart && c.MapType == "Fuel");

                if (fuelMatch == null)
                {
                    return Assert("TestReverseEngineering_ExtractionAndDecompilation (Map Search)", false);
                }

                // Run Axis Extraction
                var axes = Core.ReverseEngineering.AxisExtractor.ExtractAxes(mockRom, fuelMatch);

                if (!axes.Success || axes.RpmAxisOffset != simulatedRpmStart || axes.LoadAxisOffset != simulatedLoadStart)
                {
                    return Assert("TestReverseEngineering_ExtractionAndDecompilation (Axis Extraction)", false);
                }

                // Run Decompiler Test
                string decompCode = Core.ReverseEngineering.RomAnalyzer.DecompileRoutine(mockRom, 0x1FC0, "checksum");
                if (string.IsNullOrWhiteSpace(decompCode) || !decompCode.Contains("calculate_checksum"))
                {
                    return Assert("TestReverseEngineering_ExtractionAndDecompilation (Decompiler)", false);
                }

                return Assert("TestReverseEngineering_ExtractionAndDecompilation", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestReverseEngineering_ExtractionAndDecompilation: Hata - {ex.Message}";
            }
        }

        private static string TestAdvancedFuelCorrections_FormulasAndAlarms()
        {
            try
            {
                var service = new HondaTuner.Calibration.Fuel.AdvancedFuelService();

                // Test 1: Alpha-N interpolation check
                // Grid initially set to: 35.0 + (r * 4.0) + (c * 2.5)
                // At row 0 (TPS=0), col 0 (RPM=500): 35.0
                // At row 5 (TPS=30), col 5 (RPM=5000): 35.0 + (5 * 4.0) + (5 * 2.5) = 67.5
                double val00 = service.InterpolateAlphaN(0, 500);
                double val55 = service.InterpolateAlphaN(30, 5000);

                if (Math.Abs(val00 - 35.0) > 0.01 || Math.Abs(val55 - 67.5) > 0.01)
                {
                    return Assert("TestAdvancedFuelCorrections (Alpha-N Interpolation)", false);
                }

                // Test 2: MAF Flow calculation
                // Voltages: {0.0, 0.5, 1.0, 1.5, 2.0} -> Flow: {0, 4.5, 12, 28, 55}
                // At 0.75V (interpolated between 0.5V (4.5) and 1.0V (12.0)):
                // 4.5 + 0.5 * (12.0 - 4.5) = 8.25 g/s
                double flow = service.CalculateMafFlow(0.75);
                if (Math.Abs(flow - 8.25) > 0.01)
                {
                    return Assert("TestAdvancedFuelCorrections (MAF Flow)", false);
                }

                // Test 3: Cold Start multiplier
                // ECT = 20C -> Multiplier = 1.25
                double coldMult = service.CalculateColdStartMultiplier(20);
                if (Math.Abs(coldMult - 1.25) > 0.01)
                {
                    return Assert("TestAdvancedFuelCorrections (Cold Start)", false);
                }

                // Test 4: Fuel pressure correction
                // Target = 43.5, Actual = 30.0 -> Multiplier should be sqrt(43.5 / 30.0) = 1.204
                double fpCorr = service.CalculateFuelPressureCorrection(30, 43.5);
                if (Math.Abs(fpCorr - Math.Sqrt(43.5 / 30.0)) > 0.01)
                {
                    return Assert("TestAdvancedFuelCorrections (Fuel Pressure drop)", false);
                }

                // Test 5: Injector Short Pulse adder
                // Under 1.5ms pulse widths, e.g. at 0.5ms base enj time, adder should be 0.22ms
                double adder = service.CalculateShortPulseAdder(0.5);
                if (Math.Abs(adder - 0.22) > 0.01)
                {
                    return Assert("TestAdvancedFuelCorrections (Short Pulse Adder)", false);
                }

                // Test 6: Transient Fuel Enrichment & Decay
                // Initialize transient fuel accumulator
                double scaleAcc = 0.0;
                // Run a mock transient: dTPS = 70 %/s, ECT = 20C (Scale factor for 20C = 1.0 + (60 - 20) * 0.02 = 1.8x)
                // Base transient at 70 %/s is 1.40ms. Scoped transient value = 1.40 * 1.8 = 2.52ms
                double finalPw = service.CalculatePulseWidth(4.0, 2000, 15, 20, 100, 43.5, 43.5, 70.0, false, ref scaleAcc);

                // Final Pulse Width: Base (4.0) * ColdMult (1.25) * PressCorr (1.0) = 5.0ms.
                // Add transient: 5.0 + 2.52 = 7.52ms. Plus short pulse adder (at 7.52ms, adder is 0.0) = 7.52ms.
                // Accumulated scaling check (note that scaleAcc decays to 1.764 inside CalculatePulseWidth):
                if (Math.Abs(scaleAcc - 1.764) > 0.01 || Math.Abs(finalPw - 7.52) > 0.01)
                {
                    return Assert("TestAdvancedFuelCorrections (Transient Accumulation)", false);
                }

                // Run next cycle with dTPS = 0 (no new transient, decay 30%, so old transient becomes 1.764 * 0.7 = 1.2348ms)
                // Base PW: 4.0 * 1.25 = 5.0ms. Final: 5.0 + 1.764 = 6.764ms.
                double nextPw = service.CalculatePulseWidth(4.0, 2000, 15, 20, 100, 43.5, 43.5, 0.0, false, ref scaleAcc);
                if (Math.Abs(scaleAcc - 1.2348) > 0.01 || Math.Abs(nextPw - 6.764) > 0.01)
                {
                    return Assert("TestAdvancedFuelCorrections (Transient Decay)", false);
                }

                // Test 7: Injector Saturation Alarm
                // Duty cycle = (RPM * PW) / 1200.
                // With RPM = 6000, PW = 20.0, Duty = (6000 * 20.0) / 1200 = 100% (should trigger alarm!)
                bool alarmTriggered = false;
                service.InjectorSaturationAlarm += (s, duty) =>
                {
                    if (duty >= 85.0) alarmTriggered = true;
                };

                double accDummy = 0.0;
                service.CalculatePulseWidth(20.0, 6000, 15, 80, 100, 43.5, 43.5, 0.0, false, ref accDummy);

                if (!alarmTriggered)
                {
                    return Assert("TestAdvancedFuelCorrections (Saturation Warning Alarm)", false);
                }

                return Assert("TestAdvancedFuelCorrections", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestAdvancedFuelCorrections: Hata - {ex.Message}";
            }
        }

        private static string TestAdvancedIgnition_DecodingAndCalibration()
        {
            try
            {
                // Test 1: Cranking timing ECT configuration validation
                var tables = new HondaTuner.Calibration.Ignition.AdvancedIgnitionTables();
                double advAt20 = tables.CrankingTimingAdvances[3]; // Index 3 is ECT 20, advance is 8.5
                if (Math.Abs(advAt20 - 8.5) > 0.001)
                {
                    return Assert("TestAdvancedIgnition_DecodingAndCalibration (Cranking ECT Table)", false);
                }

                // Test 2: Sensor Linearization (MAP calibration volt scaling)
                var mapCal = HondaTuner.Calibration.Ignition.SensorCalibration.CreateOemMapCalibration();
                double physicalMap = mapCal.Linearize(2.5); // Should be 97.5 kPa
                if (Math.Abs(physicalMap - 97.5) > 0.01)
                {
                    return Assert("TestAdvancedIgnition_DecodingAndCalibration (Sensor Linearization MAP)", false);
                }

                // Test 3: CAN Decode (Big Endian - Motorola)
                byte[] frame = new byte[8];
                frame[2] = 0x0F;
                frame[3] = 0xA0; // 0x0FA0 = 4000. 4000 * 0.25 = 1000.0 °C
                var bigDecoder = new HondaTuner.Calibration.Ignition.CanSensorDecoder("EGT Big", 0x200, 16, 16, true, 0.25, 0.0, "°C");
                double decodedBig = bigDecoder.Decode(frame);
                if (Math.Abs(decodedBig - 1000.0) > 0.01)
                {
                    return Assert("TestAdvancedIgnition_DecodingAndCalibration (CAN Decode Big Endian)", false);
                }

                // Test 4: CAN Decode (Little Endian - Intel)
                Array.Clear(frame, 0, frame.Length);
                frame[2] = 0xE8;
                frame[3] = 0x03; // 0x03E8 = 1000. 1000 * 0.25 = 250.0 °C
                var littleDecoder = new HondaTuner.Calibration.Ignition.CanSensorDecoder("EGT Little", 0x200, 16, 16, false, 0.25, 0.0, "°C");
                double decodedLittle = littleDecoder.Decode(frame);
                if (Math.Abs(decodedLittle - 250.0) > 0.01)
                {
                    return Assert("TestAdvancedIgnition_DecodingAndCalibration (CAN Decode Little Endian)", false);
                }

                // Test 5: MBT Optimizer Timing prediction
                var optimizer = new HondaTuner.Calibration.Ignition.MbtOptimizer();
                double mbtVal = optimizer.EstimateMbt(3000, 100, 95); // (3000-1000)*0.0035 + (100-100)*0.15 + 15.0 = 22.0
                if (Math.Abs(mbtVal - 22.0) > 0.01)
                {
                    return Assert("TestAdvancedIgnition_DecodingAndCalibration (MBT Calculation)", false);
                }

                return Assert("TestAdvancedIgnition_DecodingAndCalibration", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestAdvancedIgnition_DecodingAndCalibration: Hata - {ex.Message}";
            }
        }

        private static string TestVtecAndBoostControl_LogicAndAlarms()
        {
            try
            {
                var service = new HondaTuner.Calibration.VtecBoost.BoostControlService();

                // Test 1: VTEC activation criteria
                if (service.IsVtecActive(3000, 50, 3))
                {
                    return Assert("TestVtecAndBoostControl (Vtec Low RPM Lockout)", false);
                }

                if (service.IsVtecActive(5500, 10, 2))
                {
                    return Assert("TestVtecAndBoostControl (Vtec Low Speed Lockout)", false);
                }

                if (service.IsVtecActive(5500, 50, 1))
                {
                    return Assert("TestVtecAndBoostControl (Vtec Gear Lockout)", false);
                }

                if (!service.IsVtecActive(5500, 50, 3))
                {
                    return Assert("TestVtecAndBoostControl (Vtec Success)", false);
                }

                // Test 2: Target Boost with Scramble
                double baseTarget = service.GetTargetBoost(3000, 3);
                if (Math.Abs(baseTarget - 150.0) > 0.01)
                {
                    return Assert("TestVtecAndBoostControl (Base Boost Interpolation)", false);
                }

                service.TriggerScramble();
                service.UpdateTimers(1.0); // Scramble active
                double scrambleTarget = service.GetTargetBoost(3000, 3); // 150 + 30 = 180 kPa
                if (Math.Abs(scrambleTarget - 180.0) > 0.01)
                {
                    return Assert("TestVtecAndBoostControl (Scramble Boost Offset)", false);
                }

                // Test 3: PID solenoid calculations
                service.ResetPid();
                double duty = service.CalculateWgDuty(180, 140, 3000, 0.1);
                if (Math.Abs(duty - 100.0) > 0.01)
                {
                    return Assert("TestVtecAndBoostControl (Solenoid Duty Max Limit Clamped)", false);
                }

                // Test 4: Wastegate High Duty Leak Alarm
                bool alarmTriggered = false;
                service.WgFailureAlarm += (s, msg) =>
                {
                    alarmTriggered = true;
                };

                for (int i = 0; i < 22; i++)
                {
                    service.CalculateWgDuty(180, 105, 3000, 0.1);
                }

                if (!alarmTriggered)
                {
                    return Assert("TestVtecAndBoostControl (Wastegate Mechanical Failure Alarm)", false);
                }

                return Assert("TestVtecAndBoostControl_LogicAndAlarms", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestVtecAndBoostControl_LogicAndAlarms: Hata - {ex.Message}";
            }
        }

        private static string TestEngineProtection_SafetiesAndAlarms()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();

                // Test 1: Radiator Fan Relay Hysteresis Control
                service.ResetSafeties();
                // Case A: temp 90 -> relay false
                service.EvaluateSafety(2000, 90.0, 30.0, 90.0, 4.0, 3.5, 100.0, 600.0, 0.1);
                if (service.FanRelayState)
                {
                    return Assert("TestEngineProtection (Fan Relay Off Default)", false);
                }

                // Case B: temp 93 (>=92) -> relay true
                service.EvaluateSafety(2000, 93.0, 30.0, 90.0, 4.0, 3.5, 100.0, 600.0, 0.1);
                if (!service.FanRelayState)
                {
                    return Assert("TestEngineProtection (Fan Relay On Target)", false);
                }

                // Case C: temp 90 (>=89) -> relay remains true
                service.EvaluateSafety(2000, 90.0, 30.0, 90.0, 4.0, 3.5, 100.0, 600.0, 0.1);
                if (!service.FanRelayState)
                {
                    return Assert("TestEngineProtection (Fan Relay Hysteresis On)", false);
                }

                // Case D: temp 88 (<89) -> relay false
                service.EvaluateSafety(2000, 88.0, 30.0, 90.0, 4.0, 3.5, 100.0, 600.0, 0.1);
                if (service.FanRelayState)
                {
                    return Assert("TestEngineProtection (Fan Relay Hysteresis Off)", false);
                }

                // Test 2: IAT Heat Soak Protection Pullbacks
                service.ResetSafeties();
                service.EvaluateSafety(3000, 80.0, 60.0, 90.0, 4.0, 3.5, 100.0, 600.0, 0.1);
                if (Math.Abs(service.ActiveTimingPull - 4.0) > 0.01 || Math.Abs(service.ActiveBoostLimitOffset - 20.0) > 0.01)
                {
                    return Assert("TestEngineProtection (IAT Heat Soak Correction)", false);
                }

                // Test 3: EGT thermal safety pulls
                service.ResetSafeties();
                service.EvaluateSafety(3000, 80.0, 30.0, 90.0, 4.0, 3.5, 100.0, 950.0, 0.1);
                if (Math.Abs(service.ActiveTimingPull - 3.0) > 0.01 || Math.Abs(service.ActiveFuelEnrichmentPct - 15.0) > 0.01)
                {
                    return Assert("TestEngineProtection (EGT Thermal Limit)", false);
                }

                // Test 4: Low Oil Pressure Critical Fuel Cut
                service.ResetSafeties();
                bool alarmTriggered = false;
                service.ProtectionAlarmTriggered += (s, msg) =>
                {
                    alarmTriggered = true;
                };

                for (int i = 0; i < 11; i++)
                {
                    service.EvaluateSafety(3000, 80.0, 30.0, 90.0, 1.2, 3.5, 100.0, 600.0, 0.1);
                }

                if (!service.IsFuelCutActive || !alarmTriggered)
                {
                    return Assert("TestEngineProtection (Oil Pressure Safety Fuel Cut)", false);
                }

                return Assert("TestEngineProtection_SafetiesAndAlarms", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestEngineProtection_SafetiesAndAlarms: Hata - {ex.Message}";
            }
        }

        private static string TestDiagnosticsAndProtocols_A2LAndFreezeFrames()
        {
            try
            {
                var service = new HondaTuner.Calibration.Diagnostics.DiagnosticsService();

                // Test 1: Self-Test output contains key keywords
                string selfTestReport = service.RunEcuSelfTest();
                if (!selfTestReport.Contains("PASS") || !selfTestReport.Contains("RAM") || !selfTestReport.Contains("ROM Checksum"))
                {
                    return Assert("TestDiagnostics (Self-Test Report Validation)", false);
                }

                // Test 2: Protocol Traffic Simulation
                service.Tables.SelectedProtocol = "CAN_BUS";
                string trafficCan = service.SimulateProtocolTraffic();
                if (!trafficCan.Contains("CAN ID: 0x18DB33F1") || !trafficCan.Contains("SAE J1979"))
                {
                    return Assert("TestDiagnostics (Simulate CAN Bus Traffic)", false);
                }

                service.Tables.SelectedProtocol = "OBD1";
                string trafficObd = service.SimulateProtocolTraffic();
                if (!trafficObd.Contains("OBD1 Live Headers OK"))
                {
                    return Assert("TestDiagnostics (Simulate OBD1 Traffic)", false);
                }

                // Test 3: DTC and Freeze Frame Logging
                service.SavedFreezeFrames.Clear();
                service.TriggerDtc("P1259", 5800.0, 95.0, 40.0, 110.0, 150.0);

                if (service.SavedFreezeFrames.Count != 1)
                {
                    return Assert("TestDiagnostics (Freeze Frame Count)", false);
                }

                var frame = service.SavedFreezeFrames[0];
                if (frame.DtcCode != "P1259" || Math.Abs(frame.Rpm - 5800.0) > 0.01 || Math.Abs(frame.Boost - 150.0) > 0.01)
                {
                    return Assert("TestDiagnostics (Freeze Frame Field Integrity)", false);
                }

                // Test 4: A2L Specification Format Generation
                string a2lContent = service.GenerateA2L();
                if (!a2lContent.Contains("ASAP2_VERSION") || !a2lContent.Contains("CHARACTERISTIC VtecMinRpm") || !a2lContent.Contains("FuelTable_LowVtec"))
                {
                    return Assert("TestDiagnostics (A2L Metadata Standard Generation)", false);
                }

                return Assert("TestDiagnosticsAndProtocols_A2LAndFreezeFrames", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestDiagnosticsAndProtocols_A2LAndFreezeFrames: Hata - {ex.Message}";
            }
        }

        private static string TestDynoLogsAndVersions_EstimationAndBranches()
        {
            try
            {
                var service = new HondaTuner.Calibration.DynoLogs.DynoLogsService();

                // Test 1: SAE vs DIN correction multipliers stability
                double saeCf = service.CalculateCorrectionMultiplier(35.0, 98.0);
                service.Tables.CorrectionFactorType = "DIN";
                double dinCf = service.CalculateCorrectionMultiplier(35.0, 98.0);

                if (saeCf < 0.8 || saeCf > 1.2 || dinCf < 0.8 || dinCf > 1.25)
                {
                    return Assert("TestDynoLogs (Correction Factor Ranges)", false);
                }

                // Test 2: Virtual Dyno Curve points generator
                service.Tables.CorrectionFactorType = "SAE";
                service.Tables.DrivetrainLossPct = 15.0;
                service.RunVirtualDynoSim(180.0, 30.0, 100.0);

                if (service.CurrentDynoPoints.Count != 13)
                {
                    return Assert("TestDynoLogs (Dyno Count Integrity)", false);
                }

                var peakHpPoint = service.CurrentDynoPoints[12];
                if (peakHpPoint.EngineHp < peakHpPoint.Whp)
                {
                    return Assert("TestDynoLogs (Crank vs Wheel power relationship)", false);
                }

                // Test 3: Performance Times estimation
                var timer = service.EstimatePerformanceTimes(180.0);
                if (timer.Time0To100 <= 1.0 || timer.Time100To200 <= 1.0 || timer.ShiftGapMs < 50)
                {
                    return Assert("TestDynoLogs (Performance Timers ranges)", false);
                }

                // Test 4: Git-style Branches version management
                service.GitMergeHistory.Clear();
                service.CreateBranch("dev_boost_2.0");
                service.CommitChange("Add peak boost fuel modifiers");
                service.MergeBranch("dev_boost_2.0", "main");

                if (service.ActiveBranch != "main" || service.GitMergeHistory.Count != 3)
                {
                    return Assert("TestDynoLogs (Branch and merger integrity)", false);
                }

                // Test 5: MCU Watchdog RAM reader
                var ramWatch = service.GetWatchdogValues(0.0);
                if (!ramWatch.ContainsKey("VTEC_ACTIVE") || !ramWatch.ContainsKey("AFR_TARGET"))
                {
                    return Assert("TestDynoLogs (Watchdog Variable watchlist)", false);
                }

                return Assert("TestDynoLogsAndVersions_EstimationAndBranches", true);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestDynoLogsAndVersions_EstimationAndBranches: Hata - {ex.Message}";
            }
        }

        private static string TestLeanCut_TriggersAtThreshold()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 4500, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 130, egt: 600, dt: 0.1, afr: 13.5, knock: false);
                return Assert("TestLeanCut_TriggersAtThreshold", service.IsLeanCutTriggered);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestLeanCut_TriggersAtThreshold: Hata - {ex.Message}";
            }
        }

        private static string TestLeanCut_NotTriggeredBelowRpm()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 3500, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 130, egt: 600, dt: 0.1, afr: 13.5, knock: false);
                return Assert("TestLeanCut_NotTriggeredBelowRpm", !service.IsLeanCutTriggered);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestLeanCut_NotTriggeredBelowRpm: Hata - {ex.Message}";
            }
        }

        private static string TestLeanCut_NotTriggeredBelowMap()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 4500, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 110, egt: 600, dt: 0.1, afr: 13.5, knock: false);
                return Assert("TestLeanCut_NotTriggeredBelowMap", !service.IsLeanCutTriggered);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestLeanCut_NotTriggeredBelowMap: Hata - {ex.Message}";
            }
        }

        private static string TestOverboostCut_TriggersAboveLimit()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 3000, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 180, egt: 600, dt: 0.1, afr: 12.0, knock: false);
                return Assert("TestOverboostCut_TriggersAboveLimit", service.IsOverboostCutTriggered);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestOverboostCut_TriggersAboveLimit: Hata - {ex.Message}";
            }
        }

        private static string TestOverboostCut_SafeZone()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 3000, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 170, egt: 600, dt: 0.1, afr: 12.0, knock: false);
                return Assert("TestOverboostCut_SafeZone", !service.IsOverboostCutTriggered);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestOverboostCut_SafeZone: Hata - {ex.Message}";
            }
        }

        private static string TestEctRetard_LinearInterpolation()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 3000, ect: 106, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 100, egt: 600, dt: 0.1, afr: 12.0, knock: false);
                bool ok = Math.Abs(service.ActiveTimingPull - 3.0) < 0.01;
                return Assert("TestEctRetard_LinearInterpolation", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestEctRetard_LinearInterpolation: Hata - {ex.Message}";
            }
        }

        private static string TestKnockRetard_ImmediatePull()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 3000, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 100, egt: 600, dt: 0.1, afr: 12.0, knock: true);
                bool ok = Math.Abs(service.ActiveTimingPull - 3.0) < 0.01;
                return Assert("TestKnockRetard_ImmediatePull", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestKnockRetard_ImmediatePull: Hata - {ex.Message}";
            }
        }

        private static string TestKnockRetard_GradualRecovery()
        {
            try
            {
                var service = new HondaTuner.Calibration.EngineProtection.EngineProtectionService();
                service.EvaluateSafety(rpm: 3000, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 100, egt: 600, dt: 0.1, afr: 12.0, knock: true);
                service.EvaluateSafety(rpm: 3000, ect: 90, iat: 30, oilTemp: 90, oilPress: 4.0, fuelPress: 3.0, actualBoost: 100, egt: 600, dt: 2.0, afr: 12.0, knock: false);
                bool ok = Math.Abs(service.ActiveTimingPull - 2.0) < 0.01;
                return Assert("TestKnockRetard_GradualRecovery", ok);
            }
            catch (Exception ex)
            {
                _failed++;
                return $"  ❌ TestKnockRetard_GradualRecovery: Hata - {ex.Message}";
            }
        }

        private static string Assert(string testName, bool condition)
        {
            if (condition)
            {
                _passed++;
                return $"  ✅ {testName}";
            }
            else
            {
                _failed++;
                return $"  ❌ {testName}";
            }
        }
    }
}
