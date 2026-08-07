using System;
using System.Collections.Generic;
using HondaTuner.Core.AutoTune.Safety;
using HondaTuner.Core.Logging;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune
{
    public class AutoTuneSafetyManager : IAutoTuneSafetyManager
    {
        private readonly ISafetyRuleProvider _ruleProvider;
        private List<ISafetyRule> _rules = new List<ISafetyRule>();
        private readonly object _lockObj = new object();

        public AutoTuneSafetyManager(ISafetyRuleProvider ruleProvider)
        {
            _ruleProvider = ruleProvider ?? throw new ArgumentNullException(nameof(ruleProvider));
        }

        public IReadOnlyList<ISafetyRule> ActiveRules
        {
            get
            {
                lock (_lockObj)
                {
                    return _rules.AsReadOnly();
                }
            }
        }

        public void ReloadSafetyLimits(string safetyLimitsFilePath)
        {
            lock (_lockObj)
            {
                _rules = _ruleProvider.LoadRules(safetyLimitsFilePath);
                ApplicationLogger.Info("AutoTuneSafetyManager", $"Güvenlik limitleri yüklendi. Etkin kural sayısı: {_rules.Count}");
            }
        }

        public SafetyResult EvaluateSafety(TelemetrySnapshot telemetry, TuneDecision decision)
        {
            if (telemetry == null)
            {
                return new SafetyResult
                {
                    Status = "Reject",
                    RuleName = "TelemetryNullValidation",
                    Severity = "Critical",
                    Reason = "Telemetri verisi boş/null."
                };
            }

            List<ISafetyRule> rulesCopy;
            lock (_lockObj)
            {
                rulesCopy = new List<ISafetyRule>(_rules);
            }

            foreach (var rule in rulesCopy)
            {
                var result = rule.Evaluate(telemetry, decision);
                if (result.Status == "Reject")
                {
                    return result; // Immediately return if any safety rule rejects the change
                }
            }

            return new SafetyResult
            {
                Status = "Allow",
                RuleName = "AllPass",
                Severity = "Info",
                Reason = "Tüm güvenlik denetimleri başarıyla tamamlandı."
            };
        }
    }
}
