# Class-Level Decorators

The `[DecoratedBy]` attribute is the primary way to apply decorators in DecoWeaver. Apply it to your service implementation classes to automatically wrap them with decorators.

!!! info "Assembly-Level Alternative"
    For cross-cutting concerns applied to many implementations, consider using [Assembly-Level Decorators](assembly-level-decorators.md) instead. Class-level decorators are best for implementation-specific needs.

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

## Factory Delegate Registration

!!! info "New in v1.0.2-beta"
    Factory delegate support was added in version 1.0.2-beta.

DecoWeaver also supports factory delegate registrations, allowing you to use custom initialization logic while still applying decorators:

### Two-Parameter Generic Factory

```csharp
[DecoratedBy<CachingRepository>]
public class UserRepository : IUserRepository
{
    // Your implementation
}

// Factory delegate with two type parameters
services.AddScoped<IUserRepository, UserRepository>(sp =>
    new UserRepository());
```

### Single-Parameter Generic Factory

```csharp
[DecoratedBy<LoggingRepository>]
public class UserRepository : IUserRepository
{
    // Your implementation
}

// Factory delegate with single type parameter
services.AddScoped<IUserRepository>(sp =>
    new UserRepository());
```

### Complex Dependencies

Factory delegates can resolve dependencies from the `IServiceProvider`:

```csharp
[DecoratedBy<CachingRepository>]
public class UserRepository : IUserRepository
{
    private readonly ILogger _logger;
    private readonly IOptions<DatabaseOptions> _options;

    public UserRepository(ILogger logger, IOptions<DatabaseOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    // Implementation
}

// Register with factory that resolves dependencies
services.AddScoped<IUserRepository, UserRepository>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<UserRepository>();
    var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();

    return new UserRepository(logger, options);
});
```

### How It Works

When using factory delegates:

1. **Your factory logic is preserved** - The lambda you provide is captured and used
2. **Decorators are applied around the result** - DecoWeaver wraps the factory's output
3. **All lifetimes are supported** - `AddScoped`, `AddTransient`, `AddSingleton`

The generated code:
- Registers your factory as a keyed service
- Creates an outer factory that calls your factory and applies decorators
- Maintains the same dependency resolution behavior you defined

```csharp
// What you write:
services.AddScoped<IUserRepository, UserRepository>(sp =>
    new UserRepository(sp.GetRequiredService<ILogger>()));

// What happens (conceptually):
// 1. Your factory is registered as keyed service
// 2. Outer factory applies decorators:
var repo = /* your factory result */;
var cached = new CachingRepository(repo);
var logged = new LoggingRepository(cached);
// 3. Logged instance is returned
```

### Factory Delegate Limitations

Factory delegates work with:
- ✅ Generic registration methods: `AddScoped<T1, T2>(factory)`, `AddScoped<T>(factory)`
- ✅ Keyed service registrations with factory delegates (as of v1.0.3-beta)
- ✅ All standard lifetimes: Scoped, Transient, Singleton
- ✅ Complex dependency resolution from `IServiceProvider`
- ✅ Multiple decorators with ordering

Not currently supported:
- ❌ Instance registrations
- ❌ Open generic registration with `typeof()` syntax

## Keyed Service Registration

!!! info "New in v1.0.3-beta"
    Keyed service support was added in version 1.0.3-beta.

DecoWeaver supports keyed service registrations (introduced in .NET 8+), allowing you to register multiple implementations of the same interface under different keys while applying decorators independently:

### Basic Keyed Service

```csharp
[DecoratedBy<LoggingRepository<>>]
public class SqlRepository<T> : IRepository<T>
{
    // Your SQL implementation
}

// Register with a key
services.AddKeyedScoped<IRepository<User>, SqlRepository<User>>("sql");

// Resolve using the key
var repo = serviceProvider.GetRequiredKeyedService<IRepository<User>>("sql");
// Returns: LoggingRepository<User> wrapping SqlRepository<User>
```

### Multiple Keys for Same Service

Register multiple implementations with different keys - each gets decorated independently:

```csharp
[DecoratedBy<CachingRepository<>>]
public class SqlRepository<T> : IRepository<T> { }

[DecoratedBy<CachingRepository<>>]
public class CosmosRepository<T> : IRepository<T> { }

// Register both with different keys
services.AddKeyedScoped<IRepository<User>, SqlRepository<User>>("sql");
services.AddKeyedScoped<IRepository<User>, CosmosRepository<User>>("cosmos");

// Each resolves with its own decorator chain
var sqlRepo = serviceProvider.GetRequiredKeyedService<IRepository<User>>("sql");
var cosmosRepo = serviceProvider.GetRequiredKeyedService<IRepository<User>>("cosmos");
```

