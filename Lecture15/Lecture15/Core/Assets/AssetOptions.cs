namespace JinjaCompiler.Core.Assets;

/// <summary>
/// Configuration options for static asset handling.
/// </summary>
public record AssetOptions
{
    /// <summary>
    /// The root directory for static assets (e.g., Flask's static folder).
    /// </summary>
    public string? StaticRoot { get; init; }

    /// <summary>
    /// How CSS files should be handled.
    /// </summary>
    public CssMode CssMode { get; init; } = CssMode.Copy;

    /// <summary>
    /// The output subdirectory for copied static assets.
    /// </summary>
    public string AssetOutputDirectory { get; init; } = "static";

    /// <summary>
    /// Whether to minify CSS when inlining.
    /// </summary>
    public bool MinifyCss { get; init; } = false;

    /// <summary>
    /// Whether to process url_for() calls in templates.
    /// </summary>
    public bool ProcessUrlFor { get; init; } = true;

    /// <summary>
    /// Custom URL prefix for static assets (e.g., "/static/").
    /// </summary>
    public string StaticUrlPrefix { get; init; } = "/static/";

    /// <summary>
    /// Whether to emit warnings for missing assets.
    /// </summary>
    public bool WarnOnMissingAssets { get; init; } = true;

    /// <summary>
    /// Whether to hash asset filenames for cache busting.
    /// </summary>
    public bool HashAssetNames { get; init; } = false;
}

/// <summary>
/// Defines how CSS files should be handled.
/// </summary>
public enum CssMode
{
    /// <summary>
    /// Copy CSS files to output directory and update paths.
    /// </summary>
    Copy,

    /// <summary>
    /// Inline CSS content into style tags in the HTML.
    /// </summary>
    Inline,

    /// <summary>
    /// Leave CSS references unchanged (passthrough).
    /// </summary>
    Passthrough
}

/// <summary>
/// Represents a resolved static asset.
/// </summary>
public record ResolvedAsset
{
    /// <summary>
    /// The original path as referenced in the template.
    /// </summary>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// The resolved file system path.
    /// </summary>
    public required string ResolvedPath { get; init; }

    /// <summary>
    /// Whether the asset file exists.
    /// </summary>
    public bool Exists { get; init; }

    /// <summary>
    /// The asset type (css, js, image, etc.).
    /// </summary>
    public AssetType Type { get; init; }

    /// <summary>
    /// The output path for the asset (in copy mode).
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// The new URL to use in the HTML.
    /// </summary>
    public string? NewUrl { get; init; }

    /// <summary>
    /// The content of the asset (for inline mode).
    /// </summary>
    public string? Content { get; init; }
}

/// <summary>
/// Types of static assets.
/// </summary>
public enum AssetType
{
    Css,
    JavaScript,
    Image,
    Font,
    Other
}

/// <summary>
/// Result of processing HTML for static assets.
/// </summary>
public record AssetProcessingResult
{
    /// <summary>
    /// The processed HTML content.
    /// </summary>
    public required string Html { get; init; }

    /// <summary>
    /// List of assets that were processed.
    /// </summary>
    public required IReadOnlyList<ResolvedAsset> ProcessedAssets { get; init; }

    /// <summary>
    /// Warnings generated during processing.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Assets that need to be copied to output.
    /// </summary>
    public required IReadOnlyList<ResolvedAsset> AssetsToCopy { get; init; }
}
