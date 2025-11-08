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

## DecorateServiceAttribute

Assembly-level attribute for applying decorators to all implementations of a service interface.

### Syntax

```csharp
[assembly: DecorateService(typeof(TService), typeof(TDecorator))]
[assembly: DecorateService(typeof(TService), typeof(TDecorator), Order = int)]
```

### Constructor

```csharp
public DecorateServiceAttribute(Type serviceType, Type decoratorType)
```

### Parameters

- `serviceType`: The service interface type (can be open generic like `IRepository<>`)
- `decoratorType`: The decorator type to apply (can be open generic like `CachingRepository<>`)

### Properties

#### Order

```csharp
public int Order { get; set; }
```

Controls the order when multiple assembly-level decorators are applied. Lower values are applied first (closer to the implementation).

**Default**: `0`

### Target

```csharp
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
```

- **Target**: `AttributeTargets.Assembly` - Applied to assemblies
- **AllowMultiple**: `true` - Multiple decorators can be defined
- **Inherited**: `false` - Not inherited

### Examples

#### Basic Usage

```csharp
using DecoWeaver.Attributes;

// Apply logging to all IRepository<T> implementations
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]
```

#### Multiple Decorators

```csharp
// Apply logging, caching, and metrics to all repositories
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 1)]
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), Order = 2)]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>), Order = 3)]
```

#### Non-Generic Services

```csharp
// Apply to non-generic service
[assembly: DecorateService(typeof(IUserService), typeof(LoggingUserService))]
```

#### Mixed Generic/Non-Generic

```csharp
// Open generic service, closed generic decorator
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingUserRepository))]
```

### Behavior

At compile time, DecoWeaver:

1. Discovers all `[assembly: DecorateService(...)]` attributes
2. Finds DI registrations matching the service type
3. Merges with class-level `[DecoratedBy]` attributes
4. Generates interceptor code applying all decorators in order

### Type Matching

Assembly-level decorators match registrations based on:

- **Open generic** `IRepository<>` matches all closed registrations (`IRepository<User>`, `IRepository<Order>`, etc.)
- **Closed generic** `IRepository<User>` matches only `IRepository<User>`
- **Non-generic** `IUserService` matches exactly

### Combining with Class-Level

Assembly-level and class-level decorators are merged:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 10)]

// UserRepository.cs
[DecoratedBy<ValidationRepository<User>>(Order = 5)]
public class UserRepository : IRepository<User> { }
```

**Resulting chain**: `LoggingRepository<User>` → `ValidationRepository<User>` → `UserRepository`

### Compile-Time Behavior

This attribute is marked with `[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]`, meaning:

- The attribute does **not** exist in the compiled assembly
- No runtime reflection is possible
- Zero metadata footprint
- Only affects compile-time code generation

### Requirements

1. **Decorator must implement service interface**
2. **Decorator must have constructor accepting interface as first parameter**
3. **Only intercepts closed generic registrations** (`AddScoped<IRepo<T>, Impl<T>>()`)
4. **Does not intercept open generic registrations** (`AddScoped(typeof(IRepo<>), typeof(Impl<>))`)

### Best Practices

1. **Group by concern** - Keep related decorators together
2. **Use gaps in Order** - Reserve ranges (10-19 for logging, 20-29 for caching, etc.)
3. **Document your strategy** - Comment why decorators are applied assembly-wide
4. **Centralize location** - Keep all assembly attributes in `GlobalUsings.cs` or similar

## DoNotDecorateAttribute

Class-level attribute for excluding specific decorators from an implementation.

### Syntax

```csharp
[DoNotDecorate(typeof(TDecorator))]
```

### Constructor

```csharp
public DoNotDecorateAttribute(Type decoratorType)
```

### Parameters

- `decoratorType`: The decorator type to exclude (can be open generic like `CachingRepository<>`)

### Target

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
```

- **Target**: `AttributeTargets.Class` - Applied to classes
- **AllowMultiple**: `true` - Multiple decorators can be excluded
- **Inherited**: `false` - Not inherited

### Examples

#### Opt Out of Assembly-Level Decorator

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

// UserRepository.cs - gets caching
public class UserRepository : IRepository<User> { }

// OrderRepository.cs - opts out of caching
[DoNotDecorate(typeof(CachingRepository<>))]
public class OrderRepository : IRepository<Order> { }
```

#### Multiple Opt-Outs

```csharp
// Opt out of caching and metrics
[DoNotDecorate(typeof(CachingRepository<>))]
[DoNotDecorate(typeof(MetricsRepository<>))]
public class OrderRepository : IRepository<Order> { }
```

#### Open Generic Matching

```csharp
// Open generic in DoNotDecorate matches all closed generics
[DoNotDecorate(typeof(CachingRepository<>))]  // Matches CachingRepository<User>
public class UserRepository : IRepository<User> { }
```

### Behavior

At compile time, DecoWeaver:

1. Discovers all `[DoNotDecorate]` attributes on classes
2. Collects decorators from class-level and assembly-level sources
3. **Filters out** decorators matching the `DoNotDecorate` directives
4. Generates interceptor code with only remaining decorators

### Type Matching Rules

- **Open generic** `typeof(Decorator<>)` matches all closed variants
- **Closed generic** `typeof(Decorator<User>)` matches only that specific type
- **Non-generic** types match exactly
- Type matching is by **definition** (ignoring type arguments)

### Isolation

`[DoNotDecorate]` only affects the specific class it's applied to:

```csharp
[DoNotDecorate(typeof(CachingRepository<>))]
public class OrderRepository : IRepository<Order> { }

// UserRepository still gets caching (not affected)
public class UserRepository : IRepository<User> { }
```

### Use Cases

1. **Excluding assembly-level decorators** - Primary use case
2. **Performance-critical code** - Remove observability overhead
3. **Implementation-specific constraints** - Special requirements
4. **Testing implementations** - Simplify test setup

### Compile-Time Behavior

This attribute is marked with `[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]`, meaning:

- The attribute does **not** exist in the compiled assembly
- No runtime reflection is possible
- Zero metadata footprint
- Only affects compile-time code generation

### Best Practices

1. **Use sparingly** - If many implementations opt out, reconsider assembly-level decorator
2. **Document why** - Comment explaining the opt-out reason
3. **Exact type match** - Ensure decorator type exactly matches what's being applied
4. **Prefer class-level for specific needs** - Use assembly-level for cross-cutting concerns

## See Also

- [Class-Level Decorators](../usage/class-level-decorators.md) - Usage guide
- [Assembly-Level Decorators](../usage/assembly-level-decorators.md) - Assembly-wide decoration
- [Opt-Out](../usage/opt-out.md) - Excluding decorators
- [Multiple Decorators](../usage/multiple-decorators.md) - Stacking decorators
- [Order and Nesting](../core-concepts/order-and-nesting.md) - Understanding order