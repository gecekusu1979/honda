using System;
using System.Text;

namespace HondaTuner.Core
{
    public enum TuningGoal
    {
        StockStreet,
        IesVtecStreet,
        NaturallyAspirated,
        TurboSafeBase,
        Economy
    }

    public sealed class TuningSetup
    {
        public TuningGoal Goal { get; set; } = TuningGoal.IesVtecStreet;
        public int InjectorCc { get; set; } = 240;
        public int MapSensorBar { get; set; } = 1;
        public double TargetAfrIdle { get; set; } = 14.7;
        public double TargetAfrCruise { get; set; } = 14.7;
        public double TargetAfrPower { get; set; } = 12.8;
        public int VtecRpm { get; set; } = 4800;
        public int RevLimitRpm { get; set; } = 7200;
        public int SpeedLimitKmh { get; set; } = 220;
        public double InjectorDeadTimeMs { get; set; } = 0.8;
    }

    public sealed class TuningResult
    {
        public byte[,] FuelMap { get; }
        public byte[,] IgnitionMap { get; }
        public string Summary { get; }

        public TuningResult(byte[,] fuelMap, byte[,] ignitionMap, string summary)
        {
            FuelMap = fuelMap;
            IgnitionMap = ignitionMap;
            Summary = summary;
        }
    }

    public static class TuningAssistant
    {
        public static TuningResult CreateBaseMap(EcuProfile profile, byte[,] currentFuel, byte[,] currentIgnition, TuningSetup setup)
        {
            var fuel = (byte[,])currentFuel.Clone();
            var ignition = (byte[,])currentIgnition.Clone();
            var notes = new StringBuilder();

            double injectorScale = setup.InjectorCc > 0 ? 240.0 / setup.InjectorCc : 1.0;
            injectorScale = Clamp(injectorScale, 0.55, 1.45);

            notes.AppendLine($"Profil: {profile.EcuCode} / {profile.EngineCode}");
            notes.AppendLine($"Hedef: {DescribeGoal(setup.Goal)}");
            notes.AppendLine($"Enjektor olcegi: {injectorScale:0.00}x ({setup.InjectorCc} cc)");

            for (int r = 0; r < fuel.GetLength(0); r++)
            {
                int rpm = Axis(profile.RpmAxis, r);
                for (int c = 0; c < fuel.GetLength(1); c++)
                {
                    int load = Axis(profile.LoadAxis, c);
                    double fuelScale = injectorScale * GoalFuelMultiplier(setup.Goal, rpm, load, setup.MapSensorBar);
                    double ignDelta = GoalIgnitionDelta(setup.Goal, rpm, load, setup.MapSensorBar);

                    fuel[r, c] = ClampByte(fuel[r, c] * fuelScale);
                    ignition[r, c] = ClampByte(ignition[r, c] + ignDelta);
                }
            }

            SmoothMap(fuel, 1);
            SmoothMap(ignition, 1);

            notes.AppendLine($"AFR hedefleri: idle {setup.TargetAfrIdle:0.0}, cruise {setup.TargetAfrCruise:0.0}, power {setup.TargetAfrPower:0.0}");
            notes.AppendLine($"Limitler: VTEC {setup.VtecRpm} rpm, rev {setup.RevLimitRpm} rpm, hiz {setup.SpeedLimitKmh} km/h");
            notes.AppendLine("Not: Bu guvenli baslangic haritasidir; dyno/wideband ile dogrulanmadan araca yazilmaz.");

            return new TuningResult(fuel, ignition, notes.ToString());
        }

