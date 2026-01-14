namespace JinjaCompiler.Core.Ast;

/// <summary>
/// Visitor pattern interface for traversing the AST.
/// </summary>
public interface INodeVisitor<T>
{
    T VisitRoot(TemplateRoot node);
    T VisitText(TextNode node);
    T VisitVariable(VariableNode node);
    T VisitExtends(ExtendsNode node);
    T VisitBlock(BlockNode node);
    T VisitInclude(IncludeNode node);
    T VisitIf(IfNode node);
    T VisitFor(ForNode node);
    T VisitSet(SetNode node);
    T VisitMacro(MacroNode node);
    T VisitMacroCall(MacroCallNode node);
    T VisitImport(ImportNode node);
    T VisitFromImport(FromImportNode node);
    T VisitComment(CommentNode node);
    T VisitSuper(SuperNode node);
    T VisitRaw(RawNode node);
    T VisitWith(WithNode node);
    T VisitAutoescape(AutoescapeNode node);
}

/// <summary>
/// Base visitor with default implementations that visit children.
/// </summary>
public abstract class BaseNodeVisitor<T> : INodeVisitor<T>
{
    protected abstract T DefaultResult { get; }
    protected abstract T CombineResults(T first, T second);

    public virtual T VisitRoot(TemplateRoot node)
    {
        return VisitChildren(node.Children);
    }

    public virtual T VisitText(TextNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitVariable(VariableNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitExtends(ExtendsNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitBlock(BlockNode node)
    {
        return VisitChildren(node.Children);
    }

    public virtual T VisitInclude(IncludeNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitIf(IfNode node)
    {
        var result = VisitChildren(node.ThenBranch);

        foreach (var elseIf in node.ElseIfBranches)
        {
            result = CombineResults(result, VisitChildren(elseIf.Body));
        }

        if (node.ElseBranch != null)
        {
            result = CombineResults(result, VisitChildren(node.ElseBranch));
        }

        return result;
    }

    public virtual T VisitFor(ForNode node)
    {
        var result = VisitChildren(node.Body);
        if (node.ElseBody != null)
        {
            result = CombineResults(result, VisitChildren(node.ElseBody));
        }
        return result;
    }

    public virtual T VisitSet(SetNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitMacro(MacroNode node)
    {
        return VisitChildren(node.Body);
    }

    public virtual T VisitMacroCall(MacroCallNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitImport(ImportNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitFromImport(FromImportNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitComment(CommentNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitSuper(SuperNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitRaw(RawNode node)
    {
        return DefaultResult;
    }

    public virtual T VisitWith(WithNode node)
    {
        return VisitChildren(node.Body);
    }

    public virtual T VisitAutoescape(AutoescapeNode node)
    {
        return VisitChildren(node.Body);
    }

    protected T VisitChildren(IEnumerable<TemplateNode> children)
    {
        var result = DefaultResult;
        foreach (var child in children)
        {
            result = CombineResults(result, child.Accept(this));
        }
        return result;
    }
}
