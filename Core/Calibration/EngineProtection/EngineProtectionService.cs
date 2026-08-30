using System;

namespace HondaTuner.Calibration.EngineProtection
{
    public class EngineProtectionService
    {
        public EngineProtectionTables Tables { get; } = new EngineProtectionTables();

        // Aktif Koruma Durumları
        public bool IsFuelCutActive { get; private set; }
        public bool IsPowerReductionActive { get; private set; }
        public bool IsThermalLimpModeActive { get; private set; }
        public bool FanRelayState { get; private set; }

        // ── YENİ: Ek Koruma Bayrakları ───────────────────────────
        public bool IsLeanCutTriggered { get; private set; }
        public bool IsOverboostCutTriggered { get; private set; }

        public double ActiveRpmLimit { get; private set; } = 8500.0;
        public double ActiveTimingPull { get; private set; } = 0.0;
        public double ActiveBoostLimitOffset { get; private set; } = 0.0;
        public double ActiveFuelEnrichmentPct { get; private set; } = 0.0;

        // Sensör Sağlık ve Tanılama Sayaçları
        public double LowOilPressTimer { get; private set; } = 0.0;
        public double LowFuelPressTimer { get; private set; } = 0.0;

        // ── YENİ: Knock Kademeli Toparlama ───────────────────────
        private double _knockRecoveryTimer = 0.0;
        private bool _knockActive = false;

        public event EventHandler<string> ProtectionAlarmTriggered;

        public void ResetSafeties()
        {
            IsFuelCutActive = false;
            IsPowerReductionActive = false;
            IsThermalLimpModeActive = false;
            FanRelayState = false;
            IsLeanCutTriggered = false;
            IsOverboostCutTriggered = false;
            ActiveRpmLimit = 8500.0;
            ActiveTimingPull = 0.0;
            ActiveBoostLimitOffset = 0.0;
            ActiveFuelEnrichmentPct = 0.0;
            LowOilPressTimer = 0.0;
            LowFuelPressTimer = 0.0;
            _knockRecoveryTimer = 0.0;
            _knockActive = false;
        }