        public static byte[,] ApplyWidebandCorrection(byte[,] fuelMap, EcuProfile profile, double measuredAfr, double targetAfr, int rpm, int load, int radius)
        {
            var result = (byte[,])fuelMap.Clone();
            if (measuredAfr <= 0 || targetAfr <= 0) return result;

            int centerRow = FindNearest(profile.RpmAxis, rpm);
            int centerCol = FindNearest(profile.LoadAxis, load);
            double correction = measuredAfr / targetAfr;
            correction = Clamp(correction, 0.85, 1.15);

            for (int r = Math.Max(0, centerRow - radius); r <= Math.Min(result.GetLength(0) - 1, centerRow + radius); r++)
            {
                for (int c = Math.Max(0, centerCol - radius); c <= Math.Min(result.GetLength(1) - 1, centerCol + radius); c++)
                {
                    double distance = Math.Abs(r - centerRow) + Math.Abs(c - centerCol);
                    double blend = 1.0 - (distance / Math.Max(1.0, radius + 1.0));
                    double cellCorrection = 1.0 + ((correction - 1.0) * blend);
                    result[r, c] = ClampByte(result[r, c] * cellCorrection);
                }
            }

            return result;
        }

        public static TuningSetup DefaultsFor(EcuProfile profile, VehicleEntry vehicle)
        {
            var setup = new TuningSetup
            {
                Goal = profile.HasVtec ? TuningGoal.IesVtecStreet : TuningGoal.StockStreet,
                VtecRpm = profile.HasVtec ? profile.VtecRpmDefault : 0,
                RevLimitRpm = profile.RevLimitDefault,
                SpeedLimitKmh = 220,
                InjectorDeadTimeMs = 0.8
            };

            if (vehicle != null && vehicle.EngineCode.StartsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                setup.Goal = TuningGoal.NaturallyAspirated;
                setup.TargetAfrPower = 12.9;
            }

            return setup;
        }

        public static string DescribeGoal(TuningGoal goal)
        {
            switch (goal)
            {
                case TuningGoal.StockStreet: return "Stock / gunluk kullanim";
                case TuningGoal.IesVtecStreet: return "iES VTEC yumurta kasa sokak ayari";
                case TuningGoal.NaturallyAspirated: return "Atmosferik performans";
                case TuningGoal.TurboSafeBase: return "Turbo guvenli basemap";
                case TuningGoal.Economy: return "Ekonomi / dusuk tuketim";
                default: return goal.ToString();
            }
        }

        private static double GoalFuelMultiplier(TuningGoal goal, int rpm, int load, int mapSensorBar)
        {
            bool power = rpm >= 4500 || load >= 110;
            bool boostRange = mapSensorBar >= 2 && load >= 100;

            switch (goal)
            {
                case TuningGoal.IesVtecStreet:
                    return power ? 1.04 : load < 60 ? 0.98 : 1.0;
                case TuningGoal.NaturallyAspirated:
                    return power ? 1.06 : 1.01;
                case TuningGoal.TurboSafeBase:
                    return boostRange ? 1.16 : power ? 1.08 : 1.02;
                case TuningGoal.Economy:
                    return power ? 1.0 : 0.96;
                default:
                    return 1.0;
            }
        }

        private static double GoalIgnitionDelta(TuningGoal goal, int rpm, int load, int mapSensorBar)
        {
            bool highLoad = load >= 100;
            switch (goal)
            {
                case TuningGoal.IesVtecStreet:
                    return highLoad && rpm >= 5000 ? -1 : 0;
                case TuningGoal.NaturallyAspirated:
                    return highLoad ? -1 : 1;
                case TuningGoal.TurboSafeBase:
                    return mapSensorBar >= 2 && highLoad ? -4 : -2;
                case TuningGoal.Economy:
                    return highLoad ? 0 : 1;
                default:
                    return 0;
            }
        }

        private static void SmoothMap(byte[,] map, int passes)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                var copy = (byte[,])map.Clone();
                for (int r = 1; r < map.GetLength(0) - 1; r++)
                {
                    for (int c = 1; c < map.GetLength(1) - 1; c++)
                    {
                        int average = (copy[r, c] * 4 + copy[r - 1, c] + copy[r + 1, c] + copy[r, c - 1] + copy[r, c + 1]) / 8;
                        map[r, c] = (byte)average;
                    }
                }
            }
        }

        private static int Axis(int[] axis, int index) => index < axis.Length ? axis[index] : 0;

        private static int FindNearest(int[] axis, int target)
        {
            int best = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < axis.Length; i++)
            {
                int distance = Math.Abs(axis[i] - target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private static byte ClampByte(double value) => (byte)Math.Round(Clamp(value, 0, 255));

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
