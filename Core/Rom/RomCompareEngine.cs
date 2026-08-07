using System;
using System.Collections.Generic;

namespace HondaTuner.Core.Rom
{
    public class RomCompareEngine
    {
        public static double[,] CompareFuelMaps(byte[,] original, byte[,] modified)
        {
            int rows = original.GetLength(0);
            int cols = original.GetLength(1);
            var diff = new double[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double origVal = original[r, c];
                    double modVal = modified[r, c];
                    if (origVal > 0)
                    {
                        diff[r, c] = ((modVal - origVal) / origVal) * 100.0;
                    }
                    else
                    {
                        diff[r, c] = 0;
                    }
                }
            }
            return diff;
        }

        public static int[,] CompareIgnitionMaps(byte[,] original, byte[,] modified)
        {
            int rows = original.GetLength(0);
            int cols = original.GetLength(1);
            var diff = new int[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    diff[r, c] = (int)modified[r, c] - (int)original[r, c];
                }
            }
            return diff;
        }

        public static List<string> CompareParameters(RomParser originalParser, RomParser modifiedParser)
        {
            var report = new List<string>();

            // VTEC Comparison
            if (originalParser.Profile.HasVtec && modifiedParser.Profile.HasVtec)
            {
                int origVtec = originalParser.ReadVtecRpm();
                int modVtec = modifiedParser.ReadVtecRpm();
                if (origVtec != modVtec)
                    report.Add($"VTEC: {origVtec} RPM -> {modVtec} RPM");

                int origVtecLoad = originalParser.ReadVtecLoadThreshold();
                int modVtecLoad = modifiedParser.ReadVtecLoadThreshold();
                if (origVtecLoad != modVtecLoad)
                    report.Add($"VTEC Yuk: {origVtecLoad} kPa -> {modVtecLoad} kPa");
            }

            // Rev Limit Comparison
            int origRev = originalParser.ReadRevLimit();
            int modRev = modifiedParser.ReadRevLimit();
            if (origRev != modRev)
                report.Add($"Rev Sınırı: {origRev} RPM -> {modRev} RPM");

            // Speed Limit Comparison
            int origSpeed = originalParser.ReadSpeedLimiter();
            int modSpeed = modifiedParser.ReadSpeedLimiter();
            if (origSpeed != modSpeed)
                report.Add($"Hız Sınırı: {origSpeed} km/h -> {modSpeed} km/h");

            // Dead Time Comparison
            double origDead = originalParser.ReadInjectorDeadTime();
            double modDead = modifiedParser.ReadInjectorDeadTime();
            if (Math.Abs(origDead - modDead) > 0.001)
                report.Add($"Enjektör Ölü Süre: {origDead:0.00} ms -> {modDead:0.00} ms");

            return report;
        }
    }
}
