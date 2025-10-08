Console.WriteLine("== Sieve of Eratosthenes ==");

while (true)
{
    uint num = 0;
    bool success = false;

    do
    {
        Console.Write("Enter the max number to search (or 'exit' to exit): ");

        string input = Console.ReadLine() ?? "";

        if (input.ToLower() == "exit")
        {
            Console.WriteLine("Ok, bye!");
            Environment.Exit(0);
        }
        
        success = uint.TryParse(input, out num);

        if (!success || num < 2)
        {
            Console.WriteLine("Please, enter a valid number.");
        }
    } while (!success || num < 2);

    bool[] array = new bool[num + 1];

    Array.Fill(array, true);

    List<uint> list = new List<uint>();

    for (uint i = 2; i*i < array.Length; i++)
    {
        if (array[i] == false) continue;

        for (uint j = i*i; j < array.Length; j += i)
        {
            if (array[j] == false) continue;

            if (j % i == 0)
            {
                array[j] = false;
            }
        }
    }


    for (uint i = 2; i < array.Length; i++)
    {
        if (array[i] == false) continue;

        list.Add(i);
    }


    if (list.Count == 0)
    {
        Console.WriteLine("There are no prime numbers.");
    } else
    {
        if (list.Count == 1)
        {
            Console.WriteLine($"The sole prime number is {list[0]}.");
        } else
        {
            Console.WriteLine("The prime numbers are: ");
            Console.WriteLine(string.Join(", ", list));
            Console.WriteLine($"There are {list.Count} prime numbers.");
        }
    }
}