# Assembly-Level Decorators

Assembly-level decorators provide a centralized way to apply decorators to multiple implementations across your codebase. Instead of applying `[DecoratedBy]` to each class individually, you can declare decorators once at the assembly level.

## Basic Syntax

Use the `[assembly: DecorateService(...)]` attribute in any `.cs` file (commonly in `GlobalUsings.cs` or `AssemblyInfo.cs`):

```csharp
using DecoWeaver.Attributes;

[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
```

This applies `CachingRepository<>` to **all** implementations of `IRepository<>` registered in the DI container.

## When to Use Assembly-Level Decorators

Assembly-level decorators are ideal for:

### Cross-Cutting Concerns

Apply the same decorator to many implementations:

```csharp
// In GlobalUsings.cs or AssemblyInfo.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>))]
```

Now **every** `IRepository<T>` implementation automatically gets logging and metrics.

### Centralized Configuration

Manage all decorators in one place instead of scattered across many classes:

```csharp
// ❌ Before: Scattered across many files
[DecoratedBy<LoggingDecorator>]
public class UserRepository : IRepository<User> { }

[DecoratedBy<LoggingDecorator>]
public class ProductRepository : IRepository<Product> { }

[DecoratedBy<LoggingDecorator>]
public class OrderRepository : IRepository<Order> { }

// ✅ After: Centralized in one place
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]

public class UserRepository : IRepository<User> { }
public class ProductRepository : IRepository<Product> { }
public class OrderRepository : IRepository<Order> { }
```

### Consistency Enforcement

Ensure all implementations follow the same patterns:

```csharp
// Enforce observability for all repositories
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 1)]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>), Order = 2)]

// Enforce caching for all query services
[assembly: DecorateService(typeof(IQueryService<>), typeof(CachingQueryService<>))]
```

## Syntax Variants

### Open Generic Service and Decorator

Most common - both service and decorator are generic:

```csharp
[assembly: DecorateService(
    typeof(IRepository<>),           // Service type
    typeof(CachingRepository<>)      // Decorator type
)]
```

### Open Generic Service, Closed Generic Decorator

Service is generic, decorator is closed:

```csharp
[assembly: DecorateService(
    typeof(IRepository<>),           // Service type
    typeof(CachingUserRepository)    // Closed decorator for User only
)]
```

This only decorates implementations where T matches the decorator's closed type.

### Non-Generic Service and Decorator

Both service and decorator are concrete:

```csharp
[assembly: DecorateService(
    typeof(IUserService),
    typeof(LoggingUserService)
)]
```

## Decorator Requirements

Assembly-level decorators have the same requirements as class-level decorators:

1. **Implement the service interface**
2. **Accept the interface as constructor parameter** (typically first)
3. **Have resolvable dependencies** from DI container

```csharp
public interface IRepository<T>
{
    Task<T> GetByIdAsync(int id);
    Task SaveAsync(T entity);
}

// ✅ Valid assembly-level decorator
public class CachingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;
    private readonly IMemoryCache _cache;

    public CachingRepository(IRepository<T> inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<T> GetByIdAsync(int id)
    {
        var key = $"{typeof(T).Name}:{id}";
        if (_cache.TryGetValue(key, out T cached))
            return cached;

        var entity = await _inner.GetByIdAsync(id);
        _cache.Set(key, entity, TimeSpan.FromMinutes(5));
        return entity;
    }

    public Task SaveAsync(T entity) => _inner.SaveAsync(entity);
}
```

## Multiple Assembly-Level Decorators

Stack multiple decorators using the `Order` property:

```csharp
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 1)]
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), Order = 2)]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>), Order = 3)]
```

**Resulting chain** for any `IRepository<T>`:
```
MetricsRepository<T>
  → CachingRepository<T>
    → LoggingRepository<T>
      → [Your Implementation]
```

Lower `Order` values are closer to the implementation (innermost).

## Combining with Class-Level Decorators

You can combine assembly-level and class-level decorators on the same implementation:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 10)]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>), Order = 20)]

// UserRepository.cs
[DecoratedBy<ValidationRepository<User>>(Order = 5)]
public class UserRepository : IRepository<User>
{
    // Implementation
}
```

**Resulting chain**:
```
MetricsRepository<User>       // Order 20 (assembly-level)
  → LoggingRepository<User>   // Order 10 (assembly-level)
    → ValidationRepository<User> // Order 5 (class-level)
      → UserRepository
```

### Precedence Rules

When combining decorators:

1. **All decorators are merged** (both class-level and assembly-level)
2. **Sorted by Order** property (ascending)
3. **Duplicates are removed** (same decorator type + order)
4. **Class-level takes precedence** over assembly-level for same decorator

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 10)]

// UserRepository.cs
[DecoratedBy<LoggingRepository<User>>(Order = 10)]  // Same type and order
public class UserRepository : IRepository<User> { }
```

**Result**: Only **one** `LoggingRepository<User>` is applied (class-level takes precedence).

## Opting Out

Use `[DoNotDecorate]` to exclude specific implementations from assembly-level decorators:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]

// UserRepository.cs - gets both decorators
public class UserRepository : IRepository<User> { }

// OrderRepository.cs - opts out of caching
[DoNotDecorate(typeof(CachingRepository<>))]
public class OrderRepository : IRepository<Order> { }

// ProductRepository.cs - opts out of both
[DoNotDecorate(typeof(CachingRepository<>))]
[DoNotDecorate(typeof(LoggingRepository<>))]
public class ProductRepository : IRepository<Product> { }
```

See [Opt-Out](opt-out.md) for complete details.

## Registration

Assembly-level decorators work with any service lifetime:

```csharp
var services = new ServiceCollection();

