using System;

namespace calculator;

class Program
{
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

                // Используем публичный API калькулятора
                string rpnExpr = "";
                decimal result = Calculator.EvaluateWithRPN(input, out rpnExpr);
                
                // Console.WriteLine($"Промежуточный результат: {rpnExpr}");
                Console.WriteLine($"результат: {result}");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки: {ex.Message}");
            }
            // break;
        }
    }
}
