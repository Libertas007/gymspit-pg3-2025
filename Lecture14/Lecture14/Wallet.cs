namespace Lecture14;

public class Wallet
{
    private List<DataEntry> data;
    private string orderColumn = "Date";
    private bool orderAscending = true;
    private int cursorIndex = 0;
    private string path;
    private string page = "main";
    private int currentEntryIndex = 0;
    private string findString = "";

    Wallet(List<DataEntry> data, string path)
    {
        this.data = data;
        this.path = path;
        
        if (data.Count == 0)
        {
            cursorIndex = -9;
        }
    }

    public static Wallet LoadCSV(string path, string columnSeparator = ";", string rowSeparator = "\n")
    {
        if (!File.Exists(path))
        {
            var f = File.Create(path);
            f.Close();
            return new Wallet(new List<DataEntry>(), path);
        }
        
        string text = File.ReadAllText(path);

        string[] lines = text.Split(rowSeparator).Select(l => l.Trim()).ToArray();

        if (lines.Length == 0)
        {
            return new Wallet(new List<DataEntry>(), path);
        }

        string[] headers = lines[0].Split(columnSeparator);
        List<DataEntry> entries = new List<DataEntry>();

        int dateIndex = Array.IndexOf(headers, "Date");
        int amountIndex = Array.IndexOf(headers, "Amount");
        int commentIndex = Array.IndexOf(headers, "Comment");
        int categoryIndex = Array.IndexOf(headers, "Category");

        try
        {
            for (int i = 1; i < lines.Length; i++)
            {
                string[] line = lines[i].Split(columnSeparator);

                DateTime date = DateTime.Parse(line[dateIndex]);
                double amount = double.Parse(line[amountIndex]);
                string comment = line[commentIndex].Trim();
                string category = line[categoryIndex].Trim();

                entries.Add(new DataEntry(date, amount, comment, category));
            }
            return new Wallet(entries, path);
        }
        catch
        {
            return new Wallet(new List<DataEntry>(), path);
        }
    }

