namespace Lecture14;

public class UIHelper
{
    public static string? PromptForString(string message)
    {
        var (left, top) = Console.GetCursorPosition();
        Console.CursorVisible = true;
        
        Console.Write("  " + message);
        string? line = Console.ReadLine();

        if (string.IsNullOrEmpty(line)) return null;
        
        Console.CursorVisible = false;
        return line.Trim();
    }

    public static string SafePromptForString(string message)
    {
        while (true)
        {
            string? data = PromptForString(message);

            if (!string.IsNullOrEmpty(data)) return data;
        }
    }
    
    public static double? PromptForDouble(string message)
    {
        var (left, top) = Console.GetCursorPosition();
        Console.CursorVisible = true;

        while (true)
        {
            Console.Write("  " + message);
            string? line = Console.ReadLine();
            
            if (string.IsNullOrEmpty(line)) return null;

            if (double.TryParse(line, out var result))
            {
                Console.CursorVisible = false;
                return result;
            }
            
            Console.SetCursorPosition(left, top);
            Console.WriteLine("Invalid number.");
        }
    }

    public static double SafePromptForDouble(string message)
    {
        while (true)
        {
            double? data = PromptForDouble(message);

            if (data != null) return data.Value;
        }
    }
    
    public static DateTime? PromptForDate(string message)
    {
        var (left, top) = Console.GetCursorPosition();
        Console.CursorVisible = true;

        while (true)
        {
            Console.Write("  " + message);
            string? line = Console.ReadLine();

            if (string.IsNullOrEmpty(line)) return null;

            if (DateTime.TryParse(line, out var result))
            {
                Console.CursorVisible = false;
                return result;
            }
            
            Console.SetCursorPosition(left, top);
            Console.WriteLine("Invalid date.");
        }
    }
    
    public static DateTime SafePromptForDate(string message)
    {
        while (true)
        {
            DateTime? data = PromptForDate(message);

            if (data != null) return data.Value;
        }
    }

    public static string FormatDate(DateTime date)
    {
        return date.ToString("d") + " " + date.ToString("t");
    }

    public static string TextEllipsis(string text, int maxLenght)
    {
        if (text.Length < maxLenght) return text;

        return text.Substring(0, maxLenght - 3) + "...";
    }
}