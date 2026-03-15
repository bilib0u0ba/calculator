using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace calculator;

class Program
{
    private static readonly HashSet<string> FunctionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "max", "min", "root", "hypot", "sqrt", "sin", "cos", "tan", "log", "ln", "lg"
    };

    private static readonly HashSet<string> ConstantNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "pi", "e"
    };

    private static readonly Regex VariableRegex = new(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);

    static void Main(string[] args)
    {
        while (true)
        {
            try
            {
                Console.Write("Введите выражение (exit для выхода): ");
                string input = Console.ReadLine() ?? "";
                if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Ошибка: выражение не введено");
                    return;
                }

                // Перед вычислением читаем значения всех переменных из выражения.
                var variableValues = ReadVariableValues(input);

                string rpnExpr = "";
                decimal result = Calculator.EvaluateWithRPN(input, out rpnExpr, variableValues);

                // Console.WriteLine($"Промежуточный результат: {rpnExpr}");
                Console.WriteLine($"результат: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки: {ex.Message}");
            }
        }
    }

    private static Dictionary<string, decimal> ReadVariableValues(string expression)
    {
        var variables = ExtractVariables(expression);
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in variables)
        {
            while (true)
            {
                Console.Write($"Введите значение для '{variable}': ");
                string rawValue = Console.ReadLine() ?? string.Empty;

                // Поддержка форматов "3.14" и "3,14".
                string normalized = rawValue.Trim().Replace(',', '.');
                if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedValue))
                {
                    values[variable] = parsedValue;
                    break;
                }

                Console.WriteLine("Ошибка: введите корректное число");
            }
        }

        return values;
    }

    private static List<string> ExtractVariables(string expression)
    {
        var variables = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in VariableRegex.Matches(expression))
        {
            string token = match.Value;
            if (FunctionNames.Contains(token))
            {
                continue;
            }

            if (ConstantNames.Contains(token))
            {
                continue;
            }

            if (seen.Add(token))
            {
                variables.Add(token);
            }
        }

        return variables;
    }
}
