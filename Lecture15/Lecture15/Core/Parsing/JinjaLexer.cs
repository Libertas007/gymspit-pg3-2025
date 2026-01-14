using System.Text;
using JinjaCompiler.Core.Exceptions;

namespace JinjaCompiler.Core.Parsing;

/// <summary>
/// Lexer for tokenizing Jinja2 templates.
/// </summary>
public class JinjaLexer
{
    private readonly string _source;
    private readonly string _templateName;
    private int _position;
    private int _line;
    private int _column;
    private LexerState _state;

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["extends"] = TokenType.Extends,
        ["block"] = TokenType.Block,
        ["endblock"] = TokenType.EndBlock,
        ["include"] = TokenType.Include,
        ["import"] = TokenType.Import,
        ["from"] = TokenType.From,
        ["as"] = TokenType.As,
        ["if"] = TokenType.If,
        ["elif"] = TokenType.ElseIf,
        ["else"] = TokenType.Else,
        ["endif"] = TokenType.EndIf,
        ["for"] = TokenType.For,
        ["in"] = TokenType.In,
        ["endfor"] = TokenType.EndFor,
        ["set"] = TokenType.Set,
        ["macro"] = TokenType.Macro,
        ["endmacro"] = TokenType.EndMacro,
        ["call"] = TokenType.Call,
        ["endcall"] = TokenType.EndCall,
        ["raw"] = TokenType.Raw,
        ["endraw"] = TokenType.EndRaw,
        ["with"] = TokenType.With,
        ["endwith"] = TokenType.EndWith,
        ["without"] = TokenType.Without,
        ["context"] = TokenType.Context,
        ["autoescape"] = TokenType.Autoescape,
        ["endautoescape"] = TokenType.EndAutoescape,
        ["recursive"] = TokenType.Recursive,
        ["scoped"] = TokenType.Scoped,
        ["and"] = TokenType.And,
        ["or"] = TokenType.Or,
        ["not"] = TokenType.Not,
        ["is"] = TokenType.Is,
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
        ["none"] = TokenType.None,
    };

    public JinjaLexer(string source, string templateName = "unknown")
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _templateName = templateName;
        _position = 0;
        _line = 1;
        _column = 1;
        _state = LexerState.Data;
    }

    public IEnumerable<Token> Tokenize()
    {
        while (!IsAtEnd)
        {
            var token = NextToken();
            if (token.Type != TokenType.Whitespace)
            {
                yield return token;
            }
        }

        yield return CreateToken(TokenType.EndOfFile, "", _position);
    }

    private Token NextToken()
    {
        return _state switch
        {
            LexerState.Data => TokenizeData(),
            LexerState.Expression => TokenizeExpression(),
            LexerState.Statement => TokenizeStatement(),
            LexerState.Comment => TokenizeComment(),
            LexerState.Raw => TokenizeRaw(),
            _ => throw new InvalidOperationException($"Unknown lexer state: {_state}")
        };
    }

    private Token TokenizeData()
    {
        var start = _position;
        var startLine = _line;
        var startColumn = _column;

        // Check for template syntax
        if (Match("{{"))
        {
            _state = LexerState.Expression;
            return CreateToken(TokenType.ExpressionStart, "{{", start, startLine, startColumn);
        }

        if (Match("{%"))
        {
            _state = LexerState.Statement;
            return CreateToken(TokenType.StatementStart, "{%", start, startLine, startColumn);
        }

        if (Match("{#"))
        {
            _state = LexerState.Comment;
            return CreateToken(TokenType.CommentStart, "{#", start, startLine, startColumn);
        }

        // Read text until we hit template syntax
        var sb = new StringBuilder();
        while (!IsAtEnd && !Check("{{") && !Check("{%") && !Check("{#"))
        {
            sb.Append(Current);
            Advance();
        }

        return CreateToken(TokenType.Text, sb.ToString(), start, startLine, startColumn);
    }

    private Token TokenizeExpression()
    {
        SkipWhitespace();

        var start = _position;
        var startLine = _line;
        var startColumn = _column;

        if (Match("}}"))
        {
            _state = LexerState.Data;
            return CreateToken(TokenType.ExpressionEnd, "}}", start, startLine, startColumn);
        }

        return TokenizeExpressionContent(start, startLine, startColumn);
    }

    private Token TokenizeStatement()
    {
        SkipWhitespace();

        var start = _position;
        var startLine = _line;
        var startColumn = _column;

        // Handle whitespace control: -%} or %}
        if (Match("-%}") || Match("%}"))
        {
            var value = _source.Substring(start, _position - start);
            _state = LexerState.Data;
            return CreateToken(TokenType.StatementEnd, value, start, startLine, startColumn);
        }

        // Handle whitespace control at start: {%- (already consumed {%)
        if (Match("-"))
        {
            // Just consume the minus for whitespace control
            return TokenizeStatement();
        }

        var token = TokenizeExpressionContent(start, startLine, startColumn);

        // Check if we just parsed 'raw' keyword - switch to raw state
        if (token.Type == TokenType.Raw)
        {
            // We need to finish the current statement first
            SkipWhitespace();
            if (Match("-%}") || Match("%}"))
            {
                _state = LexerState.Raw;
                return token;
            }
        }

        return token;
    }

    private Token TokenizeComment()
    {
        var start = _position;
        var startLine = _line;
        var startColumn = _column;

        var sb = new StringBuilder();
        while (!IsAtEnd && !Check("#}"))
        {
            sb.Append(Current);
            Advance();
        }

        if (Match("#}"))
        {
            _state = LexerState.Data;
        }

        return CreateToken(TokenType.Text, sb.ToString(), start, startLine, startColumn);
    }

    private Token TokenizeRaw()
    {
        var start = _position;
        var startLine = _line;
        var startColumn = _column;

        var sb = new StringBuilder();
        while (!IsAtEnd)
        {
            // Look for {% endraw %}
            if (Check("{%"))
            {
                var savedPos = _position;
                var savedLine = _line;
                var savedColumn = _column;

                Match("{%");
                SkipWhitespace();

                if (Check("endraw"))
                {
                    // Found endraw, restore position and break
                    _position = savedPos;
                    _line = savedLine;
                    _column = savedColumn;
                    break;
                }

                // Not endraw, restore and include the {% in the raw content
                _position = savedPos;
                _line = savedLine;
                _column = savedColumn;
            }

            sb.Append(Current);
            Advance();
        }

        _state = LexerState.Data;
        return CreateToken(TokenType.Text, sb.ToString(), start, startLine, startColumn);
    }

    private Token TokenizeExpressionContent(int start, int startLine, int startColumn)
    {
        // String literal
        if (Current == '"' || Current == '\'')
        {
            return TokenizeString(start, startLine, startColumn);
        }

        // Number
        if (char.IsDigit(Current))
        {
            return TokenizeNumber(start, startLine, startColumn);
        }

        // Identifier or keyword
        if (char.IsLetter(Current) || Current == '_')
        {
            return TokenizeIdentifier(start, startLine, startColumn);
        }

        // Operators
        return TokenizeOperator(start, startLine, startColumn);
    }

    private Token TokenizeString(int start, int startLine, int startColumn)
    {
        var quote = Current;
        Advance(); // consume opening quote

        var sb = new StringBuilder();
        while (!IsAtEnd && Current != quote)
        {
            if (Current == '\\' && _position + 1 < _source.Length)
            {
                Advance();
                sb.Append(Current switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => Current
                });
            }
            else
            {
                sb.Append(Current);
            }
            Advance();
        }

        if (!IsAtEnd)
        {
            Advance(); // consume closing quote
        }

        return CreateToken(TokenType.String, sb.ToString(), start, startLine, startColumn);
    }

    private Token TokenizeNumber(int start, int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        var isFloat = false;

        while (!IsAtEnd && (char.IsDigit(Current) || Current == '.'))
        {
            if (Current == '.')
            {
                if (isFloat) break; // Second dot - stop
                if (_position + 1 < _source.Length && !char.IsDigit(_source[_position + 1]))
                {
                    break; // Dot not followed by digit - stop
                }
                isFloat = true;
            }
            sb.Append(Current);
            Advance();
        }

        return CreateToken(
            isFloat ? TokenType.Float : TokenType.Integer,
            sb.ToString(),
            start, startLine, startColumn);
    }

    private Token TokenizeIdentifier(int start, int startLine, int startColumn)
    {
        var sb = new StringBuilder();
        while (!IsAtEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
        {
            sb.Append(Current);
            Advance();
        }

        var value = sb.ToString();
        var type = Keywords.GetValueOrDefault(value.ToLowerInvariant(), TokenType.Identifier);

        // Special handling for 'super()'
        if (value == "super" && Current == '(')
        {
            Advance(); // consume (
            if (Current == ')')
            {
                Advance(); // consume )
                return CreateToken(TokenType.Super, "super()", start, startLine, startColumn);
            }
        }

        return CreateToken(type, value, start, startLine, startColumn);
    }

    private Token TokenizeOperator(int start, int startLine, int startColumn)
    {
        // Multi-character operators first
        if (Match("**")) return CreateToken(TokenType.Power, "**", start, startLine, startColumn);
        if (Match("//")) return CreateToken(TokenType.FloorDivide, "//", start, startLine, startColumn);
        if (Match("==")) return CreateToken(TokenType.DoubleEquals, "==", start, startLine, startColumn);
        if (Match("!=")) return CreateToken(TokenType.NotEquals, "!=", start, startLine, startColumn);
        if (Match("<=")) return CreateToken(TokenType.LessThanOrEqual, "<=", start, startLine, startColumn);
        if (Match(">=")) return CreateToken(TokenType.GreaterThanOrEqual, ">=", start, startLine, startColumn);

        // Single character operators
        var c = Current;
        Advance();

        var type = c switch
        {
            '|' => TokenType.Pipe,
            '.' => TokenType.Dot,
            ',' => TokenType.Comma,
            ':' => TokenType.Colon,
            '=' => TokenType.Equals,
            '<' => TokenType.LessThan,
            '>' => TokenType.GreaterThan,
            '+' => TokenType.Plus,
            '-' => TokenType.Minus,
            '*' => TokenType.Multiply,
            '/' => TokenType.Divide,
            '%' => TokenType.Modulo,
            '~' => TokenType.Tilde,
            '(' => TokenType.OpenParen,
            ')' => TokenType.CloseParen,
            '[' => TokenType.OpenBracket,
            ']' => TokenType.CloseBracket,
            '{' => TokenType.OpenBrace,
            '}' => TokenType.CloseBrace,
            _ => TokenType.Unknown
        };

        return CreateToken(type, c.ToString(), start, startLine, startColumn);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd && char.IsWhiteSpace(Current))
        {
            Advance();
        }
    }

    private bool IsAtEnd => _position >= _source.Length;

    private char Current => IsAtEnd ? '\0' : _source[_position];

    private char Peek(int offset = 1) =>
        _position + offset < _source.Length ? _source[_position + offset] : '\0';

    private bool Check(string text)
    {
        if (_position + text.Length > _source.Length) return false;
        return _source.Substring(_position, text.Length) == text;
    }

    private bool Match(string text)
    {
        if (!Check(text)) return false;

        for (int i = 0; i < text.Length; i++)
        {
            Advance();
        }
        return true;
    }

    private void Advance()
    {
        if (!IsAtEnd)
        {
            if (Current == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }
    }

    private Token CreateToken(TokenType type, string value, int start, int? line = null, int? column = null)
    {
        return new Token(
            type,
            value,
            start,
            line ?? _line,
            column ?? _column,
            _position - start);
    }
}
