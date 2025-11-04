# Multiple Decorators

Sculptor allows you to apply multiple decorators to a single implementation, creating a chain of decorators that wrap each other. This is useful for composing cross-cutting concerns like logging, caching, metrics, and resilience.

## Applying Multiple Decorators

Stack `[DecoratedBy]` attributes to apply multiple decorators:

```csharp
[DecoratedBy<LoggingDecorator>]
[DecoratedBy<CachingDecorator>]
[DecoratedBy<MetricsDecorator>]
public class UserRepository : IUserRepository
{
    // Your implementation
}
```

Each decorator wraps the one below it, creating a chain.

## Controlling Order

Use the `Order` property to control which decorators wrap which:

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]      // Innermost
[DecoratedBy<CachingDecorator>(Order = 2)]      // Middle
[DecoratedBy<MetricsDecorator>(Order = 3)]      // Outermost
public class UserRepository : IUserRepository { }
```

**Resulting chain**:
```
MetricsDecorator
  → CachingDecorator
    → LoggingDecorator
      → UserRepository
```

**Key rules**:
- Lower `Order` values are **closer to the implementation** (innermost)
- Higher `Order` values are **further from the implementation** (outermost)
- Default order is `0` if not specified

See [Order and Nesting](../core-concepts/order-and-nesting.md) for detailed explanation.

## Common Combinations

### Observability Stack

Complete observability with logging, metrics, and tracing:

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<MetricsDecorator>(Order = 2)]
[DecoratedBy<TracingDecorator>(Order = 3)]
public class UserRepository : IUserRepository { }
```

**Why this order?**
- Tracing captures the full request (outermost)
- Metrics record timing for the full operation
- Logging provides detailed method-level information

### Caching + Logging

Cache results while logging operations:

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
public class UserRepository : IUserRepository { }
```

**Behavior**:
- Cache hit: Returns immediately, logging is skipped
- Cache miss: Logging happens, then database query

**Alternative order**:
```csharp
[DecoratedBy<CachingDecorator>(Order = 1)]
[DecoratedBy<LoggingDecorator>(Order = 2)]
public class UserRepository : IUserRepository { }
```

**Behavior**:
- Cache hit: Logs "cache hit", returns
- Cache miss: Logs "cache miss", queries database

Choose based on whether you want to log cache hits.

### Resilience Stack

Combine retry, circuit breaker, and timeout:

```csharp
[DecoratedBy<RetryDecorator>(Order = 1)]              // Innermost
[DecoratedBy<CircuitBreakerDecorator>(Order = 2)]
[DecoratedBy<TimeoutDecorator>(Order = 3)]           // Outermost
public class UserRepository : IUserRepository { }
```

**Why this order?**
- Timeout protects against hanging operations (outermost)
- Circuit breaker prevents retry storms
- Retry handles transient failures

### Validation + Authorization + Logging

Layer security and observability:

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<ValidationDecorator>(Order = 2)]
[DecoratedBy<AuthorizationDecorator>(Order = 3)]
public class UserRepository : IUserRepository { }
```

**Why this order?**
- Authorization fails fast (outermost)
- Validation runs only for authorized requests
- Logging records valid, authorized operations

### Complete Stack

Everything together:

```csharp
// Core concerns
[DecoratedBy<TransactionDecorator>(Order = 1)]
[DecoratedBy<RetryDecorator>(Order = 2)]

// Performance
[DecoratedBy<CachingDecorator>(Order = 10)]

// Observability
[DecoratedBy<LoggingDecorator>(Order = 20)]
[DecoratedBy<MetricsDecorator>(Order = 21)]
[DecoratedBy<TracingDecorator>(Order = 22)]

// Security
[DecoratedBy<ValidationDecorator>(Order = 30)]
[DecoratedBy<AuthorizationDecorator>(Order = 31)]

// Resilience
[DecoratedBy<CircuitBreakerDecorator>(Order = 40)]
[DecoratedBy<TimeoutDecorator>(Order = 41)]

public class UserRepository : IUserRepository { }
```

## Registration

Multiple decorators work with any service lifetime:

