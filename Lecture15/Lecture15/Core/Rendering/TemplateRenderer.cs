using System.Net;
using System.Text;
using JinjaCompiler.Core.Ast;
using JinjaCompiler.Core.Context;
using JinjaCompiler.Core.Exceptions;
using JinjaCompiler.Core.Expressions;
using JinjaCompiler.Core.Filters;
using JinjaCompiler.Core.Loading;
using JinjaCompiler.Core.Parsing;
using JinjaCompiler.Core.Resolution;

namespace JinjaCompiler.Core.Rendering;

/// <summary>
/// Configuration options for the template renderer.
/// </summary>
public record RenderOptions
{
    /// <summary>
    /// Whether to auto-escape HTML in variable output.
    /// </summary>
    public bool AutoEscape { get; init; } = true;

    /// <summary>
    /// How to handle missing variables.
    /// </summary>
    public MissingVariableBehavior MissingVariableBehavior { get; init; } = MissingVariableBehavior.EmptyString;

    /// <summary>
    /// Whether to trim whitespace from blocks.
    /// </summary>
    public bool TrimBlocks { get; init; } = false;

    /// <summary>
    /// Whether to strip leading whitespace from lines.
    /// </summary>
    public bool LstripBlocks { get; init; } = false;

    /// <summary>
    /// Whether to keep trailing newlines.
    /// </summary>
    public bool KeepTrailingNewline { get; init; } = true;
}

public enum MissingVariableBehavior
{
    /// <summary>
    /// Output an empty string for missing variables.
    /// </summary>
    EmptyString,

    /// <summary>
    /// Throw an exception for missing variables.
    /// </summary>
    ThrowException,

    /// <summary>
    /// Output a placeholder showing the variable name.
    /// </summary>
    ShowPlaceholder,

    /// <summary>
    /// Output undefined (kept as-is for debugging).
    /// </summary>
    Undefined
}

/// <summary>
/// Renders resolved templates to HTML strings.
/// </summary>
public class TemplateRenderer
{
    private readonly ITemplateLoader _loader;
    private readonly TemplateCache _cache;
    private readonly InheritanceResolver _inheritanceResolver;
    private readonly FilterRegistry _filters;
    private readonly ExpressionEvaluator _evaluator;
    private readonly RenderOptions _options;
    private readonly Dictionary<string, MacroNode> _macros = new(StringComparer.OrdinalIgnoreCase);

    public TemplateRenderer(
        ITemplateLoader loader,
        RenderOptions? options = null,
        FilterRegistry? filters = null)
    {
        _loader = loader;
        _options = options ?? new RenderOptions();
        _filters = filters ?? new FilterRegistry();
        _evaluator = new ExpressionEvaluator(_filters);
        _cache = new TemplateCache(loader);
        _inheritanceResolver = new InheritanceResolver(_cache);
    }

    /// <summary>
    /// Renders a template with the given context.
    /// </summary>
    public string Render(string templatePath, RenderContext context)
    {
        var resolved = _inheritanceResolver.Resolve(templatePath);
        var sb = new StringBuilder();

        // Collect macros first
        CollectMacros(resolved.Nodes, context);

        RenderNodes(resolved.Nodes, context, sb, _options.AutoEscape);

        var result = sb.ToString();

        if (!_options.KeepTrailingNewline)
        {
            result = result.TrimEnd('\n', '\r');
        }

        return result;
    }

    private void CollectMacros(IEnumerable<TemplateNode> nodes, RenderContext context)
    {
        foreach (var node in nodes)
        {
            if (node is MacroNode macro)
            {
                _macros[macro.Name] = macro;
            }
            else if (node is BlockNode block)
            {
                CollectMacros(block.Children, context);
            }
        }
    }

    private void RenderNodes(IEnumerable<TemplateNode> nodes, RenderContext context, StringBuilder sb, bool autoEscape)
    {
        foreach (var node in nodes)
        {
            RenderNode(node, context, sb, autoEscape);
        }
    }

