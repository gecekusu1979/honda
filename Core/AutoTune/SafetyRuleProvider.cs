using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using HondaTuner.Core.Telemetry;

namespace HondaTuner.Core.AutoTune.Safety
{
    public class KnockRule : ISafetyRule
    {
        private readonly int _maxKnock;
        public string Name => "KnockRule";

        public KnockRule(int maxKnock)
        {
            _maxKnock = maxKnock;
        }

        public SafetyResult Evaluate(TelemetrySnapshot telemetry, TuneDecision decision)
        {
            bool isViolated = telemetry.KnockCount > _maxKnock;
            return new SafetyResult
            {
                Status = isViolated ? "Reject" : "Allow",
                RuleName = Name,
                CurrentValue = telemetry.KnockCount,
                LimitValue = _maxKnock,
                Severity = isViolated ? "Critical" : "Info",
                Reason = isViolated ? $"Knock sayısı limit dışı: {telemetry.KnockCount} > {_maxKnock}" : "Knock limiti dahilinde."
            };
        }
    }

    public class TemperatureRule : ISafetyRule
    {
        private readonly double _maxEct;
        public string Name => "TemperatureRule";

        public TemperatureRule(double maxEct)
        {
            _maxEct = maxEct;
        }

        public SafetyResult Evaluate(TelemetrySnapshot telemetry, TuneDecision decision)
        {
            bool isViolated = telemetry.ECT > _maxEct;
            return new SafetyResult
            {
                Status = isViolated ? "Reject" : "Allow",
                RuleName = Name,
                CurrentValue = telemetry.ECT,
                LimitValue = _maxEct,
                Severity = isViolated ? "Critical" : "Info",
                Reason = isViolated ? $"ECT Motor sıcaklığı limit dışı: {telemetry.ECT:F1}°C > {_maxEct:F1}°C" : "Sıcaklık limiti dahilinde."
            };
        }
    }

    public class VoltageRule : ISafetyRule
    {
        private readonly double _minVolt;
        public string Name => "VoltageRule";

        public VoltageRule(double minVolt)
        {
            _minVolt = minVolt;
        }

        public SafetyResult Evaluate(TelemetrySnapshot telemetry, TuneDecision decision)
        {
            bool isViolated = telemetry.Battery < _minVolt;
            return new SafetyResult
            {
                Status = isViolated ? "Reject" : "Allow",
                RuleName = Name,
                CurrentValue = telemetry.Battery,
                LimitValue = _minVolt,
                Severity = isViolated ? "Warning" : "Info",
                Reason = isViolated ? $"Batarya voltajı düşük: {telemetry.Battery:F1}V < {_minVolt:F1}V" : "Batarya voltajı uygun."
            };
        }
    }

    public class AfrRule : ISafetyRule
    {
        private readonly double _maxAfrError;
        public string Name => "AfrRule";

        public AfrRule(double maxAfrError)
        {
            _maxAfrError = maxAfrError;
        }

        public SafetyResult Evaluate(TelemetrySnapshot telemetry, TuneDecision decision)
        {
            if (decision == null)
            {
                return new SafetyResult { Status = "Allow", RuleName = Name, Severity = "Info" };
            }

            // If we are modifying fuel, check how much we are changing it.
            // E.g., we check if AFR error (deviation from target or change percent) is too large.
            double err = Math.Abs(decision.ChangePercent);
            bool isViolated = err > _maxAfrError && decision.Parameter == ParameterType.Fuel;

            return new SafetyResult
            {
                Status = isViolated ? "Reject" : "Allow",
                RuleName = Name,
                CurrentValue = err,
                LimitValue = _maxAfrError,
                Severity = isViolated ? "Critical" : "Info",
                Reason = isViolated ? $"AFR düzeltme limiti aşıldı: %{err:F1} > %{_maxAfrError:F1}" : "AFR düzeltmesi limit dahilinde."
            };
        }
    }

    public class SafetyRuleProvider : ISafetyRuleProvider
    {
        public List<ISafetyRule> LoadRules(string safetyLimitsFilePath)
        {
            var rules = new List<ISafetyRule>();

            // Default safe fallbacks
            double maxAfrErr = 4.0;
            double maxFuelCorr = 20.0;
            double maxIgnDelta = 5.0;
            int maxKnock = 3;
            double maxEct = 105.0;
            double minVolt = 11.5;
            int maxLatency = 150;

            try
            {
                if (File.Exists(safetyLimitsFilePath))
                {
                    string content = File.ReadAllText(safetyLimitsFilePath);
                    using (var doc = JsonDocument.Parse(content))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("MaxAFRError", out var afrProp)) maxAfrErr = afrProp.GetDouble();
                        if (root.TryGetProperty("MaxFuelCorrection", out var fuelProp)) maxFuelCorr = fuelProp.GetDouble();
                        if (root.TryGetProperty("MaxIgnitionDelta", out var ignProp)) maxIgnDelta = ignProp.GetDouble();
                        if (root.TryGetProperty("MaxKnockCount", out var knockProp)) maxKnock = knockProp.GetInt32();
                        if (root.TryGetProperty("MaxECT", out var ectProp)) maxEct = ectProp.GetDouble();
                        if (root.TryGetProperty("MinVoltage", out var voltProp)) minVolt = voltProp.GetDouble();
                        if (root.TryGetProperty("MaxLatencyMs", out var latProp)) maxLatency = latProp.GetInt32();
                    }
                }
            }
            catch
            {
                // Fall back silently to default values
            }

            rules.Add(new KnockRule(maxKnock));
            rules.Add(new TemperatureRule(maxEct));
            rules.Add(new VoltageRule(minVolt));
            rules.Add(new AfrRule(maxAfrErr));

            return rules;
        }
    }
}
