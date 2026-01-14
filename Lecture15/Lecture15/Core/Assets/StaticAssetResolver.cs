using System.Security.Cryptography;
using System.Text;

namespace JinjaCompiler.Core.Assets;

/// <summary>
/// Resolves static asset paths from various reference formats.
/// </summary>
public class StaticAssetResolver
{
    private readonly AssetOptions _options;
    private readonly string _staticRoot;

    public StaticAssetResolver(AssetOptions options)
    {
        _options = options;
        _staticRoot = options.StaticRoot ?? ".";
    }

    /// <summary>
    /// Resolves an asset path to a file system path.
    /// </summary>
    public ResolvedAsset Resolve(string referencedPath, string? relativeTo = null)
    {
        var normalizedPath = NormalizePath(referencedPath);
        var resolvedPath = ResolveToFileSystem(normalizedPath, relativeTo);
        var exists = File.Exists(resolvedPath);
        var assetType = DetermineAssetType(normalizedPath);

        string? content = null;
        if (exists && assetType == AssetType.Css && _options.CssMode == CssMode.Inline)
        {
            content = File.ReadAllText(resolvedPath);
            if (_options.MinifyCss)
            {
                content = MinifyCssContent(content);
            }
        }

        var outputPath = DetermineOutputPath(normalizedPath, resolvedPath, exists);
        var newUrl = DetermineNewUrl(normalizedPath, outputPath, exists);

        return new ResolvedAsset
        {
            OriginalPath = referencedPath,
            ResolvedPath = resolvedPath,
            Exists = exists,
            Type = assetType,
            OutputPath = outputPath,
            NewUrl = newUrl,
            Content = content
        };
    }

    /// <summary>
    /// Resolves a url_for('static', filename='...') call to an asset.
    /// </summary>
    public ResolvedAsset ResolveUrlFor(string filename)
    {
        // url_for('static', filename='css/main.css') -> static/css/main.css
        var path = Path.Combine(_options.StaticUrlPrefix.TrimStart('/'), filename);
        return Resolve(path);
    }

    private string NormalizePath(string path)
    {
        // Remove leading slash and normalize separators
        path = path.TrimStart('/').Replace('\\', '/');

        // Remove query strings and fragments
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        var fragmentIndex = path.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            path = path[..fragmentIndex];
        }

        return path;
    }

    private string ResolveToFileSystem(string normalizedPath, string? relativeTo)
    {
        // Try multiple resolution strategies

        // 1. If path starts with static prefix, resolve relative to static root
        var staticPrefix = _options.StaticUrlPrefix.TrimStart('/').TrimEnd('/');
        if (normalizedPath.StartsWith(staticPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relativePart = normalizedPath[(staticPrefix.Length + 1)..];
            var resolved = Path.GetFullPath(Path.Combine(_staticRoot, relativePart));
            if (File.Exists(resolved))
            {
                return resolved;
            }
        }

        // 2. Try relative to static root directly
        var directPath = Path.GetFullPath(Path.Combine(_staticRoot, normalizedPath));
        if (File.Exists(directPath))
        {
            return directPath;
        }

        // 3. Try relative to the reference location
        if (relativeTo != null)
        {
            var relativeDir = Path.GetDirectoryName(relativeTo) ?? ".";
            var relativePath = Path.GetFullPath(Path.Combine(relativeDir, normalizedPath));
            if (File.Exists(relativePath))
            {
                return relativePath;
            }
        }

        // 4. Return the best guess path (may not exist)
        return directPath;
    }

    private static AssetType DetermineAssetType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".css" => AssetType.Css,
            ".js" => AssetType.JavaScript,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".webp" or ".ico" => AssetType.Image,
            ".woff" or ".woff2" or ".ttf" or ".eot" or ".otf" => AssetType.Font,
            _ => AssetType.Other
        };
    }

    private string? DetermineOutputPath(string normalizedPath, string resolvedPath, bool exists)
    {
        if (!exists || _options.CssMode == CssMode.Inline || _options.CssMode == CssMode.Passthrough)
        {
            return null;
        }

        var filename = Path.GetFileName(normalizedPath);
        var directory = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/') ?? "";

        if (_options.HashAssetNames && exists)
        {
            var hash = ComputeFileHash(resolvedPath);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);
            filename = $"{nameWithoutExt}.{hash}{extension}";
        }

        return Path.Combine(_options.AssetOutputDirectory, directory, filename).Replace('\\', '/');
    }

    private string? DetermineNewUrl(string normalizedPath, string? outputPath, bool exists)
    {
        if (_options.CssMode == CssMode.Passthrough)
        {
            return null; // Keep original
        }

        if (_options.CssMode == CssMode.Inline)
        {
            return null; // Will be inlined
        }

        if (outputPath != null)
        {
            return "/" + outputPath;
        }

        return null;
    }

    private static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    private static string MinifyCssContent(string css)
    {
        // Basic CSS minification
        var result = new StringBuilder();
        bool inComment = false;
        bool inString = false;
        char stringChar = '\0';
        bool lastWasSpace = false;

        for (int i = 0; i < css.Length; i++)
        {
            char c = css[i];
            char next = i + 1 < css.Length ? css[i + 1] : '\0';

            // Handle comments
            if (!inString && c == '/' && next == '*')
            {
                inComment = true;
                i++; // Skip next char
                continue;
            }

            if (inComment)
            {
                if (c == '*' && next == '/')
                {
                    inComment = false;
                    i++; // Skip next char
                }
                continue;
            }

            // Handle strings
            if (!inString && (c == '"' || c == '\''))
            {
                inString = true;
                stringChar = c;
                result.Append(c);
                lastWasSpace = false;
                continue;
            }

            if (inString)
            {
                if (c == stringChar && (i == 0 || css[i - 1] != '\\'))
                {
                    inString = false;
                }
                result.Append(c);
                lastWasSpace = false;
                continue;
            }

            // Handle whitespace
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace && result.Length > 0)
                {
                    // Check if space is needed
                    char lastChar = result[^1];
                    if (char.IsLetterOrDigit(lastChar) || lastChar == ')' || lastChar == '%')
                    {
                        result.Append(' ');
                        lastWasSpace = true;
                    }
                }
                continue;
            }

            // Remove space before certain characters
            if (lastWasSpace && (c == '{' || c == '}' || c == ':' || c == ';' || c == ',' || c == '>' || c == '+' || c == '~'))
            {
                if (result.Length > 0 && result[^1] == ' ')
                {
                    result.Length--;
                }
            }

            result.Append(c);
            lastWasSpace = false;

            // Remove space after certain characters
            if (c == '{' || c == '}' || c == ':' || c == ';' || c == ',' || c == '>' || c == '+' || c == '~')
            {
                while (i + 1 < css.Length && char.IsWhiteSpace(css[i + 1]))
                {
                    i++;
                }
            }
        }

        return result.ToString().Trim();
    }
}
