# Attributes API Reference

DecoWeaver provides attributes for marking classes to be decorated. These attributes are compile-time only and have zero runtime footprint.

## DecoratedByAttribute<T>

Generic attribute for specifying a decorator type.

### Syntax

```csharp
[DecoratedBy<TDecorator>]
[DecoratedBy<TDecorator>(Order = int)]
```

### Type Parameters

- `TDecorator`: The type of decorator to apply. Must implement the same interface as the decorated class.

### Properties

#### Order

```csharp
public int Order { get; set; }
```

Controls the order in which multiple decorators are applied. Lower values are applied first (closer to the implementation).

**Default**: `0`

**Example**:
```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]      // Applied first (innermost)
[DecoratedBy<CachingDecorator>(Order = 2)]      // Applied second
[DecoratedBy<MetricsDecorator>(Order = 3)]      // Applied third (outermost)
public class UserRepository : IUserRepository { }
```

### Examples

#### Basic Usage

```csharp
using DecoWeaver.Attributes;

[DecoratedBy<LoggingRepository>]
public class UserRepository : IUserRepository
{
    // Implementation
}
```

#### Multiple Decorators

```csharp
[DecoratedBy<LoggingRepository>]
[DecoratedBy<CachingRepository>]
public class UserRepository : IUserRepository
{
    // Implementation
}
```

#### With Order

```csharp
[DecoratedBy<LoggingRepository>(Order = 1)]
[DecoratedBy<CachingRepository>(Order = 2)]
public class UserRepository : IUserRepository
{
    // Implementation
}
```

#### Open Generics

```csharp
[DecoratedBy<CachingRepository<>>]
public class Repository<T> : IRepository<T> where T : class
{
    // Implementation
}
```

### Requirements

1. **TDecorator must implement the same interface** as the decorated class
2. **TDecorator must have a constructor** accepting the interface as first parameter
3. **Target class must be concrete** (not abstract or interface)
4. **Project must target C# 11+** for generic attributes

### Compile-Time Behavior

This attribute is marked with `[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]`, meaning:

- The attribute does **not** exist in the compiled assembly
- No runtime reflection is possible on this attribute
- Zero metadata footprint
- Only affects compile-time code generation

### Diagnostics

**SCULPT002**: Decorator must implement service interface
```csharp
// Error: CachingRepository doesn't implement IUserRepository
[DecoratedBy<CachingRepository>]
public class UserRepository : IUserRepository { }
```

**SCULPT003**: Decorator must accept service interface in constructor
```csharp
// Error: Constructor doesn't accept IUserRepository
public class LoggingRepository : IUserRepository
{
    public LoggingRepository(ILogger logger) { } // Missing IUserRepository parameter
}
```

## DecoratedByAttribute (Non-Generic)

Non-generic attribute for specifying a decorator type using `typeof()`.

### Syntax

```csharp
[DecoratedBy(typeof(TDecorator))]
[DecoratedBy(typeof(TDecorator), Order = int)]
```

### Constructor

```csharp
public DecoratedByAttribute(Type decoratorType)
```

### Parameters

- `decoratorType`: The `Type` of the decorator to apply. Must implement the same interface as the decorated class.

### Properties

#### Order

```csharp
public int Order { get; set; }
```

Controls the order in which multiple decorators are applied. Lower values are applied first (closer to the implementation).

**Default**: `0`

### Examples

#### Basic Usage

```csharp
using DecoWeaver.Attributes;

[DecoratedBy(typeof(LoggingRepository))]
public class UserRepository : IUserRepository
{
    // Implementation
}
```

#### Multiple Decorators

```csharp
[DecoratedBy(typeof(LoggingRepository), Order = 1)]
[DecoratedBy(typeof(CachingRepository), Order = 2)]
public class UserRepository : IUserRepository
{
    // Implementation
}
```

#### Open Generics

```csharp
[DecoratedBy(typeof(CachingRepository<>))]
public class Repository<T> : IRepository<T> where T : class
{
    // Implementation
}
```

### Generic vs Non-Generic

Both forms are equivalent. Choose based on your preference:

```csharp
// Generic - type-safe at compile time
[DecoratedBy<LoggingRepository>]

// Non-generic - same behavior
[DecoratedBy(typeof(LoggingRepository))]
```

**Recommendation**: Use the generic form (`DecoratedBy<T>`) for better type safety and IntelliSense support.

## AttributeUsage

Both attribute forms can be applied multiple times to a class:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DecoratedByAttribute<T> : Attribute { }
```

### Properties

- **Target**: `AttributeTargets.Class` - Can only be applied to classes
- **AllowMultiple**: `true` - Multiple decorators can be applied
- **Inherited**: `false` - Not inherited by derived classes

### Implications

#### AllowMultiple = true

You can stack multiple decorators:

```csharp
[DecoratedBy<Decorator1>]
[DecoratedBy<Decorator2>]
[DecoratedBy<Decorator3>]
public class MyService : IMyService { }
```

#### Inherited = false

Decorators are **not** inherited:

```csharp
[DecoratedBy<LoggingDecorator>]
public class BaseRepository : IRepository { }

// This is NOT decorated - must apply attribute explicitly
public class UserRepository : BaseRepository { }

// This IS decorated
[DecoratedBy<LoggingDecorator>]
public class UserRepository : BaseRepository { }
```

## Best Practices

### Use Explicit Ordering

```csharp
// ✅ Good: Clear order
[DecoratedBy<LoggingDecorator>(Order = 10)]
[DecoratedBy<CachingDecorator>(Order = 20)]
[DecoratedBy<MetricsDecorator>(Order = 30)]

// ❌ Confusing: What's the order?
[DecoratedBy<LoggingDecorator>]
[DecoratedBy<CachingDecorator>]
[DecoratedBy<MetricsDecorator>]
```

### Document Ordering Strategy

```csharp
// Order strategy:
// 1-9: Core infrastructure (transactions, connections)
// 10-19: Performance (caching)
// 20-29: Observability (logging, metrics)
// 30-39: Security (auth, validation)
[DecoratedBy<TransactionDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 10)]
[DecoratedBy<LoggingDecorator>(Order = 20)]
[DecoratedBy<AuthorizationDecorator>(Order = 30)]
public class OrderService : IOrderService { }
```

### Prefer Generic Form

```csharp
// ✅ Preferred: Better IntelliSense and type safety
[DecoratedBy<LoggingRepository>]

// ⚠️ Works but less ideal
[DecoratedBy(typeof(LoggingRepository))]
```

## See Also

- [Class-Level Decorators](../usage/class-level-decorators.md) - Usage guide
- [Multiple Decorators](../usage/multiple-decorators.md) - Stacking decorators
- [Order and Nesting](../core-concepts/order-and-nesting.md) - Understanding order