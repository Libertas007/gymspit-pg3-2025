namespace Lecture7
{
    class Program
    {
        static string[] students = new string[128];

        public static void Main(string[] args)
        {
            Console.WriteLine("== Student management ==");

            while (true)
            {

                Console.WriteLine("\nEnter a command:");
                Console.Write("> ");

                HandleCommand(Console.ReadLine() ?? "");
            }
        }

        public static void HandleCommand(string raw)
        {
            if (raw.Trim().Length == 0)
            {
                return;
            }

            var command = raw.Trim();

            switch (command)
            {
                case "add":
                    AddStudent();
                    break;
                case "list":
                    ListStudents();
                    break;
                case "lookup":
                    LookupStudent();
                    break;
            }
        }

        public static void AddStudent()
        {
            Console.Write("| Enter student name: ");
            string name = Console.ReadLine() ?? "";

            Console.Write("| Enter student id: ");
            string idS = Console.ReadLine() ?? "";

            if (!int.TryParse(idS, out var id)) {
                Console.WriteLine("< Invalid number");
                return;
            }

            if (!string.IsNullOrEmpty(students[id]))
            {
                Console.WriteLine("< ID occupied");
                return;
            }
        

            students[id] = name;

            Console.WriteLine($"< '{name}' set");
        }

        public static void ListStudents()
        {
            for (int i = 0; i < students.Length; i++) {
                if (students[i] != null)
                {
                    Console.WriteLine($"< {i}: {students[i]}");
                }
            } 
        }

        public static void LookupStudent()
        {
            Console.Write("| Enter student name: ");
            string name = Console.ReadLine() ?? "";

            Console.WriteLine($"< {students.Contains(name)}");
        }
    }
}
