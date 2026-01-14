namespace JinjaCompiler.Core.Ast;

/// <summary>
/// Base class for all AST nodes in the Jinja template.
/// </summary>
public abstract record TemplateNode
{
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }

    /// <summary>
    /// Accepts a visitor for the visitor pattern implementation.
    /// </summary>
    public abstract T Accept<T>(INodeVisitor<T> visitor);
}

/// <summary>
/// Represents the root of a parsed template.
/// </summary>
public record TemplateRoot(string TemplateName, IReadOnlyList<TemplateNode> Children) : TemplateNode
{
    public ExtendsNode? Extends => Children.OfType<ExtendsNode>().FirstOrDefault();

    public IEnumerable<BlockNode> Blocks => Children.OfType<BlockNode>();

    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitRoot(this);
}

/// <summary>
/// Represents raw text content (HTML, CSS, JS, etc.)
/// </summary>
public record TextNode(string Content) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitText(this);
}

/// <summary>
/// Represents a variable expression: {{ variable }} or {{ object.property }}
/// </summary>
public record VariableNode(string Expression, IReadOnlyList<FilterApplication> Filters) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitVariable(this);
}

/// <summary>
/// Represents a filter applied to a variable: {{ name|upper }}
/// </summary>
public record FilterApplication(string FilterName, IReadOnlyList<string> Arguments);

/// <summary>
/// Represents {% extends "base.html" %}
/// </summary>
public record ExtendsNode(string ParentTemplatePath) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitExtends(this);
}

/// <summary>
/// Represents {% block name %}...{% endblock %}
/// </summary>
public record BlockNode(string Name, IReadOnlyList<TemplateNode> Children, bool Scoped = false) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitBlock(this);
}

/// <summary>
/// Represents {% include "partial.html" %} with optional context
/// </summary>
public record IncludeNode(
    string TemplatePath,
    bool IgnoreMissing = false,
    bool WithContext = true,
    IReadOnlyDictionary<string, string>? ContextOverrides = null) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitInclude(this);
}

/// <summary>
/// Represents {% if condition %}...{% elif %}...{% else %}...{% endif %}
/// </summary>
public record IfNode(
    string Condition,
    IReadOnlyList<TemplateNode> ThenBranch,
    IReadOnlyList<ElseIfBranch> ElseIfBranches,
    IReadOnlyList<TemplateNode>? ElseBranch) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitIf(this);
}

public record ElseIfBranch(string Condition, IReadOnlyList<TemplateNode> Body);

/// <summary>
/// Represents {% for item in collection %}...{% else %}...{% endfor %}
/// </summary>
public record ForNode(
    string LoopVariable,
    string? IndexVariable,
    string Collection,
    IReadOnlyList<TemplateNode> Body,
    IReadOnlyList<TemplateNode>? ElseBody = null,
    bool Recursive = false) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitFor(this);
}

/// <summary>
/// Represents {% set variable = value %}
/// </summary>
public record SetNode(string VariableName, string Expression) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitSet(this);
}

/// <summary>
/// Represents {% macro name(args) %}...{% endmacro %}
/// </summary>
public record MacroNode(
    string Name,
    IReadOnlyList<MacroParameter> Parameters,
    IReadOnlyList<TemplateNode> Body) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitMacro(this);
}

public record MacroParameter(string Name, string? DefaultValue = null);

/// <summary>
/// Represents a macro call: {{ macro_name(args) }}
/// </summary>
public record MacroCallNode(string MacroName, IReadOnlyList<string> Arguments) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitMacroCall(this);
}

/// <summary>
/// Represents {% import "macros.html" as macros %}
/// </summary>
public record ImportNode(string TemplatePath, string? Alias, IReadOnlyList<string>? Names = null) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitImport(this);
}

/// <summary>
/// Represents {% from "macros.html" import macro1, macro2 %}
/// </summary>
public record FromImportNode(
    string TemplatePath,
    IReadOnlyList<ImportedName> ImportedNames) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitFromImport(this);
}

public record ImportedName(string Name, string? Alias = null);

/// <summary>
/// Represents a comment: {# comment #}
/// </summary>
public record CommentNode(string Content) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitComment(this);
}

/// <summary>
/// Represents {{ super() }} call within a block
/// </summary>
public record SuperNode : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitSuper(this);
}

/// <summary>
/// Represents {% raw %}...{% endraw %}
/// </summary>
public record RawNode(string Content) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitRaw(this);
}

/// <summary>
/// Represents {% with var = value %}...{% endwith %}
/// </summary>
public record WithNode(
    IReadOnlyDictionary<string, string> Bindings,
    IReadOnlyList<TemplateNode> Body) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitWith(this);
}

/// <summary>
/// Represents {% autoescape true/false %}...{% endautoescape %}
/// </summary>
public record AutoescapeNode(bool Enabled, IReadOnlyList<TemplateNode> Body) : TemplateNode
{
    public override T Accept<T>(INodeVisitor<T> visitor) => visitor.VisitAutoescape(this);
}
