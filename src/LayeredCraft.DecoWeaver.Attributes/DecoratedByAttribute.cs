using System;
using System.ComponentModel;
using System.Diagnostics;

namespace DecoWeaver.Attributes;

/// <summary>
/// Declares that <typeparamref name="TDecorator"/> should wrap the decorated implementation
/// when registered as its service. Order is ascending (lowest applied closest to the implementation).
/// </summary>
/// <remarks>
/// This attribute can only be applied to classes and records (class-based records).
/// The decorator pattern requires reference semantics, so structs and record structs are not supported.
/// Multiple decorators can be applied to the same class by using this attribute multiple times.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DecoratedByAttribute<TDecorator> : Attribute
    where TDecorator : class
{
    /// <summary>Wrapping order; lower numbers are applied first (closest to the implementation).</summary>
    public int Order { get; set; }

    /// <summary>
    /// Determines if the decorator should be intercepted by the source generator.
    /// When false, the decorator is present but not automatically applied via code generation.
    /// Used for decorators applied manually in Program.cs or for documentation purposes.
    /// </summary>
    public bool IsInterceptable { get; set; } = true;
}

/// <summary>
/// Non-generic variant that supports open generics and late binding.
/// </summary>
/// <remarks>
/// This attribute can only be applied to classes and records (class-based records).
/// The decorator pattern requires reference semantics, so structs and record structs are not supported.
/// Multiple decorators can be applied to the same class by using this attribute multiple times.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DecoratedByAttribute(Type decoratorType, int order = 0) : Attribute
{
    /// <summary>The decorator type to apply; must implement the same service contract.</summary>
    public Type DecoratorType { get; set; } = decoratorType;

    /// <summary>Wrapping order; lower numbers are applied first.</summary>
    public int Order { get; set; } = order;

    /// <summary>
    /// Determines if the decorator should be intercepted by the source generator.
    /// When false, the decorator is present but not automatically applied via code generation.
    /// Used for decorators applied manually in Program.cs or for documentation purposes.
    /// </summary>
    public bool IsInterceptable { get; set; } = true;
}

/// <summary>
/// Declares a decorator to be applied to all registrations of the specified service type
/// within the containing assembly. Supports open generic definitions (e.g., IRepository&lt;&gt;).
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DecorateServiceAttribute(Type serviceType, Type decoratorType, int order = 0) : Attribute
{
    /// <summary>The service type to decorate; must be an interface or base class.</summary>
    public Type ServiceType { get; set; } = serviceType;

    /// <summary>The decorator type to apply; must implement the same service contract.</summary>
    public Type DecoratorType { get; set; } = decoratorType;

    /// <summary>Wrapping order; lower numbers are applied first.</summary>
    public int Order { get; set; } = order;
}

/// <summary>
/// Opts out an implementation class from all assembly-level decorators in the same assembly.
/// Use this attribute when a specific implementation should not receive any assembly-level decorations,
/// but can still have class-level <see cref="DecoratedByAttribute"/> decorations applied.
/// </summary>
/// <remarks>
/// This attribute only affects assembly-level <see cref="DecorateServiceAttribute"/> decorators.
/// Class-level decorators declared directly on the implementation are still applied.
/// </remarks>
[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SkipAssemblyDecorationAttribute() : Attribute;

/// <summary>
/// Removes a specific decorator (by type definition) from the merged decoration set.
/// Use this attribute to surgically remove individual assembly-level decorators without
/// opting out of all assembly-level decorations.
/// </summary>
/// <remarks>
/// Matching is performed by type definition, ignoring type arguments. For example,
/// <c>[DoNotDecorate(typeof(CachingRepository&lt;&gt;))]</c> will remove all closed variants
/// of CachingRepository from the decoration chain.
/// </remarks>
[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DoNotDecorateAttribute(Type decoratorType) : Attribute
{
    /// <summary>Decorator type definition to remove (generic definition allowed).</summary>
    public Type DecoratorType { get; set; } = decoratorType;
}