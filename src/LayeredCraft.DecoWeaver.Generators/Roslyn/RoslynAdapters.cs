using DecoWeaver.Model;
using DecoWeaver.Util;
using Microsoft.CodeAnalysis;

namespace DecoWeaver.Roslyn;

internal static class RoslynAdapters
{
    public static LocationId ToLocationId(this SyntaxNode node)
    {
        var span = node.GetLocation().SourceSpan;
        var path = node.SyntaxTree.FilePath;
        return new(path, span.Start, span.Length);
    }

    public static TypeId ToTypeId(this ITypeSymbol t)
    {
        switch (t)
        {
            // Normalize: for constructed generics keep args; for unbound keep definition.
            case INamedTypeSymbol nt:
            {
                var def = nt.ConstructedFrom; // canonical definition (…`N)
                var defId = ToTypeDefId(def);

                // Open generic via typeof(Foo<>) or IsUnboundGenericType:
                if (nt.IsUnboundGenericType)
                {
                    return new(defId, TypeArgs: []);
                }

                // Closed generic or non-generic:
                if (defId.Arity == 0)
                {
                    return new(defId, TypeArgs: []);
                }

                var args = new TypeId[nt.TypeArguments.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    // For things like T in IRepository<T>, type arguments can be type parameters.
                    // Represent type parameters by their definition id (no args).
                    args[i] = nt.TypeArguments[i] switch
                    {
                        INamedTypeSymbol named => named.ToTypeId(),
                        IArrayTypeSymbol arr   => ArrayToTypeId(arr),
                        ITypeParameterSymbol tp => TypeParamToTypeId(tp),
                        _ => throw new NotSupportedException($"Unsupported type argument: {nt.TypeArguments[i]}")
                    };
                }
                return new(defId, args);
            }
            case IArrayTypeSymbol at:
                return ArrayToTypeId(at);
            case ITypeParameterSymbol tp2:
                return TypeParamToTypeId(tp2);
            default:
                // primitives, pointers, function pointers rarely appear in DI registrations; handle primitives via metadata
                return new(ToTypeDefId(t), TypeArgs: []);
        }
    }

    private static TypeId ArrayToTypeId(IArrayTypeSymbol at)
    {
        // Represent T[] as a pseudo generic: System.Array`1<T> (or your own convention)
        // Simpler: treat "T[]" as definition "SZArray" + elem type arg
        var def = new TypeDefId(
            AssemblyName: "mscorlib",
            ContainingNamespaces: ["System","Runtime"],
            ContainingTypes: [],
            MetadataName: "SZArray`1",
            Arity: 1);

        return new(def, TypeArgs: [at.ElementType.ToTypeId()]);
    }

    private static TypeId TypeParamToTypeId(ITypeParameterSymbol tp)
    {
        // Represent type parameters by a pseudo definition local to their owner
        var ownerName = tp.ContainingSymbol?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "Owner";
        var def = new TypeDefId(
            AssemblyName: tp.ContainingAssembly?.Name ?? "unknown",
            ContainingNamespaces: ["__TypeParameters__", ownerName],
            ContainingTypes: [],
            MetadataName: tp.Name, // e.g., "T"
            Arity: 0);
        return new(def, TypeArgs: []);
    }

    private static TypeDefId ToTypeDefId(ITypeSymbol t)
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
