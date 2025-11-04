using DecoWeaver.Model;
using DecoWeaver.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// RoslynAdapters

namespace DecoWeaver.Providers;

internal static class DecoratedByGenericProvider
{
    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax;

    /// <summary>
    /// Transforms all [DecoratedBy&lt;T&gt;] attributes on a single class into multiple DecoratorToIntercept records.
    /// </summary>
    internal static IEnumerable<DecoratorToIntercept?> TransformMultiple(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol implDef)
            yield break;

        // Process all [DecoratedBy<T>] attributes on this class
        foreach (var attr in ctx.Attributes)
        {
            // Only process DecoratedByAttribute<T> (generic version)
            if (attr.AttributeClass is not { IsGenericType: true, TypeArguments.Length: 1 })
                continue;

            // Verify this is the correct generic attribute using metadata name and namespace
            var attrClass = attr.AttributeClass;
            if (attrClass.MetadataName != "DecoratedByAttribute`1")
                continue;
            if (attrClass.ContainingNamespace?.ToDisplayString() != "DecoWeaver.Attributes")
                continue;

            var isInterceptable = AttributeHelpers.GetBoolNamedArg(attr, "IsInterceptable", defaultValue: true);
            if (!isInterceptable) continue;

            var order = AttributeHelpers.GetIntNamedArg(attr, "Order", defaultValue: 0);

            // Extract TDecorator from DecoratedByAttribute<TDecorator>
            var decoratorSym = attr.AttributeClass.TypeArguments[0];
            if (decoratorSym is null) continue;

            yield return new DecoratorToIntercept(
                ImplementationDef: implDef.ToTypeId().Definition,
                DecoratorDef: decoratorSym.ToTypeId().Definition,
                Order: order,
                IsInterceptable: true,
                Location: ctx.TargetNode.ToLocationId());
        }
    }
}