using JinjaCompiler.Core.Assets;
using JinjaCompiler.Core.Context;
using JinjaCompiler.Core.Loading;
using JinjaCompiler.Core.Rendering;
using JinjaCompiler.Core.Filters;

namespace JinjaCompiler.Core;

/// <summary>
/// Main entry point for the Jinja template engine.
/// Provides a fluent API for template compilation.
/// </summary>
public class JinjaEngine
{
    private readonly ITemplateLoader _loader;
    private readonly RenderOptions _options;
    private readonly AssetOptions _assetOptions;
    private readonly FilterRegistry _filters;
    private readonly TemplateRenderer _renderer;
    private readonly AssetProcessor? _assetProcessor;

    private JinjaEngine(
        ITemplateLoader loader,
        RenderOptions options,
        AssetOptions assetOptions,
        FilterRegistry filters)
    {
        _loader = loader;
        _options = options;
        _assetOptions = assetOptions;
        _filters = filters;
        _renderer = new TemplateRenderer(loader, options, filters);

        if (!string.IsNullOrEmpty(assetOptions.StaticRoot))
        {
            _assetProcessor = new AssetProcessor(assetOptions);
        }
    }

    /// <summary>
    /// Creates a new engine builder.
    /// </summary>
    public static JinjaEngineBuilder Create() => new();

    /// <summary>
    /// Renders a template with the given context.
    /// </summary>
    public string Render(string templatePath, RenderContext context)
    {
        return _renderer.Render(templatePath, context);
    }

    /// <summary>
    /// Renders a template with data from a JSON string.
    /// </summary>
    public string Render(string templatePath, string jsonData)
    {
        var context = RenderContext.FromJson(jsonData);
        return _renderer.Render(templatePath, context);
    }

    /// <summary>
    /// Renders a template with data from a dictionary.
    /// </summary>
    public string Render(string templatePath, Dictionary<string, object?> data)
    {
        var context = new RenderContext(data);
        return _renderer.Render(templatePath, context);
    }

    /// <summary>
    /// Renders a template and processes static assets.
    /// Returns the processed HTML and asset information.
    /// </summary>
    public RenderResult RenderWithAssets(string templatePath, RenderContext context)
    {
        var html = _renderer.Render(templatePath, context);
        
        if (_assetProcessor == null)
        {
            return new RenderResult
            {
                Html = html,
                TemplatePath = templatePath,
                AssetResult = null,
                Warnings = []
            };
        }

        var assetResult = _assetProcessor.ProcessHtml(html);
        
        return new RenderResult
        {
            Html = assetResult.Html,
            TemplatePath = templatePath,
            AssetResult = assetResult,
            Warnings = assetResult.Warnings.ToList()
        };
    }

    /// <summary>
    /// Gets the asset processor for direct asset operations.
    /// </summary>
    public AssetProcessor? AssetProcessor => _assetProcessor;

    /// <summary>
    /// Gets all available templates.
    /// </summary>
    public IEnumerable<string> GetAvailableTemplates()
    {
        return _loader.GetAllTemplates();
    }

    /// <summary>
    /// Checks if a template exists.
    /// </summary>
    public bool TemplateExists(string templatePath)
    {
        return _loader.TemplateExists(templatePath);
    }

    /// <summary>
    /// Registers a custom filter.
    /// </summary>
    public void RegisterFilter(string name, Func<object?, object?[], object?> filter)
    {
        _filters.Register(name, filter);
    }

    /// <summary>
    /// Builder for configuring the Jinja engine.
    /// </summary>
    public class JinjaEngineBuilder
    {
        private ITemplateLoader? _loader;
        private RenderOptions _options = new();
        private AssetOptions _assetOptions = new();
        private readonly FilterRegistry _filters = new();

        /// <summary>
        /// Sets the template directory.
        /// </summary>
        public JinjaEngineBuilder WithTemplateDirectory(string directory)
        {
            _loader = new FileSystemTemplateLoader(directory);
            return this;
        }

        /// <summary>
        /// Sets a custom template loader.
        /// </summary>
        public JinjaEngineBuilder WithLoader(ITemplateLoader loader)
        {
            _loader = loader;
            return this;
        }

        /// <summary>
        /// Sets templates from memory (useful for testing).
        /// </summary>
        public JinjaEngineBuilder WithMemoryTemplates(Dictionary<string, string> templates)
        {
            _loader = new MemoryTemplateLoader(templates);
            return this;
        }

