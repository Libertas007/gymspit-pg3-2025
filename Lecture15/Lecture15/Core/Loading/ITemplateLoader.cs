using JinjaCompiler.Core.Ast;
using JinjaCompiler.Core.Exceptions;
using JinjaCompiler.Core.Parsing;

namespace JinjaCompiler.Core.Loading;

/// <summary>
/// Interface for loading template content.
/// </summary>
public interface ITemplateLoader
{
    /// <summary>
    /// Loads the raw content of a template.
    /// </summary>
    /// <param name="templatePath">The path or name of the template to load.</param>
    /// <returns>The template content.</returns>
    string LoadTemplate(string templatePath);

    /// <summary>
    /// Checks if a template exists.
    /// </summary>
    bool TemplateExists(string templatePath);

    /// <summary>
    /// Gets the full path for a template.
    /// </summary>
    string GetFullPath(string templatePath);

    /// <summary>
    /// Gets all template files in the loader's scope.
    /// </summary>
    IEnumerable<string> GetAllTemplates();
}

/// <summary>
/// Loads templates from a file system directory.
/// </summary>
public class FileSystemTemplateLoader : ITemplateLoader
{
    private readonly string _baseDirectory;
    private readonly string[] _extensions;
    private readonly Dictionary<string, string> _cache = new();

    public FileSystemTemplateLoader(string baseDirectory, string[]? extensions = null)
    {
        _baseDirectory = Path.GetFullPath(baseDirectory);
        _extensions = extensions ?? [".html", ".jinja", ".jinja2", ".j2"];

        if (!Directory.Exists(_baseDirectory))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {_baseDirectory}");
        }
    }

    public string LoadTemplate(string templatePath)
    {
        var normalizedPath = NormalizePath(templatePath);

        if (_cache.TryGetValue(normalizedPath, out var cached))
        {
            return cached;
        }

        var fullPath = GetFullPath(normalizedPath);
        if (!File.Exists(fullPath))
        {
            throw new TemplateNotFoundException(templatePath);
        }

        var content = File.ReadAllText(fullPath);
        _cache[normalizedPath] = content;
        return content;
    }

    public bool TemplateExists(string templatePath)
    {
        var fullPath = GetFullPath(NormalizePath(templatePath));
        return File.Exists(fullPath);
    }

    public string GetFullPath(string templatePath)
    {
        var normalizedPath = NormalizePath(templatePath);
        return Path.GetFullPath(Path.Combine(_baseDirectory, normalizedPath));
    }

    public IEnumerable<string> GetAllTemplates()
    {
        foreach (var ext in _extensions)
        {
            foreach (var file in Directory.EnumerateFiles(_baseDirectory, $"*{ext}", SearchOption.AllDirectories))
            {
                yield return Path.GetRelativePath(_baseDirectory, file).Replace('\\', '/');
            }
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}

/// <summary>
/// Loads templates from an in-memory dictionary. Useful for testing.
/// </summary>
public class MemoryTemplateLoader : ITemplateLoader
{
    private readonly Dictionary<string, string> _templates;

    public MemoryTemplateLoader(Dictionary<string, string> templates)
    {
        _templates = new Dictionary<string, string>(templates, StringComparer.OrdinalIgnoreCase);
    }

    public string LoadTemplate(string templatePath)
    {
        var normalized = NormalizePath(templatePath);
        if (!_templates.TryGetValue(normalized, out var content))
        {
            throw new TemplateNotFoundException(templatePath);
        }
        return content;
    }

    public bool TemplateExists(string templatePath)
    {
        return _templates.ContainsKey(NormalizePath(templatePath));
    }

    public string GetFullPath(string templatePath)
    {
        return NormalizePath(templatePath);
    }

    public IEnumerable<string> GetAllTemplates()
    {
        return _templates.Keys;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
