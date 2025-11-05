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
                    Help();
                    break;
                case "size":
                    Console.WriteLine("< 128");
                    break;
                case "exit":
                    Environment.Exit(0);
                    break;
            }
        }

        public static void Help()
        {
            Console.WriteLine("| add - add a student");
            Console.WriteLine("| count - get the number of students");
            Console.WriteLine("| delete - delete a student");
            Console.WriteLine("| exit - quit the program");
            Console.WriteLine("| help - print this help");
            Console.WriteLine("| list - get the full list of students");
            Console.WriteLine("| lookup - find a student by name");
            Console.WriteLine("| size - get the max size of the list (aka n - 1 is the last available index)");
        }

        public static void AddStudent()
        {
            Console.Write("| Enter student name: ");
            string name = Console.ReadLine() ?? "";

            Console.Write("| Enter student id: ");
            string idS = Console.ReadLine() ?? "";


            if (!int.TryParse(idS, out var id)) {
                if (idS.Trim().Length == 0) {
                    id = Array.FindIndex(students, string.IsNullOrEmpty);
                }
                else
                {
                    Console.WriteLine("! Invalid number");
                    return;
                }
            }

            if (!string.IsNullOrEmpty(students[id]))
            {
                Console.WriteLine("! ID occupied");
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

            Console.WriteLine($"< {Array.IndexOf(students, name)}");
        }

        public static void DeleteStudent()
        {
            Console.Write("| Enter student id: ");
            string idS = Console.ReadLine() ?? "";

            if (!int.TryParse(idS, out var id))
            {
                Console.WriteLine("! Invalid number");
                return;
            }

            if (string.IsNullOrEmpty(students[id]))
            {
                Console.WriteLine("! Invalid student ID");
                return;
            }

            students[id] = null;
            Console.WriteLine($"< '{id}' deleted");
        }

        public static void CountStudents()
        {
            int count = 0;

            foreach (var student in students)
            {
                if (!string.IsNullOrEmpty(student))
                {
                    count++;
                }
            }

            Console.WriteLine($"< {count}");
        }
    }
}
