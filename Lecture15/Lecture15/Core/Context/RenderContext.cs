using System.Text.Json;
using System.Text.Json.Nodes;

namespace JinjaCompiler.Core.Context;

/// <summary>
/// Represents the context (variables) available during template rendering.
/// </summary>
public class RenderContext
{
    private readonly Stack<Dictionary<string, object?>> _scopes = new();
    private readonly RenderContext? _parent;

    public RenderContext(Dictionary<string, object?>? initialData = null)
    {
        _scopes.Push(initialData ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));
    }

    private RenderContext(RenderContext parent)
    {
        _parent = parent;
        _scopes.Push(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a child context that inherits from this context.
    /// </summary>
    public RenderContext CreateChild()
    {
        return new RenderContext(this);
    }

    /// <summary>
    /// Pushes a new scope onto the stack.
    /// </summary>
    public void PushScope(Dictionary<string, object?>? variables = null)
    {
        _scopes.Push(variables ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Pops the current scope from the stack.
    /// </summary>
    public void PopScope()
    {
        if (_scopes.Count > 1)
        {
            _scopes.Pop();
        }
    }

    /// <summary>
    /// Sets a variable in the current scope.
    /// </summary>
    public void Set(string name, object? value)
    {
        _scopes.Peek()[name] = value;
    }

    /// <summary>
    /// Tries to get a variable value.
    /// </summary>
    public bool TryGet(string name, out object? value)
    {
        // Search in local scopes first
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out value))
            {
                return true;
            }
        }

        // Then search in parent context
        if (_parent != null)
        {
            return _parent.TryGet(name, out value);
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Gets a variable value, returning null if not found.
    /// </summary>
    public object? Get(string name)
    {
        TryGet(name, out var value);
        return value;
    }

    /// <summary>
    /// Checks if a variable exists in any scope.
    /// </summary>
    public bool Contains(string name)
    {
        return TryGet(name, out _);
    }

    /// <summary>
    /// Gets all variables in the current context (flattened).
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetAllVariables()
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Start with parent variables
        if (_parent != null)
        {
            foreach (var kvp in _parent.GetAllVariables())
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        // Override with local scopes (bottom to top)
        var scopeList = _scopes.Reverse().ToList();
        foreach (var scope in scopeList)
        {
            foreach (var kvp in scope)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a context from a JSON object.
    /// </summary>
    public static RenderContext FromJson(string json)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? new Dictionary<string, JsonElement>();

        var converted = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in data)
        {
            converted[kvp.Key] = ConvertJsonElement(kvp.Value);
        }

        return new RenderContext(converted);
    }

    /// <summary>
    /// Creates a context from a JSON file.
    /// </summary>
    public static RenderContext FromJsonFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return FromJson(json);
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObject(element),
            JsonValueKind.Array => ConvertJsonArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static Dictionary<string, object?> ConvertJsonObject(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ConvertJsonElement(property.Value);
        }
        return result;
    }

    private static List<object?> ConvertJsonArray(JsonElement element)
    {
        var result = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            result.Add(ConvertJsonElement(item));
        }
        return result;
    }
}

/// <summary>
/// Special loop context available inside for loops.
/// </summary>
public class LoopContext
{
    public int Index0 { get; }
    public int Index => Index0 + 1;
    public bool First => Index0 == 0;
    public bool Last { get; }
    public int Length { get; }
    public int RevIndex0 => Length - Index0 - 1;
    public int RevIndex => Length - Index0;
    public int Depth { get; }
    public int Depth0 => Depth - 1;
    public LoopContext? Parent { get; }

    public LoopContext(int index, int length, int depth = 1, LoopContext? parent = null)
    {
        Index0 = index;
        Length = length;
        Last = index == length - 1;
        Depth = depth;
        Parent = parent;
    }

    public bool Cycle(params object[] values)
    {
        // Used for alternating values in loops
        return false;
    }

    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["index0"] = Index0,
            ["index"] = Index,
            ["first"] = First,
            ["last"] = Last,
            ["length"] = Length,
            ["revindex0"] = RevIndex0,
            ["revindex"] = RevIndex,
            ["depth"] = Depth,
            ["depth0"] = Depth0
        };
    }
}
