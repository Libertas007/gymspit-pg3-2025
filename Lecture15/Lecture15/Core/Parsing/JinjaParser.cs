using System.Text;
using JinjaCompiler.Core.Ast;
using JinjaCompiler.Core.Exceptions;

namespace JinjaCompiler.Core.Parsing;

/// <summary>
/// Parser for converting Jinja2 tokens into an AST.
/// </summary>
public class JinjaParser
{
    private readonly List<Token> _tokens;
    private readonly string _templateName;
    private int _current;

    public JinjaParser(IEnumerable<Token> tokens, string templateName)
    {
        _tokens = tokens.ToList();
        _templateName = templateName;
        _current = 0;
    }

    public TemplateRoot Parse()
    {
        var children = new List<TemplateNode>();

        while (!IsAtEnd)
        {
            var node = ParseNode();
            if (node != null)
            {
                children.Add(node);
            }
        }

        return new TemplateRoot(_templateName, children);
    }

    private TemplateNode? ParseNode()
    {
        if (Check(TokenType.Text))
        {
            return ParseText();
        }

        if (Check(TokenType.ExpressionStart))
        {
            return ParseExpression();
        }

        if (Check(TokenType.StatementStart))
        {
            return ParseStatement();
        }

        if (Check(TokenType.CommentStart))
        {
            return ParseComment();
        }

        // Skip unknown tokens
        Advance();
        return null;
    }

    private TextNode ParseText()
    {
        var token = Consume(TokenType.Text, "Expected text");
        return new TextNode(token.Value)
        {
            StartPosition = token.Position,
            EndPosition = token.EndPosition,
            Line = token.Line,
            Column = token.Column
        };
    }