```csharp
// Scoped
services.AddScoped<IUserRepository, UserRepository>();

// Singleton
services.AddSingleton<IUserRepository, UserRepository>();

// Transient
services.AddTransient<IUserRepository, UserRepository>();
```

All decorators are applied regardless of lifetime.

## Decorator Dependencies

Each decorator can have its own dependencies:

```csharp
public class LoggingDecorator : IUserRepository
{
    public LoggingDecorator(
        IUserRepository inner,
        ILogger<LoggingDecorator> logger) { }
}

public class CachingDecorator : IUserRepository
{
    public CachingDecorator(
        IUserRepository inner,
        IMemoryCache cache,
        IOptions<CacheOptions> options) { }
}

public class MetricsDecorator : IUserRepository
{
    public MetricsDecorator(
        IUserRepository inner,
        IMeterFactory meterFactory) { }
}

[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<MetricsDecorator>(Order = 3)]
public class UserRepository : IUserRepository { }

// Register all dependencies
services.AddLogging();
services.AddMemoryCache();
services.Configure<CacheOptions>(config.GetSection("Cache"));
services.AddMetrics();

// Register the service
services.AddScoped<IUserRepository, UserRepository>();
```

## Sharing State Between Decorators

Decorators in the same chain don't share state directly. If you need shared context:

### Option 1: Use DI Container

Share state through a service in the DI container:

```csharp
public class RequestContext
{
    public string TraceId { get; set; }
    public Stopwatch Timer { get; set; }
}

public class TracingDecorator : IUserRepository
{
    private readonly RequestContext _context;

    public TracingDecorator(IUserRepository inner, RequestContext context)
    {
        _context = context;
        _context.TraceId = Guid.NewGuid().ToString();
        _context.Timer = Stopwatch.StartNew();
    }
}

public class LoggingDecorator : IUserRepository
{
    private readonly RequestContext _context;

    public LoggingDecorator(IUserRepository inner, RequestContext context)
    {
        _context = context;
        // Can access TraceId and Timer from TracingDecorator
    }
}

services.AddScoped<RequestContext>();
```

### Option 2: Use AsyncLocal

Share state across async calls:

```csharp
public static class DecoratorContext
{
    private static AsyncLocal<Dictionary<string, object>> _context = new();

    public static Dictionary<string, object> Current =>
        _context.Value ??= new Dictionary<string, object>();
}

public class TracingDecorator : IUserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        DecoratorContext.Current["TraceId"] = Guid.NewGuid().ToString();
        return await _inner.GetByIdAsync(id);
    }
}

public class LoggingDecorator : IUserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        var traceId = DecoratorContext.Current["TraceId"];
        _logger.LogInformation("TraceId: {TraceId}", traceId);
        return await _inner.GetByIdAsync(id);
    }
}
```

## Conditional Decorators

Apply decorators conditionally based on configuration:

```csharp
#if DEBUG
[DecoratedBy<DetailedLoggingDecorator>]
#else
[DecoratedBy<ProductionLoggingDecorator>]
#endif
[DecoratedBy<CachingDecorator>]
public class UserRepository : IUserRepository { }
```

Or use different implementations:

```csharp
// Development
[DecoratedBy<SlowQueryDetectorDecorator>]
[DecoratedBy<DetailedLoggingDecorator>]
public class DevelopmentUserRepository : IUserRepository { }

// Production
[DecoratedBy<MetricsDecorator>]
[DecoratedBy<CachingDecorator>]
public class ProductionUserRepository : IUserRepository { }

// Register based on environment
if (env.IsDevelopment())
    services.AddScoped<IUserRepository, DevelopmentUserRepository>();
else
    services.AddScoped<IUserRepository, ProductionUserRepository>();
```

## Testing Multiple Decorators

Test the full chain or individual decorators:

### Test Full Chain

```csharp
[Fact]
public async Task FullChain_AppliesAllDecorators()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddMemoryCache();
    services.AddScoped<IUserRepository, UserRepository>();

    var provider = services.BuildServiceProvider();
    var repo = provider.GetRequiredService<IUserRepository>();

    // Act
    var user = await repo.GetByIdAsync(123);

    // Assert - verify all decorators ran
    // Check logs, cache, metrics, etc.
}
```

