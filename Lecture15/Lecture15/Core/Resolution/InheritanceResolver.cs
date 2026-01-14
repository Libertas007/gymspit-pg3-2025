using JinjaCompiler.Core.Ast;
using JinjaCompiler.Core.Exceptions;
using JinjaCompiler.Core.Loading;
using JinjaCompiler.Core.Parsing;

namespace JinjaCompiler.Core.Resolution;

/// <summary>
/// Manages template caching and parsing.
/// </summary>
public class TemplateCache
{
    private readonly ITemplateLoader _loader;
    private readonly Dictionary<string, TemplateRoot> _parsedTemplates = new();

    public TemplateCache(ITemplateLoader loader)
    {
        _loader = loader;
    }

    public TemplateRoot GetTemplate(string templatePath)
    {
        var normalized = NormalizePath(templatePath);

        if (_parsedTemplates.TryGetValue(normalized, out var cached))
        {
            return cached;
        }

        var content = _loader.LoadTemplate(normalized);
        var lexer = new JinjaLexer(content, normalized);
        var tokens = lexer.Tokenize().ToList();
        var parser = new JinjaParser(tokens, normalized);
        var template = parser.Parse();

        _parsedTemplates[normalized] = template;
        return template;
    }

    public bool TemplateExists(string templatePath)
    {
        return _loader.TemplateExists(NormalizePath(templatePath));
    }

    public void Clear()
    {
        _parsedTemplates.Clear();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}

/// <summary>
/// Resolves template inheritance and blocks.
/// </summary>
public class InheritanceResolver
{
    private readonly TemplateCache _cache;

    public InheritanceResolver(TemplateCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Resolves the complete inheritance chain and returns a flattened template.
    /// </summary>
    public ResolvedTemplate Resolve(string templatePath)
    {
        var inheritanceChain = new List<string>();
        var blockOverrides = new Dictionary<string, BlockNode>(StringComparer.OrdinalIgnoreCase);

        return ResolveRecursive(templatePath, inheritanceChain, blockOverrides);
    }

    private ResolvedTemplate ResolveRecursive(
        string templatePath,
        List<string> inheritanceChain,
        Dictionary<string, BlockNode> blockOverrides)
    {
        // Check for circular inheritance
        if (inheritanceChain.Contains(templatePath, StringComparer.OrdinalIgnoreCase))
        {
            inheritanceChain.Add(templatePath);
            throw new CircularInheritanceException(inheritanceChain);
        }

        inheritanceChain.Add(templatePath);

        var template = _cache.GetTemplate(templatePath);
        var extendsNode = template.Extends;

        // Collect blocks from this template
        CollectBlocks(template.Children, blockOverrides);

        if (extendsNode != null)
        {
            // This template extends another - resolve the parent first
            return ResolveRecursive(extendsNode.ParentTemplatePath, inheritanceChain, blockOverrides);
        }

        // This is the root template (no extends)
        // Apply all block overrides
        var resolvedNodes = ApplyBlockOverrides(template.Children, blockOverrides);

        return new ResolvedTemplate(
            templatePath,
            resolvedNodes,
            inheritanceChain.AsReadOnly(),
            blockOverrides);
    }

    private void CollectBlocks(IEnumerable<TemplateNode> nodes, Dictionary<string, BlockNode> blocks)
    {
        foreach (var node in nodes)
        {
            if (node is BlockNode block)
            {
                // Child templates override parent blocks
                if (!blocks.ContainsKey(block.Name))
                {
                    blocks[block.Name] = block;
                }

                // Also collect nested blocks
                CollectBlocks(block.Children, blocks);
            }
        }
    }

    private IReadOnlyList<TemplateNode> ApplyBlockOverrides(
        IEnumerable<TemplateNode> nodes,
        Dictionary<string, BlockNode> blockOverrides)
    {
        var result = new List<TemplateNode>();

        foreach (var node in nodes)
        {
            if (node is BlockNode block)
            {
                // Replace with override if exists
                if (blockOverrides.TryGetValue(block.Name, out var overrideBlock))
                {
                    // Recursively apply overrides to nested blocks
                    var resolvedChildren = ApplyBlockOverrides(overrideBlock.Children, blockOverrides);
                    result.Add(new BlockNode(overrideBlock.Name, resolvedChildren, overrideBlock.Scoped)
                    {
                        StartPosition = overrideBlock.StartPosition,
                        EndPosition = overrideBlock.EndPosition,
                        Line = overrideBlock.Line,
                        Column = overrideBlock.Column
                    });
                }
                else
                {
                    // Keep the original block with resolved children
                    var resolvedChildren = ApplyBlockOverrides(block.Children, blockOverrides);
                    result.Add(new BlockNode(block.Name, resolvedChildren, block.Scoped)
                    {
                        StartPosition = block.StartPosition,
                        EndPosition = block.EndPosition,
                        Line = block.Line,
                        Column = block.Column
                    });
                }
            }
            else
            {
                result.Add(node);
            }
        }

        return result;
    }
}

/// <summary>
/// Represents a fully resolved template after inheritance processing.
/// </summary>
public record ResolvedTemplate(
    string TemplatePath,
    IReadOnlyList<TemplateNode> Nodes,
    IReadOnlyList<string> InheritanceChain,
    IReadOnlyDictionary<string, BlockNode> BlockDefinitions);
