using System;
using System.Collections.Generic;
using System.Linq;

namespace HondaTuner.Core.ReverseEngineering
{
    public class AxisMatchResult
    {
        public bool Success { get; set; }
        public int RpmAxisOffset { get; set; }
        public int LoadAxisOffset { get; set; }
        public int[] RpmAxisValues { get; set; }
        public int[] LoadAxisValues { get; set; }
        public double Confidence { get; set; }
    }

    public static class AxisExtractor
    {
        public static AxisMatchResult ExtractAxes(byte[] rom, MapCandidate map)
        {
            var result = new AxisMatchResult
            {
                Success = false,
                Confidence = 0.0,
                RpmAxisValues = new int[map.Cols],
                LoadAxisValues = new int[map.Rows]
            };

            if (rom == null || rom.Length < 0x2000) return result;

            // Haritanın -128 byte öncesinden başlayıp harita bitiminden +128 sonrasına kadar tara
            int searchStart = Math.Max(0x1000, map.Offset - 128);
            int searchEnd = Math.Min(rom.Length - Math.Max(map.Rows, map.Cols), map.Offset + (map.Rows * map.Cols) + 128);

            int bestRpmOffset = -1;
            double bestRpmConf = 0.0;
            int[] bestRpmValues = null;

            int bestLoadOffset = -1;
            double bestLoadConf = 0.0;
            int[] bestLoadValues = null;

            for (int offset = searchStart; offset < searchEnd; offset++)
            {
                // RPM Eksen Tara (uzunluk: map.Cols)
                if (offset + map.Cols <= rom.Length && offset != map.Offset)
                {
                    double rpmConf = EvaluateAxisPattern(rom, offset, map.Cols, "RPM");
                    if (rpmConf > bestRpmConf)
                    {
                        bestRpmConf = rpmConf;
                        bestRpmOffset = offset;
                        bestRpmValues = GetAxisValues(rom, offset, map.Cols, "RPM");
                    }
                }

                // Load Eksen Tara (uzunluk: map.Rows)
                if (offset + map.Rows <= rom.Length && offset != map.Offset)
                {
                    double loadConf = EvaluateAxisPattern(rom, offset, map.Rows, "Load");
                    if (loadConf > bestLoadConf)
                    {
                        bestLoadConf = loadConf;
                        bestLoadOffset = offset;
                        bestLoadValues = GetAxisValues(rom, offset, map.Rows, "Load");
                    }
                }
            }

            if (bestRpmConf > 0.60 && bestLoadConf > 0.60)
            {
                result.Success = true;
                result.RpmAxisOffset = bestRpmOffset;
                result.LoadAxisOffset = bestLoadOffset;
                result.RpmAxisValues = bestRpmValues;
                result.LoadAxisValues = bestLoadValues;
                result.Confidence = (bestRpmConf + bestLoadConf) / 2.0;
            }

            return result;
        }

        private static double EvaluateAxisPattern(byte[] rom, int offset, int length, string axisType)
        {
            if (length < 4) return 0.0;

            // Tekrarlı sıfır/FF kontrolü
            int zeros = 0, ffs = 0;
            for (int i = 0; i < length; i++)
            {
                if (rom[offset + i] == 0) zeros++;
                if (rom[offset + i] == 0xFF) ffs++;
            }
            if (zeros > 1 || ffs > 1) return 0.0;

            // Monoton artış kontrolü
            int monotonicUps = 0;
            for (int i = 0; i < length - 1; i++)
            {
                byte current = rom[offset + i];
                byte next = rom[offset + i + 1];

                if (next > current)
                {
                    monotonicUps++;
                }
                else
                {
                    return 0.0; // Eksenler strictly scaling monotonic artış göstermelidir
                }

                // Adım farkı makullüğü
                int diff = next - current;
                if (axisType == "RPM")
                {
                    if (diff < 1 || diff > 25) return 0.0; // RPM artış adımı makul olmalı
                }
                else
                {
                    if (diff < 1 || diff > 40) return 0.0; // Load/Vacuum artış adımı makul olmalı
                }
            }

            // Değerlerin mutlak aralıkları
            byte startVal = rom[offset];
            byte endVal = rom[offset + length - 1];

            if (axisType == "RPM")
            {
                // RPM ölçeğinde limitler: min 500 RPM (raw=10), max 11000 RPM (raw=220)
                if (startVal < 8 || endVal > 220) return 0.0;
            }
            else
            {
                // Load ölçeğinde limitler: vacuum (raw=15) ile full boost (raw=250)
                if (startVal < 10 || endVal > 250) return 0.0;
            }

            double confidence = (double)monotonicUps / (length - 1);
            return confidence;
        }

        private static int[] GetAxisValues(byte[] rom, int offset, int length, string axisType)
        {
            int[] values = new int[length];
            for (int i = 0; i < length; i++)
            {
                byte rawVal = rom[offset + i];
                if (axisType == "RPM")
                {
                    values[i] = rawVal * 50; // Raw to RPM
                }
                else
                {
                    values[i] = rawVal; // Raw Load (kPa)
                }
            }
            return values;
        }
    }
}
