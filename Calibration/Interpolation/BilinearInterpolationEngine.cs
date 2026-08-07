using System;
using HondaTuner.Calibration.Maps;

namespace HondaTuner.Calibration.Interpolation
{
    /// <summary>
    /// İki eksenli (2D) kalibrasyon tabloları üzerinde Bilinear Enterpolasyon uygulayan motor.
    /// </summary>
    public class BilinearInterpolationEngine : IInterpolationEngine
    {
        public InterpolationResult Interpolate(double rpm, double load, TableDefinition table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (table.XAxis == null || table.YAxis == null)
                throw new InvalidOperationException("Tabloda X ve/veya Y eksenleri tanımlı değil.");

            var xAxis = table.XAxis.ConvertedValues; // RPM Eksen Değerleri (Sütunlar)
            var yAxis = table.YAxis.ConvertedValues; // Load/MAP Eksen Değerleri (Satırlar)

            if (xAxis == null || xAxis.Length == 0 || yAxis == null || yAxis.Length == 0)
                throw new InvalidOperationException("Eksen değer dizileri boş olamaz.");

            int cols = xAxis.Length;
            int rows = yAxis.Length;

            // Girişleri eksen sınırlarına kırp (clamp)
            double clampedRpm = Math.Max(xAxis[0], Math.Min(xAxis[cols - 1], rpm));
            double clampedLoad = Math.Max(yAxis[0], Math.Min(yAxis[rows - 1], load));

            // Sütun (X) dizinini bul
            int c = 0;
            for (int i = 0; i < cols - 1; i++)
            {
                if (clampedRpm >= xAxis[i] && clampedRpm <= xAxis[i + 1])
                {
                    c = i;
                    break;
                }
            }
            if (clampedRpm >= xAxis[cols - 1]) c = cols - 2;

            // Satır (Y) dizinini bul
            int r = 0;
            for (int i = 0; i < rows - 1; i++)
            {
                if (clampedLoad >= yAxis[i] && clampedLoad <= yAxis[i + 1])
                {
                    r = i;
                    break;
                }
            }
            if (clampedLoad >= yAxis[rows - 1]) r = rows - 2;

            double x0 = xAxis[c];
            double x1 = xAxis[c + 1];
            double y0 = yAxis[r];
            double y1 = yAxis[r + 1];

            // Oran hesaplamaları
            double xRatio = (clampedRpm - x0) / (x1 - x0);
            double yRatio = (clampedLoad - y0) / (y1 - y0);

            // Ağırlık hesaplamaları (TL = Top-Left, TR = Top-Right, BL = Bottom-Left, BR = Bottom-Right)
            double wTL = (1 - xRatio) * (1 - yRatio);
            double wTR = xRatio * (1 - yRatio);
            double wBL = (1 - xRatio) * yRatio;
            double wBR = xRatio * yRatio;

            // Hücre değerlerini al
            double vTL = table.ConvertedCells[r, c];
            double vTR = table.ConvertedCells[r, c + 1];
            double vBL = table.ConvertedCells[r + 1, c];
            double vBR = table.ConvertedCells[r + 1, c + 1];

            // Enterpole edilen sonuç değer
            double calculatedValue = (wTL * vTL) + (wTR * vTR) + (wBL * vBL) + (wBR * vBR);

            // Ağırlığı en yüksek olan hücreyi "Aktif Hücre" olarak tanımla
            int activeR = r;
            int activeC = c;
            double maxWeight = wTL;

            if (wTR > maxWeight)
            {
                activeR = r;
                activeC = c + 1;
                maxWeight = wTR;
            }
            if (wBL > maxWeight)
            {
                activeR = r + 1;
                activeC = c;
                maxWeight = wBL;
            }
            if (wBR > maxWeight)
            {
                activeR = r + 1;
                activeC = c + 1;
                maxWeight = wBR;
            }

            var neighborCells = new int[][]
            {
                new int[] { r, c },         // Top-Left
                new int[] { r, c + 1 },     // Top-Right
                new int[] { r + 1, c },     // Bottom-Left
                new int[] { r + 1, c + 1 }  // Bottom-Right
            };

            var cellWeights = new double[] { wTL, wTR, wBL, wBR };

            return new InterpolationResult
            {
                ActiveRow = activeR,
                ActiveCol = activeC,
                XRatio = xRatio,
                YRatio = yRatio,
                NeighborCells = neighborCells,
                CellWeights = cellWeights,
                CalculatedValue = calculatedValue,
                Confidence = 1.0 // Bilinear tam eşleşme çözünürlüğü %100 kabul edilebilir.
            };
        }
    }
}