### Different Key Types

Keys can be any object type:

```csharp
// String keys
services.AddKeyedScoped<IRepository<User>, UserRepository>("primary");

// Integer keys
services.AddKeyedScoped<IRepository<Order>, OrderRepository>(1);

// Enum keys
public enum DatabaseType { Primary, Secondary, Archive }
services.AddKeyedScoped<IRepository<Data>, DataRepository>(DatabaseType.Primary);
```

### Keyed Services with Factory Delegates

Combine keyed services with factory delegates:

```csharp
[DecoratedBy<CachingRepository<>>]
public class ConfigurableRepository<T> : IRepository<T>
{
    private readonly string _connectionString;

    public ConfigurableRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Implementation
}

// Keyed factory delegate
services.AddKeyedScoped<IRepository<Customer>, ConfigurableRepository<Customer>>(
    "primary",
    (sp, key) => new ConfigurableRepository<Customer>("Server=primary;Database=Main")
);

// Decorators still apply
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Customer>>("primary");
// Returns: CachingRepository<Customer> wrapping ConfigurableRepository<Customer>
```

### Keyed Service Lifetimes

All lifetimes are supported:

```csharp
// Scoped
services.AddKeyedScoped<IRepository<User>, UserRepository>("scoped-key");

// Transient
services.AddKeyedTransient<IRepository<Event>, EventRepository>("transient-key");

// Singleton
services.AddKeyedSingleton<IRepository<Config>, ConfigRepository>("singleton-key");
```

### How Keyed Services Work

When using keyed services with decorators:

1. **User's key is preserved** - You resolve services using your original key
2. **Nested key prevents conflicts** - DecoWeaver uses an internal nested key format
3. **Independent decoration** - Each key gets its own decorator chain

```csharp
// What you write:
services.AddKeyedScoped<IRepository<User>, SqlRepository<User>>("sql");

// What happens internally:
// 1. Nested key created: "sql|IRepository`1|SqlRepository`1"
// 2. Undecorated impl registered with nested key
// 3. Decorated factory registered with your key "sql"
// 4. When you resolve with "sql", decorators are applied
```

### Keyed Service Consumer Injection

Use the `[FromKeyedServices]` attribute to inject keyed services:

```csharp
public class UserService
{
    private readonly IRepository<User> _sqlRepo;
    private readonly IRepository<User> _cosmosRepo;

    public UserService(
        [FromKeyedServices("sql")] IRepository<User> sqlRepo,
        [FromKeyedServices("cosmos")] IRepository<User> cosmosRepo)
    {
        _sqlRepo = sqlRepo;    // Decorated SqlRepository
        _cosmosRepo = cosmosRepo; // Decorated CosmosRepository
    }
}
```

### Keyed Service Limitations

Keyed services work with:
- ✅ All registration patterns: parameterless and factory delegates
- ✅ All lifetimes: Scoped, Transient, Singleton
- ✅ All key types: string, int, enum, custom objects
- ✅ Multiple keys per service type
- ✅ Multiple decorators with ordering

Current limitations:
- ❌ Instance registrations with keys
- ❌ Open generic registration with `typeof()` syntax

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

## Comparison with Assembly-Level

| Aspect | Class-Level | Assembly-Level |
|--------|-------------|----------------|
| **Scope** | Single implementation | All implementations |
| **Attribute Location** | On class | In global file |
| **Use Case** | Implementation-specific | Cross-cutting concerns |
| **Visibility** | More explicit | Less obvious |
| **Flexibility** | Full control | Can opt-out with `[DoNotDecorate]` |

### When to Use Each

**Use Class-Level When**:
- Decorator is specific to one implementation
- You want explicit, visible decoration
- Different implementations need different decorators
- Testing or development-only decorators

**Use Assembly-Level When**:
- Same decorator applies to many implementations
- Enforcing cross-cutting concerns (logging, metrics, caching)
- Centralizing decorator configuration
- Ensuring consistency across implementations

**Combine Both When**:
- Assembly-level for common concerns
- Class-level for implementation-specific needs

```csharp
// GlobalUsings.cs - Common logging for all
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), Order = 10)]

// UserRepository.cs - Add validation specific to users
[DecoratedBy<ValidationRepository<User>>(Order = 5)]
public class UserRepository : IRepository<User> { }
```

## Next Steps

- Learn about [Assembly-Level Decorators](assembly-level-decorators.md) for cross-cutting concerns
- Learn about [Multiple Decorators](multiple-decorators.md)
- Explore [Open Generic Support](open-generics.md)
- See [Examples](../examples/index.md) of real-world usage