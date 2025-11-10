using DecoWeaver.Model;
using DecoWeaver.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DecoWeaver.Providers;

internal static class SkipAssemblyDecoratorProvider
{
    /// <summary>
    /// Filters to classes only (AttributeTargets.Class). ForAttributeWithMetadataName passes all
    /// node types that could have attributes, so we pre-filter here to avoid semantic analysis on
    /// structs, interfaces, enums, etc. Decorator pattern requires reference semantics (classes/records),
    /// not value types (structs/record structs).
    /// </summary>
    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax;

    internal static SkipAssemblyDecoratorsMarker? Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol implDef)
            return null;

        return new SkipAssemblyDecoratorsMarker(
            ImplementationDef: implDef.ToTypeId().Definition
        );
    }
}