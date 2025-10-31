using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Sculptor.Attributes;

/// <summary>
/// Declares that <typeparamref name="TDecorator"/> should wrap the decorated implementation
/// when registered as its service. Order is ascending (lowest applied closest to the implementation).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[Conditional("SCULPTOR_EMIT_ATTRIBUTE_METADATA")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DecoratedByAttribute<TDecorator> : Attribute
    where TDecorator : class
{
    /// <summary>Wrapping order; lower numbers are applied first (closest to the implementation).</summary>
    public int Order { get; init; }
    public bool IsInterceptable { get; init; } = true;
}

/// <summary>
/// Non-generic variant that supports open generics and late binding.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[Conditional("SCULPTOR_EMIT_ATTRIBUTE_METADATA")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DecoratedByAttribute(Type decoratorType, int order = 0) : Attribute
{
    /// <summary>The decorator type to apply; must implement the same service contract.</summary>
    public Type DecoratorType { get; } = decoratorType;

    /// <summary>Wrapping order; lower numbers are applied first.</summary>
    public int Order { get; } = order;
    public bool IsInterceptable { get; init; } = true;
}

/// <summary>
/// Declares a decorator for every registration of <paramref name="serviceType"/> in this assembly.
/// Applies to each implementation of the service unless explicitly opted out.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[Conditional("SCULPTOR_EMIT_ATTRIBUTE_METADATA")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DecorateServiceAttribute(Type serviceType, Type decoratorType, int order = 0) : Attribute
{
    public Type ServiceType { get; } = serviceType;
    public Type DecoratorType { get; } = decoratorType;
    public int Order { get; } = order;
    public bool IsInterceptable { get; init; } = true;
}
