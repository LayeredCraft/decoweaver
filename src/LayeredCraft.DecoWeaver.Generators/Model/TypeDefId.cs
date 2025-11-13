using DecoWeaver.Util;
using Microsoft.CodeAnalysis;

namespace DecoWeaver.Model;

/// <summary>
/// Identity of a type definition (no type arguments).
/// Stable across compilations: namespace, metadata name, arity, assembly.
/// </summary>
public readonly record struct TypeDefId(
    string AssemblyName,
    EquatableArray<string> ContainingNamespaces, // outermost -> innermost
    EquatableArray<string> ContainingTypes,      // for nested types, outermost -> innermost (metadata names)
    string MetadataName,           // e.g., "IRepository`1"
    int Arity                      // 0 for non-generic; matches `N in `N
);

internal static class TypeDefIdExtensions
{
    extension(TypeDefId typeDefId)
    {
        internal static TypeDefId Create(ITypeSymbol t)
        {
            // For named types: collect containers + assembly
            var assembly = t.ContainingAssembly?.Name ?? "unknown";

            // Walk containing types (for nested)
            var containingTypes = new Stack<string>();
            var curType = t.ContainingType;
            while (curType is not null)
            {
                containingTypes.Push(curType.MetadataName); // includes `N
                curType = curType.ContainingType;
            }

            // Walk namespaces
            var nsParts = new Stack<string>();
            var ns = t.ContainingNamespace;
            while (ns is { IsGlobalNamespace: false })
            {
                nsParts.Push(ns.Name);
                ns = ns.ContainingNamespace;
            }

            var metadataName = (t as INamedTypeSymbol)?.MetadataName ?? t.Name; // includes `N for generics
            var arity = (t as INamedTypeSymbol)?.Arity ?? 0;

            return new(
                AssemblyName: assembly,
                ContainingNamespaces: new EquatableArray<string>(nsParts.ToArray()),
                ContainingTypes: new EquatableArray<string>(containingTypes.ToArray()),
                MetadataName: metadataName,
                Arity: arity
            );
        }
    }
}