using System;

namespace HondaTuner.Calibration.Ignition
{
    public class MbtOptimizer
    {
        // MBT Ateşleme Tahmin Algoritması
        public double EstimateMbt(double rpm, double loadKpa, double octaneRating)
        {
            // Temel fiziksel ateşleme modeli:
            // Düşük devirde ve yüksek yükte ateşleme geride olur (knock ve hızlı yanma).
            // Yüksek devirde ve düşük yükte avans artmalıdır.

            // RPM katkısı: Devir arttıkça avans doğrusal artar
            double rpmTerm = (rpm - 1000.0) * 0.0035; // 8000 RPM için: (7000 * 0.0035) = 24.5 derece avans artışı

            // Load (kPa) katkısı: Basınç arttıkça avans düşer
            double loadTerm = (100.0 - loadKpa) * 0.15; // 30 kPa vakum için: +10.5 derece, 200 kPa boost için: -15.0 derece

            // Temel referans avans (15 derece @ 1000rpm, 100kPa)
            double baseMbt = 15.0;

            // Oktan bonusu: 95 oktan üstü her puan için küçük bir avans toleransı (+0.25 derece/octane)
            double octaneBonus = Math.Max(0.0, octaneRating - 95.0) * 0.25;

            double targetMbt = baseMbt + rpmTerm + loadTerm + octaneBonus;

            // Limit avans sınırları (güvenli sınırlar: 5 derece ile 45 derece arası)
            return Math.Max(5.0, Math.Min(45.0, targetMbt));
        }

        // Öneri ve Mevcut Harita Fark Analizi
        public double CalculateDeviation(double currentAdvance, double mbtAdvance)
        {
            return currentAdvance - mbtAdvance;
        }

        public string GetKnockProximityStatus(double currentAdvance, double mbtAdvance, double octane)
        {
            double diff = currentAdvance - mbtAdvance;
            if (diff > 2.0)
            {
                return "⚠️ RISK: Mevcut Avans MBT Üzerinde! Vuruntu (Knock) Tehlikesi Var.";
            }
            if (diff < -5.0)
            {
                return "ℹ️ OPTİMİZASYON: Avans MBT'nin Çok Gerisinde. Güç Kazanmak İçin Avansı Artırın.";
            }
            return "✅ GÜVENLİ: Ateşleme Zamanlaması MBT Noktasına Çok Yakın.";
        }
    }
}
