using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sculptor.Model;
using Sculptor.Roslyn;

namespace Sculptor.Providers;

internal static class DecoratedByNonGenericProvider
{
    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax;

    /// <summary>
    /// Processes all [DecoratedBy] attributes on a class, yielding one DecoratorToIntercept per attribute.
    /// This allows multiple decorators to be applied to the same implementation.
    /// </summary>
    internal static IEnumerable<DecoratorToIntercept?> TransformMultiple(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.Node is not ClassDeclarationSyntax classSyntax)
            yield break;

        var symbol = ctx.SemanticModel.GetDeclaredSymbol(classSyntax, ct);
        if (symbol is not INamedTypeSymbol implDef)
            yield break;

        // Get all [DecoratedBy] attributes on this class
        foreach (var attr in implDef.GetAttributes())
        {
            // Only process DecoratedByAttribute (non-generic version)
            if (attr.AttributeClass?.ToDisplayString() != "Sculptor.Attributes.DecoratedByAttribute")
                continue;

            var isInterceptable = GetBoolNamedArg(attr, "IsInterceptable", defaultValue: true);
            if (!isInterceptable) continue;

            // Order can be either ctor arg #1 (int) or named arg "Order"
            var order = GetIntNamedArg(attr, "Order", defaultValue: 0);
            if (order == 0 && attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[1].Value is int ctorOrder)
                order = ctorOrder;

            // First ctor arg is the Type
            if (attr.ConstructorArguments.Length == 0 || attr.ConstructorArguments[0].Kind != TypedConstantKind.Type)
                continue;

            var decoratorSym = (ITypeSymbol?)attr.ConstructorArguments[0].Value;
            if (decoratorSym is null) continue;

            yield return new DecoratorToIntercept(
                ImplementationDef: implDef.ToTypeId().Definition,
                DecoratorDef: decoratorSym.ToTypeId().Definition,
                Order: order,
                IsInterceptable: true,
                Location: classSyntax.ToLocationId());
        }
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