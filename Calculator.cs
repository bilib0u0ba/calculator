using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace calculator;

/// <summary>
/// Публичный класс калькулятора для использования в других проектах без консольного вывода
/// </summary>
public class Calculator
{
    // Регулярное выражение для разбиения выражения на токены (числа, операторы, функции, переменные)
    private const string TOKEN_PATTERN = @"(max|min|root|hypot|sqrt|sin|cos|tan|log|ln|lg|[A-Za-z_][A-Za-z0-9_]*|\d+\.\d+|\d+|[-+*/(),^!])";
    
    // Набор бинарных операторов: +, -, *, /, ^
    private static readonly HashSet<string> OPERATORS = new() { "+", "-", "*", "/", "^" };
    
    // Постфиксные унарные операторы (применяются после операнда): !
    private static readonly HashSet<string> POSTFIX_OPERATORS = new() { "!" };
    
    // Унарные функции (принимают один аргумент): sqrt, sin, cos, tan, ln, lg
    private static readonly HashSet<string> UNARY_FUNCTIONS = new() { "sqrt", "sin", "cos", "tan", "ln", "lg" };
    
    // Бинарные функции (принимают два аргумента): max, min, root, hypot, log
    private static readonly HashSet<string> BINARY_FUNCTIONS = new() { "max", "min", "root", "hypot", "log" };
    
    // Объединённый набор всех функций (исключая постфиксные операторы которые обрабатываются отдельно)
    private static readonly HashSet<string> ALL_FUNCTIONS = new(UNARY_FUNCTIONS.Concat(BINARY_FUNCTIONS));

