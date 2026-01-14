using System.CommandLine;
using System.Text.Json;
using JinjaCompiler.Core;
using JinjaCompiler.Core.Assets;
using JinjaCompiler.Core.Context;
using JinjaCompiler.Core.Rendering;

namespace JinjaCompiler.Cli;

/// <summary>
/// Command-line interface for the Jinja template compiler.
/// </summary>
public class JinjaCompilerCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        var rootCommand = new RootCommand("Jinja2/Flask template compiler - generates static HTML from templates")
        {
            Name = "jinja-compiler"
        };

        // Build command
        var buildCommand = CreateBuildCommand();
        rootCommand.AddCommand(buildCommand);

        // Watch command
        var watchCommand = CreateWatchCommand();
        rootCommand.AddCommand(watchCommand);

        // List command
        var listCommand = CreateListCommand();
        rootCommand.AddCommand(listCommand);

        // Validate command
        var validateCommand = CreateValidateCommand();
        rootCommand.AddCommand(validateCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateBuildCommand()
    {
        var templateDirOption = new Option<DirectoryInfo>(
            aliases: ["--templates", "-t"],
            description: "Directory containing Jinja2 templates")
        {
            IsRequired = true
        };

        var dataFileOption = new Option<FileInfo?>(
            aliases: ["--data", "-d"],
            description: "JSON file containing template variables");

        var outputDirOption = new Option<DirectoryInfo>(
            aliases: ["--output", "-o"],
            description: "Output directory for generated HTML files")
        {
            IsRequired = true
        };

        var templateOption = new Option<string?>(
            aliases: ["--template", "-T"],
            description: "Specific template to compile (optional, compiles all if not specified)");

        var noEscapeOption = new Option<bool>(
            aliases: ["--no-escape"],
            description: "Disable HTML auto-escaping");

        var strictOption = new Option<bool>(
            aliases: ["--strict"],
            description: "Throw errors for missing variables instead of using empty strings");

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Enable verbose output");

        // New options for static assets
        var staticRootOption = new Option<DirectoryInfo?>(
            aliases: ["--static-root", "-s"],
            description: "Root directory for static assets (e.g., Flask's static folder)");

        var cssModeOption = new Option<string>(
            aliases: ["--css-mode"],
            description: "How to handle CSS files: 'copy', 'inline', or 'passthrough'")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        cssModeOption.SetDefaultValue("copy");

        var staticPrefixOption = new Option<string>(
            aliases: ["--static-prefix"],
            description: "URL prefix for static assets (default: /static/)");
        staticPrefixOption.SetDefaultValue("/static/");

        var hashAssetsOption = new Option<bool>(
            aliases: ["--hash-assets"],
            description: "Add content hash to asset filenames for cache busting");

        var minifyCssOption = new Option<bool>(
            aliases: ["--minify-css"],
            description: "Minify CSS when using inline mode");

        var command = new Command("build", "Compile templates to static HTML files")
        {
            templateDirOption,
            dataFileOption,
            outputDirOption,
            templateOption,
            noEscapeOption,
            strictOption,
            verboseOption,
            staticRootOption,
            cssModeOption,
            staticPrefixOption,
            hashAssetsOption,
            minifyCssOption
        };

        command.SetHandler(async (context) =>
        {
            var templateDir = context.ParseResult.GetValueForOption(templateDirOption)!;
            var dataFile = context.ParseResult.GetValueForOption(dataFileOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption)!;
            var template = context.ParseResult.GetValueForOption(templateOption);
            var noEscape = context.ParseResult.GetValueForOption(noEscapeOption);
            var strict = context.ParseResult.GetValueForOption(strictOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var staticRoot = context.ParseResult.GetValueForOption(staticRootOption);
            var cssMode = context.ParseResult.GetValueForOption(cssModeOption) ?? "copy";
            var staticPrefix = context.ParseResult.GetValueForOption(staticPrefixOption) ?? "/static/";
            var hashAssets = context.ParseResult.GetValueForOption(hashAssetsOption);
            var minifyCss = context.ParseResult.GetValueForOption(minifyCssOption);

            var buildOptions = new BuildOptions
            {
                TemplateDir = templateDir,
                DataFile = dataFile,
                OutputDir = outputDir,
                SpecificTemplate = template,
                NoEscape = noEscape,
                Strict = strict,
                Verbose = verbose,
                StaticRoot = staticRoot,
                CssMode = ParseCssMode(cssMode),
                StaticPrefix = staticPrefix,
                HashAssets = hashAssets,
                MinifyCss = minifyCss
            };

            await BuildTemplates(buildOptions);
        });

        return command;
    }

    private static Command CreateWatchCommand()
    {
        var templateDirOption = new Option<DirectoryInfo>(
            aliases: ["--templates", "-t"],
            description: "Directory containing Jinja2 templates")
        {
            IsRequired = true
        };

        var dataFileOption = new Option<FileInfo?>(
            aliases: ["--data", "-d"],
            description: "JSON file containing template variables");

        var outputDirOption = new Option<DirectoryInfo>(
            aliases: ["--output", "-o"],
            description: "Output directory for generated HTML files")
        {
            IsRequired = true
        };

        var noEscapeOption = new Option<bool>(
            aliases: ["--no-escape"],
            description: "Disable HTML auto-escaping");

        var staticRootOption = new Option<DirectoryInfo?>(
            aliases: ["--static-root", "-s"],
            description: "Root directory for static assets");

        var cssModeOption = new Option<string>(
            aliases: ["--css-mode"],
            description: "How to handle CSS files: 'copy', 'inline', or 'passthrough'");
        cssModeOption.SetDefaultValue("copy");

        var command = new Command("watch", "Watch for template changes and auto-rebuild")
        {
            templateDirOption,
            dataFileOption,
            outputDirOption,
            noEscapeOption,
            staticRootOption,
            cssModeOption
        };

        command.SetHandler(async (context) =>
        {
            var templateDir = context.ParseResult.GetValueForOption(templateDirOption)!;
            var dataFile = context.ParseResult.GetValueForOption(dataFileOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption)!;
            var noEscape = context.ParseResult.GetValueForOption(noEscapeOption);
            var staticRoot = context.ParseResult.GetValueForOption(staticRootOption);
            var cssMode = context.ParseResult.GetValueForOption(cssModeOption) ?? "copy";

            var buildOptions = new BuildOptions
            {
                TemplateDir = templateDir,
                DataFile = dataFile,
                OutputDir = outputDir,
                NoEscape = noEscape,
                Verbose = true,
                StaticRoot = staticRoot,
                CssMode = ParseCssMode(cssMode)
            };

            await WatchTemplates(buildOptions);
        });

        return command;
    }

    private static Command CreateListCommand()
    {
        var templateDirOption = new Option<DirectoryInfo>(
            aliases: ["--templates", "-t"],
            description: "Directory containing Jinja2 templates")
        {
            IsRequired = true
        };

        var command = new Command("list", "List all available templates")
        {
            templateDirOption
        };

        command.SetHandler((templateDir) =>
        {
            ListTemplates(templateDir);
        }, templateDirOption);

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var templateDirOption = new Option<DirectoryInfo>(
            aliases: ["--templates", "-t"],
            description: "Directory containing Jinja2 templates")
        {
            IsRequired = true
        };

        var dataFileOption = new Option<FileInfo?>(
            aliases: ["--data", "-d"],
            description: "JSON file containing template variables");

        var staticRootOption = new Option<DirectoryInfo?>(
            aliases: ["--static-root", "-s"],
            description: "Root directory for static assets (validates CSS references)");

        var command = new Command("validate", "Validate templates without generating output")
        {
            templateDirOption,
            dataFileOption,
            staticRootOption
        };

        command.SetHandler((templateDir, dataFile, staticRoot) =>
        {
            ValidateTemplates(templateDir, dataFile, staticRoot);
        }, templateDirOption, dataFileOption, staticRootOption);

        return command;
    }

    private static async Task BuildTemplates(BuildOptions options)
    {
        try
        {
            if (!options.TemplateDir.Exists)
            {
                Console.Error.WriteLine($"Error: Template directory not found: {options.TemplateDir.FullName}");
                Environment.ExitCode = 1;
                return;
            }

            // Create output directory if it doesn't exist
            if (!options.OutputDir.Exists)
            {
                options.OutputDir.Create();
            }

            // Load context data
            var context = LoadContext(options.DataFile);

            // Build engine with asset processing
            var engineBuilder = JinjaEngine.Create()
                .WithTemplateDirectory(options.TemplateDir.FullName)
                .WithAutoEscape(!options.NoEscape)
                .WithMissingVariableBehavior(options.Strict
                    ? MissingVariableBehavior.ThrowException
                    : MissingVariableBehavior.EmptyString);

            // Configure static assets if specified
            if (options.StaticRoot?.Exists == true)
            {
                engineBuilder
                    .WithStaticRoot(options.StaticRoot.FullName)
                    .WithCssMode(options.CssMode)
                    .WithStaticUrlPrefix(options.StaticPrefix)
                    .WithAssetHashing(options.HashAssets)
                    .WithCssMinification(options.MinifyCss);
            }

            var engine = engineBuilder.Build();

            // Get templates to compile
            var templates = options.SpecificTemplate != null
                ? new[] { options.SpecificTemplate }
                : engine.GetAvailableTemplates()
                    .Where(t => !IsPartialTemplate(t))
                    .ToArray();

            var successCount = 0;
            var errorCount = 0;
            var warningCount = 0;
            var allAssetResults = new List<AssetProcessingResult>();

            foreach (var template in templates)
            {
                try
                {
                    if (options.Verbose)
                    {
                        Console.WriteLine($"Compiling: {template}");
                    }

                    // Render with asset processing if static root is configured
                    RenderResult result;
                    if (options.StaticRoot?.Exists == true)
                    {
                        result = engine.RenderWithAssets(template, context);
                        if (result.AssetResult != null)
                        {
                            allAssetResults.Add(result.AssetResult);
                        }
                    }
                    else
                    {
                        var html = engine.Render(template, context);
                        result = new RenderResult
                        {
                            Html = html,
                            TemplatePath = template,
                            Warnings = []
                        };
                    }

                    // Output warnings
                    foreach (var warning in result.Warnings)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  Warning: {warning}");
                        Console.ResetColor();
                        warningCount++;
                    }

                    var outputPath = Path.Combine(
                        options.OutputDir.FullName,
                        Path.ChangeExtension(template, ".html"));

                    var outputFileDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputFileDir) && !Directory.Exists(outputFileDir))
                    {
                        Directory.CreateDirectory(outputFileDir);
                    }

                    await File.WriteAllTextAsync(outputPath, result.Html);
                    successCount++;

                    if (options.Verbose)
                    {
                        Console.WriteLine($"  -> {outputPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error compiling {template}: {ex.Message}");
                    errorCount++;
                }
            }

            // Copy assets if using copy mode
            if (options.CssMode == CssMode.Copy && engine.AssetProcessor != null && allAssetResults.Count > 0)
            {
                if (options.Verbose)
                {
                    Console.WriteLine("\nCopying static assets...");
                }

                await engine.AssetProcessor.CopyAllAssetsAsync(allAssetResults, options.OutputDir.FullName);

                var totalAssets = allAssetResults.SelectMany(r => r.AssetsToCopy).DistinctBy(a => a.OutputPath).Count();
                if (options.Verbose && totalAssets > 0)
                {
                    Console.WriteLine($"  Copied {totalAssets} asset(s)");
                }
            }

            Console.WriteLine($"\nBuild complete: {successCount} succeeded, {errorCount} failed, {warningCount} warnings");

            if (errorCount > 0)
            {
                Environment.ExitCode = 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static async Task WatchTemplates(BuildOptions options)
    {
        Console.WriteLine($"Watching for changes in: {options.TemplateDir.FullName}");
        if (options.StaticRoot?.Exists == true)
        {
            Console.WriteLine($"Static root: {options.StaticRoot.FullName}");
            Console.WriteLine($"CSS mode: {options.CssMode}");
        }
        Console.WriteLine("Press Ctrl+C to stop...\n");

        using var watcher = new FileSystemWatcher(options.TemplateDir.FullName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        var debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
        var changedFiles = new HashSet<string>();

        debounceTimer.Elapsed += async (s, e) =>
        {
            string[] filesToProcess;
            lock (changedFiles)
            {
                filesToProcess = changedFiles.ToArray();
                changedFiles.Clear();
            }

            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Detected changes in {filesToProcess.Length} file(s)");
            await BuildTemplates(options);
        };

        void OnChange(object sender, FileSystemEventArgs e)
        {
            if (!IsTemplateFile(e.FullPath) && !IsCssFile(e.FullPath)) return;

            lock (changedFiles)
            {
                changedFiles.Add(e.FullPath);
            }

            debounceTimer.Stop();
            debounceTimer.Start();
        }

        watcher.Changed += OnChange;
        watcher.Created += OnChange;
        watcher.Deleted += OnChange;
        watcher.Renamed += (s, e) => OnChange(s, e);

        // Also watch the data file if specified
        FileSystemWatcher? dataWatcher = null;
        if (options.DataFile?.Exists == true)
        {
            dataWatcher = new FileSystemWatcher(options.DataFile.DirectoryName!)
            {
                Filter = options.DataFile.Name,
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            dataWatcher.Changed += (s, e) =>
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Data file changed");
                debounceTimer.Stop();
                debounceTimer.Start();
            };
        }

        // Watch static directory if specified
        FileSystemWatcher? staticWatcher = null;
        if (options.StaticRoot?.Exists == true)
        {
            staticWatcher = new FileSystemWatcher(options.StaticRoot.FullName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            staticWatcher.Changed += (s, e) =>
            {
                if (!IsCssFile(e.FullPath)) return;
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] CSS file changed: {e.Name}");
                debounceTimer.Stop();
                debounceTimer.Start();
            };
        }

        // Initial build
        await BuildTemplates(options);

        // Wait for Ctrl+C
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nStopping watcher...");
        }
        finally
        {
            dataWatcher?.Dispose();
            staticWatcher?.Dispose();
        }
    }

    private static void ListTemplates(DirectoryInfo templateDir)
    {
        try
        {
            var engine = JinjaEngine.Create()
                .WithTemplateDirectory(templateDir.FullName)
                .Build();

            var templates = engine.GetAvailableTemplates().ToList();

            Console.WriteLine($"Found {templates.Count} template(s) in {templateDir.FullName}:\n");

            foreach (var template in templates.OrderBy(t => t))
            {
                var isPartial = IsPartialTemplate(template);
                var prefix = isPartial ? "  [partial] " : "  ";
                Console.WriteLine($"{prefix}{template}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static void ValidateTemplates(DirectoryInfo templateDir, FileInfo? dataFile, DirectoryInfo? staticRoot)
    {
        try
        {
            var engineBuilder = JinjaEngine.Create()
                .WithTemplateDirectory(templateDir.FullName)
                .WithMissingVariableBehavior(MissingVariableBehavior.ShowPlaceholder);

            if (staticRoot?.Exists == true)
            {
                engineBuilder.WithStaticRoot(staticRoot.FullName);
            }

            var engine = engineBuilder.Build();

            var context = LoadContext(dataFile);
            var templates = engine.GetAvailableTemplates()
                .Where(t => !IsPartialTemplate(t))
                .ToList();

            var validCount = 0;
            var errorCount = 0;
            var warningCount = 0;

            Console.WriteLine($"Validating {templates.Count} template(s)...\n");

            foreach (var template in templates)
            {
                try
                {
                    RenderResult result;
                    if (staticRoot?.Exists == true)
                    {
                        result = engine.RenderWithAssets(template, context);
                    }
                    else
                    {
                        var html = engine.Render(template, context);
                        result = new RenderResult
                        {
                            Html = html,
                            TemplatePath = template,
                            Warnings = []
                        };
                    }

                    if (result.Warnings.Count > 0)
                    {
                        Console.WriteLine($"  ? {template}");
                        foreach (var warning in result.Warnings)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"      {warning}");
                            Console.ResetColor();
                            warningCount++;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ? {template}");
                    }
                    validCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ? {template}: {ex.Message}");
                    errorCount++;
                }
            }

            Console.WriteLine($"\nValidation complete: {validCount} valid, {errorCount} errors, {warningCount} warnings");

            if (errorCount > 0)
            {
                Environment.ExitCode = 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static RenderContext LoadContext(FileInfo? dataFile)
    {
        if (dataFile == null || !dataFile.Exists)
        {
            return new RenderContext();
        }

        try
        {
            return RenderContext.FromJsonFile(dataFile.FullName);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Warning: Invalid JSON in {dataFile.Name}: {ex.Message}");
            return new RenderContext();
        }
    }

    private static CssMode ParseCssMode(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "copy" => CssMode.Copy,
            "inline" => CssMode.Inline,
            "passthrough" or "pass" => CssMode.Passthrough,
            _ => CssMode.Copy
        };
    }

    private static bool IsPartialTemplate(string templatePath)
    {
        var fileName = Path.GetFileName(templatePath);
        return fileName.StartsWith('_') ||
               templatePath.Contains("partials/", StringComparison.OrdinalIgnoreCase) ||
               templatePath.Contains("includes/", StringComparison.OrdinalIgnoreCase) ||
               templatePath.Contains("layouts/", StringComparison.OrdinalIgnoreCase) ||
               templatePath.Contains("macros/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTemplateFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".html" or ".jinja" or ".jinja2" or ".j2";
    }

    private static bool IsCssFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".css";
    }
}

/// <summary>
/// Build options container.
/// </summary>
internal record BuildOptions
{
    public required DirectoryInfo TemplateDir { get; init; }
    public FileInfo? DataFile { get; init; }
    public required DirectoryInfo OutputDir { get; init; }
    public string? SpecificTemplate { get; init; }
    public bool NoEscape { get; init; }
    public bool Strict { get; init; }
    public bool Verbose { get; init; }
    public DirectoryInfo? StaticRoot { get; init; }
    public CssMode CssMode { get; init; } = CssMode.Copy;
    public string StaticPrefix { get; init; } = "/static/";
    public bool HashAssets { get; init; }
    public bool MinifyCss { get; init; }
}
