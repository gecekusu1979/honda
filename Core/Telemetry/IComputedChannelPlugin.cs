using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HondaTuner.Core.Telemetry
{
    /// <summary>
    /// Telemetri akışından gelen canlı verilere özel formüller uygulayarak
    /// ek sanal kanallar (Calculated Load, AFR Error vb.) hesaplayan arayüzdür.
    /// </summary>
    public interface IComputedChannelPlugin
    {
        string OutputChannelId { get; }
        double Calculate(TelemetrySnapshot snapshot, Func<string, double> valueProvider);
    }

    /// <summary>
    /// JSON içinde tanımlanan formül ifadelerini (örn: "[AFR] - 14.7") çalışma zamanında 
    /// ayrıştıran ve hesaplayan hafif bir matematiksel ifade motorudur.
    /// </summary>
    public static class TelemetryFormulaEvaluator
    {
        private static readonly Regex VariableRegex = new Regex(@"\[([a-zA-Z0-9_]+)\]", RegexOptions.Compiled);

        /// <summary>
        /// İfadeyi ayrıştırır ve değişkenleri güncel değerleriyle değiştirerek çözer.
        /// Desteklenen işlemler: +, -, *, /
        /// </summary>
        public static double Evaluate(string formula, Func<string, double> getVal)
        {
            if (string.IsNullOrWhiteSpace(formula)) return 0.0;

            try
            {
                // Değişkenleri eşleştir ve ata, örn: "[AFR]" -> 14.7
                string evaluatedExpression = VariableRegex.Replace(formula, match =>
                {
                    string varName = match.Groups[1].Value;
                    double varVal = getVal(varName);
                    return varVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                });

                return SimpleEvaluate(evaluatedExpression);
            }
            catch
            {
                return 0.0; // Hata durumunda güvenli sıfır dönüşü
            }
        }

        private static double SimpleEvaluate(string expression)
        {
            // Basit dört işlem çözücü
            // Emniyetli olması için sadece sayısal karakterler ve işlemleri barındırmalı
            expression = expression.Replace(" ", "");

            // Sırayla +, -, *, / işlemleri için basit ayrışım yapalım
            if (expression.Contains("+"))
            {
                var parts = expression.Split('+', 2);
                return SimpleEvaluate(parts[0]) + SimpleEvaluate(parts[1]);
            }
            if (expression.Contains("-"))
            {
                // Eksi işareti negatif sayı da olabileceği için dikkat edilmeli
                int index = expression.LastIndexOf('-');
                if (index > 0 && !IsOperator(expression[index - 1]))
                {
                    return SimpleEvaluate(expression.Substring(0, index)) - SimpleEvaluate(expression.Substring(index + 1));
                }
            }
            if (expression.Contains("*"))
            {
                var parts = expression.Split('*', 2);
                return SimpleEvaluate(parts[0]) * SimpleEvaluate(parts[1]);
            }
            if (expression.Contains("/"))
            {
                var parts = expression.Split('/', 2);
                double divisor = SimpleEvaluate(parts[1]);
                if (divisor == 0.0) return 0.0; // Sıfıra bölme hatasından kaçın
                return SimpleEvaluate(parts[0]) / divisor;
            }

            if (double.TryParse(expression, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            return 0.0;
        }

        private static bool IsOperator(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/';
        }
    }

    /// <summary>
    /// Genel formül tabanlı hesaplanan kanal eklentisi.
    /// </summary>
    public class FormulaComputedPlugin : IComputedChannelPlugin
    {
        public string OutputChannelId { get; }
        private readonly string _formula;

        public FormulaComputedPlugin(string outputChannelId, string formula)
        {
            OutputChannelId = outputChannelId;
            _formula = formula;
        }

        public double Calculate(TelemetrySnapshot snapshot, Func<string, double> valueProvider)
        {
            return TelemetryFormulaEvaluator.Evaluate(_formula, valueProvider);
        }
    }
}
