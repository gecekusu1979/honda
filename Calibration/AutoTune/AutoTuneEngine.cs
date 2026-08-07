using System;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Calibration.AutoTune
{
    /// <summary>
    /// AutoTune Motoru — Wideband AFR hedef/ölçülen farkına göre
    /// hücre bazlı yakıt düzeltme önerisi hesaplar.
    /// 
    /// ÖNEMLİ: Bu motor otomatik ROM yazma yapmaz.
    /// Tüm düzeltmeler kullanıcı onayı gerektirir.
    /// </summary>
    public class AutoTuneEngine : IAutoTuneEngine
    {
        private readonly AutoTuneValidator _validator;
        private readonly int[] _rpmAxis;
        private readonly int[] _loadAxis;

        public bool IsEnabled { get; set; } = false;

        public AutoTuneEngine(int[] rpmAxis, int[] loadAxis)
        {
            _validator = new AutoTuneValidator();
            _rpmAxis = rpmAxis ?? throw new ArgumentNullException(nameof(rpmAxis));
            _loadAxis = loadAxis ?? throw new ArgumentNullException(nameof(loadAxis));
        }

        /// <summary>
        /// Tek bir telemetri çerçevesini işler ve düzeltme önerisi döner.
        /// Validator onayı olmadan hiçbir düzeltme kabul edilmez.
        /// </summary>
        public CorrectionSuggestion ProcessFrame(TelemetryFrameData frame, double targetAfr)
        {
            if (!IsEnabled)
                return InvalidSuggestion("AutoTune devre dışı.");

            if (frame == null)
                return InvalidSuggestion("Telemetri verisi boş.");

            // Güvenlik doğrulaması
            var validation = _validator.Validate(frame);
            if (!validation.IsValid)
            {
                return new CorrectionSuggestion
                {
                    IsValid = false,
                    RejectionReason = validation.Reason,
                    TargetRow = -1,
                    TargetCol = -1
                };
            }

            // Hedef hücreyi bul
            int row = FindNearestIndex(_rpmAxis, (int)frame.Rpm);
            int col = FindNearestIndex(_loadAxis, (int)frame.Map);

            // AFR düzeltme hesabı
            // Target 13.0, Measured 12.2 → zengin → yakıtı azalt: -6.2%
            // Target 13.0, Measured 14.0 → fakir → yakıtı artır: +7.7%
            double rawCorrection = ((targetAfr / frame.Afr) - 1.0) * 100.0;

            // Güvenlik sınırına kırp
            double clampedCorrection = _validator.ClampCorrection(rawCorrection);

            string direction = clampedCorrection > 0 ? "Richen" : "Lean";

            ApplicationLogger.Debug("AutoTuneEngine",
                $"Düzeltme: RPM={frame.Rpm:F0} MAP={frame.Map:F0} " +
                $"Target={targetAfr:F1} Actual={frame.Afr:F1} → {clampedCorrection:+0.0;-0.0}% ({direction})");

            return new CorrectionSuggestion
            {
                TargetRow = row,
                TargetCol = col,
                PercentAdjustment = Math.Round(clampedCorrection, 1),
                Direction = direction,
                IsValid = true,
                RejectionReason = null
            };
        }

        private static int FindNearestIndex(int[] axis, int target)
        {
            int best = 0;
            int bestDist = int.MaxValue;
            for (int i = 0; i < axis.Length; i++)
            {
                int d = Math.Abs(axis[i] - target);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private static CorrectionSuggestion InvalidSuggestion(string reason)
        {
            return new CorrectionSuggestion
            {
                IsValid = false,
                RejectionReason = reason,
                TargetRow = -1,
                TargetCol = -1
            };
        }
    }
}
