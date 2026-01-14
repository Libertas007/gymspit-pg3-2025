using System.Text.RegularExpressions;
using JinjaCompiler.Core.Context;
using JinjaCompiler.Core.Filters;

namespace JinjaCompiler.Core.Expressions;

/// <summary>
/// Evaluates Jinja2 expressions against a render context.
/// </summary>
public class ExpressionEvaluator
{
    private readonly FilterRegistry _filters;

    public ExpressionEvaluator(FilterRegistry filters)
    {
        _filters = filters;
    }

    /// <summary>
    /// Evaluates an expression and returns the result.
    /// </summary>
    public object? Evaluate(string expression, RenderContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        expression = expression.Trim();

        // Handle literal values
        if (TryParseLiteral(expression, out var literalValue))
        {
            return literalValue;
        }

        // Handle parenthesized expressions
        if (expression.StartsWith('(') && expression.EndsWith(')'))
        {
            return Evaluate(expression[1..^1], context);
        }

        // Handle boolean operations
        var boolResult = EvaluateBooleanExpression(expression, context);
        if (boolResult.HasValue)
        {
            return boolResult.Value;
        }

        // Handle comparison operations
        var compResult = EvaluateComparison(expression, context);
        if (compResult != null)
        {
            return compResult;
        }

        // Handle arithmetic operations
        var arithResult = EvaluateArithmetic(expression, context);
        if (arithResult != null)
        {
            return arithResult;
        }

        // Handle string concatenation with ~
        if (expression.Contains('~'))
        {
            return EvaluateConcatenation(expression, context);
        }

        // Handle not operator
        if (expression.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
        {
            var inner = Evaluate(expression[4..], context);
            return !FilterRegistry.IsTruthy(inner);
        }

        // Handle variable access with filters
        return EvaluateVariable(expression, context);
    }

    /// <summary>
    /// Evaluates a condition expression and returns a boolean.
    /// </summary>
    public bool EvaluateCondition(string expression, RenderContext context)
    {
        var result = Evaluate(expression, context);
        return FilterRegistry.IsTruthy(result);
    }

    private bool TryParseLiteral(string expression, out object? value)
    {
        value = null;

        // String literals
        if ((expression.StartsWith('"') && expression.EndsWith('"')) ||
            (expression.StartsWith('\'') && expression.EndsWith('\'')))
        {
            value = expression[1..^1];
            return true;
        }

        // Boolean literals
        if (expression.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }
        if (expression.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        // None/null literal
        if (expression.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            expression.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            value = null;
            return true;
        }

        // Integer literals
        if (long.TryParse(expression, out var intValue))
        {
            value = intValue;
            return true;
        }

        // Float literals
        if (double.TryParse(expression, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
        {
            value = floatValue;
            return true;
        }

        // List literals [a, b, c]
        if (expression.StartsWith('[') && expression.EndsWith(']'))
        {
            value = ParseListLiteral(expression);
            return true;
        }

        // Dict literals {a: b, c: d}
        if (expression.StartsWith('{') && expression.EndsWith('}'))
        {
            value = ParseDictLiteral(expression);
            return true;
        }

        return false;
    }

    private List<object?> ParseListLiteral(string expression)
    {
        var inner = expression[1..^1].Trim();
        if (string.IsNullOrEmpty(inner))
        {
            return new List<object?>();
        }

        var result = new List<object?>();
        foreach (var item in SplitByComma(inner))
        {
            if (TryParseLiteral(item.Trim(), out var value))
            {
                result.Add(value);
            }
            else
            {
                result.Add(item.Trim());
            }
        }
        return result;
    }

    private Dictionary<string, object?> ParseDictLiteral(string expression)
    {
        var inner = expression[1..^1].Trim();
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(inner))
        {
            return result;
        }

        foreach (var pair in SplitByComma(inner))
        {
            var colonIndex = pair.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = pair[..colonIndex].Trim().Trim('"', '\'');
                var valueStr = pair[(colonIndex + 1)..].Trim();

                if (TryParseLiteral(valueStr, out var value))
                {
                    result[key] = value;
                }
                else
                {
                    result[key] = valueStr;
                }
            }
        }

        return result;
    }

    private IEnumerable<string> SplitByComma(string input)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;
        bool inString = false;
        char stringChar = '\0';

        foreach (var c in input)
        {
            if (!inString && (c == '"' || c == '\''))
            {
                inString = true;
                stringChar = c;
            }
            else if (inString && c == stringChar)
            {
                inString = false;
            }

            if (!inString)
            {
                if (c == '[' || c == '{' || c == '(') depth++;
                if (c == ']' || c == '}' || c == ')') depth--;

                if (c == ',' && depth == 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    private bool? EvaluateBooleanExpression(string expression, RenderContext context)
    {
        // Handle 'and' (outside of strings and parentheses)
        var andIndex = FindOperatorIndex(expression, " and ");
        if (andIndex >= 0)
        {
            var left = Evaluate(expression[..andIndex], context);
            var right = Evaluate(expression[(andIndex + 5)..], context);
            return FilterRegistry.IsTruthy(left) && FilterRegistry.IsTruthy(right);
        }

        // Handle 'or'
        var orIndex = FindOperatorIndex(expression, " or ");
        if (orIndex >= 0)
        {
            var left = Evaluate(expression[..orIndex], context);
            if (FilterRegistry.IsTruthy(left))
            {
                return true;
            }
            var right = Evaluate(expression[(orIndex + 4)..], context);
            return FilterRegistry.IsTruthy(right);
        }

        return null;
    }

    private object? EvaluateComparison(string expression, RenderContext context)
    {
        // Order matters - check longer operators first
        var operators = new[]
        {
            ("==", new Func<object?, object?, bool>((a, b) => Equals(a, b))),
            ("!=", new Func<object?, object?, bool>((a, b) => !Equals(a, b))),
            (">=", new Func<object?, object?, bool>((a, b) => Compare(a, b) >= 0)),
            ("<=", new Func<object?, object?, bool>((a, b) => Compare(a, b) <= 0)),
            (">", new Func<object?, object?, bool>((a, b) => Compare(a, b) > 0)),
            ("<", new Func<object?, object?, bool>((a, b) => Compare(a, b) < 0)),
        };

        foreach (var (op, func) in operators)
        {
            var index = FindOperatorIndex(expression, op);
            if (index >= 0)
            {
                var left = Evaluate(expression[..index].Trim(), context);
                var right = Evaluate(expression[(index + op.Length)..].Trim(), context);
                return func(left, right);
            }
        }

        // Handle 'in' operator
        var inIndex = FindOperatorIndex(expression, " in ");
        if (inIndex >= 0)
        {
            var item = Evaluate(expression[..inIndex].Trim(), context);
            var collection = Evaluate(expression[(inIndex + 4)..].Trim(), context);
            return IsIn(item, collection);
        }

        // Handle 'not in' operator
        var notInIndex = FindOperatorIndex(expression, " not in ");
        if (notInIndex >= 0)
        {
            var item = Evaluate(expression[..notInIndex].Trim(), context);
            var collection = Evaluate(expression[(notInIndex + 8)..].Trim(), context);
            return !IsIn(item, collection);
        }

        // Handle 'is' operator
        var isNotIndex = FindOperatorIndex(expression, " is not ");
        if (isNotIndex >= 0)
        {
            var value = Evaluate(expression[..isNotIndex].Trim(), context);
            var test = expression[(isNotIndex + 8)..].Trim();
            return !EvaluateTest(value, test);
        }

        var isIndex = FindOperatorIndex(expression, " is ");
        if (isIndex >= 0)
        {
            var value = Evaluate(expression[..isIndex].Trim(), context);
            var test = expression[(isIndex + 4)..].Trim();
            return EvaluateTest(value, test);
        }

        return null;
    }

    private object? EvaluateArithmetic(string expression, RenderContext context)
    {
        // Handle subtraction and addition at same precedence (left to right)
        var addSubIndex = FindLastOperatorIndex(expression, "+", "-");
        if (addSubIndex >= 0)
        {
            var op = expression[addSubIndex];
            var left = Evaluate(expression[..addSubIndex].Trim(), context);
            var right = Evaluate(expression[(addSubIndex + 1)..].Trim(), context);
            var leftNum = Convert.ToDouble(left ?? 0);
            var rightNum = Convert.ToDouble(right ?? 0);
            return op == '+' ? leftNum + rightNum : leftNum - rightNum;
        }

        // Handle multiplication and division
        var mulDivIndex = FindLastOperatorIndex(expression, "*", "/", "%");
        if (mulDivIndex >= 0)
        {
            var op = expression[mulDivIndex];
            var left = Evaluate(expression[..mulDivIndex].Trim(), context);
            var right = Evaluate(expression[(mulDivIndex + 1)..].Trim(), context);
            var leftNum = Convert.ToDouble(left ?? 0);
            var rightNum = Convert.ToDouble(right ?? 0);
            return op switch
            {
                '*' => leftNum * rightNum,
                '/' => rightNum != 0 ? leftNum / rightNum : 0,
                '%' => rightNum != 0 ? leftNum % rightNum : 0,
                _ => null
            };
        }

        return null;
    }

    private string EvaluateConcatenation(string expression, RenderContext context)
    {
        var parts = expression.Split('~');
        var result = new System.Text.StringBuilder();

        foreach (var part in parts)
        {
            var value = Evaluate(part.Trim(), context);
            result.Append(value?.ToString() ?? "");
        }

        return result.ToString();
    }

    private object? EvaluateVariable(string expression, RenderContext context)
    {
        expression = expression.Trim();

        // Check for function call like range(10)
        var funcMatch = Regex.Match(expression, @"^(\w+)\s*\((.*)\)$");
        if (funcMatch.Success)
        {
            return EvaluateFunction(funcMatch.Groups[1].Value, funcMatch.Groups[2].Value, context);
        }

        // Handle dot notation: object.property.subproperty
        var parts = expression.Split('.');
        var firstPart = parts[0];

        // Handle array indexing in first part
        var indexMatch = Regex.Match(firstPart, @"^(\w+)\[(.+)\]$");
        object? value;

        if (indexMatch.Success)
        {
            var varName = indexMatch.Groups[1].Value;
            var indexExpr = indexMatch.Groups[2].Value;
            var index = Evaluate(indexExpr, context);

            if (!context.TryGet(varName, out value))
            {
                return null;
            }

            value = GetIndexedValue(value, index);
        }
        else
        {
            if (!context.TryGet(firstPart, out value))
            {
                return null;
            }
        }

        // Navigate through remaining parts
        for (int i = 1; i < parts.Length && value != null; i++)
        {
            var part = parts[i];

            // Handle indexing like items[0]
            var partIndexMatch = Regex.Match(part, @"^(\w+)\[(.+)\]$");
            if (partIndexMatch.Success)
            {
                var propName = partIndexMatch.Groups[1].Value;
                var indexExpr = partIndexMatch.Groups[2].Value;
                var index = Evaluate(indexExpr, context);

                value = FilterRegistry.GetNestedValue(value, propName);
                value = GetIndexedValue(value, index);
            }
            else
            {
                value = FilterRegistry.GetNestedValue(value, part);
            }
        }

        return value;
    }

    private object? GetIndexedValue(object? collection, object? index)
    {
        if (collection == null) return null;

        if (index is long longIndex)
        {
            return GetAtIndex(collection, (int)longIndex);
        }

        if (index is int intIndex)
        {
            return GetAtIndex(collection, intIndex);
        }

        if (index is string stringKey && collection is Dictionary<string, object?> dict)
        {
            return dict.TryGetValue(stringKey, out var val) ? val : null;
        }

        return null;
    }

    private object? GetAtIndex(object collection, int index)
    {
        return collection switch
        {
            IList<object?> list when index >= 0 && index < list.Count => list[index],
            System.Collections.IList list when index >= 0 && index < list.Count => list[index],
            string s when index >= 0 && index < s.Length => s[index].ToString(),
            _ => null
        };
    }

    private object? EvaluateFunction(string funcName, string argsStr, RenderContext context)
    {
        var args = string.IsNullOrWhiteSpace(argsStr)
            ? Array.Empty<object?>()
            : SplitByComma(argsStr).Select(a => Evaluate(a.Trim(), context)).ToArray();

        return funcName.ToLowerInvariant() switch
        {
            "range" => EvaluateRange(args),
            "dict" => EvaluateDict(args),
            "lipsum" => EvaluateLipsum(args),
            "cycler" => args.ToList(),
            "joiner" => new Joiner(args.Length > 0 ? args[0]?.ToString() ?? ", " : ", "),
            "namespace" => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            _ => null
        };
    }

    private object EvaluateRange(object?[] args)
    {
        int start = 0, stop = 0, step = 1;

        switch (args.Length)
        {
            case 1:
                stop = Convert.ToInt32(args[0] ?? 0);
                break;
            case 2:
                start = Convert.ToInt32(args[0] ?? 0);
                stop = Convert.ToInt32(args[1] ?? 0);
                break;
            case >= 3:
                start = Convert.ToInt32(args[0] ?? 0);
                stop = Convert.ToInt32(args[1] ?? 0);
                step = Convert.ToInt32(args[2] ?? 1);
                break;
        }

        if (step == 0) step = 1;

        var result = new List<int>();
        if (step > 0)
        {
            for (int i = start; i < stop; i += step)
            {
                result.Add(i);
            }
        }
        else
        {
            for (int i = start; i > stop; i += step)
            {
                result.Add(i);
            }
        }

        return result;
    }

    private Dictionary<string, object?> EvaluateDict(object?[] args)
    {
        // dict() function - create dictionary from keyword arguments
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    private string EvaluateLipsum(object?[] args)
    {
        var n = args.Length > 0 ? Convert.ToInt32(args[0] ?? 5) : 5;
        var html = args.Length > 1 && FilterRegistry.IsTruthy(args[1]);

        const string lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. ";
        var result = string.Concat(Enumerable.Repeat(lorem, n));

        return html ? $"<p>{result}</p>" : result;
    }

    private bool EvaluateTest(object? value, string test)
    {
        var testLower = test.ToLowerInvariant().Trim();

        // Handle test with arguments like "divisibleby(3)"
        var match = Regex.Match(testLower, @"^(\w+)\s*\((.+)\)$");
        if (match.Success)
        {
            var testName = match.Groups[1].Value;
            var arg = match.Groups[2].Value.Trim();

            return testName switch
            {
                "divisibleby" => Convert.ToDouble(value ?? 0) % Convert.ToDouble(arg) == 0,
                "sameas" => ReferenceEquals(value, arg), // Simplified
                "equalto" or "eq" => Equals(value?.ToString(), arg.Trim('"', '\'')),
                "greaterthan" or "gt" => Compare(value, double.Parse(arg)) > 0,
                "lessthan" or "lt" => Compare(value, double.Parse(arg)) < 0,
                "ge" => Compare(value, double.Parse(arg)) >= 0,
                "le" => Compare(value, double.Parse(arg)) <= 0,
                _ => false
            };
        }

        return testLower switch
        {
            "defined" => value != null,
            "undefined" => value == null,
            "none" => value == null,
            "true" => value is true,
            "false" => value is false,
            "string" => value is string,
            "number" => value is int or long or float or double or decimal,
            "integer" => value is int or long,
            "float" => value is float or double,
            "sequence" or "iterable" => value is System.Collections.IEnumerable && value is not string,
            "mapping" => value is System.Collections.IDictionary,
            "callable" => false, // We don't support callable in static context
            "odd" => Convert.ToInt64(value ?? 0) % 2 != 0,
            "even" => Convert.ToInt64(value ?? 0) % 2 == 0,
            "lower" => value is string s && s == s.ToLowerInvariant(),
            "upper" => value is string str && str == str.ToUpperInvariant(),
            "empty" => value == null || (value is string es && string.IsNullOrEmpty(es)) ||
                      (value is System.Collections.ICollection c && c.Count == 0),
            _ => false
        };
    }

    private bool IsIn(object? item, object? collection)
    {
        if (collection == null) return false;

        if (collection is string s && item != null)
        {
            return s.Contains(item.ToString()!);
        }

        if (collection is System.Collections.IEnumerable enumerable)
        {
            foreach (var elem in enumerable)
            {
                if (Equals(elem, item)) return true;
            }
        }

        return false;
    }

    private int FindOperatorIndex(string expression, string op)
    {
        int depth = 0;
        bool inString = false;
        char stringChar = '\0';

        for (int i = 0; i <= expression.Length - op.Length; i++)
        {
            var c = expression[i];

            if (!inString && (c == '"' || c == '\''))
            {
                inString = true;
                stringChar = c;
                continue;
            }

            if (inString && c == stringChar)
            {
                inString = false;
                continue;
            }

            if (inString) continue;

            if (c == '(' || c == '[' || c == '{') depth++;
            if (c == ')' || c == ']' || c == '}') depth--;

            if (depth == 0 && expression[i..].StartsWith(op, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindLastOperatorIndex(string expression, params string[] ops)
    {
        int depth = 0;
        bool inString = false;
        char stringChar = '\0';
        int lastIndex = -1;

        for (int i = 0; i < expression.Length; i++)
        {
            var c = expression[i];

            if (!inString && (c == '"' || c == '\''))
            {
                inString = true;
                stringChar = c;
                continue;
            }

            if (inString && c == stringChar)
            {
                inString = false;
                continue;
            }

            if (inString) continue;

            if (c == '(' || c == '[' || c == '{') depth++;
            if (c == ')' || c == ']' || c == '}') depth--;

            if (depth == 0)
            {
                foreach (var op in ops)
                {
                    if (expression[i..].StartsWith(op))
                    {
                        lastIndex = i;
                    }
                }
            }
        }

        return lastIndex;
    }

    private static bool Equals(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // Normalize numeric comparisons
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Convert.ToDouble(a) == Convert.ToDouble(b);
        }

        return a.Equals(b) || a.ToString() == b.ToString();
    }

    private static int Compare(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        if (IsNumeric(a) && IsNumeric(b))
        {
            return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));
        }

        if (a is string sa && b is string sb)
        {
            return string.Compare(sa, sb, StringComparison.Ordinal);
        }

        if (a is IComparable ca)
        {
            return ca.CompareTo(b);
        }

        return 0;
    }

    private static bool IsNumeric(object? value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}

/// <summary>
/// Helper class for the joiner() function.
/// </summary>
public class Joiner
{
    private readonly string _separator;
    private bool _first = true;

    public Joiner(string separator = ", ")
    {
        _separator = separator;
    }

    public override string ToString()
    {
        if (_first)
        {
            _first = false;
            return "";
        }
        return _separator;
    }
}
