using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace JinjaCompiler.Core.Filters;

/// <summary>
/// Registry and execution engine for Jinja2 filters.
/// </summary>
public class FilterRegistry
{
    private readonly Dictionary<string, Func<object?, object?[], object?>> _filters = new(StringComparer.OrdinalIgnoreCase);

    public FilterRegistry()
    {
        RegisterBuiltInFilters();
    }

    public void Register(string name, Func<object?, object?[], object?> filter)
    {
        _filters[name] = filter;
    }

    public object? Apply(string filterName, object? value, params object?[] arguments)
    {
        if (!_filters.TryGetValue(filterName, out var filter))
        {
            // Unknown filter - return value unchanged with a warning
            return value;
        }

        try
        {
            return filter(value, arguments ?? Array.Empty<object?>());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Filter '{filterName}' failed: {ex.Message}", ex);
        }
    }

    public bool HasFilter(string name) => _filters.ContainsKey(name);

    private void RegisterBuiltInFilters()
    {
        // String filters
        Register("upper", (v, _) => v?.ToString()?.ToUpperInvariant());
        Register("lower", (v, _) => v?.ToString()?.ToLowerInvariant());
        Register("capitalize", (v, _) => Capitalize(v?.ToString()));
        Register("title", (v, _) => ToTitleCase(v?.ToString()));
        Register("trim", (v, _) => v?.ToString()?.Trim());
        Register("striptags", (v, _) => StripHtmlTags(v?.ToString()));
        Register("safe", (v, _) => new SafeString(v?.ToString() ?? ""));
        Register("escape", (v, _) => WebUtility.HtmlEncode(v?.ToString() ?? ""));
        Register("e", (v, _) => WebUtility.HtmlEncode(v?.ToString() ?? ""));
        Register("urlencode", (v, _) => Uri.EscapeDataString(v?.ToString() ?? ""));
        Register("replace", (v, args) => Replace(v?.ToString(), args));
        Register("truncate", (v, args) => Truncate(v?.ToString(), args));
        Register("wordwrap", (v, args) => WordWrap(v?.ToString(), args));
        Register("center", (v, args) => Center(v?.ToString(), args));
        Register("indent", (v, args) => Indent(v?.ToString(), args));

        // Number filters
        Register("abs", (v, _) => Math.Abs(Convert.ToDouble(v ?? 0)));
        Register("round", (v, args) => Round(v, args));
        Register("int", (v, _) => Convert.ToInt64(v ?? 0));
        Register("float", (v, _) => Convert.ToDouble(v ?? 0));
        Register("string", (v, _) => v?.ToString() ?? "");

        // List/collection filters
        Register("length", (v, _) => GetLength(v));
        Register("count", (v, _) => GetLength(v));
        Register("first", (v, _) => GetFirst(v));
        Register("last", (v, _) => GetLast(v));
        Register("reverse", (v, _) => Reverse(v));
        Register("sort", (v, args) => Sort(v, args));
        Register("join", (v, args) => Join(v, args));
        Register("unique", (v, _) => Unique(v));
        Register("list", (v, _) => ToList(v));
        Register("batch", (v, args) => Batch(v, args));
        Register("slice", (v, args) => Slice(v, args));
        Register("map", (v, args) => Map(v, args));
        Register("select", (v, args) => Select(v, args));
        Register("reject", (v, args) => Reject(v, args));
        Register("selectattr", (v, args) => SelectAttr(v, args));
        Register("rejectattr", (v, args) => RejectAttr(v, args));
        Register("sum", (v, args) => Sum(v, args));
        Register("max", (v, _) => Max(v));
        Register("min", (v, _) => Min(v));

        // Object/dictionary filters
        Register("attr", (v, args) => GetAttribute(v, args));
        Register("items", (v, _) => GetItems(v));
        Register("keys", (v, _) => GetKeys(v));
        Register("values", (v, _) => GetValues(v));
        Register("dictsort", (v, args) => DictSort(v, args));

        // Type testing/conversion
        Register("default", (v, args) => v ?? (args.Length > 0 ? args[0] : null));
        Register("d", (v, args) => v ?? (args.Length > 0 ? args[0] : null));
        Register("tojson", (v, _) => System.Text.Json.JsonSerializer.Serialize(v));
        Register("pprint", (v, _) => PrettyPrint(v));

        // Boolean filters
        Register("bool", (v, _) => IsTruthy(v));

        // Format filters
        Register("format", (v, args) => Format(v?.ToString(), args));
        Register("filesizeformat", (v, _) => FileSizeFormat(v));
        Register("wordcount", (v, _) => WordCount(v?.ToString()));

        // Special
        Register("xmlattr", (v, _) => XmlAttr(v));
        Register("random", (v, _) => RandomElement(v));
        Register("groupby", (v, args) => GroupBy(v, args));
    }

