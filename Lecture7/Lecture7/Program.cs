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
                case "count":
                    CountStudents();
                    break;
                case "delete":
                    DeleteStudent();
                    break;
                case "help":
                    PrintHelp();
                    break;
                case "exit":
                    Environment.Exit(0);
                    break;
                case "size":
                    Console.WriteLine("< 128");
                    break;
                default:
                    Console.WriteLine("< Invalid command");
                    break;
            }
        }

        public static void AddStudent()
        {
            Console.Write("| Enter student name: ");
            string name = Console.ReadLine() ?? "";

            Console.Write("| Enter student id (empty for next available): ");
            string idS = Console.ReadLine() ?? "";

            int id;

            if (string.IsNullOrEmpty(idS))
            {
                id = Array.FindIndex(students, string.IsNullOrEmpty);
            } else if (!int.TryParse(idS, out id)) {
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

            // Returning the index of the student because it's more practical
            Console.WriteLine($"< {Array.IndexOf(students, name)}");
        }

        public static void DeleteStudent()
        {
            Console.Write("| Enter student id: ");
            string idS = Console.ReadLine() ?? "";

            if (!int.TryParse(idS, out var id)) {
                Console.WriteLine("< Invalid number");
                return;
            }

            if (string.IsNullOrEmpty(students[id]))
            {
                Console.WriteLine("< Record does not exist");
                return;
            }

            students[id] = null;
            Console.WriteLine($"< '{id}' removed");
        }

        public static void PrintHelp()
        {
            Console.WriteLine(@"
- add: Add a new student
- delete: Delete a student
- exit: Exit
- help: Display help
- list: List students
- lookup: Lookup a student
");
        }
    }
}
