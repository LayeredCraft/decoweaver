using DecoWeaver.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DecoWeaver.Providers;

internal static class DecoratedByGenericProvider
{
    /// <summary>
    /// Filters to classes only (AttributeTargets.Class). ForAttributeWithMetadataName passes all
    /// node types that could have attributes, so we pre-filter here to avoid semantic analysis on
    /// structs, interfaces, enums, etc. Decorator pattern requires reference semantics (classes/records),
    /// not value types (structs/record structs).
    /// </summary>
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
            // Only process DecoratedByAttribute<T> (generic version) with pattern matching for namespace
            if (attr.AttributeClass is not
                {
                    IsGenericType: true,
                    TypeArguments.Length: 1,
                    MetadataName: AttributeNames.GenericDecoratedByMetadataName,
                    ContainingNamespace:
                    {
                        Name: "Attributes",
                        ContainingNamespace: { Name: "DecoWeaver", ContainingNamespace.IsGlobalNamespace: true }
                    }
                })
                continue;

            var isInterceptable = AttributeHelpers.GetBoolNamedArg(attr, "IsInterceptable", defaultValue: true);
            if (!isInterceptable) continue;

            var order = AttributeHelpers.GetIntNamedArg(attr, "Order", defaultValue: 0);

            // Extract TDecorator from DecoratedByAttribute<TDecorator>
            var decoratorSym = attr.AttributeClass.TypeArguments[0];
            if (decoratorSym is null) continue;

            yield return new DecoratorToIntercept(
                ImplementationDef: TypeId.Create(implDef).Definition,
                DecoratorDef: TypeId.Create(decoratorSym).Definition,
                Order: order,
                IsInterceptable: true);
        }
    }
}