    #region Filter Implementations

    private static string? Capitalize(string? s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
    }

    private static string? ToTitleCase(string? s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
    }

    private static string StripHtmlTags(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return Regex.Replace(s, "<[^>]*>", "");
    }

    private static string? Replace(string? s, object?[] args)
    {
        if (s == null || args.Length < 2) return s;
        var oldValue = args[0]?.ToString() ?? "";
        var newValue = args[1]?.ToString() ?? "";
        return s.Replace(oldValue, newValue);
    }

    private static string? Truncate(string? s, object?[] args)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var length = args.Length > 0 ? Convert.ToInt32(args[0] ?? 255) : 255;
        var killwords = args.Length > 1 && IsTruthy(args[1]);
        var end = args.Length > 2 ? args[2]?.ToString() ?? "..." : "...";

        if (s.Length <= length) return s;

        if (killwords)
        {
            return s[..(length - end.Length)] + end;
        }

        // Truncate at word boundary
        var truncated = s[..(length - end.Length)];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 0)
        {
            truncated = truncated[..lastSpace];
        }
        return truncated + end;
    }

    private static string? WordWrap(string? s, object?[] args)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var width = args.Length > 0 ? Convert.ToInt32(args[0] ?? 79) : 79;
        var breakLongWords = args.Length <= 1 || IsTruthy(args[1]);

        var sb = new StringBuilder();
        var lines = s.Split('\n');

        foreach (var line in lines)
        {
            var currentLine = new StringBuilder();
            var words = line.Split(' ');

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 <= width)
                {
                    if (currentLine.Length > 0) currentLine.Append(' ');
                    currentLine.Append(word);
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        sb.AppendLine(currentLine.ToString());
                        currentLine.Clear();
                    }
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0)
            {
                sb.AppendLine(currentLine.ToString());
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string? Center(string? s, object?[] args)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var width = args.Length > 0 ? Convert.ToInt32(args[0] ?? 80) : 80;
        return s.PadLeft((width + s.Length) / 2).PadRight(width);
    }

    private static string? Indent(string? s, object?[] args)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var width = args.Length > 0 ? Convert.ToInt32(args[0] ?? 4) : 4;
        var indentFirst = args.Length > 1 && IsTruthy(args[1]);
        var indent = new string(' ', width);

        var lines = s.Split('\n');
        var sb = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0 || indentFirst)
            {
                sb.Append(indent);
            }
            sb.Append(lines[i]);
            if (i < lines.Length - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static double Round(object? v, object?[] args)
    {
        var value = Convert.ToDouble(v ?? 0);
        var precision = args.Length > 0 ? Convert.ToInt32(args[0] ?? 0) : 0;
        return Math.Round(value, precision);
    }

    private static int GetLength(object? v)
    {
        return v switch
        {
            null => 0,
            string s => s.Length,
            ICollection<object> c => c.Count,
            System.Collections.ICollection c => c.Count,
            System.Collections.IEnumerable e => e.Cast<object>().Count(),
            _ => v.ToString()?.Length ?? 0
        };
    }

    private static object? GetFirst(object? v)
    {
        return v switch
        {
            null => null,
            string s => s.Length > 0 ? s[0].ToString() : null,
            IEnumerable<object?> e => e.FirstOrDefault(),
            System.Collections.IEnumerable e => e.Cast<object?>().FirstOrDefault(),
            _ => null
        };
    }

    private static object? GetLast(object? v)
    {
        return v switch
        {
            null => null,
            string s => s.Length > 0 ? s[^1].ToString() : null,
            IEnumerable<object?> e => e.LastOrDefault(),
            System.Collections.IEnumerable e => e.Cast<object?>().LastOrDefault(),
            _ => null
        };
    }

    private static object? Reverse(object? v)
    {
        return v switch
        {
            null => null,
            string s => new string(s.Reverse().ToArray()),
            IEnumerable<object?> e => e.Reverse().ToList(),
            System.Collections.IEnumerable e => e.Cast<object?>().Reverse().ToList(),
            _ => v
        };
    }

    private static object? Sort(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        var list = enumerable.Cast<object?>().ToList();

        var reverse = args.Length > 0 && IsTruthy(args[0]);
        var key = args.Length > 1 ? args[1]?.ToString() : null;

        if (key != null)
        {
            list = reverse
                ? list.OrderByDescending(x => GetNestedValue(x, key)).ToList()
                : list.OrderBy(x => GetNestedValue(x, key)).ToList();
        }
        else
        {
            list = reverse
                ? list.OrderByDescending(x => x).ToList()
                : list.OrderBy(x => x).ToList();
        }

        return list;
    }

    private static string Join(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v?.ToString() ?? "";
        var separator = args.Length > 0 ? args[0]?.ToString() ?? "" : "";
        return string.Join(separator, enumerable.Cast<object>());
    }

    private static object? Unique(object? v)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        return enumerable.Cast<object?>().Distinct().ToList();
    }

    private static List<object?> ToList(object? v)
    {
        return v switch
        {
            null => new List<object?>(),
            string s => s.Select(c => (object?)c.ToString()).ToList(),
            IEnumerable<object?> e => e.ToList(),
            System.Collections.IEnumerable e => e.Cast<object?>().ToList(),
            _ => new List<object?> { v }
        };
    }

    private static object? Batch(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        var list = enumerable.Cast<object?>().ToList();
        var linecount = args.Length > 0 ? Convert.ToInt32(args[0] ?? 1) : 1;
        var fillWith = args.Length > 1 ? args[1] : null;

        var result = new List<List<object?>>();
        for (int i = 0; i < list.Count; i += linecount)
        {
            var batch = list.Skip(i).Take(linecount).ToList();
            while (batch.Count < linecount && fillWith != null)
            {
                batch.Add(fillWith);
            }
            result.Add(batch);
        }
        return result;
    }

    private static object? Slice(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        var list = enumerable.Cast<object?>().ToList();
        var slices = args.Length > 0 ? Convert.ToInt32(args[0] ?? 1) : 1;
        var fillWith = args.Length > 1 ? args[1] : null;

        var result = new List<List<object?>>();
        var perSlice = (int)Math.Ceiling((double)list.Count / slices);

        for (int i = 0; i < slices; i++)
        {
            var slice = list.Skip(i * perSlice).Take(perSlice).ToList();
            while (slice.Count < perSlice && fillWith != null && result.Count < slices - 1)
            {
                slice.Add(fillWith);
            }
            result.Add(slice);
        }
        return result;
    }

    private static object? Map(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable || args.Length == 0) return v;
        var attribute = args[0]?.ToString();
        if (string.IsNullOrEmpty(attribute)) return v;

        return enumerable.Cast<object?>()
            .Select(item => GetNestedValue(item, attribute))
            .ToList();
    }

    private static object? Select(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        // Select items that are truthy
        return enumerable.Cast<object?>().Where(IsTruthy).ToList();
    }

    private static object? Reject(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        return enumerable.Cast<object?>().Where(x => !IsTruthy(x)).ToList();
    }

    private static object? SelectAttr(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable || args.Length == 0) return v;
        var attr = args[0]?.ToString();
        if (string.IsNullOrEmpty(attr)) return v;

        return enumerable.Cast<object?>()
            .Where(x => IsTruthy(GetNestedValue(x, attr)))
            .ToList();
    }

    private static object? RejectAttr(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable || args.Length == 0) return v;
        var attr = args[0]?.ToString();
        if (string.IsNullOrEmpty(attr)) return v;

        return enumerable.Cast<object?>()
            .Where(x => !IsTruthy(GetNestedValue(x, attr)))
            .ToList();
    }

    private static object? Sum(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable) return 0;
        var list = enumerable.Cast<object?>().ToList();

        var attribute = args.Length > 0 ? args[0]?.ToString() : null;
        var start = args.Length > 1 ? Convert.ToDouble(args[1] ?? 0) : 0;

        var values = attribute != null
            ? list.Select(x => GetNestedValue(x, attribute))
            : list;

        return start + values.Sum(x => Convert.ToDouble(x ?? 0));
    }

    private static object? Max(object? v)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        return enumerable.Cast<object?>().Max();
    }

    private static object? Min(object? v)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        return enumerable.Cast<object?>().Min();
    }

    private static object? GetAttribute(object? v, object?[] args)
    {
        if (args.Length == 0) return v;
        var attrName = args[0]?.ToString();
        if (string.IsNullOrEmpty(attrName)) return v;
        return GetNestedValue(v, attrName);
    }

    private static object? GetItems(object? v)
    {
        if (v is Dictionary<string, object?> dict)
        {
            return dict.Select(kvp => new List<object?> { kvp.Key, kvp.Value }).ToList();
        }
        return new List<object?>();
    }

    private static object? GetKeys(object? v)
    {
        if (v is Dictionary<string, object?> dict)
        {
            return dict.Keys.ToList();
        }
        return new List<object?>();
    }

    private static object? GetValues(object? v)
    {
        if (v is Dictionary<string, object?> dict)
        {
            return dict.Values.ToList();
        }
        return new List<object?>();
    }

    private static object? DictSort(object? v, object?[] args)
    {
        if (v is not Dictionary<string, object?> dict) return v;

        var byValue = args.Length > 1 && args[0]?.ToString() == "value";
        var reverse = args.Length > 1 && IsTruthy(args[1]);

        var items = dict.ToList();
        if (byValue)
        {
            items = reverse
                ? items.OrderByDescending(x => x.Value).ToList()
                : items.OrderBy(x => x.Value).ToList();
        }
        else
        {
            items = reverse
                ? items.OrderByDescending(x => x.Key).ToList()
                : items.OrderBy(x => x.Key).ToList();
        }

        return items.Select(kvp => new List<object?> { kvp.Key, kvp.Value }).ToList();
    }

    private static string PrettyPrint(object? v)
    {
        return System.Text.Json.JsonSerializer.Serialize(v, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string? Format(string? template, object?[] args)
    {
        if (string.IsNullOrEmpty(template)) return template;
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    private static string FileSizeFormat(object? v)
    {
        var bytes = Convert.ToDouble(v ?? 0);
        string[] sizes = ["Bytes", "KB", "MB", "GB", "TB"];
        int order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes /= 1024;
        }
        return $"{bytes:0.##} {sizes[order]}";
    }

    private static int WordCount(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        return s.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string XmlAttr(object? v)
    {
        if (v is not Dictionary<string, object?> dict) return "";
        var attrs = dict.Select(kvp => $"{kvp.Key}=\"{WebUtility.HtmlEncode(kvp.Value?.ToString() ?? "")}\"");
        return string.Join(" ", attrs);
    }

    private static readonly Random _random = new();
    private static object? RandomElement(object? v)
    {
        if (v is not System.Collections.IEnumerable enumerable) return v;
        var list = enumerable.Cast<object?>().ToList();
        if (list.Count == 0) return null;
        return list[_random.Next(list.Count)];
    }

    private static object? GroupBy(object? v, object?[] args)
    {
        if (v is not System.Collections.IEnumerable enumerable || args.Length == 0) return v;
        var attribute = args[0]?.ToString();
        if (string.IsNullOrEmpty(attribute)) return v;

        return enumerable.Cast<object?>()
            .GroupBy(x => GetNestedValue(x, attribute))
            .Select(g => new Dictionary<string, object?>
            {
                ["grouper"] = g.Key,
                ["list"] = g.ToList()
            })
            .ToList();
    }

    #endregion

    #region Helpers

    public static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            ICollection<object?> c => c.Count > 0,
            System.Collections.ICollection c => c.Count > 0,
            _ => true
        };
    }

    public static object? GetNestedValue(object? obj, string path)
    {
        if (obj == null || string.IsNullOrEmpty(path)) return null;

        var parts = path.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            if (current == null) return null;

            // Handle array indexing like "items[0]"
            var match = Regex.Match(part, @"^(\w+)\[(\d+)\]$");
            if (match.Success)
            {
                var propName = match.Groups[1].Value;
                var index = int.Parse(match.Groups[2].Value);

                current = GetPropertyValue(current, propName);
                if (current is System.Collections.IList list && index < list.Count)
                {
                    current = list[index];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                current = GetPropertyValue(current, part);
            }
        }

        return current;
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj is Dictionary<string, object?> dict)
        {
            return dict.TryGetValue(propertyName, out var value) ? value : null;
        }

        if (obj is IDictionary<string, object> idict)
        {
            return idict.TryGetValue(propertyName, out var value) ? value : null;
        }

        var prop = obj.GetType().GetProperty(propertyName, 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.IgnoreCase);

        return prop?.GetValue(obj);
    }

    #endregion
}

/// <summary>
/// Marker class for strings that should not be escaped.
/// </summary>
public class SafeString
{
    public string Value { get; }

    public SafeString(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;
}
