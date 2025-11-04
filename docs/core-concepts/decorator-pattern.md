# The Decorator Pattern

The decorator pattern is a structural design pattern that allows you to add behavior to objects dynamically by wrapping them. Sculptor brings this pattern to .NET dependency injection at compile time.

## What is a Decorator?

A decorator wraps an existing implementation of an interface to add additional behavior without modifying the original code:

```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
}

// Original implementation
public class UserRepository : IUserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        // Database access logic
    }
}

// Decorator that adds logging
public class LoggingUserRepository : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly ILogger _logger;

    public LoggingUserRepository(IUserRepository inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        _logger.LogInformation("Getting user {Id}", id);
        var result = await _inner.GetByIdAsync(id);
        _logger.LogInformation("Retrieved user {Id}", id);
        return result;
    }
}
```

The decorator implements the same interface and delegates to the wrapped instance while adding its own behavior.

## Why Use Decorators?

### Separation of Concerns

Decorators let you keep cross-cutting concerns separate from business logic:

```csharp
// Business logic - no logging, caching, or metrics
public class UserRepository : IUserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        return await _db.Users.FindAsync(id);
    }
}

// Cross-cutting concerns added via decorators
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<MetricsDecorator>(Order = 3)]
public class UserRepository : IUserRepository { }
```

### Open/Closed Principle

Decorators follow the Open/Closed Principle - classes are open for extension but closed for modification:

- You can add new behaviors without changing existing code
- Original implementation remains untested and stable
- Decorators are independently testable

### Composition Over Inheritance

Unlike inheritance, decorators use composition:

```csharp
// ❌ Inheritance approach - rigid, hard to combine
public class LoggingUserRepository : UserRepository { }
public class CachingUserRepository : UserRepository { }
// How do you get both logging AND caching?

// ✅ Decorator approach - flexible, composable
var repo = new CachingDecorator(
    new LoggingDecorator(
        new UserRepository()));
```

## Common Use Cases

### Logging

Log method calls, parameters, and results:

```csharp
public class LoggingDecorator<T> : T where T : class
{
    private readonly T _inner;
    private readonly ILogger _logger;

    public async Task<TResult> MethodAsync<TResult>(params object[] args)
    {
        _logger.LogInformation("Calling method with args: {Args}", args);
        var result = await _inner.MethodAsync<TResult>(args);
        _logger.LogInformation("Method returned: {Result}", result);
        return result;
    }
}
```

### Caching

Cache expensive operations:

```csharp
public class CachingUserRepository : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly IMemoryCache _cache;

    public async Task<User> GetByIdAsync(int id)
    {
        var key = $"user:{id}";

        if (_cache.TryGetValue(key, out User cached))
            return cached;

        var user = await _inner.GetByIdAsync(id);
        _cache.Set(key, user, TimeSpan.FromMinutes(5));
        return user;
    }
}
```

### Metrics and Telemetry

Track performance and usage:

```csharp
public class MetricsDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly IMetrics _metrics;

    public async Task<User> GetByIdAsync(int id)
    {
        using var timer = _metrics.Time("user.get");
        _metrics.Increment("user.get.calls");

        try
        {
            return await _inner.GetByIdAsync(id);
        }
        catch
        {
            _metrics.Increment("user.get.errors");
            throw;
        }
    }
}
```

### Retry Logic

Add resilience with automatic retries:

```csharp
public class RetryDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly int _maxRetries = 3;

    public async Task<User> GetByIdAsync(int id)
    {
        for (int i = 0; i < _maxRetries; i++)
        {
            try
            {
                return await _inner.GetByIdAsync(id);
            }
            catch (TransientException) when (i < _maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
        }

        return await _inner.GetByIdAsync(id); // Final attempt
    }
}
```

### Circuit Breaking

Prevent cascading failures:

```csharp
public class CircuitBreakerDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly CircuitBreaker _breaker;

    public async Task<User> GetByIdAsync(int id)
    {
        if (_breaker.IsOpen)
            throw new CircuitBreakerOpenException();

        try
        {
            var result = await _inner.GetByIdAsync(id);
            _breaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            _breaker.RecordFailure(ex);
            throw;
        }
    }
}
```

## Decorator Characteristics

A proper decorator must:

1. **Implement the same interface** as the wrapped object
2. **Accept the interface via constructor** to wrap the inner implementation
3. **Delegate method calls** to the inner instance
4. **Add behavior** before, after, or around delegated calls

## Decorators vs Alternatives

### Decorators vs Middleware

- **Middleware**: Pipeline-based, processes all requests
- **Decorators**: Per-service, type-specific behavior

Use decorators when behavior is specific to a service type.

### Decorators vs AOP

- **AOP**: Aspect-oriented programming with interceptors at runtime
- **Decorators**: Explicit, compile-time wrapping

Decorators are more explicit and have zero runtime overhead with Sculptor.

### Decorators vs Base Classes

- **Base Classes**: Tight coupling, single inheritance
- **Decorators**: Loose coupling, unlimited composition

Decorators are more flexible and testable.

## Testing Decorators

Decorators are easy to test in isolation:

```csharp
[Fact]
public async Task LoggingDecorator_LogsMethodCalls()
{
    // Arrange
    var inner = Substitute.For<IUserRepository>();
    var logger = Substitute.For<ILogger>();
    var decorator = new LoggingDecorator(inner, logger);

    inner.GetByIdAsync(123).Returns(new User { Id = 123 });

    // Act
    await decorator.GetByIdAsync(123);

    // Assert
    logger.Received().LogInformation(
        Arg.Is<string>(s => s.Contains("Getting user")),
        123);
}
```

## Best Practices

### Keep Decorators Focused

Each decorator should have a single responsibility:

```csharp
// ✅ Good - single concern
public class CachingDecorator { }
public class LoggingDecorator { }

// ❌ Bad - multiple concerns
public class CachingAndLoggingDecorator { }
```

### Make Decorators Generic

Create reusable decorators that work with any interface:

```csharp
// ✅ Reusable across all repository types
public class CachingDecorator<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;
    // ...
}

// ❌ Specific to one type
public class CachingUserRepository : IUserRepository { }
```

### Order Matters

Think carefully about decorator order:

```csharp
// Cache results AFTER logging
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]

// Log → Cache → Service
// Logging sees all calls, cache only sees cache misses
```

## Next Steps

- Learn about [Order and Nesting](order-and-nesting.md) of multiple decorators
- See real examples in [Examples](../examples/index.md)
- Understand [How It Works](how-it-works.md) under the hood