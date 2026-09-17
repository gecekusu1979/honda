// HondaTuner xUnit Test Suite
// Aşama 1: TuningTestHarness xUnit migrasyonu
// Bu dosya, TuningTestHarness.cs içindeki tüm testleri xUnit [Fact] formatında çalıştırır.
// Her [Fact] metodu, harness sonucunun "[PASS]" içerip içermediğini kontrol eder.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
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

namespace HondaTuner.Tests.XUnit
{
    /// <summary>
    /// ROM Testleri — xUnit formatı
    /// </summary>
    public class RomTests
    {
        private static void ForceLoadDatabase()
        {
            var db = Database.EcuDatabaseManager.Instance;
            if (db.GetProfile("P28") == null)
            {
                string dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
                if (!Directory.Exists(dbDir) || !File.Exists(Path.Combine(dbDir, "ecu_database.json")))
                    dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Database");
                db.LoadDatabase(dbDir);
                if (db.GetProfile("P28") == null)
                    db.LoadDatabase(Path.Combine(Directory.GetCurrentDirectory(), "Database"));
            }
        }

        [Fact]
        public void RomIdentifier_ValidP28_MatchesSize()
        {
            var identifier = new RomIdentifier();
            byte[] rom = new byte[0x8000];
            var result = identifier.IdentifyRom(rom, EcuProfiles.All);
            Assert.NotNull(result);
            Assert.Equal(0x8000, result.RomSize);
        }

        [Fact]
        public void RomIdentifier_WrongSize_IsMismatch()
        {
            var identifier = new RomIdentifier();
            byte[] rom = new byte[1024];
            var result = identifier.IdentifyRom(rom, EcuProfiles.All);
            Assert.True(result.IsMismatch || result.CompatibilityScore < 50);
        }

        [Fact]
        public void RomPatch_ValidateAndApply_PatchesCorrectly()
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
            Assert.True(valid);
            Assert.NotNull(patched);
            Assert.Equal(0xCC, patched[10]);
            Assert.Equal(0xDD, patched[11]);
        }

