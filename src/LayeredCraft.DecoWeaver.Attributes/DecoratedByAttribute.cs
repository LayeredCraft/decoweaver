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
    public bool IsInterceptable { get; set; } = true;
}