// All of these get decorated by assembly-level decorators
services.AddScoped<IRepository<User>, UserRepository>();
services.AddSingleton<IRepository<Product>, ProductRepository>();
services.AddTransient<IRepository<Order>, OrderRepository>();
```

DecoWeaver automatically intercepts these registrations and applies the decorators.

## How It Works

At compile time, DecoWeaver:

1. **Discovers** all `[assembly: DecorateService(...)]` attributes
2. **Finds** DI registration calls like `AddScoped<IRepo<T>, Impl<T>>()`
3. **Matches** implementations against service types
4. **Merges** with any class-level decorators
5. **Generates** interceptor code that wraps the implementation

No runtime reflection or assembly scanning - everything happens at build time.

## Common Patterns

### Observability for All Services

```csharp
// Apply logging and metrics to all repositories
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 1)]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>), Order = 2)]

// Apply to all query services
[assembly: DecorateService(typeof(IQueryService<>), typeof(LoggingQueryService<>), Order = 1)]
[assembly: DecorateService(typeof(IQueryService<>), typeof(MetricsQueryService<>), Order = 2)]
```

### Caching Layer

```csharp
// Cache all read operations
[assembly: DecorateService(typeof(IReadRepository<>), typeof(CachingRepository<>))]

// But not write operations (no attribute for IWriteRepository<>)
```

### Security Layer

```csharp
// Enforce authorization on all commands
[assembly: DecorateService(typeof(ICommandHandler<>), typeof(AuthorizationHandler<>), Order = 1)]

// Validate all commands
[assembly: DecorateService(typeof(ICommandHandler<>), typeof(ValidationHandler<>), Order = 2)]
```

### Resilience

```csharp
// Add retry to all external service calls
[assembly: DecorateService(typeof(IExternalService), typeof(RetryDecorator<>), Order = 1)]

// Add circuit breaker
[assembly: DecorateService(typeof(IExternalService), typeof(CircuitBreakerDecorator<>), Order = 2)]
```

## Organization

### Single File Approach

Keep all assembly-level decorators in one file:

```csharp
// GlobalUsings.cs or AssemblyDecorators.cs
using DecoWeaver.Attributes;

// Repositories
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 10)]
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), Order = 20)]

// Query Services
[assembly: DecorateService(typeof(IQueryService<>), typeof(LoggingQueryService<>), Order = 10)]
[assembly: DecorateService(typeof(IQueryService<>), typeof(CachingQueryService<>), Order = 20)]

// Command Handlers
[assembly: DecorateService(typeof(ICommandHandler<>), typeof(ValidationHandler<>), Order = 10)]
[assembly: DecorateService(typeof(ICommandHandler<>), typeof(AuthorizationHandler<>), Order = 20)]
```

### Multiple File Approach

Group by concern:

```csharp
// Observability.Assembly.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>))]

// Performance.Assembly.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

// Security.Assembly.cs
[assembly: DecorateService(typeof(ICommandHandler<>), typeof(AuthorizationHandler<>))]
```

## Comparison with Class-Level

| Aspect | Assembly-Level | Class-Level |
|--------|---------------|-------------|
| **Scope** | All implementations | Single implementation |
| **Location** | Global file | On class |
| **Use Case** | Cross-cutting concerns | Specific needs |
| **Maintenance** | Centralized | Distributed |
| **Visibility** | Less obvious | More explicit |
| **Flexibility** | Can opt-out | Full control |

**When to use each**:

- **Assembly-level**: Cross-cutting concerns (logging, metrics, caching)
- **Class-level**: Implementation-specific decorators (validation, transformation)
- **Both**: Combine for layered concerns

## Troubleshooting

### Decorator Not Applied

If your assembly-level decorator isn't being applied:

1. **Verify attribute syntax**: Ensure `[assembly: ...]` at the start
2. **Check service type match**: Service type must match registration
3. **Rebuild**: Assembly-level changes require full rebuild
4. **Check for opt-out**: Verify no `[DoNotDecorate]` on the class
5. **Verify dependencies**: Ensure decorator dependencies are registered

### Wrong Type Argument

```csharp
// ❌ Error: Type argument mismatch
[assembly: DecorateService(typeof(IRepository), typeof(CachingRepository<>))]

// ✅ Fixed: Match generic arity
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
```

### Not Intercepting

Assembly-level decorators only intercept **closed generic registrations**:

```csharp
// ✅ Intercepted
services.AddScoped<IRepository<User>, UserRepository>();

// ❌ NOT intercepted (open generic registration)
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

DecoWeaver only intercepts the `AddScoped<TService, TImplementation>()` syntax.

## Best Practices

1. **Keep assembly-level for cross-cutting concerns** - Don't overuse
2. **Document your assembly decorators** - They're less visible than class-level
3. **Use consistent ordering strategy** - Reserve ranges for each concern (10-19 for logging, 20-29 for caching, etc.)
4. **Prefer class-level for implementation-specific logic** - More explicit and maintainable
5. **Group attributes by concern** - Makes it easier to find and modify
6. **Use DoNotDecorate sparingly** - If many implementations opt out, reconsider assembly-level

## Next Steps

- Learn about [Opt-Out](opt-out.md) with `[DoNotDecorate]`
- Understand [Order and Nesting](../core-concepts/order-and-nesting.md) in depth
- See how to combine with [Class-Level Decorators](class-level-decorators.md)
- Explore [Examples](../examples/index.md) of real-world usage
