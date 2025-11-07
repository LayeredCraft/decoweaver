using DecoWeaver.Model;
using DecoWeaver.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DecoWeaver.Providers;

internal static class ServiceDecoratedByProvider
{
    /// <summary>
    /// Filters to classes only (AttributeTargets.Class). ForAttributeWithMetadataName passes all
    /// node types that could have attributes, so we pre-filter here to avoid semantic analysis on
    /// structs, interfaces, enums, etc. Decorator pattern requires reference semantics (classes/records),
    /// not value types (structs/record structs).
    /// </summary>
    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is CompilationUnitSyntax cu
           && cu.AttributeLists.Any(al =>
               al.Target is { Identifier.ValueText: "assembly" or "module" });

    /// <summary>
    /// Processes all [DecorateService] attributes on an assembly, yielding one DecoratorToIntercept per attribute.
    /// This allows multiple decorators to be applied to the same implementation.
    /// </summary>
    internal static IEnumerable<DecoratorToIntercept?> TransformMultiple(GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        if (ctx.TargetSymbol is not IAssemblySymbol asm)
            yield break;

        // Process all [DecorateService] attributes on this class (pre-filtered by ForAttributeWithMetadataName)
        foreach (var attr in ctx.Attributes)
        {
            // Only process DecorateServiceAttribute with pattern matching for namespace
            if (attr.AttributeClass is not
                {
                    MetadataName: AttributeNames.ServiceDecoratedByMetadataName,
                    ContainingNamespace:
                    {
                        Name: "Attributes",
                        ContainingNamespace: { Name: "DecoWeaver", ContainingNamespace.IsGlobalNamespace: true }
                    }
                })
                continue;
            // Order can either ctor arg #2 (int) or named arg "Order"
            var order = AttributeHelpers.GetIntNamedArg(attr, "Order", defaultValue: 0);
            if (order == 0 && attr.ConstructorArguments is [_, _, { Value: int ctorOrder } _, ..])
                order = ctorOrder;

            // First ctor arg is the Type
            if (attr.ConstructorArguments.Length == 0 || attr.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
                attr.ConstructorArguments[1].Kind != TypedConstantKind.Type)
                continue;

            var serviceSym = (ITypeSymbol?)attr.ConstructorArguments[0].Value;
            var decoratorSym = (ITypeSymbol?)attr.ConstructorArguments[1].Value;
            if (serviceSym is null || decoratorSym is null) continue;

            yield return new DecoratorToIntercept(
                ImplementationDef: serviceSym.ToTypeId().Definition,
                DecoratorDef: decoratorSym.ToTypeId().Definition,
                Order: order,
                IsInterceptable: true);
        }
    }
}