        // Ana Limit ve Güvenlik Motoru
        // afr    : Anlık Hava/Yakıt Oranı (Wideband)
        // knock  : Vuruntu algılama sinyali (true = aktif)
        public void EvaluateSafety(
            double rpm,
            double ect,
            double iat,
            double oilTemp,
            double oilPress,
            double fuelPress,
            double actualBoost,
            double egt,
            double dt,
            double afr = 14.7,
            bool knock = false)
        {
            if (dt <= 0.0001) dt = 0.1;

            // Varsayılan çalışma durumlarını yükle
            ActiveRpmLimit = 8500.0;
            ActiveTimingPull = 0.0;
            ActiveBoostLimitOffset = 0.0;
            ActiveFuelEnrichmentPct = 0.0;
            IsFuelCutActive = false;
            IsPowerReductionActive = false;
            IsThermalLimpModeActive = false;
            IsLeanCutTriggered = false;
            IsOverboostCutTriggered = false;

            // 1. Yağ Sıcaklığı Kontrolü (Thermal Limp Mode)
            if (oilTemp >= Tables.MaxOilTemp)
            {
                IsThermalLimpModeActive = true;
                ActiveRpmLimit = Tables.ThermalLimpModeRpm;
                ProtectionAlarmTriggered?.Invoke(this, $"🚨 YÜKSEK YAĞ SICAKLIĞI ({oilTemp}°C): Motor koruma modu devrede, RPM limiti {Tables.ThermalLimpModeRpm} RPM.");
            }

            // 2. Yağ Basıncı vs RPM Kontrolü (Fuel Cut)
            double requiredPress = InterpolateMinOilPressure(rpm);
            if (rpm > 800.0 && oilPress < requiredPress)
            {
                LowOilPressTimer += dt;
                if (LowOilPressTimer >= 1.0)
                {
                    IsFuelCutActive = true;
                    ActiveRpmLimit = 0.0; // Devir tamamen kesilir
                    ProtectionAlarmTriggered?.Invoke(this, $"🚨 KRİTİK DÜŞÜK YAĞ BASINCI ({oilPress} Bar): Motor hasar mekanizması nedeniyle YAKIT KESİLDİ!");
                }
            }
            else
            {
                LowOilPressTimer = 0.0;
            }

            // 3. Yakıt Basıncı Kaybı Kontrolü (Power Reduction)
            if (actualBoost >= 120.0 && fuelPress < Tables.MinFuelPressure)
            {
                LowFuelPressTimer += dt;
                if (LowFuelPressTimer >= 0.5)
                {
                    IsPowerReductionActive = true;
                    ActiveTimingPull += 5.0; // Avansı 5 derece geriye al
                    ActiveBoostLimitOffset += 30.0; // Güvenli boost payı limitini azalt
                    ProtectionAlarmTriggered?.Invoke(this, $"🚨 DÜŞÜK YAKIT BASINCI ({fuelPress} Bar): Yağlama basıncı yetersiz, avans geriye çekildi, limitler düşürüldü.");
                }
            }
            else
            {
                LowFuelPressTimer = 0.0;
            }

            // 4. Radyatör Fanı Histerezis Kontrolü
            if (ect >= Tables.FanTargetTemp)
            {
                FanRelayState = true;
            }
            else if (ect < Tables.FanTargetTemp - 3.0)
            {
                // Fan 3 derece soğuyunca kapanır
                FanRelayState = false;
            }

            // 5. IAT Heat Soak Düzeltme Kontrolü
            if (iat >= Tables.IatHeatSoakRetardThreshold)
            {
                ActiveTimingPull += Tables.IatHeatSoakRetard;
                ActiveBoostLimitOffset += Tables.IatBoostLimitReduction;
                ProtectionAlarmTriggered?.Invoke(this, $"⚠️ EMME HAVA ENJEKTÖRÜ SICAK (HEAT SOAK): IAT {iat}°C. Koruma amaçlı avans kısılıyor (-{Tables.IatHeatSoakRetard}°), boost payı kısıtlanıyor.");
            }

            // 6. EGT Emniyet Zenginleştirmesi
            if (egt >= Tables.MaxEgtLimit)
            {
                ActiveTimingPull += Tables.EgtTimingPull;
                ActiveFuelEnrichmentPct += Tables.EgtFuelEnrichment;
                ProtectionAlarmTriggered?.Invoke(this, $"❗ CRITICAL EGT LIMIT ({egt}°C): Avans kısılıyor (-{Tables.EgtTimingPull}°), enjeksiyon %{Tables.EgtFuelEnrichment} zenginleştirilerek yanma ısısı düşürülüyor.");
            }

            // 7. [YENİ] Lean Cut — Yüksek yük/devir koşulunda fakir karışım koruması
            // RPM > 4000 ve MAP > 120 kPa iken AFR > 12.8 ise yanma hasarı riski yüksek
            if (rpm > Tables.LeanCutRpmThreshold && actualBoost > Tables.LeanCutMapThreshold && afr > Tables.LeanCutAfrThreshold)
            {
                IsLeanCutTriggered = true;
                ActiveFuelEnrichmentPct += 10.0; // Acil zenginleştirme
                ProtectionAlarmTriggered?.Invoke(this, $"🚨 LEAN CUT: RPM={rpm:0} — MAP={actualBoost:0} kPa — AFR={afr:0.00} (eşik: >{Tables.LeanCutAfrThreshold}). Fakir yanma koruması aktif!");
            }

            // 8. [YENİ] Overboost Cut — Boost aşımı koruması
            double boostSafeLimit = Tables.TargetBoostKpa + Tables.OverboostMarginKpa;
            if (actualBoost > boostSafeLimit)
            {
                IsOverboostCutTriggered = true;
                ActiveBoostLimitOffset += actualBoost - boostSafeLimit; // Aşım kadar kısıt ekle
                ProtectionAlarmTriggered?.Invoke(this, $"🚨 OVERBOOST: MAP={actualBoost:0} kPa > Limit={boostSafeLimit:0} kPa. Boost kesme koruması devrede!");
            }

            // 9. [YENİ] ECT Timing Retard — Soğutma sıcaklığı kritik eşiği aştığında avans dinamik geri çekmesi
            if (ect > Tables.EctCriticalRetardThreshold)
            {
                // Lineer interpolasyon: 102°C → -2.0°, 110°C → -4.0°
                double overHeat = Math.Min(ect - Tables.EctCriticalRetardThreshold, 8.0);
                double ectPull = Tables.EctTimingRetardMin + (overHeat / 8.0) * (Tables.EctTimingRetardMax - Tables.EctTimingRetardMin);
                ActiveTimingPull += ectPull;
                ProtectionAlarmTriggered?.Invoke(this, $"⚠️ ECT AŞIRI SICAKLIK ({ect}°C): Avans -{ectPull:0.0}° kısıldı (dinamik retard).");
            }

            // 10. [YENİ] Knock Timing Retard — Vuruntu algılandığında anlık avans geri çekme ve kademeli toparlama
            if (knock)
            {
                _knockActive = true;
                _knockRecoveryTimer = 0.0; // Vuruntu sürerse toparlamayı sıfırla
                ActiveTimingPull += Tables.KnockTimingRetard; // Anlık -3.0°
                ProtectionAlarmTriggered?.Invoke(this, $"🔔 KNOCK ALGILANDI: Avans -{Tables.KnockTimingRetard:0.0}° geri çekildi.");
            }
            else if (_knockActive)
            {
                // Kademeli toparlama: Her dt adımında 0.5° geri kazan, limiti 3.0°
                _knockRecoveryTimer += dt;
                double recovered = Math.Min(_knockRecoveryTimer * Tables.KnockRecoveryRate, Tables.KnockTimingRetard);
                double remainingPull = Tables.KnockTimingRetard - recovered;
                if (remainingPull <= 0.0)
                {
                    _knockActive = false;
                    _knockRecoveryTimer = 0.0;
                }
                else
                {
                    ActiveTimingPull += remainingPull; // Henüz tam toparlanmadı
                }
            }
        }

        private double InterpolateMinOilPressure(double rpm)
        {
            var rpms = Tables.OilPressRpmBins;
            var press = Tables.MinOilPressureCurve;
            int len = rpms.Length;

            if (rpm <= rpms[0]) return press[0];
            if (rpm >= rpms[len - 1]) return press[len - 1];

            for (int i = 0; i < len - 1; i++)
            {
                if (rpm >= rpms[i] && rpm <= rpms[i + 1])
                {
                    double pct = (rpm - rpms[i]) / (rpms[i + 1] - rpms[i]);
                    return press[i] + pct * (press[i + 1] - press[i]);
                }
            }
            return press[0];
        }
    }
}
