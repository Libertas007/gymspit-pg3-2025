namespace Lecture7Calc
{
    class Program
    {
        static Dictionary<char, Func<double, double, double>> operations = new Dictionary<char, Func<double, double, double>>();

        public static void Main(string[] args)
        {
            PopulateOperations();

            while (true)
            {
                PrintMenu();
                char operation = ReadOperation();

                if (operation == ' ')
                {
                    Environment.Exit(0);
                }

                double a = ReadDouble();
                double b = ReadDouble(nonZero: operation == '/');

                double res = Compute(operation, a, b);
                PrintResult(operation, a, b, res);
            }
        }

        static void PopulateOperations()
        {
            operations.Clear();
            operations.Add('+', (a, b) => a + b);
            operations.Add('-', (a, b) => a - b);
            operations.Add('*', (a, b) => a * b);
            operations.Add('/', (a, b) => a / b);
            operations.Add('%', (a, b) => a % b);
            operations.Add('^', Math.Pow);
        }

        static void PrintMenu()
        {
            Console.WriteLine("\n| Welcome to this marvelous calculator!");
            Console.WriteLine("| Select one of these operations below by entering it.");
            Console.WriteLine("| Or press Esc key to exit.");

            foreach (char c in operations.Keys)
            {
                Console.WriteLine($"| {c}");
            }

            Console.WriteLine();
        }

        static char ReadOperation()
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter) continue;
                if (key.Key == ConsoleKey.Escape) return ' ';

                if (!operations.Keys.Contains(key.KeyChar))
                {
                    Console.WriteLine("! Invalid operand.");
                    continue;
                }

                Console.WriteLine($"| Selected operation: {key.KeyChar}");
                return key.KeyChar;
            }
        }

        static double ReadDouble(bool nonZero = false)
        {

            while (true)
            {
                Console.Write("< Enter a number: ");
                String val = Console.ReadLine() ?? "";

                if (val.Trim().Length == 0)
                {
                    continue;
                }

                double d = 0.0;

                if (!double.TryParse(val, out d))
                {
                    Console.WriteLine("! Invalid number.");
                    continue;
                }

                if (nonZero && d == 0)
                {
                    Console.WriteLine("! This value cannot be zero.");
                    continue;
                }

                return d;
            }
        }

        static double Compute(char operation, double operand1, double operand2) {
            if (!operations.ContainsKey(operation))
            {
                Console.WriteLine("! Unknown operation.");
                return double.NaN;
            }

            return operations[operation].Invoke(operand1, operand2);
        }

        static void PrintResult(char operation, double operand1, double operand2, double result)
        {
            Console.WriteLine($"> {operand1} {operation} {operand2} = {result}");
        }
    }
}