    public void Loop()
    {
        if (page == "main")
        {
            MainMenu();

            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.DownArrow:
                case ConsoleKey.RightArrow:
                    cursorIndex++;
                    if (cursorIndex == data.Count)
                    {
                        cursorIndex = -9;
                    }
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.LeftArrow:
                    cursorIndex--;
                    if (cursorIndex < -9)
                    {
                        cursorIndex = data.Count - 1;
                    }
                    break;
                case ConsoleKey.Enter:
                    RunMainMenuAction();
                    break;
            }
        } else if (page == "detail")
        {
            EntryPage(data[currentEntryIndex]);
            
            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.DownArrow:
                case ConsoleKey.RightArrow:
                    cursorIndex++;
                    if (cursorIndex == 4)
                    {
                        cursorIndex = -2;
                    }
                    break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.LeftArrow:
                    cursorIndex--;
                    if (cursorIndex < -2)
                    {
                        cursorIndex = 3;
                    }
                    break;
                case ConsoleKey.Escape:
                    page = "main";
                    break;
                case ConsoleKey.Enter:
                    RunDetailAction();
                    break;
            }
        } else if (page == "new")
        {
            DataEntry newEntry = NewPage();
            
            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    page = "main";
                    break;
                case ConsoleKey.Enter:
                    data.Add(newEntry);
                    page = "detail";
                    currentEntryIndex = data.Count - 1;
                    break;
            }
        }
        else if (page == "categories")
        {
            CategoriesPage();
            
            ConsoleKeyInfo key = Console.ReadKey();
            
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                case ConsoleKey.Enter: 
                    page = "main";
                    break;
            }
        }
        
        Loop();
    }

    public void MainMenu()
    {
        Console.Clear();
        Console.WriteLine("  WALLET\n");

        if (findString != "")
        {
            Console.WriteLine($"  Filter: {findString}\n");
        }
        
        Console.Write("  ");
        PrintHeader("Date", 20, isSelected: cursorIndex == -4);
        Console.Write(" | ");
        PrintHeader("Amount", 18, isSelected: cursorIndex == -3);
        Console.Write(" | ");
        PrintHeader("Comment", 40, isSelected: cursorIndex == -2);
        Console.Write(" | ");
        PrintHeader("Category", 20, isSelected: cursorIndex == -1);
        Console.WriteLine();
        Console.WriteLine("  " + new string('-', 20 + 13 + 40 + 20 + 3*3) + "  ");

        List<DataEntry> sorted = data;
        
        sorted.Sort((a, b) =>
        {
            return orderColumn switch
            {
                "Date" => a.Date.CompareTo(b.Date) * (orderAscending ? 1 : -1),
                "Amount" => a.Amount.CompareTo(b.Amount) * (orderAscending ? 1 : -1),
                "Comment" => a.Comment.CompareTo(b.Comment) * (orderAscending ? 1 : -1),
                "Category" => a.Category.CompareTo(b.Category) * (orderAscending ? 1 : -1),
                _ => 0,
            };
        });

        sorted = sorted.Where(e =>
            e.Comment.Contains(findString) || e.Category.Contains(findString) ||
            e.Amount.ToString("F").Contains(findString) || UIHelper.FormatDate(e.Date).Contains(findString)).ToList();
        
        for (int i = 0; i < sorted.Count; i++)
        {
            PrintEntry(sorted[i], isSelected: i == cursorIndex);
        }
        
        Console.WriteLine("\n  STATS\n");

        double income = data.Select(e => e.Amount).Where(a => a > 0).Sum();
        double expenses = -data.Select(e => e.Amount).Where(a => a < 0).Sum();
        
        PrintLabelData("Total income", income.ToString("F") + " Kč");
        PrintLabelData("Total expenses", expenses.ToString("F") + " Kč");
        PrintLabelData("Balance", (income - expenses).ToString("F") + " Kč");
        
        Console.WriteLine();
        
        PrintButton("Add new entry", isSelected: cursorIndex == -9);
        PrintButton("Show categories", isSelected: cursorIndex == -8);
        PrintButton("Filter", isSelected: cursorIndex == -7);
        PrintButton("Save", isSelected: cursorIndex == -6);
        PrintButton("Save and exit", isSelected: cursorIndex == -5);
        
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n\n  Hint: press Enter on a transaction to see it in detail.\n");
        Console.ResetColor();
    }

    private void EntryPage(DataEntry entry)
    {
        Console.Clear();
        Console.WriteLine("  WALLET\n");
        Console.WriteLine("  Transaction details");
        Console.WriteLine("  -------------------\n");
        
        PrintLabelData("Date", UIHelper.FormatDate(entry.Date), isSelected: cursorIndex == 0);
        PrintLabelData("Amount", entry.Amount.ToString("F") + " Kč", isSelected: cursorIndex == 1);
        PrintLabelData("Comment", entry.Comment, isSelected: cursorIndex == 2);
        PrintLabelData("Category", entry.Category, isSelected: cursorIndex == 3);
        
        Console.WriteLine();
        
        PrintButton("Back", isSelected: cursorIndex == -2);
        PrintButton("Delete entry", isSelected: cursorIndex == -1, accentColor: ConsoleColor.Red);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n\n  Hint: press Enter on a field to edit it.\n");
        Console.ResetColor();
    }
    
    private void CategoriesPage()
    {
        Console.Clear();
        Console.WriteLine("  WALLET\n");
        Console.WriteLine("  Categories");
        Console.WriteLine("  ----------\n");

        string[] categories = data.Select(e => e.Category).ToHashSet().ToArray();

        foreach (var category in categories)
        {
            double income = data.Where(e => e.Category == category && e.Amount > 0).Select(e => e.Amount).Sum();
            double expenses = -data.Where(e => e.Category == category && e.Amount < 0).Select(e => e.Amount).Sum();
            int count = data.Count(e => e.Category == category);
            
            Console.WriteLine("  " + category + ":");
            PrintLabelData("Transactions count", count.ToString());
            PrintLabelData("Total income", income.ToString("F") + " Kč");
            PrintLabelData("Total expenses", expenses.ToString("F") + " Kč");
            PrintLabelData("Category balance", (income - expenses).ToString("F") + " Kč");
            Console.WriteLine();
        }
        
        
        PrintButton("Back", isSelected: true);
        
        Console.WriteLine();
    }

    private DataEntry NewPage()
    {
        cursorIndex = 0;
        Console.Clear();
        Console.WriteLine("  WALLET\n");
        Console.WriteLine("  New transaction");
        Console.WriteLine("  -------------------\n");
        
        double amount = UIHelper.SafePromptForDouble("Set amount: ");
        string comment = UIHelper.SafePromptForString("Write comment: ");
        string category = UIHelper.SafePromptForString("Set category: ");
        
        Console.WriteLine();
        
        PrintButton("Save", isSelected: true);
        
        Console.WriteLine();

        return new DataEntry(DateTime.Now, amount, comment, category);
    }

    private void PrintHeader(string title, int width, bool isSelected = false)
    {
        Console.BackgroundColor = isSelected ? ConsoleColor.Blue : Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.White;

        string toPrint = "";

        toPrint += isSelected ? "> " : "";

        if (orderColumn == title)
        {
            toPrint += orderAscending ? "↑ " : "↓ ";
        }

        toPrint += title;
        int spaceLeft = width - toPrint.Length - (isSelected ? 2 : 0);
        if (spaceLeft < 0)
        {
            toPrint = toPrint.Substring(0, toPrint.Length + spaceLeft);
        }
        else
        {
            toPrint += new string(' ', spaceLeft);
        }
        toPrint += isSelected ? " <" : "";
        
        Console.Write(toPrint);
        Console.ResetColor();
    }

    private void PrintEntry(DataEntry entry, bool isSelected = false)
    {
        Console.BackgroundColor = isSelected ? ConsoleColor.Blue : Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.Write(isSelected ? "> " : "  ");
        Console.Write("{0,-20} | {1,15:N2} Kč | {2,-40} | {3,-20}", UIHelper.FormatDate(entry.Date), entry.Amount, UIHelper.TextEllipsis(entry.Comment, 40), UIHelper.TextEllipsis(entry.Category, 20));
        Console.WriteLine(isSelected ? " <" : "  ");
        
        Console.ResetColor();
    }

    private void PrintLabelData(string label, string data, bool isSelected = false)
    {
        Console.BackgroundColor = isSelected ? ConsoleColor.Blue : Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.Write(isSelected ? "> " : "  ");
        Console.Write("{0,20}: {1}", label, data);
        Console.WriteLine(isSelected ? " <" : "  ");
        
        Console.ResetColor();
    }

    private void PrintButton(string text, bool isSelected = false, ConsoleColor accentColor = ConsoleColor.Blue)
    {
        Console.Write("  ");
        Console.BackgroundColor = isSelected ? accentColor : ConsoleColor.DarkGray;
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.Write(isSelected ? "> " : "  ");
        Console.Write(text);
        Console.Write(isSelected ? " <" : "  ");
        
        Console.ResetColor();
        Console.Write("  ");
    }

    private void RunMainMenuAction()
    {
        if (cursorIndex >= 0 && cursorIndex < data.Count)
        {
            page = "detail";
            currentEntryIndex = cursorIndex;
            cursorIndex = 0;
        } else if (cursorIndex == -1)
        {
            if (orderColumn != "Category")
            {
                orderColumn = "Category";
                orderAscending = true;
            }
            else
            {
                orderAscending = !orderAscending;
            }
        } else if (cursorIndex == -2)
        {
            if (orderColumn != "Comment")
            {
                orderColumn = "Comment";
                orderAscending = true;
            }
            else
            {
                orderAscending = !orderAscending;
            }
        } else if (cursorIndex == -3)
        {
            if (orderColumn != "Amount")
            {
                orderColumn = "Amount";
                orderAscending = true;
            }
            else
            {
                orderAscending = !orderAscending;
            }
        } else if (cursorIndex == -4)
        {
            if (orderColumn != "Date")
            {
                orderColumn = "Date";
                orderAscending = true;
            }
            else
            {
                orderAscending = !orderAscending;
            }
        } else if (cursorIndex == -5)
        {
            SaveToFile();
            Environment.Exit(0);
        }
        else if (cursorIndex == -6)
        {
            SaveToFile();
        }
        else if (cursorIndex == -7)
        {
            string? filter = UIHelper.PromptForString("Find: ");
            findString = string.IsNullOrEmpty(filter) ? "" : filter;
        }
        else if (cursorIndex == -8)
        {
            page = "categories";
        }
        else if (cursorIndex == -9)
        {
            page = "new";
        }
    }

    private void RunDetailAction()
    {
        if (cursorIndex is >= 0 and < 4)
        {
            if (cursorIndex == 0)
            {
                DateTime? date = UIHelper.PromptForDate("Edit date (leave empty not to change): ");
                if (date != null) data[currentEntryIndex].Date = date.Value;
            }
            else if (cursorIndex == 1)
            {
                double? amount = UIHelper.PromptForDouble("Edit amount (leave empty not to change): ");
                if (amount != null) data[currentEntryIndex].Amount = amount.Value;
            }
            else if (cursorIndex == 2)
            {
                string? comment = UIHelper.PromptForString("Edit comment (leave empty not to change): ");
                if (!string.IsNullOrEmpty(comment)) data[currentEntryIndex].Comment = comment;
            }
            else if (cursorIndex == 3)
            {
                string? category = UIHelper.PromptForString("Edit category (leave empty not to change): ");
                if (!string.IsNullOrEmpty(category)) data[currentEntryIndex].Category = category;
            }
        } else if (cursorIndex == -1)
        {
            data.RemoveAt(currentEntryIndex);
            page = "main";
        } else if (cursorIndex == -2)
        {
            page = "main";
        }
    }

    public void SaveToFile()
    {
        string csv = "Date;Amount;Comment;Category";

        foreach (var entry in data)
        {
            csv += $"\n{entry.Date:g};{entry.Amount};{entry.Comment};{entry.Category}";
        }
        
        File.WriteAllText(path, csv);
    }
}