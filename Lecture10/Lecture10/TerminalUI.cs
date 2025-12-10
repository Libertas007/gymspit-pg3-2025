namespace Lecture10
{
    public class TerminalUI
    {
        public static void PrintLogo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("   __           _ __  __           \r\n  / /__      __(_) /_/ /____  _____\r\n / __/ | /| / / / __/ __/ _ \\/ ___/\r\n/ /_ | |/ |/ / / /_/ /_/  __/ /    \r\n\\__/ |__/|__/_/\\__/\\__/\\___/_/     \r\n                                   ");
            Console.WriteLine();
            Console.ResetColor();
        }

        public static string GetFilePrompt()
        {
            Console.WriteLine("Enter the file name of the instance, or write 'temp' for a temporary instance.");
            Console.Write("> ");
            string path = (Console.ReadLine() ?? "").Trim();

            return path == "temp" ? "" : path;
        }

        public static void ClearAndLogo()
        {
            Console.Clear();
            PrintLogo();
        }

        public static void PrintPost(Post post)
        {
            Console.WriteLine($"+{new string('-', Console.WindowWidth - 2)}+");
            Console.WriteLine($"| @{post.Author} {new string(' ', Console.WindowWidth - 2 - 3 - post.Author.Length)}|");
            Console.WriteLine($"| {post.Content} {new string(' ', Console.WindowWidth - 2 - 2 - post.Content.Length)}|");
            Console.WriteLine($"+{new string('-', Console.WindowWidth - 2)}+");
            Console.WriteLine();
        }

        public static string LoginPrompt()
        {
            Console.WriteLine("Enter your username:");
            Console.Write("> ");
            return (Console.ReadLine() ?? "").Trim();
        }
    }
}
