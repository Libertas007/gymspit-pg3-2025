// See https://aka.ms/new-console-template for more information

int defaultDelay = 1000;




void FancyWrite(string msg, bool newLine = true)
{
    Console.CursorVisible = false;
    char[] letters = msg.ToCharArray();
    char[] temp = new char[letters.Length];

    int x, y;

    (x, y) = Console.GetCursorPosition();

    for (int i = 0; i < letters.Length; i++) 
    {
        char current = letters[i];

        int difference = current - ' ' + 1;

        for (char j = ' '; j <= current; j++) 
        {
            Console.SetCursorPosition(x + i, y);
            Console.Write(j);

            Thread.Sleep(defaultDelay / difference);

            if (j >= current) {
              break;
            }
        }
    }

    if (newLine) Console.WriteLine();

    Console.CursorVisible = true;
}

void ScrollWrite(string msg, int limitLength, int iterations)
{
    Console.CursorVisible = false;

    int x, y;
    (x, y) = Console.GetCursorPosition();

    int i = 0;

    while (i < iterations)
    {
        string temp = string.Concat(Enumerable.Repeat(" ", limitLength)) + msg + string.Concat(Enumerable.Repeat(" ", limitLength * 2));

        for (int j = 0; j < msg.Length * 2; j++)
        {
            Console.SetCursorPosition(x, y);
            Thread.Sleep(100);
            Console.Write(temp.Substring(j, limitLength));
        }
        i++;
    }
    Console.CursorVisible = true;
    Console.WriteLine();
}

ScrollWrite("Scrolling text demo! ", 20, 3);

Console.WriteLine($"Default delay is {defaultDelay} ms. Enter a new delay in ms or press Enter to keep the default: ");

if (!int.TryParse(Console.ReadLine(), out defaultDelay))
{
    defaultDelay = 1000;
}


FancyWrite("Hello, World!");
FancyWrite("Enter something: ", false);
FancyWrite(Console.ReadLine() ?? "No input provided.");

Console.WriteLine("\n\nWonderful!");