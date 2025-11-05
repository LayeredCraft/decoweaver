# Class-Level Decorators

The `[DecoratedBy]` attribute is the primary way to apply decorators in DecoWeaver. Apply it to your service implementation classes to automatically wrap them with decorators.

## Basic Syntax

### Generic Attribute

The most common and type-safe approach:

```csharp
using DecoWeaver.Attributes;

[DecoratedBy<LoggingDecorator>]
public class UserRepository : IUserRepository
{
    // Your implementation
}
```

### Non-Generic Attribute

Alternative syntax using `typeof()`:

```csharp
using DecoWeaver.Attributes;

[DecoratedBy(typeof(LoggingDecorator))]
public class UserRepository : IUserRepository
{
    // Your implementation
}
```

Both syntaxes are equivalent - use whichever you prefer.

## Where to Apply

Apply `[DecoratedBy]` to **implementation classes**, not interfaces:

```csharp
// ✅ Correct: Apply to implementation
[DecoratedBy<LoggingDecorator>]
public class UserRepository : IUserRepository { }

// ❌ Incorrect: Don't apply to interface
[DecoratedBy<LoggingDecorator>]
public interface IUserRepository { }
```

## Decorator Requirements

For DecoWeaver to work correctly, your decorator must:

1. **Implement the same interface** as the decorated class
2. **Accept the interface as a constructor parameter** (typically first parameter)
3. **Have all dependencies resolvable** from the DI container

```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
}

// ✅ Valid decorator
public class LoggingUserRepository : IUserRepository
{
    private readonly IUserRepository _inner;  // 1. Same interface
    private readonly ILogger _logger;

    // 2. Accept interface in constructor
    public LoggingUserRepository(IUserRepository inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        _logger.LogInformation("Getting user {Id}", id);
        return await _inner.GetByIdAsync(id);  // 3. Delegate to inner
    }
}

// Apply the decorator
[DecoratedBy<LoggingUserRepository>]
public class UserRepository : IUserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        // Your implementation
    }
}
```

## Registration

Once you've applied the attribute, register your service normally:

```csharp
var services = new ServiceCollection();

// DecoWeaver automatically applies the decorator
services.AddScoped<IUserRepository, UserRepository>();

// Or with other lifetimes
services.AddSingleton<IUserRepository, UserRepository>();
services.AddTransient<IUserRepository, UserRepository>();
```

DecoWeaver intercepts these registration calls and wraps your implementation with the decorator.

## Decorator Dependencies

Decorators can have their own dependencies, resolved from the DI container:

```csharp
public class CachingRepository : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly IMemoryCache _cache;      // Resolved from DI
    private readonly ILogger _logger;          // Resolved from DI
    private readonly IOptions<CacheOptions> _options; // Resolved from DI

    public CachingRepository(
        IUserRepository inner,
        IMemoryCache cache,
        ILogger<CachingRepository> logger,
        IOptions<CacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
        _options = options;
    }

    // Implementation
}
```

Just make sure all dependencies are registered:

```csharp
services.AddMemoryCache();
services.AddLogging();
services.Configure<CacheOptions>(config.GetSection("Cache"));

[DecoratedBy<CachingRepository>]
public class UserRepository : IUserRepository { }

services.AddScoped<IUserRepository, UserRepository>();
```

## Multiple Interfaces

If your implementation implements multiple interfaces, decorators apply per interface:

```csharp
public interface IReadRepository
{
    Task<User> GetByIdAsync(int id);
}

public interface IWriteRepository
{
    Task SaveAsync(User user);
}

// Decorates both interfaces
[DecoratedBy<LoggingDecorator>]
public class UserRepository : IReadRepository, IWriteRepository
{
    public Task<User> GetByIdAsync(int id) { }
    public Task SaveAsync(User user) { }
}

// Register each interface separately
services.AddScoped<IReadRepository, UserRepository>();
services.AddScoped<IWriteRepository, UserRepository>();
```

Both registrations will be decorated.

## Inheritance

The `[DecoratedBy]` attribute is **not inherited**:

