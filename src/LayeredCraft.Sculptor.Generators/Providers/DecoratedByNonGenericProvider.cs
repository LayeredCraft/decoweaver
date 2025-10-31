using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sculptor.Model;
using Sculptor.Roslyn;

namespace Sculptor.Providers;

internal static class DecoratedByNonGenericProvider
{
    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax;

    internal static DecoratorToIntercept? Transformer(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol implDef)
            return null;

        var attr = ctx.Attributes[0]; // matched [DecoratedBy(typeof(...), order: ?)]

        var isInterceptable = GetBoolNamedArg(attr, "IsInterceptable", defaultValue: true);
        if (!isInterceptable) return null;

        // Order can be either ctor arg #1 (int) or named arg "Order"
        var order = GetIntNamedArg(attr, "Order", defaultValue: 0);
        if (order == 0 && attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is int ctorOrder)
            order = ctorOrder;

        // First ctor arg is the Type
        if (attr.ConstructorArguments.Length == 0 || attr.ConstructorArguments[0].Kind != TypedConstantKind.Type)
            return null;

        var decoratorSym = (ITypeSymbol?)attr.ConstructorArguments[0].Value;
        if (decoratorSym is null) return null;

        return new DecoratorToIntercept(
            ImplementationDef: implDef.ToTypeId().Definition,
            DecoratorDef:      decoratorSym.ToTypeId().Definition,
            Order:             order,
            IsInterceptable:   true,
            Location:          ctx.TargetNode.ToLocationId());
    }

    private static bool GetBoolNamedArg(AttributeData a, string name, bool defaultValue)
    {
        foreach (var (n, v) in a.NamedArguments)
            if (n == name && v.Value is bool b) return b;
        return defaultValue;
    }

    private static int GetIntNamedArg(AttributeData a, string name, int defaultValue)
    {
        foreach (var (n, v) in a.NamedArguments)
            if (n == name && v.Value is int i) return i;
        return defaultValue;
    }
}