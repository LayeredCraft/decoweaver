using Microsoft.CodeAnalysis;

namespace DecoWeaver.Model;

/// <summary>
/// A (possibly generic) type: definition + zero or more type arguments.
/// For open generics, TypeArgs.Length == 0 and Arity > 0 (unbound).
/// For closed generics, TypeArgs.Length == Arity and each arg is a TypeId.
/// </summary>
public readonly record struct TypeId(TypeDefId Definition, TypeId[] TypeArgs)
{
    public bool IsOpenGeneric => Definition.Arity > 0 && (TypeArgs is null || TypeArgs.Length == 0);
    public bool IsClosedGeneric => Definition.Arity == TypeArgs.Length && Definition.Arity > 0;
}

internal static class TypeIdExtensions
{
    extension(TypeId typeId)
    {
        internal static TypeId Create(ITypeSymbol t)
        {
            switch (t)
            {
                // Normalize: for constructed generics keep args; for unbound keep definition.
                case INamedTypeSymbol nt:
                {
                    var def = nt.ConstructedFrom; // canonical definition (…`N)
                    var defId = TypeDefId.Create(def);

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
                            INamedTypeSymbol named => TypeId.Create(named),
                            IArrayTypeSymbol arr   => TypeId.CreateFromArray(arr),
                            ITypeParameterSymbol tp => TypeId.CreateFromParameter(tp),
                            _ => throw new NotSupportedException($"Unsupported type argument: {nt.TypeArguments[i]}")
                        };
                    }
                    return new(defId, args);
                }
                case IArrayTypeSymbol at:
                    return TypeId.CreateFromArray(at);
                case ITypeParameterSymbol tp2:
                    return TypeId.CreateFromParameter(tp2);
                default:
                    // primitives, pointers, function pointers rarely appear in DI registrations; handle primitives via metadata
                    return new(TypeDefId.Create(t), TypeArgs: []);
            }

        }
        
        private static TypeId CreateFromArray(IArrayTypeSymbol at)
        {
            // Represent T[] as a pseudo generic: System.Array`1<T> (or your own convention)
            // Simpler: treat "T[]" as definition "SZArray" + elem type arg
            var def = new TypeDefId(
                AssemblyName: "mscorlib",
                ContainingNamespaces: ["System","Runtime"],
                ContainingTypes: [],
                MetadataName: "SZArray`1",
                Arity: 1);

            return new(def, TypeArgs: [TypeId.Create(at.ElementType)]);
        }

        private static TypeId CreateFromParameter(ITypeParameterSymbol tp)
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

    }
}