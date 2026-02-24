using System;

namespace calculator;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.Write("Введите выражение: ");
            string input = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Ошибка: выражение не введено");
                return;
            }

            // Используем публичный API калькулятора
            string rpnExpr = "";
            decimal result = Calculator.EvaluateWithRPN(input, out rpnExpr);
            
            // Console.WriteLine($"Промежуточный результат: {rpnExpr}");
            Console.WriteLine($"Финальный результат: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки: {ex.Message}");
        }
    }
}
