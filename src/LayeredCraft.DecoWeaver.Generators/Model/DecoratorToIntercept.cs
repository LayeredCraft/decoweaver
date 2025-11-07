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
    bool IsInterceptable
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

/// <summary>
/// One assembly-level decoration rule discovered from
/// <c>[assembly: DecorateService(typeof(Service<>), typeof(Decorator<>), order: ...)]</c>.
/// Applies only within the declaring assembly.
/// </summary>
internal readonly record struct ServiceDecoration(
    string AssemblyName,     // e.g., "LayeredCraft.Data"
    TypeDefId ServiceDef,    // e.g., IRepository`1
    TypeDefId DecoratorDef,  // e.g., CachingRepository`1
    int Order
);

/// <summary>
/// Marks that a given implementation type has opted out of all assembly-level decorations
/// via <c>[SkipAssemblyDecorators]</c>.
/// </summary>
internal readonly record struct SkipAssemblyDecoratorsMarker(
    TypeDefId ImplementationDef
);

/// <summary>
/// Removes a specific decorator (by type definition) from the merged set for the given implementation,
/// discovered from <c>[DoNotDecorate(typeof(Decorator<>))]</c>.
/// </summary>
internal readonly record struct DoNotDecorateDirective(
    TypeDefId ImplementationDef, // where the attribute appears
    TypeDefId DecoratorDef       // definition to remove (generic def allowed)
);

/// <summary>
/// Origin of a decorator in the final merged chain (used for deterministic sorting and precedence).
/// </summary>
internal enum DecorationSource : byte
{
    /// <summary>Declared directly on the implementation (via <c>[DecoratedBy]</c>).</summary>
    Class = 0,

    /// <summary>Declared at the assembly level (via <c>[assembly: DecorateService]</c>).</summary>
    Assembly = 1
}

/// <summary>
/// Unified, already-merged view of a single decorator to emit for a specific registration
/// (service + implementation). Sorting is by Order asc, then Source asc (Class before Assembly),
/// then by a stable tiebreaker in the pipeline.
/// </summary>
internal readonly record struct MergedDecoration(
    TypeDefId DecoratorDef,
    int Order,
    DecorationSource Source,
    bool IsInterceptable // true for class-level unless explicitly disabled; assembly-level defaults to true
);
