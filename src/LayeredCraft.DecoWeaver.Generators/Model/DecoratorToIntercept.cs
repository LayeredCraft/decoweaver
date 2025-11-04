using DecoWeaver.Util;

namespace DecoWeaver.Model;

/// <summary>
/// One decoration declared on an implementation type definition (works great for open generics).
/// Example: [DecoratedBy(typeof(CachingRepo<>), order: 5)] on SqlRepo&lt;&gt;
/// </summary>
internal readonly record struct DecoratorToIntercept(
    TypeDefId ImplementationDef,    // e.g., SqlRepository`1
    TypeDefId DecoratorDef,         // e.g., CachingRepository`1
    int Order,                      // default 0
    bool IsInterceptable,           // default true; if false, generator should ignore
    LocationId Location
);
/// <summary>Lifetime of a DI registration.</summary>
public enum DiLifetime : byte { Transient, Scoped, Singleton }

/// <summary>Compact file location for diagnostics/dedup.</summary>
public readonly record struct LocationId(string FilePath, int Start, int Length);

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

/// <summary>A single ServiceCollection registration we discovered.</summary>
public readonly record struct RegistrationOccurrence(
    DiLifetime Lifetime,
    TypeId Service,
    TypeId Implementation,
    bool WasGenericMethod,   // true if AddX<TSvc, TImpl>, false if AddX(..., typeof(...), typeof(...))
    LocationId Location
);