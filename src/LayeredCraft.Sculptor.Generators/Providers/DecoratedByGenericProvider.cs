using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sculptor.Model;
using Sculptor.Roslyn; // RoslynAdapters

namespace Sculptor.Providers;

internal static class DecoratedByGenericProvider
{
    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax;

    internal static DecoratorToIntercept? Transformer(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol implDef)
            return null;

        var attr = ctx.Attributes[0]; // matched [DecoratedBy<TDecorator>]

        // Named arg: IsInterceptable (default true)
        var isInterceptable = GetBoolNamedArg(attr, "IsInterceptable", defaultValue: true);
        if (!isInterceptable) return null;

        // Named arg: Order (default 0)
        var order = GetIntNamedArg(attr, "Order", defaultValue: 0);

        // Extract TDecorator from DecoratedByAttribute<TDecorator>
        if (attr.AttributeClass is not { TypeArguments.Length: 1 } g)
            return null;

        var decoratorSym = g.TypeArguments[0];
        if (decoratorSym is null) return null;

        return new DecoratorToIntercept(
            ImplementationDef: implDef.ToTypeId().Definition, // def-only identity
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