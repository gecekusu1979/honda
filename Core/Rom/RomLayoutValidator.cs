using System;
using System.Collections.Generic;
using System.Linq;
using HondaTuner.Calibration.Maps;

namespace HondaTuner.Core.Rom
{
    public class LayoutValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = null!;
    }

    public class MemoryRegion
    {
        public string Name { get; set; } = null!;
        public int StartOffset { get; set; }
        public int Length { get; set; }
        public int EndOffset => StartOffset + Length;
    }

    public class RomLayoutValidator
    {
        /// <summary>
        /// ROM bufferının boyutu, EcuProfile ve hedeflenen patch/map region'ının limitleri ile çakışmaları analiz eder.
        /// </summary>
        public LayoutValidationResult Validate(byte[] romBuffer, EcuProfile profile, List<MapDefinition> mapDefinitions, string targetRegionName, int targetOffset, int targetLength)
        {
            if (romBuffer == null || romBuffer.Length == 0)
                return new LayoutValidationResult { IsValid = false, ErrorMessage = "ROM buffer boş." };

            if (profile == null)
                return new LayoutValidationResult { IsValid = false, ErrorMessage = "EcuProfile tanımsız (null). Başka bir offset denenmeyecek (BLOCKED)." };

            // 1. BOUNDS CHECKING
            if (targetOffset < 0)
                return new LayoutValidationResult { IsValid = false, ErrorMessage = $"Negatif offset tespit edildi: {targetOffset} (BLOCKED)" };
            if (targetLength <= 0)
                return new LayoutValidationResult { IsValid = false, ErrorMessage = $"Geçersiz geometri boyutu: {targetLength} (BLOCKED)" };

            // Integer Overflow control
            try
            {
                int endBoundary = checked(targetOffset + targetLength);
                if (endBoundary > romBuffer.Length)
                    return new LayoutValidationResult { IsValid = false, ErrorMessage = "Offset + Length, ROM sınırlarını aşıyor (BLOCKED)." };
            }
            catch (OverflowException)
            {
                return new LayoutValidationResult { IsValid = false, ErrorMessage = "Offset ve Length toplamı matematiksel taşmaya sebep oluyor (Integer Overflow BLOCKED)." };
            }

            // 2. GEOMETRY CHECK FOR MAPS
            // Eğer hedef bir map ise, MapDefinition'daki row*col hesaplaması boyutla aynı olmalı.
            if (mapDefinitions != null)
            {
                var targetMap = mapDefinitions.FirstOrDefault(m => string.Equals(m.MapName, targetRegionName, StringComparison.OrdinalIgnoreCase));
                if (targetMap != null)
                {
                    int expectedSize = targetMap.Rows * targetMap.Columns; // Eğer 16-bit elemanlar varsa değişebilir, şimdilik Rows*Cols
                    if (expectedSize != targetLength)
                    {
                        return new LayoutValidationResult { IsValid = false, ErrorMessage = $"Map Geometry Mismatch: {targetRegionName} için Rows*Cols ({expectedSize}) ile hedeflenen limit ({targetLength}) uyuşmuyor (BLOCKED)." };
                    }
                }
            }

            // 3. REGION OVERLAP CHECK
            var regions = ExtractAllRegions(profile!, mapDefinitions!)!;

            // Kendi hedef alanımızı geçici olarak registered kabul edelim:
            var currentTargetRegion = new MemoryRegion { Name = targetRegionName, StartOffset = targetOffset, Length = targetLength };

            // Eğer targetRegion zaten registered ise, overlap checkte kendisiyle çakışmasını engellemek lazım, 
            // ama patch/write senaryosunda bu region maplerden biri de olabilir, dış bir şey de. 
            // Pairwise overlap:
            regions.Add(currentTargetRegion);

            for (int i = 0; i < regions.Count; i++)
            {
                for (int j = i + 1; j < regions.Count; j++)
                {
                    var r1 = regions[i];
                    var r2 = regions[j];

                    // Ignore self overlap if they have the exact same name (e.g. we added the target region that was already in maps)
                    if (r1.Name == r2.Name) continue;

                    if (IsOverlapping(r1, r2))
                    {
                        return new LayoutValidationResult { IsValid = false, ErrorMessage = $"Overlap Detected: {r1.Name} [{r1.StartOffset:X4}-{r1.EndOffset:X4}) ile {r2.Name} [{r2.StartOffset:X4}-{r2.EndOffset:X4}) çakışıyor (BLOCKED)." };
                    }
                }
            }

            return new LayoutValidationResult { IsValid = true };
        }

        private bool IsOverlapping(MemoryRegion r1, MemoryRegion r2)
        {
            // r1.StartOffset < r2.EndOffset AND r2.StartOffset < r1.EndOffset
            return r1.StartOffset < r2.EndOffset && r2.StartOffset < r1.EndOffset;
        }

        private List<MemoryRegion> ExtractAllRegions(EcuProfile profile, List<MapDefinition> mapDefinitions)
        {
            var list = new List<MemoryRegion>();

            // Profile regionları eklenecek
            // InjectorDeadTime 
            if (profile.InjectorOffset > 0)
                list.Add(new MemoryRegion { Name = "InjectorDeadTime", StartOffset = profile.InjectorOffset, Length = 1 });

            // Idle, RevLimit vs
            if (profile.IdleOffset > 0)
                list.Add(new MemoryRegion { Name = "IdleTarget", StartOffset = profile.IdleOffset, Length = 1 });
            if (profile.RevLimitOffset > 0)
                list.Add(new MemoryRegion { Name = "RevLimit", StartOffset = profile.RevLimitOffset, Length = 2 });
            if (profile.VtecRpmOffset > 0)
                list.Add(new MemoryRegion { Name = "VtecRpm", StartOffset = profile.VtecRpmOffset, Length = 1 });

            if (mapDefinitions != null)
            {
                foreach (var map in mapDefinitions)
                {
                    // Map offsetleri
                    if (map.Offset > 0 && map.Rows > 0 && map.Columns > 0)
                    {
                        list.Add(new MemoryRegion { Name = map.MapName, StartOffset = map.Offset, Length = map.Rows * map.Columns });
                    }
                }
            }

            return list;
        }
    }
}