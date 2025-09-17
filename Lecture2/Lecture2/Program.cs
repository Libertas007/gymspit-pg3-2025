Console.WriteLine("== Kalkulačka ==");

double a = 0, b = 0;

int x, y;
(x, y) = Console.GetCursorPosition();

while (true)
{
    Console.WriteLine($"a = {a} (změna šipkami nahoru a dolů, potvrď Enterem)");

    var key = Console.ReadKey().Key;

    if (key == ConsoleKey.Enter) break;

    if (key == ConsoleKey.UpArrow) a++;
    if (key == ConsoleKey.DownArrow) a--;
    Console.SetCursorPosition(x, y);
}


(x, y) = Console.GetCursorPosition();

while (true)
{
    Console.WriteLine($"b = {b} (změna šipkami nahoru a dolů, potvrď Enterem)");

    var key = Console.ReadKey().Key;

    if (key == ConsoleKey.Enter) break;

    if (key == ConsoleKey.UpArrow) b++;
    if (key == ConsoleKey.DownArrow) b--;
    Console.SetCursorPosition(x, y);
}

Console.WriteLine();
Console.WriteLine($"a + b = {a + b}");
Console.WriteLine($"a - b = {a - b}");
Console.WriteLine($"a * b = {a * b}");
Console.WriteLine($"a / b = {a / b}");
