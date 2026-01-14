namespace JinjaCompiler.Core.Exceptions;

/// <summary>
/// Base exception for all Jinja compiler errors.
/// </summary>
public class JinjaCompilerException : Exception
{
    public string? TemplateName { get; }
    public int? Line { get; }
    public int? Column { get; }

    public JinjaCompilerException(string message) : base(message)
    {
    }

    public JinjaCompilerException(string message, string? templateName, int? line = null, int? column = null)
        : base(FormatMessage(message, templateName, line, column))
    {
        TemplateName = templateName;
        Line = line;
        Column = column;
    }

    public JinjaCompilerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private static string FormatMessage(string message, string? templateName, int? line, int? column)
    {
        var location = templateName ?? "unknown";
        if (line.HasValue)
        {
            location += $":{line}";
            if (column.HasValue)
            {
                location += $":{column}";
            }
        }
        return $"[{location}] {message}";
    }
}

/// <summary>
/// Exception thrown when a template file cannot be found.
/// </summary>
public class TemplateNotFoundException : JinjaCompilerException
{
    public string RequestedTemplate { get; }

    public TemplateNotFoundException(string requestedTemplate, string? referencedFrom = null)
        : base($"Template '{requestedTemplate}' not found" +
               (referencedFrom != null ? $" (referenced from '{referencedFrom}')" : ""))
    {
        RequestedTemplate = requestedTemplate;
    }
}

/// <summary>
/// Exception thrown when there's a circular dependency in template inheritance.
/// </summary>
public class CircularInheritanceException : JinjaCompilerException
{
    public IReadOnlyList<string> InheritanceChain { get; }

    public CircularInheritanceException(IEnumerable<string> chain)
        : base($"Circular inheritance detected: {string.Join(" -> ", chain)}")
    {
        InheritanceChain = chain.ToList().AsReadOnly();
    }
}

/// <summary>
/// Exception thrown when parsing fails.
/// </summary>
public class TemplateParseException : JinjaCompilerException
{
    public TemplateParseException(string message, string? templateName, int? line = null, int? column = null)
        : base(message, templateName, line, column)
    {
    }
}

/// <summary>
/// Exception thrown when a variable is missing during rendering.
/// </summary>
public class MissingVariableException : JinjaCompilerException
{
    public string VariableName { get; }

    public MissingVariableException(string variableName, string? templateName = null)
        : base($"Missing variable: '{variableName}'", templateName)
    {
        VariableName = variableName;
    }
}

/// <summary>
/// Exception thrown when a block is referenced but not defined.
/// </summary>
public class UndefinedBlockException : JinjaCompilerException
{
    public string BlockName { get; }

    public UndefinedBlockException(string blockName, string? templateName = null)
        : base($"Undefined block: '{blockName}'", templateName)
    {
        BlockName = blockName;
    }
}