    private void RenderNode(TemplateNode node, RenderContext context, StringBuilder sb, bool autoEscape)
    {
        switch (node)
        {
            case TextNode text:
                sb.Append(text.Content);
                break;

            case VariableNode variable:
                RenderVariable(variable, context, sb, autoEscape);
                break;

            case BlockNode block:
                RenderNodes(block.Children, context, sb, autoEscape);
                break;

            case IncludeNode include:
                RenderInclude(include, context, sb, autoEscape);
                break;

            case IfNode ifNode:
                RenderIf(ifNode, context, sb, autoEscape);
                break;

            case ForNode forNode:
                RenderFor(forNode, context, sb, autoEscape);
                break;

            case SetNode setNode:
                RenderSet(setNode, context);
                break;

            case WithNode withNode:
                RenderWith(withNode, context, sb, autoEscape);
                break;

            case MacroNode:
                // Macros are collected but not rendered inline
                break;

            case RawNode raw:
                sb.Append(raw.Content);
                break;

            case AutoescapeNode autoescapeNode:
                RenderNodes(autoescapeNode.Body, context, sb, autoescapeNode.Enabled);
                break;

            case CommentNode:
                // Comments are not rendered
                break;

            case ImportNode importNode:
                HandleImport(importNode, context);
                break;

            case FromImportNode fromImport:
                HandleFromImport(fromImport, context);
                break;

            case ExtendsNode:
                // Already handled by inheritance resolver
                break;

            case SuperNode:
                // Handled within block resolution
                break;
        }
    }

    private void RenderVariable(VariableNode variable, RenderContext context, StringBuilder sb, bool autoEscape)
    {
        var value = _evaluator.Evaluate(variable.Expression, context);

        // Apply filters
        foreach (var filter in variable.Filters)
        {
            var args = filter.Arguments
                .Select(a => _evaluator.Evaluate(a, context))
                .ToArray();
            value = _filters.Apply(filter.FilterName, value, args!);
        }

        // Handle missing value
        if (value == null)
        {
            value = _options.MissingVariableBehavior switch
            {
                MissingVariableBehavior.ThrowException =>
                    throw new MissingVariableException(variable.Expression),
                MissingVariableBehavior.ShowPlaceholder => $"{{{{ {variable.Expression} }}}}",
                MissingVariableBehavior.Undefined => "undefined",
                _ => ""
            };
        }

        // Convert to string and handle escaping
        var output = value.ToString() ?? "";

        // Safe strings bypass escaping
        if (value is SafeString safe)
        {
            sb.Append(safe.Value);
        }
        else if (autoEscape)
        {
            sb.Append(WebUtility.HtmlEncode(output));
        }
        else
        {
            sb.Append(output);
        }
    }

    private void RenderInclude(IncludeNode include, RenderContext context, StringBuilder sb, bool autoEscape)
    {
        try
        {
            if (!_loader.TemplateExists(include.TemplatePath))
            {
                if (include.IgnoreMissing)
                {
                    return;
                }
                throw new TemplateNotFoundException(include.TemplatePath);
            }

            var template = _cache.GetTemplate(include.TemplatePath);

            // Create context for the included template
            var includeContext = include.WithContext ? context : new RenderContext();

            // Apply context overrides
            if (include.ContextOverrides != null)
            {
                foreach (var (key, valueExpr) in include.ContextOverrides)
                {
                    var value = _evaluator.Evaluate(valueExpr, context);
                    includeContext.Set(key, value);
                }
            }

            RenderNodes(template.Children, includeContext, sb, autoEscape);
        }
        catch (TemplateNotFoundException) when (include.IgnoreMissing)
        {
            // Silently ignore missing templates when ignore_missing is set
        }
    }

    private void RenderIf(IfNode ifNode, RenderContext context, StringBuilder sb, bool autoEscape)
    {
        // Evaluate main condition
        if (_evaluator.EvaluateCondition(ifNode.Condition, context))
        {
            RenderNodes(ifNode.ThenBranch, context, sb, autoEscape);
            return;
        }

        // Evaluate elif branches
        foreach (var elseIf in ifNode.ElseIfBranches)
        {
            if (_evaluator.EvaluateCondition(elseIf.Condition, context))
            {
                RenderNodes(elseIf.Body, context, sb, autoEscape);
                return;
            }
        }

        // Render else branch if present
        if (ifNode.ElseBranch != null)
        {
            RenderNodes(ifNode.ElseBranch, context, sb, autoEscape);
        }
    }

