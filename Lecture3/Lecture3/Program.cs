Console.WriteLine("== Kalkulačka ==\n");

bool exit = false;

while (!exit) 
{
    Console.WriteLine("| Podporované operace: +, -, *, /, exit");

    Console.Write("> Zadejte operaci: ");

    string operation = (Console.ReadLine() ?? string.Empty).Trim();

    if (operation.ToLower() == "exit")
    {
        exit = true;
        continue;
    }
    
    if (operation == string.Empty || !(new[] {"+", "-", "*", "/"}).Contains(operation))
    {
        ErrorWriteLine("! Neplatná operace, zkuste to znovu.");
        Console.WriteLine("|");
        continue;
    }


    Console.WriteLine("|");

    double a, b;

    while (true)
    {
        Console.Write("> Zadejte první číslo: ");
        string inputA = Console.ReadLine() ?? string.Empty;

        if (!double.TryParse(inputA, out a))
        {
            ErrorWriteLine("! Neplatný vstup, zadejte číslo znovu: ");
        }
        else
        {
            break;
        }
    }

    while (true)
    {
        Console.Write("> Zadejte druhé číslo: ");
        string inputB = Console.ReadLine() ?? string.Empty;

        if (!double.TryParse(inputB, out b) || (operation == "/" && b == 0))
        {
            ErrorWriteLine("! Neplatný vstup, zadejte číslo znovu: ");
        }
        else
        {
            break;
        }
    }

    double c = 0;

    switch (operation)
    {
        case "+":
            c = a + b;
            break;
        case "-":
            c = a - b;
            break;
        case "*":
            c = a * b;
            break;
        case "/":
            c = a / b;
            break;
    }

    Console.WriteLine("|");

    Console.Write(
        $"| a {operation} b = {a} {operation} {b} = "
    );
    Console.BackgroundColor = ConsoleColor.DarkGray;
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($" {c} ");
    Console.ResetColor();
    Console.WriteLine("|");
}

void ErrorWriteLine(string message)
{
    Console.BackgroundColor = ConsoleColor.Red;
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(message);
    Console.ResetColor();
}