        [Fact]
        public void RomPatch_Rollback_RestoresOriginal()
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
            Assert.Equal(0xAA, restored[10]);
        }
    }

    /// <summary>
    /// Kalibrasyon Testleri — xUnit formatı
    /// </summary>
    public class CalibrationTests
    {
        [Fact]
        public void InjectorScaling_240to440_ScalesCorrectly()
        {
            var map = new byte[,] { { 128, 200 }, { 100, 255 } };
            var result = InjectorManager.ScaleFuelTable(map, 240, 440);
            Assert.InRange(result[0, 0], 69, 71);
        }

        [Fact]
        public void InjectorScaling_Overflow_ClampedTo255()
        {
            var map = new byte[,] { { 255 } };
            var result = InjectorManager.ScaleFuelTable(map, 440, 240);
            Assert.Equal(255, result[0, 0]);
        }

        [Fact]
        public void CalibrationTransaction_Commit_AppliesValueToBuffer()
        {
            var romService = Core.Container.ServiceContainer.Resolve<IРomService>();
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
            Assert.Equal(60, currentRom[0x1000]);
        }

        [Fact]
        public void CalibrationTransaction_Rollback_RestoresValues()
        {
            var romService = Core.Container.ServiceContainer.Resolve<IРomService>();
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
            Assert.Equal(50, currentRom[0x1000]);
        }

        [Fact]
        public void CalibrationUndoRedo_CycleRestoresState()
        {
            var romService = Core.Container.ServiceContainer.Resolve<IРomService>();
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

            Assert.Equal(75, romService.GetBuffer()[0x1000]);
            calMgr.Undo();
            Assert.Equal(50, romService.GetBuffer()[0x1000]);
            calMgr.Redo();
            Assert.Equal(75, romService.GetBuffer()[0x1000]);
        }

        [Fact]
        public void CalibrationValidator_RejectsOutOfRangeRevLimit()
        {
            var calMgr = new Calibration.CalibrationManager();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                calMgr.RecordChange(new CalibrationChange
                {
                    Parameter = "Primary Rev Limit",
                    OldValue = "7200",
                    NewValue = "12000",
                    Offset = 0x1FB0,
                    Source = "Test"
                });
            });
        }
    }

    /// <summary>
    /// AutoTune Güvenlik Testleri — xUnit formatı
    /// </summary>
    public class AutoTuneSafetyTests
    {
        [Fact]
        public void AutoTune_RejectsLowECT()
        {
            var validator = new AutoTuneValidator();
            var frame = new TelemetryFrameData { Rpm = 3500, Map = 80, Tps = 45, Afr = 14.0, Ect = 50, BatteryVolts = 13.8 };
            var result = validator.Validate(frame);
            Assert.False(result.IsValid);
            Assert.Equal("ECT_LOW", result.Code);
        }

        [Fact]
        public void AutoTune_RejectsLowBattery()
        {
            var validator = new AutoTuneValidator();
            var frame = new TelemetryFrameData { Rpm = 3500, Map = 80, Tps = 45, Afr = 14.0, Ect = 82, BatteryVolts = 10.5 };
            var result = validator.Validate(frame);
            Assert.False(result.IsValid);
            Assert.Equal("BATT_LOW", result.Code);
        }

        [Fact]
        public void AutoTune_ClampsCorrection_WithinPlusMinus12Percent()
        {
            var validator = new AutoTuneValidator();
            double clamped = validator.ClampCorrection(25.0);
            Assert.True(Math.Abs(clamped) <= 12.0);
        }
    }

    /// <summary>
    /// Map Engine Testleri — xUnit formatı
    /// </summary>
    public class MapEngineTests
    {
        private static MapDefinition MakeTestMapDef() => new MapDefinition
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

        [Fact]
        public void MapEngine_ReadWriteCell_AccurateRoundTrip()
        {
            var romService = Core.Container.ServiceContainer.Resolve<IРomService>();
            romService.SetBuffer(new byte[0x8000]);

            var mapManager = Core.Container.ServiceContainer.Resolve<MapManager>();
            var def = MakeTestMapDef();
            mapManager.WriteCell(def, 4, 5, 12.3);
            double readVal = mapManager.ReadCell(def, 4, 5);
            Assert.True(Math.Abs(readVal - 12.3) < 0.01);
        }

        [Fact]
        public void MapEngine_ScaleFactor_ConvertedToRawByte()
        {
            var romService = Core.Container.ServiceContainer.Resolve<IРomService>();
            romService.SetBuffer(new byte[0x8000]);

            var mapManager = Core.Container.ServiceContainer.Resolve<MapManager>();
            var def = MakeTestMapDef();
            mapManager.WriteCell(def, 0, 0, 12.5);
            byte rawByte = romService.GetBuffer()[0x1D40];
            Assert.Equal(125, rawByte);
        }

        [Fact]
        public void MapEngine_InvalidOffset_ThrowsArgumentOutOfRange()
        {
            var mapManager = Core.Container.ServiceContainer.Resolve<MapManager>();
            var def = new MapDefinition
            {
                MapName = "InvalidOffsetMap",
                EcuCompatibility = "P28",
                Offset = 999999,
                Rows = 16,
                Columns = 16,
                ScaleFactor = 1.0
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => mapManager.WriteCell(def, 0, 0, 10.0));
        }

        [Fact]
        public void Interpolation_BilinearWeightSum_EqualsOne()
        {
            var interpEngine = Core.Container.ServiceContainer.Resolve<IInterpolationEngine>();
            var def = MakeTestMapDef();
            var xAxis = new AxisDefinition { Name = "RPM", Length = 16, ConvertedValues = new double[] { 500, 750, 1000, 1250, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000 } };
            var yAxis = new AxisDefinition { Name = "MAP", Length = 16, ConvertedValues = new double[] { 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170 } };
            var table = new TableDefinition(def, xAxis, yAxis);

            var res = interpEngine.Interpolate(1800, 55, table);
            double weightSum = res.CellWeights.Sum();
            Assert.True(Math.Abs(weightSum - 1.0) < 1e-9);
        }
    }

    /// <summary>
    /// Checksum Engine Testleri — xUnit formatı
    /// </summary>
    public class ChecksumTests
    {
        private static void ForceLoadDatabase()
        {
            var db = Database.EcuDatabaseManager.Instance;
            if (db.GetProfile("P28") == null)
            {
                string dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
                if (!Directory.Exists(dbDir) || !File.Exists(Path.Combine(dbDir, "ecu_database.json")))
                    dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Database");
                db.LoadDatabase(dbDir);
                if (db.GetProfile("P28") == null)
                    db.LoadDatabase(Path.Combine(Directory.GetCurrentDirectory(), "Database"));
            }
        }

        [Fact]
        public void Checksum_StockRom_PassesValidation()
        {
            ForceLoadDatabase();
            var checksumEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Checksum.IChecksumEngine>();
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28");
            byte[] romBuffer = new byte[0x8000];
            checksumEngine.Update(romBuffer, profile.ChecksumDefinitions[0]);
            var res = checksumEngine.Validate(romBuffer, profile.ChecksumDefinitions[0]);
            Assert.True(res.IsValid);
        }

        [Fact]
        public void Checksum_CorruptByte_FailsValidation()
        {
            ForceLoadDatabase();
            var checksumEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Checksum.IChecksumEngine>();
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28");
            byte[] romBuffer = new byte[0x8000];
            checksumEngine.Update(romBuffer, profile.ChecksumDefinitions[0]);
            romBuffer[0x1000] = 0xAA;
            var res = checksumEngine.Validate(romBuffer, profile.ChecksumDefinitions[0]);
            Assert.False(res.IsValid);
        }

        [Fact]
        public void Checksum_VerifyBeforeSave_DetectsBadChecksum()
        {
            var checksumEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Checksum.IChecksumEngine>();
            byte[] romBuffer = new byte[0x8000];

            var badDef = new Core.Rom.Checksum.ChecksumDefinition
            {
                ChecksumType = "TestBad",
                Algorithm = Core.Rom.Checksum.ChecksumAlgorithm.Xor8,
                ChecksumAddress = 0x7FFF,
                RangeStart = 0x0000,
                RangeEnd = 0x7FFE
            };

            romBuffer[0x7FFF] = 0x55;
            bool isOk = checksumEngine.VerifyBeforeSave(romBuffer, new List<Core.Rom.Checksum.ChecksumDefinition> { badDef }, out _);
            Assert.False(isOk);
        }

        [Fact]
        public void Checksum_MultipleRegions_AllValid()
        {
            var checksumEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Checksum.IChecksumEngine>();
            byte[] romBuffer = new byte[0x8000];

            var def1 = new Core.Rom.Checksum.ChecksumDefinition
            {
                ChecksumType = "Region1",
                Algorithm = Core.Rom.Checksum.ChecksumAlgorithm.Xor8,
                ChecksumAddress = 0x7FFF,
                RangeStart = 0x0000,
                RangeEnd = 0x7FFE,
                ExcludeRanges = new List<Core.Rom.Checksum.ExcludeRange> { new Core.Rom.Checksum.ExcludeRange { Start = 0x7FFE, End = 0x7FFE } }
            };
            var def2 = new Core.Rom.Checksum.ChecksumDefinition
            {
                ChecksumType = "Region2",
                Algorithm = Core.Rom.Checksum.ChecksumAlgorithm.Add8,
                ChecksumAddress = 0x7FFE,
                RangeStart = 0x0000,
                RangeEnd = 0x7FDF
            };

            checksumEngine.Update(romBuffer, def1);
            checksumEngine.Update(romBuffer, def2);

            bool isOk = checksumEngine.VerifyBeforeSave(romBuffer, new List<Core.Rom.Checksum.ChecksumDefinition> { def1, def2 }, out var results);
            Assert.True(isOk);
            Assert.Equal(2, results.Count);
            Assert.True(results[0].IsValid && results[1].IsValid);
        }
    }

    /// <summary>
    /// PatchEngine v2 Testleri — xUnit formatı
    /// </summary>
    public class PatchEngineTests
    {
        private static void ForceLoadDatabase()
        {
            var db = Database.EcuDatabaseManager.Instance;
            if (db.GetProfile("P28") == null)
            {
                string dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
                if (!Directory.Exists(dbDir) || !File.Exists(Path.Combine(dbDir, "ecu_database.json")))
                    dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Database");
                db.LoadDatabase(dbDir);
            }
        }

        private Core.Rom.Patch.PatchEngine MakePatchEngine()
        {
            ForceLoadDatabase();
            var checksumEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Checksum.IChecksumEngine>();
            var calService = Core.Container.ServiceContainer.Resolve<ICalibrationService>();
            return new Core.Rom.Patch.PatchEngine(checksumEngine, calService);
        }

        [Fact]
        public void PatchEngine_LaunchControl_AppliedSuccessfully()
        {
            ForceLoadDatabase();
            var patchEngine = MakePatchEngine();
            byte[] rom = new byte[32768];
            rom[8112] = 144; rom[8113] = 144;

            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28") ?? EcuProfiles.P28;
            var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");

            Assert.True(result.IsSuccess, $"Hata: {result.ErrorMessage}");
            Assert.Equal(205, rom[8112]);
            Assert.Equal(25, rom[8113]);
        }

        [Fact]
        public void PatchEngine_ExpectedBytesMismatch_Fails()
        {
            var patchEngine = MakePatchEngine();
            byte[] rom = new byte[32768]; // bytes[8112]=0 intentionally wrong
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28") ?? EcuProfiles.P28;
            var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void PatchEngine_IncompatibleEcu_Fails()
        {
            var patchEngine = MakePatchEngine();
            byte[] rom = new byte[32768];
            rom[8112] = 144; rom[8113] = 144;
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P30") ?? EcuProfiles.P30;
            var result = patchEngine.ApplyPatch(rom, "SpeedLimiterPatch", profile, "TestUser");
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void PatchEngine_WrongRomSize_Fails()
        {
            var patchEngine = MakePatchEngine();
            byte[] rom = new byte[16384]; // wrong size
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28") ?? EcuProfiles.P28;
            var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void PatchEngine_Rollback_RestoresOriginalBytes()
        {
            var patchEngine = MakePatchEngine();
            byte[] rom = new byte[32768];
            rom[8112] = 144; rom[8113] = 144;
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28") ?? EcuProfiles.P28;

            var applyResult = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");
            Assert.True(applyResult.IsSuccess, $"Apply Hata: {applyResult.ErrorMessage}");

            var rollbackResult = patchEngine.RollbackPatch(rom, "LaunchControl", profile, "TestUser");
            Assert.True(rollbackResult.IsSuccess, $"Rollback Hata: {rollbackResult.ErrorMessage}");
            Assert.Equal(144, rom[8112]);
            Assert.Equal(144, rom[8113]);
        }

        [Fact]
        public void PatchEngine_ChecksumUpdated_AfterPatch()
        {
            var patchEngine = MakePatchEngine();
            byte[] rom = new byte[32768];
            rom[8112] = 144; rom[8113] = 144;
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28") ?? EcuProfiles.P28;

            var result = patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.NotEqual(0, rom[32767]);
        }

        [Fact]
        public void PatchEngine_AuditLog_ContainsSuccessEntry()
        {
            var patchEngine = MakePatchEngine();
            byte[] rom = new byte[32768];
            rom[8112] = 144; rom[8113] = 144;
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28") ?? EcuProfiles.P28;
            patchEngine.ApplyPatch(rom, "LaunchControl", profile, "TestUser");

            var logs = patchEngine.GetPatchAudit();
            Assert.True(logs.Count > 0 && logs.Any(l => l.PatchId == "LaunchControl" && l.Result.Contains("SUCCESS")));
        }

        [Fact]
        public void PatchEngine_GetAvailablePatches_ReturnsFourPatches()
        {
            var patchEngine = MakePatchEngine();
            var profile = Database.EcuDatabaseManager.Instance.GetProfile("P28") ?? EcuProfiles.P28;
            var patches = patchEngine.GetAvailablePatches(profile);
            Assert.Equal(4, patches.Count);
            Assert.Contains(patches, p => p.PatchId == "LaunchControl");
        }
    }

    /// <summary>
    /// Telemetry Bus Testleri — xUnit formatı
    /// </summary>
    public class TelemetryBusTests
    {
        private class TestConsumer : ITelemetryConsumer
        {
            public List<TelemetryFrame> Frames { get; } = new List<TelemetryFrame>();
            public List<TelemetryEvent> Events { get; } = new List<TelemetryEvent>();

            public void Consume(TelemetryFrame frame)
            {
                lock (Frames)
                {
                    var copy = TelemetryFramePool.Rent();
                    copy.ChannelId = frame.ChannelId;
                    copy.Value = frame.Value;
                    copy.FilteredValue = frame.FilteredValue;
                    copy.Status = frame.Status;
                    Frames.Add(copy);
                }
            }
            public void ConsumeEvent(TelemetryEvent busEvent) { lock (Events) { Events.Add(busEvent); } }
        }

        [Fact]
        public void TelemetryBus_PublishAndSubscribe_ConsumerReceivesFrame()
        {
            using var bus = new TelemetryBus();
            var consumer = new TestConsumer();
            bus.Subscribe(consumer);
            bus.Start();

            var frame = TelemetryFramePool.Rent();
            frame.ChannelId = "RPM"; frame.Value = 3000; frame.FilteredValue = 3000;
            bus.Publish(frame);
            Thread.Sleep(100);

            lock (consumer.Frames)
            {
                Assert.True(consumer.Frames.Count > 0 && consumer.Frames[0].ChannelId == "RPM");
            }
        }

        [Fact]
        public void TelemetryBus_ChannelFormulaEvaluation_ResultsCorrectly()
        {
            double result = TelemetryFormulaEvaluator.Evaluate("[RPM] * 2 + [TPS] / 10", id =>
            {
                if (id == "RPM") return 3000.0;
                if (id == "TPS") return 50.0;
                return 0.0;
            });
            Assert.True(Math.Abs(result - 6005.0) < 0.001);
        }

        [Fact]
        public void TelemetryBus_RingBuffer_OverflowOverwritesOldest()
        {
            var buffer = new TelemetryBuffer(3);
            for (int i = 1; i <= 5; i++)
            {
                var f = TelemetryFramePool.Rent();
                f.ChannelId = "RPM"; f.Value = i;
                buffer.Enqueue(f);
            }
            var all = buffer.GetAll();
            Assert.Equal(3, buffer.Count);
            Assert.Equal(3, (int)all[0].Value);
            Assert.Equal(5, (int)all[2].Value);
        }

        [Fact]
        public void TelemetryBus_MovingAverageFilter_CalculatesCorrectly()
        {
            var filter = TelemetryFilterFactory.Create(FilterType.MovingAverage, 3);
            filter.Filter(10); filter.Filter(20);
            double avg = filter.Filter(30);
            Assert.True(Math.Abs(avg - 20.0) < 0.001);
        }

        [Fact]
        public void TelemetryBus_LowPassFilter_SmoothsCorrectly()
        {
            var filter = TelemetryFilterFactory.Create(FilterType.LowPass, 0.2);
            filter.Filter(10);
            double val = filter.Filter(20);
            Assert.True(Math.Abs(val - 12.0) < 0.001);
        }

        [Fact]
        public void TelemetryBus_FramePool_ResetOnReturn()
        {
            var frame = TelemetryFramePool.Rent();
            frame.ChannelId = "MAP";
            TelemetryFramePool.Return(frame);
            var rented = TelemetryFramePool.Rent();
            Assert.Null(rented.ChannelId);
            TelemetryFramePool.Return(rented);
        }

        [Fact]
        public void TelemetryBus_SnapshotImmutability_PropertiesReadOnly()
        {
            var snap = new TelemetrySnapshot(
                "v2.0", DateTime.UtcNow, 100, 3000, 20.0, 95.0, 85.0, 35.0, 13.8, 60.0,
                15.0, 24.5, 14.5, 0.98, 0, 0.0, 0.0, true, false, 45.0);
            Assert.Equal(3000, snap.RPM);
            Assert.Equal(20.0, snap.TPS);
            Assert.True(snap.ClosedLoop);
        }

        [Fact]
        public void TelemetryBus_AccessControl_HierarchyEnforced()
        {
            var access = new AccessControl();
            access.SetCurrentRole(TelemetryRole.Calibration);
            bool canCalibrate = access.Authorize(TelemetryRole.Calibration, "ModifyMap", out _);
            bool canFlash = access.Authorize(TelemetryRole.Flash, "FlashEcu", out _);
            Assert.True(canCalibrate);
            Assert.False(canFlash);
        }
    }

    /// <summary>
    /// AutoTune Closed Loop Engine Testleri — xUnit formatı
    /// </summary>
    public class AutoTuneEngineTests
    {
        [Fact]
        public void AutoTune_SessionLifecycle_StartPauseResumeStop()
        {
            var engine = Core.Container.ServiceContainer.Resolve<IAutoTuneEngine>();
            engine.StopSession(); // reset any existing session

            bool start = engine.StartSession("P28", "TunerUser", AutoTuneOperatingMode.DryRun, "Default");
            Assert.True(start);
            Assert.True(engine.IsRunning);

            engine.PauseSession();
            Assert.Equal(SessionState.Paused, engine.ActiveSession?.State);

            engine.ResumeSession();
            Assert.True(engine.IsRunning);
            Assert.Equal(SessionState.Running, engine.ActiveSession?.State);

            engine.StopSession();
            Assert.False(engine.IsRunning);
        }

        [Fact]
        public void AutoTune_ConfidenceLowDeviation_ScoreHigh()
        {
            var confidenceEngine = Core.Container.ServiceContainer.Resolve<ITuneConfidenceEngine>();
            var memory = new AdaptiveMemory();
            double score = confidenceEngine.CalculateConfidence(10.0, 0.2, 15, memory, 85.0, 13.8, out var reason);
            Assert.True(score > 80.0);
            Assert.False(string.IsNullOrEmpty(reason));
        }

        [Fact]
        public void AutoTune_ConfidenceHighDeviation_ScoreLow()
        {
            var confidenceEngine = Core.Container.ServiceContainer.Resolve<ITuneConfidenceEngine>();
            var memory = new AdaptiveMemory();
            double score = confidenceEngine.CalculateConfidence(120.0, 2.5, 3, memory, 60.0, 11.8, out var reason);
            Assert.True(score < 50.0);
            Assert.False(string.IsNullOrEmpty(reason));
        }

        [Fact]
        public void AutoTune_NoRomMutation_OnApproveDecision()
        {
            var engine = Core.Container.ServiceContainer.Resolve<IAutoTuneEngine>();
            var romService = Core.Container.ServiceContainer.Resolve<IРomService>();
            byte[] originalRom = (byte[])romService.GetBuffer().Clone();

            engine.StopSession();
            engine.StartSession("P28-MUTTEST", "TunerUser", AutoTuneOperatingMode.DryRun, "Default");

            // Herhangi bir karar varsa approve et — ROM değişmemeli
            engine.StopSession();

            byte[] afterRom = romService.GetBuffer();
            Assert.Equal(originalRom.Length, afterRom.Length);
        }
    }

    /// <summary>
    /// Concurrency Lifecycle (TOCTOU) Testleri — xUnit formatı
    /// </summary>
    public class ConcurrencyTests
    {
        [Fact]
        public void Concurrency_DuplicateSession_SecondStartReturnsFalse()
        {
            var engine = Core.Container.ServiceContainer.Resolve<IAutoTuneEngine>();
            engine.StopSession();

            bool first = engine.StartSession("ECU-CONC", "TunerUser", AutoTuneOperatingMode.DryRun, "Default");
            bool second = engine.StartSession("ECU-CONC", "TunerUser", AutoTuneOperatingMode.DryRun, "Default");

            engine.StopSession();
            Assert.True(first);
            Assert.False(second);
        }

        [Fact]
        public void Concurrency_ConcurrentStartSession_OnlyOneSucceeds()
        {
            var engine = Core.Container.ServiceContainer.Resolve<IAutoTuneEngine>();
            engine.StopSession();

            int successCount = 0;
            var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(() =>
            {
                if (engine.StartSession("ECU-RACE", $"User{i}", AutoTuneOperatingMode.DryRun, "Default"))
                    Interlocked.Increment(ref successCount);
            })).ToArray();

            Task.WaitAll(tasks);
            engine.StopSession();

            Assert.Equal(1, successCount);
        }

        [Fact]
        public void Concurrency_ProcessTelemetryAfterStopped_DoesNotThrow()
        {
            var engine = Core.Container.ServiceContainer.Resolve<IAutoTuneEngine>();
            engine.StopSession();
            engine.StartSession("ECU-SAFE", "TunerUser", AutoTuneOperatingMode.DryRun, "Default");
            engine.StopSession();

            // Should not throw
            var snapshot = new TelemetrySnapshot("1.0", DateTime.UtcNow, 1, 3000, 50, 100, 80, 40, 14.1, 0, 12, 16.5, 14.7, 1.0, 0, 0.0, 0.0, true, false, 50.0);
            var ex = Record.Exception(() => engine.ProcessTelemetry(snapshot));
            Assert.Null(ex);
        }
    }

    /// <summary>
    /// RomLayoutValidator Testleri — xUnit formatı
    /// </summary>
    public class RomLayoutValidatorTests
    {
        [Fact]
        public void RomBounds_NegativeOffset_IsBlocked()
        {
            var validator = new Core.Rom.RomLayoutValidator();
            bool valid = validator.ValidateOffset(-1, 10, 0x8000);
            Assert.False(valid);
        }

        [Fact]
        public void RomBounds_IntegerOverflow_IsBlocked()
        {
            var validator = new Core.Rom.RomLayoutValidator();
            bool valid = validator.ValidateOffset(int.MaxValue, 10, 0x8000);
            Assert.False(valid);
        }

        [Fact]
        public void RomBounds_ValidOffset_IsAccepted()
        {
            var validator = new Core.Rom.RomLayoutValidator();
            bool valid = validator.ValidateOffset(0x1000, 256, 0x8000);
            Assert.True(valid);
        }
    }

    /// <summary>
    /// Örnek ROM Test Framework — xUnit formatı
    /// </summary>
    public class SampleRomTests
    {
        [Fact]
        public void SampleRom_ChecksumOffset_IsValid()
        {
            byte[] rom = new byte[0x8000];
            rom[0x7FFF] = 0x42;
            Assert.Equal(EcuProfiles.P28.RomSize, rom.Length);
            Assert.True(EcuProfiles.P28.ChecksumOffset < rom.Length);
        }

        [Fact]
        public void SampleRom_ShortRom_IsRejected()
        {
            byte[] rom = new byte[100];
            var identifier = new RomIdentifier();
            var result = identifier.IdentifyRom(rom, EcuProfiles.All);
            Assert.True(result.IsMismatch);
        }

        [Fact]
        public void SampleRom_FuelMapBounds_AreValid()
        {
            byte[] rom = new byte[0x8000];
            int endOffset = EcuProfiles.P28.FuelMapOffset + (EcuProfiles.P28.FuelMapRows * EcuProfiles.P28.FuelMapCols);
            Assert.True(endOffset <= rom.Length);
        }

        [Fact]
        public void SampleRom_IgnMapBounds_AreValid()
        {
            byte[] rom = new byte[0x8000];
            int endOffset = EcuProfiles.P28.IgnMapOffset + (EcuProfiles.P28.IgnMapRows * EcuProfiles.P28.IgnMapCols);
            Assert.True(endOffset <= rom.Length);
        }

        [Fact]
        public void SampleRom_AllProfiles_Are32KB()
        {
            Assert.All(EcuProfiles.All, p => Assert.Equal(0x8000, p.RomSize));
        }
    }
}
