namespace JinjaCompiler.Core.Assets;

/// <summary>
/// Coordinates all asset processing for generated HTML.
/// </summary>
public class AssetProcessor
{
    private readonly AssetOptions _options;
    private readonly StaticAssetResolver _resolver;
    private readonly CssProcessor _cssProcessor;
    private readonly UrlForResolver _urlForResolver;

    public AssetProcessor(AssetOptions options)
    {
        _options = options;
        _resolver = new StaticAssetResolver(options);
        _cssProcessor = new CssProcessor(_resolver, options);
        _urlForResolver = new UrlForResolver(_resolver, options);
    }

    /// <summary>
    /// Gets the URL for resolver for preprocessing templates.
    /// </summary>
    public UrlForResolver UrlForResolver => _urlForResolver;

    /// <summary>
    /// Gets the static asset resolver.
    /// </summary>
    public StaticAssetResolver AssetResolver => _resolver;

    /// <summary>
    /// Processes all assets in the rendered HTML.
    /// </summary>
    public AssetProcessingResult ProcessHtml(string html)
    {
        // First, process any remaining url_for() calls
        html = _urlForResolver.PostprocessUrlFor(html);

        // Then process CSS references
        var result = _cssProcessor.ProcessCss(html);

        // Combine warnings
        var allWarnings = result.Warnings.Concat(_urlForResolver.Warnings).ToList();

        return result with { Warnings = allWarnings.AsReadOnly() };
    }

    /// <summary>
    /// Copies all assets that need to be copied to the output directory.
    /// </summary>
    public async Task CopyAssetsAsync(IEnumerable<ResolvedAsset> assets, string outputDirectory)
    {
        foreach (var asset in assets.Where(a => a.Exists && a.OutputPath != null))
        {
            var destPath = Path.Combine(outputDirectory, asset.OutputPath!);
            var destDir = Path.GetDirectoryName(destPath);

            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Process CSS content (resolve relative URLs, etc.)
            if (asset.Type == AssetType.Css)
            {
                var cssContent = await File.ReadAllTextAsync(asset.ResolvedPath);
                cssContent = ProcessCssForCopy(cssContent, asset);
                await File.WriteAllTextAsync(destPath, cssContent);
            }
            else
            {
                File.Copy(asset.ResolvedPath, destPath, overwrite: true);
            }
        }
    }

    /// <summary>
    /// Copies all referenced assets from a set of processing results.
    /// </summary>
    public async Task CopyAllAssetsAsync(IEnumerable<AssetProcessingResult> results, string outputDirectory)
    {
        var allAssets = results
            .SelectMany(r => r.AssetsToCopy)
            .DistinctBy(a => a.ResolvedPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await CopyAssetsAsync(allAssets, outputDirectory);

        // Also copy any secondary assets referenced in CSS files
        await CopySecondaryAssetsAsync(allAssets, outputDirectory);
    }

    private async Task CopySecondaryAssetsAsync(IEnumerable<ResolvedAsset> cssAssets, string outputDirectory)
    {
        foreach (var cssAsset in cssAssets.Where(a => a.Type == AssetType.Css && a.Exists))
        {
            var cssContent = await File.ReadAllTextAsync(cssAsset.ResolvedPath);
            var cssDir = Path.GetDirectoryName(cssAsset.ResolvedPath) ?? ".";

            // Extract and copy url() references
            var urlPattern = new System.Text.RegularExpressions.Regex(
                @"url\s*\(\s*[""']?([^""'\)]+)[""']?\s*\)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in urlPattern.Matches(cssContent))
            {
                var url = match.Groups[1].Value.Trim();

                // Skip data URIs and external URLs
                if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) continue;
                if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) continue;
                if (url.StartsWith("//")) continue;

                var referencedAsset = _resolver.Resolve(url, cssAsset.ResolvedPath);
                if (referencedAsset.Exists && referencedAsset.OutputPath != null)
                {
                    var destPath = Path.Combine(outputDirectory, referencedAsset.OutputPath);
                    var destDir = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    if (!File.Exists(destPath))
                    {
                        File.Copy(referencedAsset.ResolvedPath, destPath, overwrite: true);
                    }
                }
            }
        }
    }

    private string ProcessCssForCopy(string cssContent, ResolvedAsset cssAsset)
    {
        // Update relative URLs in the CSS to point to the new location
        var urlPattern = new System.Text.RegularExpressions.Regex(
            @"url\s*\(\s*[""']?([^""'\)]+)[""']?\s*\)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return urlPattern.Replace(cssContent, match =>
        {
            var url = match.Groups[1].Value.Trim();

            // Skip data URIs and external URLs
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return match.Value;
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return match.Value;
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return match.Value;
            if (url.StartsWith("//")) return match.Value;

            var referencedAsset = _resolver.Resolve(url, cssAsset.ResolvedPath);
            if (referencedAsset.NewUrl != null)
            {
                return $"url(\"{referencedAsset.NewUrl}\")";
            }

            return match.Value;
        });
    }
}
