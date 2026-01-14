using System.Text;
using System.Text.RegularExpressions;

namespace JinjaCompiler.Core.Assets;

/// <summary>
/// Processes CSS references in HTML content.
/// </summary>
public partial class CssProcessor
{
    private readonly StaticAssetResolver _resolver;
    private readonly AssetOptions _options;
    private readonly HashSet<string> _processedCssFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _warnings = [];

    // Regex patterns for CSS detection
    [GeneratedRegex(@"<link\s+[^>]*rel\s*=\s*[""']stylesheet[""'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex LinkTagPattern();

    [GeneratedRegex(@"href\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefPattern();

    [GeneratedRegex(@"<style[^>]*>(.*?)</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex StyleTagPattern();

    public CssProcessor(StaticAssetResolver resolver, AssetOptions options)
    {
        _resolver = resolver;
        _options = options;
    }

    /// <summary>
    /// Processes CSS references in the HTML content.
    /// </summary>
    public AssetProcessingResult ProcessCss(string html)
    {
        _processedCssFiles.Clear();
        _warnings.Clear();

        var processedAssets = new List<ResolvedAsset>();
        var assetsToCopy = new List<ResolvedAsset>();

        switch (_options.CssMode)
        {
            case CssMode.Inline:
                html = ProcessCssInlineMode(html, processedAssets);
                break;

            case CssMode.Copy:
                html = ProcessCssCopyMode(html, processedAssets, assetsToCopy);
                break;

            case CssMode.Passthrough:
                // Just collect info without modifying
                CollectCssReferences(html, processedAssets);
                break;
        }

        return new AssetProcessingResult
        {
            Html = html,
            ProcessedAssets = processedAssets.AsReadOnly(),
            Warnings = _warnings.AsReadOnly(),
            AssetsToCopy = assetsToCopy.AsReadOnly()
        };
    }

    private string ProcessCssInlineMode(string html, List<ResolvedAsset> processedAssets)
    {
        var inlinedStyles = new StringBuilder();
        var linksToRemove = new List<(int Start, int Length)>();

        // Find all link tags
        var matches = LinkTagPattern().Matches(html);
        foreach (Match match in matches)
        {
            var hrefMatch = HrefPattern().Match(match.Value);
            if (!hrefMatch.Success) continue;

            var href = hrefMatch.Groups[1].Value;

            // Skip external URLs
            if (IsExternalUrl(href)) continue;

            // Skip already processed
            if (!_processedCssFiles.Add(href)) continue;

            var asset = _resolver.Resolve(href);
            processedAssets.Add(asset);

            if (!asset.Exists)
            {
                if (_options.WarnOnMissingAssets)
                {
                    _warnings.Add($"CSS file not found: {href} (resolved to: {asset.ResolvedPath})");
                }
                continue;
            }

            // Read and inline the CSS
            var cssContent = asset.Content ?? File.ReadAllText(asset.ResolvedPath);

            // Process @import statements recursively
            cssContent = ProcessCssImports(cssContent, asset.ResolvedPath, processedAssets);

            // Process url() references in CSS
            cssContent = ProcessCssUrls(cssContent, asset.ResolvedPath);

            inlinedStyles.AppendLine($"/* Inlined from: {href} */");
            inlinedStyles.AppendLine(cssContent);
            inlinedStyles.AppendLine();

            linksToRemove.Add((match.Index, match.Length));
        }

        // Remove link tags (in reverse order to preserve indices)
        var result = new StringBuilder(html);
        foreach (var (start, length) in linksToRemove.OrderByDescending(x => x.Start))
        {
            result.Remove(start, length);
        }

        // Insert inlined styles before </head> or at the start
        if (inlinedStyles.Length > 0)
        {
            var styleTag = $"\n<style>\n{inlinedStyles}</style>\n";
            var headCloseIndex = result.ToString().IndexOf("</head>", StringComparison.OrdinalIgnoreCase);

            if (headCloseIndex >= 0)
            {
                result.Insert(headCloseIndex, styleTag);
            }
            else
            {
                // Fallback: insert after opening html tag or at start
                var htmlOpenIndex = result.ToString().IndexOf("<html", StringComparison.OrdinalIgnoreCase);
                if (htmlOpenIndex >= 0)
                {
                    var closeTagIndex = result.ToString().IndexOf('>', htmlOpenIndex);
                    if (closeTagIndex >= 0)
                    {
                        result.Insert(closeTagIndex + 1, styleTag);
                    }
                }
            }
        }

        return result.ToString();
    }

    private string ProcessCssCopyMode(string html, List<ResolvedAsset> processedAssets, List<ResolvedAsset> assetsToCopy)
    {
        return LinkTagPattern().Replace(html, match =>
        {
            var hrefMatch = HrefPattern().Match(match.Value);
            if (!hrefMatch.Success) return match.Value;

            var href = hrefMatch.Groups[1].Value;

            // Skip external URLs
            if (IsExternalUrl(href)) return match.Value;

            var asset = _resolver.Resolve(href);
            processedAssets.Add(asset);

            if (!asset.Exists)
            {
                if (_options.WarnOnMissingAssets)
                {
                    _warnings.Add($"CSS file not found: {href} (resolved to: {asset.ResolvedPath})");
                }
                return match.Value;
            }

            // Add to copy list (avoid duplicates)
            if (!assetsToCopy.Any(a => a.ResolvedPath.Equals(asset.ResolvedPath, StringComparison.OrdinalIgnoreCase)))
            {
                assetsToCopy.Add(asset);
            }

            // Update the href to the new path
            if (asset.NewUrl != null)
            {
                return match.Value.Replace(
                    hrefMatch.Value,
                    $"href=\"{asset.NewUrl}\"");
            }

            return match.Value;
        });
    }

    private void CollectCssReferences(string html, List<ResolvedAsset> processedAssets)
    {
        var matches = LinkTagPattern().Matches(html);
        foreach (Match match in matches)
        {
            var hrefMatch = HrefPattern().Match(match.Value);
            if (!hrefMatch.Success) continue;

            var href = hrefMatch.Groups[1].Value;
            if (IsExternalUrl(href)) continue;

            var asset = _resolver.Resolve(href);
            processedAssets.Add(asset);

            if (!asset.Exists && _options.WarnOnMissingAssets)
            {
                _warnings.Add($"CSS file not found: {href}");
            }
        }
    }

    private string ProcessCssImports(string css, string cssFilePath, List<ResolvedAsset> processedAssets)
    {
        // Match @import statements
        var importPattern = new Regex(@"@import\s+(?:url\s*\(\s*)?[""']?([^""'\)]+)[""']?\s*\)?[^;]*;", RegexOptions.IgnoreCase);

        return importPattern.Replace(css, match =>
        {
            var importPath = match.Groups[1].Value.Trim();

            // Skip external URLs
            if (IsExternalUrl(importPath)) return match.Value;

            // Skip already processed
            if (!_processedCssFiles.Add(importPath)) return "/* Already imported */";

            var cssDir = Path.GetDirectoryName(cssFilePath) ?? ".";
            var asset = _resolver.Resolve(importPath, cssFilePath);
            processedAssets.Add(asset);

            if (!asset.Exists)
            {
                if (_options.WarnOnMissingAssets)
                {
                    _warnings.Add($"Imported CSS file not found: {importPath}");
                }
                return match.Value;
            }

            var importedCss = File.ReadAllText(asset.ResolvedPath);

            // Recursively process imports
            importedCss = ProcessCssImports(importedCss, asset.ResolvedPath, processedAssets);

            return $"/* @import {importPath} */\n{importedCss}\n";
        });
    }

    private string ProcessCssUrls(string css, string cssFilePath)
    {
        // Match url() references in CSS
        var urlPattern = new Regex(@"url\s*\(\s*[""']?([^""'\)]+)[""']?\s*\)", RegexOptions.IgnoreCase);

        return urlPattern.Replace(css, match =>
        {
            var url = match.Groups[1].Value.Trim();

            // Skip data URIs and external URLs
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return match.Value;
            if (IsExternalUrl(url)) return match.Value;

            // Resolve the URL relative to the CSS file
            var asset = _resolver.Resolve(url, cssFilePath);

            if (!asset.Exists)
            {
                if (_options.WarnOnMissingAssets)
                {
                    _warnings.Add($"Asset referenced in CSS not found: {url}");
                }
                return match.Value;
            }

            if (asset.NewUrl != null)
            {
                return $"url(\"{asset.NewUrl}\")";
            }

            return match.Value;
        });
    }

    private static bool IsExternalUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("//");
    }
}
