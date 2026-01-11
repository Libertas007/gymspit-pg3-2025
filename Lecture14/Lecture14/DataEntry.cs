namespace Lecture14;

public class DataEntry
{
    public DataEntry(DateTime Date, double Amount, string Comment, string Category)
    {
        this.Date = Date;
        this.Amount = Amount;
        this.Comment = Comment;
        this.Category = Category;
    }

    public DateTime Date { get; set;  }
    public double Amount { get; set;  }
    public string Comment { get; set;  }
    public string Category { get; set;  }

    public void Deconstruct(out DateTime Date, out double Amount, out string Comment, out string Category)
    {
        Date = this.Date;
        Amount = this.Amount;
        Comment = this.Comment;
        Category = this.Category;
    }
}