using DecoWeaver.Model;
using DecoWeaver.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DecoWeaver.Providers;

internal static class DecoratedByNonGenericProvider
{
    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax;

    /// <summary>
    /// Processes all [DecoratedBy] attributes on a class, yielding one DecoratorToIntercept per attribute.
    /// This allows multiple decorators to be applied to the same implementation.
    /// </summary>
    internal static IEnumerable<DecoratorToIntercept?> TransformMultiple(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol implDef)
            yield break;

        // Process all [DecoratedBy(typeof(...))] attributes on this class
        foreach (var attr in ctx.Attributes)
        {
            // Only process DecoratedByAttribute (non-generic version) with pattern matching for namespace
            if (attr.AttributeClass is not
                {
                    MetadataName: AttributeNames.DecoratedByMetadataName,
                    ContainingNamespace:
                    {
                        Name: "Attributes",
                        ContainingNamespace: { Name: "DecoWeaver", ContainingNamespace.IsGlobalNamespace: true }
                    }
                })
                continue;

            var isInterceptable = AttributeHelpers.GetBoolNamedArg(attr, "IsInterceptable", defaultValue: true);
            if (!isInterceptable) continue;

            // Order can be either ctor arg #1 (int) or named arg "Order"
            var order = AttributeHelpers.GetIntNamedArg(attr, "Order", defaultValue: 0);
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
                Location: ctx.TargetNode.ToLocationId());
        }
    }
}