    // Встроенные математические константы (доступны без ввода пользователем)
    private static readonly IReadOnlyDictionary<string, decimal> CONSTANTS = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["pi"] = (decimal)Math.PI,
        ["e"] = (decimal)Math.E
    };

    /// <summary>
    /// Вычисляет математическое выражение и возвращает результат
    /// </summary>
    /// <param name="expression">Математическое выражение в виде строки (например, "2+3*4")</param>
    /// <returns>Результат вычисления</returns>
    /// <exception cref="ArgumentException">Выбрасывается при ошибке в выражении</exception>
    public static decimal Evaluate(string expression)
    {
        return Evaluate(expression, null);
    }

    /// <summary>
    /// Вычисляет математическое выражение с переменными и возвращает результат
    /// </summary>
    /// <param name="expression">Математическое выражение в виде строки (например, "a+b*4")</param>
    /// <param name="variables">Словарь значений переменных</param>
    /// <returns>Результат вычисления</returns>
    /// <exception cref="ArgumentException">Выбрасывается при ошибке в выражении</exception>
    public static decimal Evaluate(string expression, IReadOnlyDictionary<string, decimal>? variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Выражение не должно быть пустым", nameof(expression));

        try
        {
            var parser = new ExpressionParser(expression, variables);
            var rpnExpression = parser.Parse();
            var rpnQueue = new Queue<string>(rpnExpression.Split(' '));
            var calculator = new RPNCalculator(rpnQueue);
            return calculator.Calculate();
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Ошибка при обработке выражения: {ex.Message} (Inner: {ex.InnerException?.Message})", nameof(expression), ex);
        }
    }

    /// <summary>
    /// Вычисляет выражение и возвращает результат с дополнительной информацией о RPN представлении
    /// </summary>
    /// <param name="expression">Математическое выражение</param>
    /// <param name="rpnExpression">Выходной параметр с выражением в обратной польской нотации</param>
    /// <returns>Результат вычисления</returns>
    public static decimal EvaluateWithRPN(string expression, out string rpnExpression)
    {
        return EvaluateWithRPN(expression, out rpnExpression, null);
    }

    /// <summary>
    /// Вычисляет выражение с переменными и возвращает результат с дополнительной информацией о RPN представлении
    /// </summary>
    /// <param name="expression">Математическое выражение</param>
    /// <param name="rpnExpression">Выходной параметр с выражением в обратной польской нотации</param>
    /// <param name="variables">Словарь значений переменных</param>
    /// <returns>Результат вычисления</returns>
    public static decimal EvaluateWithRPN(string expression, out string rpnExpression, IReadOnlyDictionary<string, decimal>? variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Выражение не должно быть пустым", nameof(expression));

        try
        {
            var parser = new ExpressionParser(expression, variables);
            rpnExpression = parser.Parse();
            var rpnQueue = new Queue<string>(rpnExpression.Split(' '));
            var calculator = new RPNCalculator(rpnQueue);
            return calculator.Calculate();
        }
        catch (Exception ex)
        {
            rpnExpression = "";
            throw new ArgumentException($"Ошибка при обработке выражения: {ex.Message}", nameof(expression), ex);
        }
    }

    /// Класс для парсинга и обработки математических выражений
    private class ExpressionParser
    {
        private readonly string expression;
        private readonly IReadOnlyDictionary<string, decimal> variableValues;
        private readonly Queue<string> tokenQueue;
        private readonly Stack<string> operatorStack;

        // Конструктор инициализирует парсер с исходным выражением
        public ExpressionParser(string expression, IReadOnlyDictionary<string, decimal>? variables)
        {
            this.expression = expression;
            variableValues = variables is null
                ? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, decimal>(variables, StringComparer.OrdinalIgnoreCase);
            tokenQueue = new Queue<string>();
            operatorStack = new Stack<string>();
        }

        public string Parse()
        {
            Tokenize();
            var rpn = ProcessTokens();
            return rpn;
        }

        private void Tokenize()
        {
            // Находим все совпадения TOKEN_PATTERN в выражении
            MatchCollection matches = Regex.Matches(expression, TOKEN_PATTERN);
            int currentPosition = 0;

            // Добавляем найденные токены в очередь
            foreach (Match match in matches)
            {
                if (match.Index > currentPosition)
                {
                    string skipped = expression.Substring(currentPosition, match.Index - currentPosition);
                    if (!string.IsNullOrWhiteSpace(skipped))
                    {
                        throw new InvalidOperationException($"Недопустимый токен '{skipped}' в позиции {currentPosition + 1}");
                    }
                }

                tokenQueue.Enqueue(match.Value);
                currentPosition = match.Index + match.Length;
            }

            if (currentPosition < expression.Length)
            {
                string tail = expression.Substring(currentPosition);
                if (!string.IsNullOrWhiteSpace(tail))
                {
                    throw new InvalidOperationException($"Недопустимый токен '{tail}' в позиции {currentPosition + 1}");
                }
            }
        }

        private string ProcessTokens()
        {
            var result = new List<string>();
            var tokens = new List<string>();

            // Преобразуем очередь в список для удобства обработки
            while (tokenQueue.Count > 0)
            {
                tokens.Add(tokenQueue.Dequeue());
            }



            // ЭТАП 1: Обработка унарных операторов минуса
            // Унарный минус преобразуется путём добавления 0 перед ним: -5 становится 0-5
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                // Минус считается унарным если:
                // 1) Находится в начале выражения
                // 2) Следует за другим оператором (+, -, *, /, ^)
                // 3) Следует за открывающей скобкой (
                // 4) Следует за названием функции (не постфиксного оператора)
                if (token == "-" && (i == 0 || IsOperator(tokens[i - 1]) || tokens[i - 1] == "(" || (IsFunctionName(tokens[i - 1]) && !IsPostfixOperator(tokens[i - 1]))))
                {
                    // Вставляем 0 перед унарным минусом
                    tokens.Insert(i, "0");
                    i++;
                }
            }

            // Добавляем обработанные токены обратно в очередь
            foreach (var token in tokens)
            {
                tokenQueue.Enqueue(token);
            }

            // ЭТАП 2: Алгоритм Shunting Yard для преобразования инфиксной нотации в обратную польскую нотацию (RPN)
            while (tokenQueue.Count > 0)
            {
                string token = tokenQueue.Dequeue();

                if (IsNumber(token))
                {
                    // Числа сразу добавляются в результат
                    result.Add(token);
                }
                else if (IsVariableName(token))
                {
                    // Переменная/константа заменяется на числовое значение
                    if (CONSTANTS.TryGetValue(token, out decimal constantValue))
                    {
                        result.Add(constantValue.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (variableValues.TryGetValue(token, out decimal variableValue))
                    {
                        result.Add(variableValue.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Неизвестная переменная: {token}");
                    }
                }
                else if (IsPostfixOperator(token))
                {
                    // Постфиксные операторы (например, факториал) добавляются в результат сразу
                    result.Add(token);
                }
                else if (IsFunctionName(token))
                {
                    // Функции добавляются в стек операторов
                    operatorStack.Push(token);
                }
                else if (token == "(")
                {
                    // Открывающая скобка добавляется в стек для отслеживания уровня вложенности
                    operatorStack.Push(token);
                }
                else if (token == ",")
                {
                    // Запятая разделяет аргументы функции - нужно выполнить все операции до открывающей скобки
                    while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    {
                        result.Add(operatorStack.Pop());
                    }
                    // Сама запятая в результат не добавляется
                }
                else if (token == ")")
                {
                    // Закрывающая скобка: выводим операторы до открывающей скобки
                    while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    {
                        result.Add(operatorStack.Pop());
                    }
                    if (operatorStack.Count > 0)
                    {
                        operatorStack.Pop(); // Удаляем открывающую скобку
                    }
                    // Если на стеке осталась функция, добавляем её в результат
                    if (operatorStack.Count > 0 && IsFunctionName(operatorStack.Peek()))
                    {
                        result.Add(operatorStack.Pop());
                    }
                }
                else if (IsOperator(token))
                {
                    // Для операторов проверяем приоритет и ассоциативность
                    // Выводим все операторы с большим или равным приоритетом
                    while (operatorStack.Count > 0 &&
                           operatorStack.Peek() != "(" &&
                           !IsFunctionName(operatorStack.Peek()) &&
                           IsOperator(operatorStack.Peek()) &&
                           GetPrecedence(operatorStack.Peek()) >= GetPrecedence(token))
                    {
                        result.Add(operatorStack.Pop());
                    }
                    // Добавляем текущий оператор в стек
                    operatorStack.Push(token);
                }
            }

            // В конце добавляем все оставшиеся операторы со стека в результат
            while (operatorStack.Count > 0)
            {
                result.Add(operatorStack.Pop());
            }

            return string.Join(" ", result);
        }

        private static int GetPrecedence(string op)
        {
            // Приоритет операции определяет порядок вычисления в выражении
            // Больший приоритет = вычисляется раньше
            return op switch
            {
                // Приоритет 1: сложение и вычитание (низкий приоритет)
                "+" or "-" => 1,
                // Приоритет 2: умножение и деление (средний приоритет)
                "*" or "/" => 2,
                // Приоритет 3: возведение в степень (высокий приоритет)
                "^" => 3,
                // Приоритет 4: унарные функции (самый высокий приоритет)
                "sqrt" or "sin" or "cos" or "tan" => 4,
                // Приоритет 4: логарифмические функции (самый высокий приоритет)
                "log" or "ln" or "lg" => 4,
                // Приоритет 4: специальные бинарные функции (самый высокий приоритет)
                "max" or "min" or "root" or "hypot" => 4,
                _ => 0
            };
        }

        private static bool IsNumber(string token)
        {
            // Проверяем, является ли токен числом (целым или дробным)
            return decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value);
        }

        private static bool IsOperator(string token)
        {
            // Проверяем, является ли токен бинарным оператором
            return OPERATORS.Contains(token);
        }

        private static bool IsPostfixOperator(string token)
        {
            // Проверяем, является ли токен постфиксным оператором (например, !)
            return POSTFIX_OPERATORS.Contains(token);
        }

        private static bool IsFunctionName(string token)
        {
            // Проверяем, является ли токен названием функции (унарной или бинарной), исключая постфиксные операторы
            return ALL_FUNCTIONS.Contains(token) && !POSTFIX_OPERATORS.Contains(token);
        }

        private static bool IsVariableName(string token)
        {
            // Переменная: идентификатор, который не является именем функции
            return Regex.IsMatch(token, @"^[A-Za-z_][A-Za-z0-9_]*$") && !IsFunctionName(token);
        }

        private static bool IsBinaryFunction(string token)
        {
            // Проверяем, является ли токен бинарной функцией (принимающей два аргумента)
            return BINARY_FUNCTIONS.Contains(token);
        }
    }

    /// Класс вычисления результата выражения в обратной польской нотации (RPN)
    /// Обратная польская нотация позволяет вычислять выражение без скобок:
    /// вместо "2+3" используется "2 3 +", вместо "(2+3)*4" - "2 3 + 4 *"
    private class RPNCalculator
    {
        private readonly Queue<string> rpnQueue;

        // Конструктор инициализирует калькулятор с очередью токенов в RPN
        public RPNCalculator(Queue<string> rpnQueue)
        {
            this.rpnQueue = rpnQueue;
        }

        public decimal Calculate()
        {
            // Стек для хранения промежуточных результатов вычислений
            var stack = new Stack<decimal>();

            // Обработаем каждый токен из выражения в обратной польской нотации
            while (rpnQueue.Count > 0)
            {
                string token = rpnQueue.Dequeue();

                // Если токен - это число, добавляем его на стек
                if (decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
                {
                    stack.Push(number);
                }
                // Если токен - постфиксный оператор (например, факториал), берём один аргумент со стека и применяем оператор
                else if (POSTFIX_OPERATORS.Contains(token))
                {
                    decimal a = stack.Pop();
                    decimal result = token switch
                    {
                        // Факториал: n! = n × (n-1) × ... × 2 × 1
                        "!" => Factorial((int)a),
                        _ => throw new InvalidOperationException($"Неизвестный постфиксный оператор: {token}")
                    };
                    stack.Push(result);
                }
                // Если токен - унарная функция, берём один аргумент со стека и применяем функцию
                else if (UNARY_FUNCTIONS.Contains(token))
                {
                    decimal a = stack.Pop();
                    decimal result = token switch
                    {
                        // Квадратный корень: sqrt(a) = a^0.5
                        "sqrt" => (decimal)Math.Sqrt((double)a >= 0 ? (double)a : throw new InvalidOperationException("Невозможно вычислить квадратный корень из отрицательного числа")),
                        // Синус (угол в радианах)
                        "sin" => (decimal)Math.Sin((double)a),
                        // Косинус (угол в радианах)
                        "cos" => (decimal)Math.Cos((double)a),
                        // Тангенс (угол в радианах)
                        "tan" => (decimal)Math.Tan((double)a),
                        // Натуральный логарифм: ln(a) = log_e(a)
                        "ln" => a > 0 ? (decimal)Math.Log((double)a) : throw new InvalidOperationException("Невозможно вычислить натуральный логарифм от неположительного числа"),
                        // Десятичный логарифм: lg(a) = log_10(a)
                        "lg" => a > 0 ? (decimal)Math.Log10((double)a) : throw new InvalidOperationException("Невозможно вычислить десятичный логарифм от неположительного числа"),
                        _ => throw new InvalidOperationException($"Неизвестная функция: {token}")
                    };
                    stack.Push(result);
                }
                // Если токен - бинарная функция, берём два аргумента со стека и применяем функцию
                else if (BINARY_FUNCTIONS.Contains(token))
                {
                    decimal b = stack.Pop(); // Второй аргумент (извлекается первым из-за структуры стека)
                    decimal a = stack.Pop(); // Первый аргумент
                    decimal result = token switch
                    {
                        // Максимум: выбираем большее из двух чисел
                        "max" => a > b ? a : b,
                        // Минимум: выбираем меньшее из двух чисел
                        "min" => a < b ? a : b,
                        // Корень степени a из b: a-th root of b = b^(1/a)
                        "root" => b > 0 ? (decimal)Math.Pow((double)b, 1.0 / (double)a) : throw new InvalidOperationException("Невозможно вычислить корень из отрицательного числа"),
                        // Гипотенуза: hypot(a,b) = sqrt(a² + b²) для прямоугольного треугольника
                        "hypot" => (decimal)Math.Sqrt((double)(a * a + b * b)),
                        // Логарифм по основанию a: log_a(b) = ln(b) / ln(a)
                        "log" => a > 0 && b > 0 && a != 1 ? (decimal)Math.Log((double)b, (double)a) : throw new InvalidOperationException("Невозможно вычислить логарифм с неправильными параметрами"),
                        _ => throw new InvalidOperationException($"Неизвестная функция: {token}")
                    };
                    stack.Push(result);
                }
                // Если токен - бинарный оператор, берём два операнда и применяем операцию
                else if (OPERATORS.Contains(token))
                {
                    decimal b = stack.Pop(); // Второй операнд
                    decimal a = stack.Pop(); // Первый операнд
                    decimal result = token switch
                    {
                        "+" => a + b,           // Сложение
                        "-" => a - b,           // Вычитание
                        "*" => a * b,           // Умножение
                        "/" => b == 0 ? throw new DivideByZeroException("Деление на ноль") : a / b,  // Деление с проверкой на ноль
                        "^" => (decimal)Math.Pow((double)a, (double)b),  // Возведение в степень
                        _ => throw new InvalidOperationException($"Неизвестный оператор: {token}")
                    };
                    stack.Push(result);
                }
            }
            // Возвращаем последний элемент стека - это результат вычисления
            return stack.Pop();
        }

        private static decimal Factorial(int n)
        {
            // Факториал не определён для отрицательных чисел
            if (n < 0)
                throw new InvalidOperationException("Факториал не определен для отрицательных чисел");
            
            // Базовый случай: 0! = 1 и 1! = 1
            if (n == 0 || n == 1)
                return 1;
            
            // Максимальное значение для факториала decimal
            // 20! = 2432902008176640000, 21! уже слишком большой
            if (n > 20)
                throw new InvalidOperationException($"Факториал {n} слишком большой для типа Decimal");
            
            // Вычисляем факториал итеративно: n! = n × (n-1) × (n-2) × ... × 2 × 1
            decimal result = 1;
            for (int i = 2; i <= n; i++)
            {
                result *= i;
            }
            return result;
        }
    }
}
