
/* Kalkulačka, která umí parsovat platný matematický vzorec jako vstup.
 * 
 * (Trochu se mi vnutil Copilot, takže něco vygeneroval, ale jelikož jsem už lexer a parser jednou dělal, tak tomu rozumím.)
 * 
 * Kalkulačka podporuje celá i desetinná čísla, +, -, *, / a závorky.
 */


Console.WriteLine("== Kalkulačka ==");

while (true)
{ 
    Console.WriteLine("Zadejte příklad ('exit' pro konec)");
    Console.Write("> ");
    string line = Console.ReadLine() ?? "";

    if (string.IsNullOrEmpty(line))
    {
        continue;
    }

    if (line.ToLower() == "exit")
    {
        Console.WriteLine("Konec :(");
        break;
    }

    var tokens = new Lexer(line).Tokenize();

    var tree = new Parser(tokens).Parse();

    Console.WriteLine($"{tree.ToString()} = {tree.Evaluate()}");
}


class Error
{
    public static void Write(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
        throw new Exception(message);
    }

    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}

class Token
{
    public double Value { get; set; }
    public TokenType Type { get; set; }

    public Token(TokenType type, double value = 0)
    {
        Type = type;
        Value = value;
    }
}

enum TokenType
{
    Number,
    Plus,
    Minus,
    Multiply,
    Divide,
    LeftParen,
    RightParen,
    EOF
}

class Lexer
{
    string text;
    int position = 0;

    public Lexer(string text)
    {
        this.text = text;
    }

    public char CurrentChar => position < text.Length ? text[position] : '\0';
    public int Advance() => ++position < text.Length ? position : -1;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (CurrentChar != '\0')
        {
            if (char.IsWhiteSpace(CurrentChar))
            {
                Advance();
                continue;
            }

            if (char.IsDigit(CurrentChar))
            {
                tokens.Add(ReadNumber());
                continue;
            }

            switch (CurrentChar)
            {
                case '+':
                    tokens.Add(new Token(TokenType.Plus));
                    break;
                case '-':
                    tokens.Add(new Token(TokenType.Minus));
                    break;
                case '*':
                    tokens.Add(new Token(TokenType.Multiply));
                    break;
                case '/':
                    tokens.Add(new Token(TokenType.Divide));
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.LeftParen));
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.RightParen));
                    break;
                default:
                    Error.Warning($"Skipping unknown character '{CurrentChar}'.");
                    Advance();
                    break;
            }

            Advance();
        }

        return tokens;
    }

    private Token ReadNumber()
    {
        string numberStr = "";
        while (char.IsDigit(CurrentChar) || CurrentChar == '.' || CurrentChar == ',')
        {
            numberStr += CurrentChar;
            Advance();
        }

        numberStr = numberStr.Replace('.', ',');

        double val;

        if (!double.TryParse(numberStr, out val))
        {
            Error.Write($"Invalid number {numberStr}");
        }
        return new Token(TokenType.Number, val);
    }
}

abstract class TreeNode
{
    public abstract double Evaluate();
    public abstract string ToString();
}

class NumberNode : TreeNode
{
    public double Value { get; set; }
    public NumberNode(double value)
    {
        Value = value;
    }
    public override double Evaluate() => Value;
    public override string ToString() => Value.ToString();
}

class UnaryOperatorNode : TreeNode
{
    public TreeNode Node { get; set; }
    public TokenType Operator { get; set; }
    public UnaryOperatorNode(TreeNode node, TokenType operator_)
    {
        Node = node;
        Operator = operator_;
    }
    public override double Evaluate() => (Operator == TokenType.Plus ? 1 : -1) * Node.Evaluate();
    public override string ToString() => (Operator == TokenType.Minus ? "-" : "") + Node.ToString();
}

class ExpressionNode : TreeNode
{
    public TreeNode Left { get; set; }
    public TreeNode Right { get; set; }
    public TokenType Operator { get; set; }
    public ExpressionNode(TreeNode left, TreeNode right, TokenType operator_)
    {
        Left = left;
        Right = right;
        Operator = operator_;
    }
    public override double Evaluate() => Operator == TokenType.Plus ? Left.Evaluate() + Right.Evaluate() : Left.Evaluate() - Right.Evaluate();
    public override string ToString() => Left.ToString() + " " + (Operator == TokenType.Plus ? "+" : "-") + " " + Right.ToString();
}

class ParenNode : TreeNode
{
    public TreeNode Node { get; set; }
    public ParenNode(TreeNode node)
    {
        Node = node;
    }
    public override double Evaluate() => Node.Evaluate();
    public override string ToString() => "(" + Node.ToString() + ")";
}

class TermNode : TreeNode
{
    public TreeNode Left { get; set; }
    public TreeNode Right { get; set; }
    public TokenType Operator { get; set; }
    public TermNode(TreeNode left, TreeNode right, TokenType operator_)
    {
        Left = left;
        Right = right;
        Operator = operator_;
    }
    public override double Evaluate() => Operator == TokenType.Multiply ? Left.Evaluate() * Right.Evaluate() : Left.Evaluate() / Right.Evaluate(); 
    public override string ToString() => Left.ToString() + " " + (Operator == TokenType.Multiply ? "*" : "/") + " " + Right.ToString();
}

class Parser
{
    public List<Token> Tokens { get; set; }
    int position = 0;
    public Parser(List<Token> tokens)
    {
        Tokens = tokens;
    }

    public Token CurrentToken => position < Tokens.Count ? Tokens[position] : new Token(TokenType.EOF);
    public int Advance() => ++position < Tokens.Count ? position : -1;

    public TreeNode Parse()
    {
        return ParseExpression();
    }

    private TreeNode ParseExpression()
    {
        var node = ParseTerm();
        while (CurrentToken.Type == TokenType.Plus || CurrentToken.Type == TokenType.Minus)
        {
            var operator_ = CurrentToken.Type;
            Advance();
            var right = ParseTerm();
            node = new ExpressionNode(node, right, operator_);
        }
        return node;
    }

    private TreeNode ParseTerm()
    {
        var node = ParseFactor();
        while (CurrentToken.Type == TokenType.Multiply || CurrentToken.Type == TokenType.Divide)
        {
            var operator_ = CurrentToken.Type;
            Advance();
            var right = ParseFactor();
            node = new TermNode(node, right, operator_);
        }
        return node;
    }

    private TreeNode ParseFactor()
    {
        if (CurrentToken.Type == TokenType.Plus || CurrentToken.Type == TokenType.Minus)
        {
            var op = CurrentToken.Type;
            Advance();
            var node = ParseFactor();

            return new UnaryOperatorNode(node, op);
        }

        if (CurrentToken.Type == TokenType.LeftParen)
        {
            Advance();
            var node = ParseExpression();
            if (CurrentToken.Type != TokenType.RightParen)
            {
                Error.Write("Expected ')'.");
            }
            Advance();
            return new ParenNode(node);
        }

        if (CurrentToken.Type == TokenType.Number)
        {
            var node = new NumberNode(CurrentToken.Value);
            Advance();
            return node;
        }

        Error.Write($"Parsing failed (somewhere around {CurrentToken.Type} {CurrentToken.Value}).");
        return new NumberNode(-1.0);
    }
}