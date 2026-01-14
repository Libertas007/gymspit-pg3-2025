using System.Text.RegularExpressions;

namespace JinjaCompiler.Core.Assets;

/// <summary>
/// Resolves Flask's url_for() calls to static file paths.
/// </summary>
public partial class UrlForResolver
{
    private readonly StaticAssetResolver _assetResolver;
    private readonly AssetOptions _options;
    private readonly List<string> _warnings = [];

    // Pattern for url_for('static', filename='...') or url_for("static", filename="...")
    [GeneratedRegex(@"\{\{\s*url_for\s*\(\s*['""]static['""]\s*,\s*filename\s*=\s*['""]([^'""]+)['""]\s*\)\s*\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex UrlForPattern();

    // Pattern for Jinja expressions that might contain url_for
    [GeneratedRegex(@"url_for\s*\(\s*['""]static['""]\s*,\s*filename\s*=\s*['""]([^'""]+)['""]\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlForExpressionPattern();

    public UrlForResolver(StaticAssetResolver assetResolver, AssetOptions options)
    {
        _assetResolver = assetResolver;
        _options = options;
    }

    /// <summary>
    /// Gets warnings generated during the last processing.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    /// <summary>
    /// Processes url_for() calls in HTML content before template rendering.
    /// This should be called on the raw template content before Jinja processing.
    /// </summary>
    public string PreprocessUrlFor(string templateContent)
    {
        if (!_options.ProcessUrlFor)
        {
            return templateContent;
        }

        _warnings.Clear();

        // Replace url_for('static', filename='...') with resolved paths
        return UrlForPattern().Replace(templateContent, match =>
        {
            var filename = match.Groups[1].Value;
            return ResolveStaticUrl(filename);
        });
    }

    /// <summary>
    /// Processes url_for() calls in rendered HTML content.
    /// This handles any url_for() that made it through template rendering.
    /// </summary>
    public string PostprocessUrlFor(string htmlContent)
    {
        if (!_options.ProcessUrlFor)
        {
            return htmlContent;
        }

        // Handle any remaining url_for patterns that might have been 
        // rendered as literal strings or missed during preprocessing
        return UrlForPattern().Replace(htmlContent, match =>
        {
            var filename = match.Groups[1].Value;
            return ResolveStaticUrl(filename);
        });
    }

    /// <summary>
    /// Resolves a static filename to its appropriate URL.
    /// </summary>
    public string ResolveStaticUrl(string filename)
    {
        var asset = _assetResolver.ResolveUrlFor(filename);

        if (!asset.Exists && _options.WarnOnMissingAssets)
        {
            _warnings.Add($"Static file not found: {filename} (url_for resolution)");
        }

        // Return the appropriate URL based on mode
        if (_options.CssMode == CssMode.Copy && asset.NewUrl != null)
        {
            return asset.NewUrl;
        }

        // Default: return the standard static path
        return $"{_options.StaticUrlPrefix.TrimEnd('/')}/{filename}";
    }

    /// <summary>
    /// Extracts all static filenames referenced via url_for in the content.
    /// </summary>
    public IEnumerable<string> ExtractStaticReferences(string content)
    {
        var filenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in UrlForPattern().Matches(content))
        {
            filenames.Add(match.Groups[1].Value);
        }

        foreach (Match match in UrlForExpressionPattern().Matches(content))
        {
            filenames.Add(match.Groups[1].Value);
        }

        return filenames;
    }
}
