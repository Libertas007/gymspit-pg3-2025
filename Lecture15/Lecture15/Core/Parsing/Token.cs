namespace JinjaCompiler.Core.Parsing;

/// <summary>
/// Token types for the Jinja2 lexer.
/// </summary>
public enum TokenType
{
    // Text content
    Text,

    // Delimiters
    ExpressionStart,    // {{
    ExpressionEnd,      // }}
    StatementStart,     // {%
    StatementEnd,       // %}
    CommentStart,       // {#
    CommentEnd,         // #}

    // Keywords
    Extends,
    Block,
    EndBlock,
    Include,
    Import,
    From,
    As,
    If,
    ElseIf,
    Else,
    EndIf,
    For,
    In,
    EndFor,
    Set,
    Macro,
    EndMacro,
    Call,
    EndCall,
    Raw,
    EndRaw,
    With,
    EndWith,
    Without,
    Context,
    Autoescape,
    EndAutoescape,
    Super,
    Recursive,
    Scoped,
    IgnoreMissing,

    // Operators
    Pipe,               // |
    Dot,                // .
    Comma,              // ,
    Colon,              // :
    Equals,             // =
    DoubleEquals,       // ==
    NotEquals,          // !=
    LessThan,           // <
    LessThanOrEqual,    // <=
    GreaterThan,        // >
    GreaterThanOrEqual, // >=
    Plus,               // +
    Minus,              // -
    Multiply,           // *
    Divide,             // /
    FloorDivide,        // //
    Modulo,             // %
    Power,              // **
    Tilde,              // ~
    OpenParen,          // (
    CloseParen,         // )
    OpenBracket,        // [
    CloseBracket,       // ]
    OpenBrace,          // {
    CloseBrace,         // }

    // Logical operators
    And,
    Or,
    Not,
    Is,
    IsNot,

    // Literals
    String,
    Integer,
    Float,
    True,
    False,
    None,

    // Identifiers
    Identifier,

    // Special
    Whitespace,
    Newline,
    EndOfFile,

    // Unknown
    Unknown
}

/// <summary>
/// Represents a token in the Jinja2 template.
/// </summary>
public record Token(
    TokenType Type,
    string Value,
    int Position,
    int Line,
    int Column,
    int Length)
{
    public int EndPosition => Position + Length;

    public override string ToString() => $"{Type}({Value}) at {Line}:{Column}";
}

/// <summary>
/// Represents the current state of the lexer.
/// </summary>
public enum LexerState
{
    /// <summary>
    /// Normal text parsing mode.
    /// </summary>
    Data,

    /// <summary>
    /// Inside {{ ... }} expression.
    /// </summary>
    Expression,

    /// <summary>
    /// Inside {% ... %} statement.
    /// </summary>
    Statement,

    /// <summary>
    /// Inside {# ... #} comment.
    /// </summary>
    Comment,

    /// <summary>
    /// Inside {% raw %} ... {% endraw %} block.
    /// </summary>
    Raw
}
