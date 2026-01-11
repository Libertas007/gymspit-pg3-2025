namespace Lecture14;

class Program
{
    static void Main(string[] args)
    {
        Console.CursorVisible = false;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        
        Console.WriteLine("\n  Welcome to your wallet management!");
        string path = UIHelper.SafePromptForString("Enter the path name: ");

        Wallet wallet = Wallet.LoadCSV(path);
        
        wallet.Loop();

        Console.ReadKey();
    }
}