using System;
using System.Collections.Generic;
using System.Linq;

namespace HondaTuner.Core.ReverseEngineering
{
    public class MapCandidate
    {
        public int Offset { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public double Confidence { get; set; }
        public string MapType { get; set; } // "Fuel" veya "Ignition"
        public string Description { get; set; }
    }

    public static class MapSearchHelper
    {
        private static readonly (int rows, int cols)[] TypicalDimensions = new[]
        {
            (20, 20),
            (10, 10),
            (16, 20),
            (12, 10)
        };

        public static List<MapCandidate> Search(byte[] rom)
        {
            var candidates = new List<MapCandidate>();
            if (rom == null || rom.Length < 0x2000) return candidates;

            // ROM içinde harita olabilecek makul veri alanlarını tara (0x1000 - 0x7E00)
            int startOffset = 0x1000;
            int endOffset = rom.Length - 100;

            for (int offset = startOffset; offset < endOffset; offset += 4)
            {
                foreach (var dim in TypicalDimensions)
                {
                    int mapSize = dim.rows * dim.cols;
                    if (offset + mapSize > rom.Length) continue;

                    // Hücre veri analizlerini yap
                    double fuelScore = EvaluatePattern(rom, offset, dim.rows, dim.cols, "Fuel");
                    double ignScore = EvaluatePattern(rom, offset, dim.rows, dim.cols, "Ignition");

                    if (fuelScore > 0.70)
                    {
                        candidates.Add(new MapCandidate
                        {
                            Offset = offset,
                            Rows = dim.rows,
                            Cols = dim.cols,
                            Confidence = fuelScore,
                            MapType = "Fuel",
                            Description = $"Potansiyel Fuel Haritası ({dim.rows}x{dim.cols})"
                        });
                    }

                    if (ignScore > 0.70)
                    {
                        candidates.Add(new MapCandidate
                        {
                            Offset = offset,
                            Rows = dim.rows,
                            Cols = dim.cols,
                            Confidence = ignScore,
                            MapType = "Ignition",
                            Description = $"Potansiyel Ateşleme Haritası ({dim.rows}x{dim.cols})"
                        });
                    }
                }
            }

            // Çakışan adayları ele (Non-Overlapping local maxima filtering)
            return FilterOverlapping(candidates).OrderByDescending(c => c.Confidence).ToList();
        }

        private static double EvaluatePattern(byte[] rom, int offset, int rows, int cols, string type)
        {
            // İki boyutlu matrisi simüle et
            byte[,] map = new byte[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    map[r, c] = rom[offset + (r * cols) + c];
                }
            }

            int totalChecks = 0;
            int smoothChecks = 0;
            int trendMatches = 0;

            // Gradyan analizi
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    byte val = map[r, c];

                    // Yatay yakınlık
                    if (c < cols - 1)
                    {
                        totalChecks++;
                        byte right = map[r, c + 1];
                        int diff = Math.Abs(val - right);
                        if (diff < 15) smoothChecks++; // Hücreler arası ani sıçrama olmamalı

                        if (type == "Fuel")
                        {
                            // Fuel: RPM arttıkça genelde yakıt artar veya sabit kalır
                            if (right >= val - 5) trendMatches++;
                        }
                        else
                        {
                            // Ignition: RPM arttıkça genelde avans artar
                            if (right >= val - 3) trendMatches++;
                        }
                    }

                    // Dikey yakınlık
                    if (r < rows - 1)
                    {
                        totalChecks++;
                        byte down = map[r + 1, c];
                        int diff = Math.Abs(val - down);
                        if (diff < 15) smoothChecks++;

                        if (type == "Fuel")
                        {
                            // Fuel: Yük (down) arttıkça yakıt artar (enjeksiyon süresi uzar)
                            if (down >= val - 4) trendMatches++;
                        }
                        else
                        {
                            // Ignition: Yük (down - vakumdan basınca doğru) arttıkça avans düşer
                            if (down <= val + 4) trendMatches++;
                        }
                    }
                }
            }

            if (totalChecks == 0) return 0.0;

            double smoothness = (double)smoothChecks / totalChecks;
            double monotonicity = (double)trendMatches / totalChecks;

            // Her haritada boş veri (sıfırlar veya FF'ler) miktarını sınırla
            int emptyBytes = 0;
            for (int i = 0; i < rows * cols; i++)
            {
                byte b = rom[offset + i];
                if (b == 0x00 || b == 0xFF) emptyBytes++;
            }
            double emptyRatio = (double)emptyBytes / (rows * cols);
            if (emptyRatio > 0.25) return 0.0; // Kalibrasyon haritalarından boş alan fazlaysa reddet

            // Birleştirilmiş skor
            return (smoothness * 0.4) + (monotonicity * 0.6);
        }

        private static List<MapCandidate> FilterOverlapping(List<MapCandidate> candidates)
        {
            var result = new List<MapCandidate>();
            var sorted = candidates.OrderByDescending(c => c.Confidence).ToList();

            foreach (var candidate in sorted)
            {
                int candStart = candidate.Offset;
                int candEnd = candidate.Offset + (candidate.Rows * candidate.Cols);

                bool overlaps = false;
                foreach (var existing in result)
                {
                    int extStart = existing.Offset;
                    int extEnd = existing.Offset + (existing.Rows * existing.Cols);

                    // Eksen aralık çakışması kontrolü
                    if (Math.Max(candStart, extStart) < Math.Min(candEnd, extEnd))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    result.Add(candidate);
                }
            }

            return result;
        }
    }
}
