using DecoWeaver.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DecoWeaver.Providers;

/// <summary>
/// Discovers [DoNotDecorate(typeof(...))] attributes on implementation classes.
/// These directives exclude specific decorators from the merged decoration chain,
/// allowing fine-grained control over which decorators apply to each implementation.
/// </summary>
internal static class DoNotDecorateProvider
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
    /// Processes all [DoNotDecorate] attributes on a class, yielding one DoNotDecorateDirective per attribute.
    /// This allows multiple decorators to be excluded from the same implementation.
    /// </summary>
    internal static IEnumerable<DoNotDecorateDirective?> TransformMultiple(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol implSym)
            yield break;

        // Process all [DoNotDecorate] attributes on this class (pre-filtered by ForAttributeWithMetadataName)
        foreach (var attr in ctx.Attributes)
        {
            // First ctor arg is the Type to exclude
            if (attr.ConstructorArguments.Length == 0 || attr.ConstructorArguments[0].Kind != TypedConstantKind.Type)
                continue;

            var decoratorSym = (ITypeSymbol?)attr.ConstructorArguments[0].Value;
            if (decoratorSym is null)
                continue;

            yield return new DoNotDecorateDirective(
                ImplementationDef: TypeId.Create(implSym).Definition,
                DecoratorDef: TypeId.Create(decoratorSym).Definition);
        }
    }
}