```csharp
[DecoratedBy<LoggingDecorator>]
public class BaseRepository : IRepository { }

// This is NOT decorated - attribute doesn't inherit
public class UserRepository : BaseRepository { }

// You must apply it explicitly
[DecoratedBy<LoggingDecorator>]
public class UserRepository : BaseRepository { }
```

## Closed Generic Types

For closed generic types, use the decorator directly:

```csharp
// Closed generic implementation
[DecoratedBy<CachingUserRepository>]
public class UserRepository : IRepository<User>
{
    // Implementation
}

// Closed generic decorator
public class CachingUserRepository : IRepository<User>
{
    private readonly IRepository<User> _inner;

    public CachingUserRepository(IRepository<User> inner, IMemoryCache cache)
    {
        _inner = inner;
    }
}

// Registration
services.AddScoped<IRepository<User>, UserRepository>();
```

## Order Property

Control the order when using multiple decorators:

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<MetricsDecorator>(Order = 3)]
public class UserRepository : IUserRepository { }
```

Lower order numbers are closer to the implementation. See [Order and Nesting](../core-concepts/order-and-nesting.md) for details.

## Common Patterns

### Logging Decorator

```csharp
public class LoggingDecorator<T> : T where T : class
{
    private readonly T _inner;
    private readonly ILogger _logger;

    public LoggingDecorator(T inner, ILogger<LoggingDecorator<T>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    // Implement all interface methods with logging
}

[DecoratedBy<LoggingDecorator<IUserRepository>>]
public class UserRepository : IUserRepository { }
```

### Caching Decorator

```csharp
public class CachingDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly IMemoryCache _cache;

    public CachingDecorator(IUserRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

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

[DecoratedBy<CachingDecorator>]
public class UserRepository : IUserRepository { }
```

### Metrics Decorator

```csharp
public class MetricsDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly IMeterFactory _meterFactory;

    public MetricsDecorator(IUserRepository inner, IMeterFactory meterFactory)
    {
        _inner = inner;
        _meterFactory = meterFactory;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        using var activity = _meterFactory.StartActivity("user.get");

        try
        {
            var result = await _inner.GetByIdAsync(id);
            activity.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch
        {
            activity.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}

[DecoratedBy<MetricsDecorator>]
public class UserRepository : IUserRepository { }
```

## Troubleshooting

### Decorator Not Applied

If your decorator isn't being applied:

1. Check C# language version is set to 11 or higher in .csproj
2. Verify the decorator implements the same interface
3. Ensure all decorator dependencies are registered in DI
4. Rebuild the solution to regenerate interceptor code
5. Check for build errors in the Error List

### Missing Dependencies

If you get runtime errors about missing dependencies:

```csharp
// ❌ Error: IMemoryCache not registered
[DecoratedBy<CachingDecorator>]
public class UserRepository : IUserRepository { }

services.AddScoped<IUserRepository, UserRepository>();
// Missing: services.AddMemoryCache();

// ✅ Fixed: Register all dependencies
services.AddMemoryCache();
services.AddScoped<IUserRepository, UserRepository>();
```

### Wrong Constructor Parameter

The decorated interface must be the first parameter (or properly identified):

```csharp
// ❌ Incorrect: Interface not first
public class LoggingDecorator : IUserRepository
{
    public LoggingDecorator(ILogger logger, IUserRepository inner) { }
}

// ✅ Correct: Interface first
public class LoggingDecorator : IUserRepository
{
    public LoggingDecorator(IUserRepository inner, ILogger logger) { }
}
```

## Best Practices

1. **Keep decorators focused** on a single concern (logging, caching, etc.)
2. **Make decorators reusable** with generic type parameters when possible
3. **Use meaningful order numbers** (10, 20, 30 instead of 1, 2, 3) to allow insertion
4. **Document decorator behavior** with XML comments
5. **Test decorators in isolation** with mocked inner implementations

## Next Steps

- Learn about [Multiple Decorators](multiple-decorators.md)
- Explore [Open Generic Support](open-generics.md)
- See [Examples](../examples/index.md) of real-world usage