    private TemplateNode ParseExpression()
    {
        var startToken = Consume(TokenType.ExpressionStart, "Expected '{{'");

        // Check for super() call
        if (Check(TokenType.Super))
        {
            Advance();
            Consume(TokenType.ExpressionEnd, "Expected '}}'");
            return new SuperNode
            {
                StartPosition = startToken.Position,
                Line = startToken.Line,
                Column = startToken.Column
            };
        }

        var expression = ParseExpressionContent();
        var filters = ParseFilters();

        Consume(TokenType.ExpressionEnd, "Expected '}}'");

        return new VariableNode(expression, filters)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private string ParseExpressionContent()
    {
        var sb = new StringBuilder();

        while (!IsAtEnd && !Check(TokenType.ExpressionEnd) && !Check(TokenType.Pipe) && !Check(TokenType.StatementEnd))
        {
            var token = Current;

            // Handle different token types appropriately
            if (token.Type == TokenType.String)
            {
                sb.Append('"').Append(token.Value).Append('"');
            }
            else
            {
                sb.Append(token.Value);
            }

            Advance();
        }

        return sb.ToString().Trim();
    }

    private IReadOnlyList<FilterApplication> ParseFilters()
    {
        var filters = new List<FilterApplication>();

        while (Match(TokenType.Pipe))
        {
            var filterName = Consume(TokenType.Identifier, "Expected filter name").Value;
            var arguments = new List<string>();

            if (Match(TokenType.OpenParen))
            {
                // Parse filter arguments
                if (!Check(TokenType.CloseParen))
                {
                    do
                    {
                        var arg = ParseFilterArgument();
                        arguments.Add(arg);
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.CloseParen, "Expected ')' after filter arguments");
            }

            filters.Add(new FilterApplication(filterName, arguments));
        }

        return filters;
    }

    private string ParseFilterArgument()
    {
        var sb = new StringBuilder();
        int parenDepth = 0;

        while (!IsAtEnd)
        {
            if (Check(TokenType.CloseParen) && parenDepth == 0) break;
            if (Check(TokenType.Comma) && parenDepth == 0) break;

            var token = Current;
            if (token.Type == TokenType.OpenParen) parenDepth++;
            if (token.Type == TokenType.CloseParen) parenDepth--;

            if (token.Type == TokenType.String)
            {
                sb.Append('"').Append(token.Value).Append('"');
            }
            else
            {
                sb.Append(token.Value);
            }

            Advance();
        }

        return sb.ToString().Trim();
    }

    private TemplateNode ParseStatement()
    {
        var startToken = Consume(TokenType.StatementStart, "Expected '{%'");

        var node = Current.Type switch
        {
            TokenType.Extends => ParseExtends(startToken),
            TokenType.Block => ParseBlock(startToken),
            TokenType.Include => ParseInclude(startToken),
            TokenType.Import => ParseImport(startToken),
            TokenType.From => ParseFromImport(startToken),
            TokenType.If => ParseIf(startToken),
            TokenType.For => ParseFor(startToken),
            TokenType.Set => ParseSet(startToken),
            TokenType.Macro => ParseMacro(startToken),
            TokenType.With => ParseWith(startToken),
            TokenType.Autoescape => ParseAutoescape(startToken),
            TokenType.Raw => ParseRaw(startToken),
            _ => ParseUnknownStatement(startToken)
        };

        return node;
    }

    private ExtendsNode ParseExtends(Token startToken)
    {
        Consume(TokenType.Extends, "Expected 'extends'");
        var templatePath = Consume(TokenType.String, "Expected template path").Value;
        ConsumeStatementEnd();

        return new ExtendsNode(templatePath)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private BlockNode ParseBlock(Token startToken)
    {
        Consume(TokenType.Block, "Expected 'block'");
        var blockName = Consume(TokenType.Identifier, "Expected block name").Value;
        var scoped = Match(TokenType.Scoped);
        ConsumeStatementEnd();

        var children = new List<TemplateNode>();
        while (!IsAtEnd && !CheckStatement(TokenType.EndBlock))
        {
            var node = ParseNode();
            if (node != null)
            {
                children.Add(node);
            }
        }

        ConsumeStatement(TokenType.EndBlock, "Expected 'endblock'");
        // Optional block name after endblock
        Match(TokenType.Identifier);
        ConsumeStatementEnd();

        return new BlockNode(blockName, children, scoped)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private IncludeNode ParseInclude(Token startToken)
    {
        Consume(TokenType.Include, "Expected 'include'");
        var templatePath = Consume(TokenType.String, "Expected template path").Value;

        var ignoreMissing = false;
        var withContext = true;

        // Parse modifiers
        while (!Check(TokenType.StatementEnd))
        {
            if (Match(TokenType.IgnoreMissing))
            {
                ignoreMissing = true;
            }
            else if (Match(TokenType.With))
            {
                Match(TokenType.Context);
                withContext = true;
            }
            else if (Match(TokenType.Without))
            {
                Match(TokenType.Context);
                withContext = false;
            }
            else
            {
                Advance(); // Skip unknown modifier
            }
        }

        ConsumeStatementEnd();

        return new IncludeNode(templatePath, ignoreMissing, withContext)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private ImportNode ParseImport(Token startToken)
    {
        Consume(TokenType.Import, "Expected 'import'");
        var templatePath = Consume(TokenType.String, "Expected template path").Value;
        string? alias = null;

        if (Match(TokenType.As))
        {
            alias = Consume(TokenType.Identifier, "Expected alias name").Value;
        }

        ConsumeStatementEnd();

        return new ImportNode(templatePath, alias)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private FromImportNode ParseFromImport(Token startToken)
    {
        Consume(TokenType.From, "Expected 'from'");
        var templatePath = Consume(TokenType.String, "Expected template path").Value;
        Consume(TokenType.Import, "Expected 'import'");

        var importedNames = new List<ImportedName>();
        do
        {
            var name = Consume(TokenType.Identifier, "Expected imported name").Value;
            string? alias = null;
            if (Match(TokenType.As))
            {
                alias = Consume(TokenType.Identifier, "Expected alias").Value;
            }
            importedNames.Add(new ImportedName(name, alias));
        } while (Match(TokenType.Comma));

        ConsumeStatementEnd();

        return new FromImportNode(templatePath, importedNames)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private IfNode ParseIf(Token startToken)
    {
        Consume(TokenType.If, "Expected 'if'");
        var condition = ParseConditionExpression();
        ConsumeStatementEnd();

        var thenBranch = new List<TemplateNode>();
        while (!IsAtEnd && !CheckStatement(TokenType.ElseIf) && !CheckStatement(TokenType.Else) && !CheckStatement(TokenType.EndIf))
        {
            var node = ParseNode();
            if (node != null)
            {
                thenBranch.Add(node);
            }
        }

        var elseIfBranches = new List<ElseIfBranch>();
        while (CheckStatement(TokenType.ElseIf))
        {
            ConsumeStatementStart();
            Consume(TokenType.ElseIf, "Expected 'elif'");
            var elifCondition = ParseConditionExpression();
            ConsumeStatementEnd();

            var elifBody = new List<TemplateNode>();
            while (!IsAtEnd && !CheckStatement(TokenType.ElseIf) && !CheckStatement(TokenType.Else) && !CheckStatement(TokenType.EndIf))
            {
                var node = ParseNode();
                if (node != null)
                {
                    elifBody.Add(node);
                }
            }

            elseIfBranches.Add(new ElseIfBranch(elifCondition, elifBody));
        }

        List<TemplateNode>? elseBranch = null;
        if (CheckStatement(TokenType.Else))
        {
            ConsumeStatementStart();
            Consume(TokenType.Else, "Expected 'else'");
            ConsumeStatementEnd();

            elseBranch = new List<TemplateNode>();
            while (!IsAtEnd && !CheckStatement(TokenType.EndIf))
            {
                var node = ParseNode();
                if (node != null)
                {
                    elseBranch.Add(node);
                }
            }
        }

        ConsumeStatement(TokenType.EndIf, "Expected 'endif'");
        ConsumeStatementEnd();

        return new IfNode(condition, thenBranch, elseIfBranches, elseBranch)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private ForNode ParseFor(Token startToken)
    {
        Consume(TokenType.For, "Expected 'for'");

        var loopVariable = Consume(TokenType.Identifier, "Expected loop variable").Value;
        string? indexVariable = null;

        if (Match(TokenType.Comma))
        {
            indexVariable = loopVariable;
            loopVariable = Consume(TokenType.Identifier, "Expected loop variable").Value;
        }

        Consume(TokenType.In, "Expected 'in'");
        var collection = ParseConditionExpression();
        var recursive = Match(TokenType.Recursive);
        ConsumeStatementEnd();

        var body = new List<TemplateNode>();
        while (!IsAtEnd && !CheckStatement(TokenType.EndFor) && !CheckStatement(TokenType.Else))
        {
            var node = ParseNode();
            if (node != null)
            {
                body.Add(node);
            }
        }

        List<TemplateNode>? elseBody = null;
        if (CheckStatement(TokenType.Else))
        {
            ConsumeStatementStart();
            Consume(TokenType.Else, "Expected 'else'");
            ConsumeStatementEnd();

            elseBody = new List<TemplateNode>();
            while (!IsAtEnd && !CheckStatement(TokenType.EndFor))
            {
                var node = ParseNode();
                if (node != null)
                {
                    elseBody.Add(node);
                }
            }
        }

        ConsumeStatement(TokenType.EndFor, "Expected 'endfor'");
        ConsumeStatementEnd();

        return new ForNode(loopVariable, indexVariable, collection, body, elseBody, recursive)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private SetNode ParseSet(Token startToken)
    {
        Consume(TokenType.Set, "Expected 'set'");
        var variableName = Consume(TokenType.Identifier, "Expected variable name").Value;
        Consume(TokenType.Equals, "Expected '='");
        var expression = ParseConditionExpression();
        ConsumeStatementEnd();

        return new SetNode(variableName, expression)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private MacroNode ParseMacro(Token startToken)
    {
        Consume(TokenType.Macro, "Expected 'macro'");
        var macroName = Consume(TokenType.Identifier, "Expected macro name").Value;

        var parameters = new List<MacroParameter>();
        if (Match(TokenType.OpenParen))
        {
            if (!Check(TokenType.CloseParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name").Value;
                    string? defaultValue = null;
                    if (Match(TokenType.Equals))
                    {
                        defaultValue = ParseFilterArgument();
                    }
                    parameters.Add(new MacroParameter(paramName, defaultValue));
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.CloseParen, "Expected ')'");
        }

        ConsumeStatementEnd();

        var body = new List<TemplateNode>();
        while (!IsAtEnd && !CheckStatement(TokenType.EndMacro))
        {
            var node = ParseNode();
            if (node != null)
            {
                body.Add(node);
            }
        }

        ConsumeStatement(TokenType.EndMacro, "Expected 'endmacro'");
        ConsumeStatementEnd();

        return new MacroNode(macroName, parameters, body)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private WithNode ParseWith(Token startToken)
    {
        Consume(TokenType.With, "Expected 'with'");

        var bindings = new Dictionary<string, string>();
        do
        {
            var varName = Consume(TokenType.Identifier, "Expected variable name").Value;
            Consume(TokenType.Equals, "Expected '='");
            var value = ParseConditionExpression();
            bindings[varName] = value;
        } while (Match(TokenType.Comma));

        ConsumeStatementEnd();

        var body = new List<TemplateNode>();
        while (!IsAtEnd && !CheckStatement(TokenType.EndWith))
        {
            var node = ParseNode();
            if (node != null)
            {
                body.Add(node);
            }
        }

        ConsumeStatement(TokenType.EndWith, "Expected 'endwith'");
        ConsumeStatementEnd();

        return new WithNode(bindings, body)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private AutoescapeNode ParseAutoescape(Token startToken)
    {
        Consume(TokenType.Autoescape, "Expected 'autoescape'");

        var enabled = true;
        if (Check(TokenType.True))
        {
            Advance();
            enabled = true;
        }
        else if (Check(TokenType.False))
        {
            Advance();
            enabled = false;
        }

        ConsumeStatementEnd();

        var body = new List<TemplateNode>();
        while (!IsAtEnd && !CheckStatement(TokenType.EndAutoescape))
        {
            var node = ParseNode();
            if (node != null)
            {
                body.Add(node);
            }
        }

        ConsumeStatement(TokenType.EndAutoescape, "Expected 'endautoescape'");
        ConsumeStatementEnd();

        return new AutoescapeNode(enabled, body)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private RawNode ParseRaw(Token startToken)
    {
        Consume(TokenType.Raw, "Expected 'raw'");
        ConsumeStatementEnd();

        // Collect all text until endraw
        var content = new StringBuilder();
        while (!IsAtEnd && !CheckStatement(TokenType.EndRaw))
        {
            if (Check(TokenType.Text))
            {
                content.Append(Current.Value);
            }
            Advance();
        }

        ConsumeStatement(TokenType.EndRaw, "Expected 'endraw'");
        ConsumeStatementEnd();

        return new RawNode(content.ToString())
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private TemplateNode ParseUnknownStatement(Token startToken)
    {
        // Skip to statement end
        while (!IsAtEnd && !Check(TokenType.StatementEnd))
        {
            Advance();
        }
        ConsumeStatementEnd();

        return new TextNode("")
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private CommentNode ParseComment()
    {
        var startToken = Consume(TokenType.CommentStart, "Expected '{#'");
        var content = Check(TokenType.Text) ? Consume(TokenType.Text, "Expected comment content").Value : "";

        // Comment end is consumed by lexer state change
        return new CommentNode(content)
        {
            StartPosition = startToken.Position,
            Line = startToken.Line,
            Column = startToken.Column
        };
    }

    private string ParseConditionExpression()
    {
        var sb = new StringBuilder();
        int parenDepth = 0;

        while (!IsAtEnd && !Check(TokenType.StatementEnd))
        {
            var token = Current;

            if (token.Type == TokenType.OpenParen) parenDepth++;
            if (token.Type == TokenType.CloseParen)
            {
                if (parenDepth == 0) break;
                parenDepth--;
            }

            // Stop at certain keywords when not in parens
            if (parenDepth == 0)
            {
                if (token.Type == TokenType.Recursive ||
                    token.Type == TokenType.Scoped)
                {
                    break;
                }
            }

            if (token.Type == TokenType.String)
            {
                sb.Append('"').Append(token.Value).Append('"');
            }
            else
            {
                sb.Append(token.Value);
            }

            Advance();
        }

        return sb.ToString().Trim();
    }

    #region Helper Methods

    private bool IsAtEnd => _current >= _tokens.Count || Current.Type == TokenType.EndOfFile;

    private Token Current => _tokens[_current];

    private Token Previous => _tokens[_current - 1];

    private bool Check(TokenType type)
    {
        if (IsAtEnd) return false;
        return Current.Type == type;
    }

    private bool CheckStatement(TokenType statementType)
    {
        if (!Check(TokenType.StatementStart)) return false;
        if (_current + 1 >= _tokens.Count) return false;
        return _tokens[_current + 1].Type == statementType;
    }

    private bool Match(TokenType type)
    {
        if (Check(type))
        {
            Advance();
            return true;
        }
        return false;
    }

    private Token Advance()
    {
        if (!IsAtEnd) _current++;
        return Previous;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();

        throw new TemplateParseException(
            $"{message}, got {Current.Type} ('{Current.Value}')",
            _templateName,
            Current.Line,
            Current.Column);
    }

    private void ConsumeStatementStart()
    {
        Consume(TokenType.StatementStart, "Expected '{%'");
    }

    private void ConsumeStatementEnd()
    {
        Consume(TokenType.StatementEnd, "Expected '%}'");
    }

    private void ConsumeStatement(TokenType keywordType, string message)
    {
        ConsumeStatementStart();
        Consume(keywordType, message);
    }

    #endregion
}
