# Calculator - Библиотека для математических расчётов

Публичная библиотека для вычисления математических выражений без консольного вывода.

## Установка

Скопируйте файл `Calculator.cs` в ваш проект или добавьте ссылку на проект `calculator.csproj`.

## Быстрый старт

```csharp
using сalculator;

// Простой расчёт
decimal result = Calculator.Evaluate("2+3*4");
Console.WriteLine(result); // 14
```

## API

### Calculator.Evaluate(string expression)

Вычисляет математическое выражение и возвращает результат.

**Параметры:**
- `expression` - строка с математическим выражением

**Возвращает:** `decimal` - результат вычисления

**Выбрасывает:** `ArgumentException` при ошибке в выражении

**Пример:**
```csharp
try
{
    decimal result = Calculator.Evaluate("sqrt(16) + sin(0)");
    Console.WriteLine($"Результат: {result}"); // 4
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}
```

### Calculator.EvaluateWithRPN(string expression, out string rpnExpression)

Вычисляет выражение и дополнительно возвращает его представление в обратной польской нотации (RPN).

**Параметры:**
- `expression` - строка с математическим выражением
- `rpnExpression` - выходной параметр (RPN представление)

**Возвращает:** `decimal` - результат вычисления

**Выбрасывает:** `ArgumentException` при ошибке

**Пример:**
```csharp
decimal result = Calculator.EvaluateWithRPN("2+3*4", out string rpn);
Console.WriteLine($"RPN: {rpn}");      // 2 3 4 * +
Console.WriteLine($"Результат: {result}"); // 14
```

## Поддерживаемые операции

### Арифметические операторы
- `+` - сложение
- `-` - вычитание
- `*` - умножение
- `/` - деление
- `^` - возведение в степень

### Математические функции
- `sqrt(x)` - квадратный корень
- `sin(x)` - синус (в радианах)
- `cos(x)` - косинус (в радианах)
- `tan(x)` - тангенс (в радианах)
- `!(x)` - факториал (целое число)

### Другое
- `(...)` - скобки для группировки
- Унарный минус поддерживается (например, `-5`)

### Константы
- `pi` - число π (3.141592653589793...)
- `e` - число Эйлера (2.718281828459045...)

## Примеры использования

```csharp
// Базовая арифметика
Calculator.Evaluate("2+3");           // 5
Calculator.Evaluate("10-5*2");        // 0
Calculator.Evaluate("2^3+1");         // 9
Calculator.Evaluate("10/2+3*4");      // 17

// Функции
Calculator.Evaluate("sqrt(16)/2");    // 2
Calculator.Evaluate("sin(0)");        // 0
Calculator.Evaluate("cos(0)+sin(0)"); // 1
Calculator.Evaluate("5!");            // 120

// Сложные выражения
Calculator.Evaluate("sin(3.14159/2)"); // ≈ 1
Calculator.Evaluate("sqrt(12-3*2^(-5))"); // ≈ 3.45
Calculator.Evaluate("(2+3)*4-10/2"); // 15
Calculator.Evaluate("cos(pi)");      // -1
Calculator.Evaluate("ln(e)");        // 1

// С десятичными числами
Calculator.Evaluate("3.14*2");        // 6.28
Calculator.Evaluate("sqrt(2.5)");     // ≈ 1.58
```

## Обработка ошибок

```csharp
try
{
    var result = Calculator.Evaluate("sqrt(-1)");
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message); // ArgumentException: Невозможно вычислить квадратный корень из отрицательного числа
}
```

## Внутренняя реализация

Калькулятор использует алгоритм Shunting Yard (железнодорожный алгоритм) для преобразования инфиксной нотации в обратную польскую нотацию (RPN), затем вычисляет результат.

- `ExpressionParser` - парсит выражение в RPN
- `RPNCalculator` - вычисляет RPN выражение

## Ограничения

- Тригонометрические функции работают с радианами (не градусами)
- Факториал работает только с целыми неотрицательными числами до 170
- Деление на ноль выбросит исключение `DivideByZeroException`
- Квадратный корень из отрицательных чисел выбросит исключение

## Лицензия

Публичная библиотека, свободна для использования.
