Console.WriteLine("== Zvolte program ==");
Console.WriteLine("1 - Kalkulačka\n2 - FizzBuzz\n");

ConsoleKeyInfo key = Console.ReadKey();
Console.WriteLine();

switch (key.KeyChar)
{
    case '1':
        calculator();
        break;
    case '2':
        fizzBuzz();
        break;
}

void fizzBuzz()
{
    Console.WriteLine("== FizzBuzz ==");

    int num = 0;

    while (true)
    {
        Console.Write("Zadejte počet iterací: ");

        string val = Console.ReadLine() ?? string.Empty;
        if (int.TryParse(val, out int parsedNum))
        {
            num = parsedNum;
            break;
        }

        Console.WriteLine("Číslo je neplatné.");
    }

    for (int i = 0; i <= num; i++)
    {
        string message = "";

        if (i % 3 == 0)
        {
            message += "Fizz ";
        }
        
        if (i % 5 == 0) 
        {
            message += "Buzz";
        }

        if (message == "")
        {
            message = i.ToString();
        }
        
        Console.WriteLine(message);
    }
}

void calculator()
{
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
}