### Test Individual Decorator

```csharp
[Fact]
public async Task CachingDecorator_CachesResults()
{
    // Arrange
    var inner = Substitute.For<IUserRepository>();
    var cache = new MemoryCache(new MemoryCacheOptions());
    var decorator = new CachingDecorator(inner, cache);

    inner.GetByIdAsync(123).Returns(new User { Id = 123 });

    // Act
    await decorator.GetByIdAsync(123);
    await decorator.GetByIdAsync(123);

    // Assert
    await inner.Received(1).GetByIdAsync(123); // Only called once
}
```

## Performance Considerations

Each decorator adds overhead. Keep chains reasonable:

```csharp
// ✅ Good: 3-5 decorators
[DecoratedBy<LoggingDecorator>]
[DecoratedBy<CachingDecorator>]
[DecoratedBy<MetricsDecorator>]
public class UserRepository : IUserRepository { }

// ⚠️ Consider impact: 10+ decorators
[DecoratedBy<Decorator1>]
[DecoratedBy<Decorator2>]
[DecoratedBy<Decorator3>]
// ... many more ...
[DecoratedBy<Decorator10>]
public class UserRepository : IUserRepository { }
```

Profile your application to understand the impact.

## Common Patterns

### Layer by Concern

Group decorators by their purpose:

```csharp
// Infrastructure (Order 1-9)
[DecoratedBy<TransactionDecorator>(Order = 1)]
[DecoratedBy<ConnectionManagementDecorator>(Order = 2)]

// Performance (Order 10-19)
[DecoratedBy<CachingDecorator>(Order = 10)]
[DecoratedBy<BatchingDecorator>(Order = 11)]

// Observability (Order 20-29)
[DecoratedBy<LoggingDecorator>(Order = 20)]
[DecoratedBy<MetricsDecorator>(Order = 21)]

// Security (Order 30-39)
[DecoratedBy<AuthorizationDecorator>(Order = 30)]
[DecoratedBy<ValidationDecorator>(Order = 31)]

public class UserRepository : IUserRepository { }
```

### Feature Flags

Enable/disable decorators with feature flags:

```csharp
public class FeatureFlaggedCachingDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly IFeatureManager _features;
    private readonly CachingDecorator _caching;

    public async Task<User> GetByIdAsync(int id)
    {
        if (await _features.IsEnabledAsync("Caching"))
            return await _caching.GetByIdAsync(id);

        return await _inner.GetByIdAsync(id);
    }
}

[DecoratedBy<FeatureFlaggedCachingDecorator>]
public class UserRepository : IUserRepository { }
```

## Best Practices

1. **Use gaps in Order numbers** (10, 20, 30) to allow future insertion
2. **Group related decorators** (observability, resilience, security)
3. **Keep chains focused** - avoid excessive decorators
4. **Document your ordering strategy** with comments
5. **Test both individually and as a chain**
6. **Profile performance impact** of decorator chains
7. **Consider conditional application** based on environment

## Troubleshooting

### Wrong Order

If decorators aren't wrapping in the right order:

1. Check `Order` property values
2. Remember: lower = inner, higher = outer
3. Verify default order (0) behavior

### Missing Dependencies

If a decorator's dependencies aren't resolved:

```csharp
// ❌ Error: IMemoryCache not registered
[DecoratedBy<CachingDecorator>]
public class UserRepository : IUserRepository { }

// ✅ Fix: Register all dependencies
services.AddMemoryCache();
services.AddLogging();
// ... etc
```

### Circular Dependencies

If you get circular dependency errors:

- Ensure decorators don't depend on the service they're decorating
- Check that Sculptor is using keyed services correctly
- Verify .NET 8+ runtime for keyed services support

## Next Steps

- Learn about [Open Generic decorators](open-generics.md)
- Understand [Order and Nesting](../core-concepts/order-and-nesting.md) in depth
- See [Examples](../examples/index.md) of real-world decorator chains