        /// <summary>
        /// Configures auto-escaping.
        /// </summary>
        public JinjaEngineBuilder WithAutoEscape(bool enabled = true)
        {
            _options = _options with { AutoEscape = enabled };
            return this;
        }

        /// <summary>
        /// Configures missing variable behavior.
        /// </summary>
        public JinjaEngineBuilder WithMissingVariableBehavior(MissingVariableBehavior behavior)
        {
            _options = _options with { MissingVariableBehavior = behavior };
            return this;
        }

        /// <summary>
        /// Configures block trimming.
        /// </summary>
        public JinjaEngineBuilder WithTrimBlocks(bool enabled = true)
        {
            _options = _options with { TrimBlocks = enabled };
            return this;
        }

        /// <summary>
        /// Configures leading whitespace stripping.
        /// </summary>
        public JinjaEngineBuilder WithLstripBlocks(bool enabled = true)
        {
            _options = _options with { LstripBlocks = enabled };
            return this;
        }

        /// <summary>
        /// Registers a custom filter.
        /// </summary>
        public JinjaEngineBuilder WithFilter(string name, Func<object?, object?[], object?> filter)
        {
            _filters.Register(name, filter);
            return this;
        }

        /// <summary>
        /// Configures static assets root directory.
        /// </summary>
        public JinjaEngineBuilder WithStaticRoot(string staticRoot)
        {
            _assetOptions = _assetOptions with { StaticRoot = staticRoot };
            return this;
        }

        /// <summary>
        /// Configures CSS handling mode.
        /// </summary>
        public JinjaEngineBuilder WithCssMode(CssMode mode)
        {
            _assetOptions = _assetOptions with { CssMode = mode };
            return this;
        }

        /// <summary>
        /// Configures the output directory for static assets.
        /// </summary>
        public JinjaEngineBuilder WithAssetOutputDirectory(string directory)
        {
            _assetOptions = _assetOptions with { AssetOutputDirectory = directory };
            return this;
        }

        /// <summary>
        /// Configures URL prefix for static assets.
        /// </summary>
        public JinjaEngineBuilder WithStaticUrlPrefix(string prefix)
        {
            _assetOptions = _assetOptions with { StaticUrlPrefix = prefix };
            return this;
        }

        /// <summary>
        /// Configures whether to process url_for() calls.
        /// </summary>
        public JinjaEngineBuilder WithUrlForProcessing(bool enabled = true)
        {
            _assetOptions = _assetOptions with { ProcessUrlFor = enabled };
            return this;
        }

        /// <summary>
        /// Configures whether to warn on missing assets.
        /// </summary>
        public JinjaEngineBuilder WithMissingAssetWarnings(bool enabled = true)
        {
            _assetOptions = _assetOptions with { WarnOnMissingAssets = enabled };
            return this;
        }

        /// <summary>
        /// Configures whether to hash asset filenames for cache busting.
        /// </summary>
        public JinjaEngineBuilder WithAssetHashing(bool enabled = true)
        {
            _assetOptions = _assetOptions with { HashAssetNames = enabled };
            return this;
        }

        /// <summary>
        /// Configures whether to minify CSS when inlining.
        /// </summary>
        public JinjaEngineBuilder WithCssMinification(bool enabled = true)
        {
            _assetOptions = _assetOptions with { MinifyCss = enabled };
            return this;
        }

        /// <summary>
        /// Configures complete asset options.
        /// </summary>
        public JinjaEngineBuilder WithAssetOptions(AssetOptions options)
        {
            _assetOptions = options;
            return this;
        }

        /// <summary>
        /// Builds the engine.
        /// </summary>
        public JinjaEngine Build()
        {
            if (_loader == null)
            {
                throw new InvalidOperationException("Template loader must be configured. Use WithTemplateDirectory() or WithLoader().");
            }

            return new JinjaEngine(_loader, _options, _assetOptions, _filters);
        }
    }
}

/// <summary>
/// Result of rendering a template with asset processing.
/// </summary>
public record RenderResult
{
    /// <summary>
    /// The processed HTML content.
    /// </summary>
    public required string Html { get; init; }

    /// <summary>
    /// The template path that was rendered.
    /// </summary>
    public required string TemplatePath { get; init; }

    /// <summary>
    /// Asset processing result, if assets were processed.
    /// </summary>
    public AssetProcessingResult? AssetResult { get; init; }

    /// <summary>
    /// Warnings generated during rendering and asset processing.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}
