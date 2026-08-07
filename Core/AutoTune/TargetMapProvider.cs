using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace HondaTuner.Core.AutoTune
{
    public class TargetMapProvider
    {
        public List<int> RpmBins { get; private set; } = new List<int>();
        public List<int> LoadBins { get; private set; } = new List<int>();
        public List<List<double>> AfrTargets { get; private set; } = new List<List<double>>();
        public List<List<double>> LambdaTargets { get; private set; } = new List<List<double>>();
        public List<List<double>> IgnitionTargets { get; private set; } = new List<List<double>>();
        public List<List<double>> VeTargets { get; private set; } = new List<List<double>>();

        public void LoadTargets(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LoadFallbacks();
                return;
            }
            try
            {
                string json = File.ReadAllText(filePath);
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    RpmBins = DeserializeIntList(root.GetProperty("RpmBins"));
                    LoadBins = DeserializeIntList(root.GetProperty("LoadBins"));
                    AfrTargets = DeserializeDoubleMatrix(root.GetProperty("AfrTargets"));
                    LambdaTargets = DeserializeDoubleMatrix(root.GetProperty("LambdaTargets"));
                    IgnitionTargets = DeserializeDoubleMatrix(root.GetProperty("IgnitionTargets"));
                    VeTargets = DeserializeDoubleMatrix(root.GetProperty("VeTargets"));
                }
            }
            catch
            {
                LoadFallbacks();
            }
        }

        private List<int> DeserializeIntList(JsonElement elem)
        {
            var lst = new List<int>();
            foreach (var item in elem.EnumerateArray()) lst.Add(item.GetInt32());
            return lst;
        }

        private List<List<double>> DeserializeDoubleMatrix(JsonElement elem)
        {
            var mat = new List<List<double>>();
            foreach (var row in elem.EnumerateArray())
            {
                var r = new List<double>();
                foreach (var col in row.EnumerateArray()) r.Add(col.GetDouble());
                mat.Add(r);
            }
            return mat;
        }

        private void LoadFallbacks()
        {
            RpmBins = new List<int> { 500, 1000, 2000, 3000, 4000, 5000, 6000, 7000 };
            LoadBins = new List<int> { 10, 30, 50, 70, 90 };

            AfrTargets = new List<List<double>>();
            LambdaTargets = new List<List<double>>();
            IgnitionTargets = new List<List<double>>();
            VeTargets = new List<List<double>>();

            for (int r = 0; r < 8; r++)
            {
                var rowA = new List<double>();
                var rowL = new List<double>();
                var rowI = new List<double>();
                var rowV = new List<double>();
                for (int c = 0; c < 5; c++)
                {
                    rowA.Add(14.7);
                    rowL.Add(1.0);
                    rowI.Add(15.0);
                    rowV.Add(60.0);
                }
                AfrTargets.Add(rowA);
                LambdaTargets.Add(rowL);
                IgnitionTargets.Add(rowI);
                VeTargets.Add(rowV);
            }
        }

        public double GetTargetValue(List<List<double>> table, double rpm, double load)
        {
            if (table == null || table.Count == 0) return 0.0;

            int rpmIndex = FindClosestBin(RpmBins, rpm);
            int loadIndex = FindClosestBin(LoadBins, load);

            rpmIndex = Math.Clamp(rpmIndex, 0, table.Count - 1);
            loadIndex = Math.Clamp(loadIndex, 0, table[rpmIndex].Count - 1);

            return table[rpmIndex][loadIndex];
        }

        public int FindClosestRpmBin(double rpm) => FindClosestBin(RpmBins, rpm);
        public int FindClosestLoadBin(double load) => FindClosestBin(LoadBins, load);

        private int FindClosestBin(List<int> bins, double val)
        {
            if (bins == null || bins.Count == 0) return 0;
            int minIndex = 0;
            double minDiff = double.MaxValue;
            for (int i = 0; i < bins.Count; i++)
            {
                double diff = Math.Abs(bins[i] - val);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    minIndex = i;
                }
            }
            return minIndex;
        }
    }
}