    private void RenderFor(ForNode forNode, RenderContext context, StringBuilder sb, bool autoEscape)
    {
        var collectionValue = _evaluator.Evaluate(forNode.Collection, context);

        if (collectionValue is not System.Collections.IEnumerable enumerable || 
            collectionValue is string)
        {
            // Collection is empty or invalid
            if (forNode.ElseBody != null)
            {
                RenderNodes(forNode.ElseBody, context, sb, autoEscape);
            }
            return;
        }

        var items = enumerable.Cast<object?>().ToList();

        if (items.Count == 0)
        {
            if (forNode.ElseBody != null)
            {
                RenderNodes(forNode.ElseBody, context, sb, autoEscape);
            }
            return;
        }

        // Push a new scope for the loop
        context.PushScope();

        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var loopContext = new LoopContext(i, items.Count);

                // Set loop variables
                context.Set(forNode.LoopVariable, item);
                context.Set("loop", loopContext.ToDictionary());

                // Set index variable if tuple unpacking
                if (forNode.IndexVariable != null)
                {
                    context.Set(forNode.IndexVariable, i);
                }

                // Handle tuple unpacking for dicts
                if (item is KeyValuePair<string, object?> kvp)
                {
                    // If looping over dict.items(), unpack key/value
                    if (forNode.IndexVariable != null)
                    {
                        context.Set(forNode.IndexVariable, kvp.Key);
                        context.Set(forNode.LoopVariable, kvp.Value);
                    }
                }
                else if (item is IList<object?> tuple && forNode.IndexVariable != null)
                {
                    // Handle list tuple unpacking
                    if (tuple.Count >= 2)
                    {
                        context.Set(forNode.IndexVariable, tuple[0]);
                        context.Set(forNode.LoopVariable, tuple[1]);
                    }
                }

                RenderNodes(forNode.Body, context, sb, autoEscape);
            }
        }
        finally
        {
            context.PopScope();
        }
    }

    private void RenderSet(SetNode setNode, RenderContext context)
    {
        var value = _evaluator.Evaluate(setNode.Expression, context);
        context.Set(setNode.VariableName, value);
    }

    private void RenderWith(WithNode withNode, RenderContext context, StringBuilder sb, bool autoEscape)
    {
        context.PushScope();

        try
        {
            // Set the with bindings
            foreach (var (name, expression) in withNode.Bindings)
            {
                var value = _evaluator.Evaluate(expression, context);
                context.Set(name, value);
            }

            RenderNodes(withNode.Body, context, sb, autoEscape);
        }
        finally
        {
            context.PopScope();
        }
    }

    private void HandleImport(ImportNode importNode, RenderContext context)
    {
        var template = _cache.GetTemplate(importNode.TemplatePath);

        // Collect macros from the imported template
        var importedMacros = new Dictionary<string, MacroNode>(StringComparer.OrdinalIgnoreCase);
        CollectMacrosFromNodes(template.Children, importedMacros);

        if (importNode.Alias != null)
        {
            // Import as namespace: {% import "macros.html" as m %}
            var ns = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, macro) in importedMacros)
            {
                ns[name] = new MacroReference(macro, this, context);
            }
            context.Set(importNode.Alias, ns);
        }
    }

    private void HandleFromImport(FromImportNode fromImport, RenderContext context)
    {
        var template = _cache.GetTemplate(fromImport.TemplatePath);

        var importedMacros = new Dictionary<string, MacroNode>(StringComparer.OrdinalIgnoreCase);
        CollectMacrosFromNodes(template.Children, importedMacros);

        foreach (var imported in fromImport.ImportedNames)
        {
            if (importedMacros.TryGetValue(imported.Name, out var macro))
            {
                var name = imported.Alias ?? imported.Name;
                context.Set(name, new MacroReference(macro, this, context));
            }
        }
    }

    private void CollectMacrosFromNodes(IEnumerable<TemplateNode> nodes, Dictionary<string, MacroNode> macros)
    {
        foreach (var node in nodes)
        {
            if (node is MacroNode macro)
            {
                macros[macro.Name] = macro;
            }
        }
    }

    /// <summary>
    /// Renders a macro with arguments.
    /// </summary>
    internal string RenderMacro(MacroNode macro, object?[] args, RenderContext parentContext)
    {
        var context = new RenderContext();

        // Set parameter values
        for (int i = 0; i < macro.Parameters.Count; i++)
        {
            var param = macro.Parameters[i];
            object? value;

            if (i < args.Length)
            {
                value = args[i];
            }
            else if (param.DefaultValue != null)
            {
                value = _evaluator.Evaluate(param.DefaultValue, parentContext);
            }
            else
            {
                value = null;
            }

            context.Set(param.Name, value);
        }

        // Set special macro variables
        context.Set("varargs", args.Skip(macro.Parameters.Count).ToList());
        context.Set("kwargs", new Dictionary<string, object?>());

        var sb = new StringBuilder();
        RenderNodes(macro.Body, context, sb, _options.AutoEscape);
        return sb.ToString();
    }
}

/// <summary>
/// Reference to a macro that can be called.
/// </summary>
public class MacroReference
{
    private readonly MacroNode _macro;
    private readonly TemplateRenderer _renderer;
    private readonly RenderContext _context;

    public MacroReference(MacroNode macro, TemplateRenderer renderer, RenderContext context)
    {
        _macro = macro;
        _renderer = renderer;
        _context = context;
    }

    public string Call(params object?[] args)
    {
        return _renderer.RenderMacro(_macro, args, _context);
    }

    public override string ToString()
    {
        return $"<macro '{_macro.Name}'>";
    }
}
