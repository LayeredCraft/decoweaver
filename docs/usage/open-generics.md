# Generic Type Decoration

DecoWeaver supports decorating generic types like `IRepository<T>` with open generic decorators, making it easy to apply cross-cutting concerns to generic service patterns.

!!! warning "Registration Requirement"
    DecoWeaver requires **closed generic registrations** using the `AddScoped<TService, TImplementation>()` syntax. Open generic registrations using `AddScoped(typeof(IRepository<>), typeof(Repository<>))` are **NOT supported** and will not apply decorators.

## What are Open Generics?

Open generic types are generic types with unspecified type parameters:

```csharp
// Open generic interface
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task SaveAsync(T entity);
}

// Open generic implementation
public class Repository<T> : IRepository<T> where T : class
{
    // Implementation
}
```

With DecoWeaver, register each closed generic type to apply decorators:

```csharp
// Register each closed generic type explicitly - required for DecoWeaver
services.AddScoped<IRepository<User>, Repository<User>>();
services.AddScoped<IRepository<Product>, Repository<Product>>();

// Resolves decorated instances
var userRepo = provider.GetRequiredService<IRepository<User>>();
var productRepo = provider.GetRequiredService<IRepository<Product>>();
```

## Decorating Open Generics

Use the `<>` syntax to create open generic decorators:

```csharp
// Open generic decorator
public class CachingRepository<T> : IRepository<T> where T : class
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

    public Task SaveAsync(T entity)
    {
        _cache.Remove($"{typeof(T).Name}:{GetEntityId(entity)}");
        return _inner.SaveAsync(entity);
    }
}

// Apply with empty angle brackets
[DecoratedBy<CachingRepository<>>]
public class Repository<T> : IRepository<T> where T : class
{
    // Implementation
}
```

**Important**: Use `<>` (empty angle brackets) to indicate an open generic decorator.

## Registration

DecoWeaver requires **closed generic registrations** (specific type instantiations):

```csharp
services.AddMemoryCache();

// DecoWeaver intercepts closed generic registrations
services.AddScoped<IRepository<User>, Repository<User>>();
services.AddScoped<IRepository<Product>, Repository<Product>>();

// Each instance gets its own decorated version
var userRepo = provider.GetRequiredService<IRepository<User>>();
// Returns: CachingRepository<User> wrapping Repository<User>

var productRepo = provider.GetRequiredService<IRepository<Product>>();
// Returns: CachingRepository<Product> wrapping Repository<Product>
```

### Supported Registration Signatures

✅ **Supported - Parameterless Registration:**
```csharp
services.AddScoped<IRepository<Customer>, SqlRepository<Customer>>();
services.AddTransient<ICommand<CreateOrder>, CreateOrderCommand>();
services.AddSingleton<ICache<string>, MemoryCache<string>>();
```

❌ **Not Supported - Open Generic Registration:**
```csharp
// This will NOT intercept decorators
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

❌ **Not Supported - Factory Delegates:**
```csharp
// Decorators will NOT be applied
services.AddScoped<IRepository<User>, Repository<User>>(sp =>
    new Repository<User>(sp.GetRequiredService<ILogger>()));
```

❌ **Not Supported - Keyed Services:**
```csharp
// Decorators will NOT be applied
services.AddScoped<IRepository<User>, Repository<User>>("primary");
```

❌ **Not Supported - Instance Registration:**
```csharp
// Decorators will NOT be applied
services.AddScoped<IRepository<User>>(new Repository<User>());
```

> **Note**: Support for factory delegates and other registration patterns is tracked in [Issue #3](https://github.com/layeredcraft/decoweaver/issues/3). For now, use the parameterless registration and inject dependencies through the constructor.

## Type Constraints

Decorators must match the constraints of the decorated type:

```csharp
// Implementation has constraint
public class Repository<T> : IRepository<T>
    where T : class, IEntity
{
    // Implementation
}

// ✅ Decorator matches constraint
[DecoratedBy<CachingRepository<>>]
public class CachingRepository<T> : IRepository<T>
    where T : class, IEntity
{
    // Implementation
}

// ❌ Decorator missing constraint - won't compile
public class CachingRepository<T> : IRepository<T>
    where T : class
{
    // Missing IEntity constraint
}
```

## Multiple Type Parameters

DecoWeaver supports multiple type parameters:

```csharp
public interface IKeyValueStore<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key);
    Task SetAsync(TKey key, TValue value);
}

public class CachingKeyValueStore<TKey, TValue> : IKeyValueStore<TKey, TValue>
{
    private readonly IKeyValueStore<TKey, TValue> _inner;
    private readonly IMemoryCache _cache;

    public CachingKeyValueStore(
        IKeyValueStore<TKey, TValue> inner,
        IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    // Implementation
}

[DecoratedBy<CachingKeyValueStore<,>>]  // Note: comma for second parameter
public class KeyValueStore<TKey, TValue> : IKeyValueStore<TKey, TValue>
{
    // Implementation
}

// Register each closed generic type - required for DecoWeaver
services.AddScoped<IKeyValueStore<string, int>, KeyValueStore<string, int>>();
services.AddScoped<IKeyValueStore<string, User>, KeyValueStore<string, User>>();
```

## Nested Generic Types

DecoWeaver handles nested generic types:

```csharp
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
}

public class CachingRepository<T> : IRepository<T> where T : class
{
    private readonly IRepository<T> _inner;

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        // Cache a collection of T
        var key = $"{typeof(T).Name}:all";
        if (_cache.TryGetValue(key, out IEnumerable<T> cached))
            return cached;

        var entities = await _inner.GetAllAsync();
        _cache.Set(key, entities);
        return entities;
    }
}

[DecoratedBy<CachingRepository<>>]
public class Repository<T> : IRepository<T> where T : class
{
    // Implementation
}
```

## Multiple Decorators

Stack multiple open generic decorators:

```csharp
[DecoratedBy<LoggingRepository<>>(Order = 1)]
[DecoratedBy<CachingRepository<>>(Order = 2)]
[DecoratedBy<MetricsRepository<>>(Order = 3)]
public class Repository<T> : IRepository<T> where T : class
{
    // Implementation
}

// All closed generics get all decorators in order
var userRepo = provider.GetRequiredService<IRepository<User>>();
// MetricsRepository<User> → CachingRepository<User> → LoggingRepository<User> → Repository<User>
```

## Type-Specific Decorators

Mix open generic and closed generic decorators:

```csharp
// Open generic decorator for all types
public class CachingRepository<T> : IRepository<T> where T : class { }

// Closed generic decorator for specific type
public class AuditedUserRepository : IRepository<User>
{
    private readonly IRepository<User> _inner;
    private readonly IAuditLog _auditLog;

    public AuditedUserRepository(IRepository<User> inner, IAuditLog auditLog)
    {
        _inner = inner;
        _auditLog = auditLog;
    }

    public async Task SaveAsync(User user)
    {
        await _auditLog.LogAsync($"Saving user {user.Id}");
        await _inner.SaveAsync(user);
    }
}

// Generic implementation
[DecoratedBy<CachingRepository<>>]
public class Repository<T> : IRepository<T> where T : class { }

// User-specific implementation gets both decorators
[DecoratedBy<CachingRepository<>>]
[DecoratedBy<AuditedUserRepository>]
public class UserRepository : Repository<User>
{
    // User-specific implementation
}

// Register each type - DecoWeaver requires closed registrations
services.AddScoped<IRepository<Product>, Repository<Product>>();
services.AddScoped<IRepository<User>, UserRepository>();  // User-specific
```

## Dependency Injection with Open Generics

Decorators can have dependencies that work with the generic type:

```csharp
public class ValidationRepository<T> : IRepository<T>
    where T : class, IValidatable
{
    private readonly IRepository<T> _inner;
    private readonly IValidator<T> _validator;  // Generic dependency

    public ValidationRepository(
        IRepository<T> inner,
        IValidator<T> validator)
    {
        _inner = inner;
        _validator = validator;
    }

    public async Task SaveAsync(T entity)
    {
        await _validator.ValidateAndThrowAsync(entity);
        await _inner.SaveAsync(entity);
    }
}

[DecoratedBy<ValidationRepository<>>]
public class Repository<T> : IRepository<T>
    where T : class, IValidatable
{
    // Implementation
}

// Register validators (standard .NET DI - not decorated by DecoWeaver)
services.AddScoped(typeof(IValidator<>), typeof(Validator<>));

// Register repositories - DecoWeaver requires closed registrations
services.AddScoped<IRepository<User>, Repository<User>>();
services.AddScoped<IRepository<Product>, Repository<Product>>();

var userRepo = provider.GetRequiredService<IRepository<User>>();
// Resolves: ValidationRepository<User> wrapping Repository<User>, with IValidator<User> injected
```

## Accessing Type Information

Use `typeof(T)` inside decorators to access type information:

```csharp
public class CachingRepository<T> : IRepository<T> where T : class
{
    private readonly IMemoryCache _cache;

    public async Task<T> GetByIdAsync(int id)
    {
        // Use type name in cache key
        var key = $"{typeof(T).Name}:{id}";

        if (_cache.TryGetValue(key, out T cached))
        {
            Console.WriteLine($"Cache hit for {typeof(T).FullName}");
            return cached;
        }

        var entity = await _inner.GetByIdAsync(id);

        // Different cache duration based on type
        var duration = typeof(T).GetCustomAttribute<CacheDurationAttribute>()
            ?.Duration ?? TimeSpan.FromMinutes(5);

        _cache.Set(key, entity, duration);
        return entity;
    }
}

[CacheDuration(Minutes = 10)]
public class User : IEntity { }

[CacheDuration(Minutes = 60)]
public class Product : IEntity { }
```

## How It Works

For open generic decorators, DecoWeaver generates interceptors that:

1. **Detect closed generic registration**:
   ```csharp
   // You register each closed generic type
   services.AddScoped<IRepository<User>, Repository<User>>();
   ```

2. **Generate interceptor that wraps with factory**:
   ```csharp
   // DecoWeaver generates code that intercepts the above call and rewrites it to:

   // 1. Register undecorated implementation as keyed service
   services.AddKeyedScoped<IRepository<User>, Repository<User>>(key);

   // 2. Register factory that resolves keyed service and applies decorators
   services.AddScoped<IRepository<User>>(sp =>
   {
       // Get the undecorated implementation
       var impl = sp.GetRequiredKeyedService<IRepository<User>>(key);

       // Close the open generic decorator at runtime: CachingRepository<> → CachingRepository<User>
       var decoratorType = typeof(CachingRepository<>).MakeGenericType(typeof(User));

       // Resolve decorator dependencies and construct
       var cache = sp.GetRequiredService<IMemoryCache>();
       return (IRepository<User>)Activator.CreateInstance(decoratorType, impl, cache);
   });
   ```

3. **Runtime type closing for each registered type**:
   - `IRepository<User>` registration → `CachingRepository<User>` (closed at runtime)
   - `IRepository<Product>` registration → `CachingRepository<Product>` (closed at runtime)
   - Each closed registration gets its own interceptor

## Common Patterns

### Logging Repository

```csharp
public class LoggingRepository<T> : IRepository<T> where T : class
{
    private readonly IRepository<T> _inner;
    private readonly ILogger<LoggingRepository<T>> _logger;

    public LoggingRepository(
        IRepository<T> inner,
        ILogger<LoggingRepository<T>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<T> GetByIdAsync(int id)
    {
        _logger.LogInformation(
            "Getting {EntityType} with id {Id}",
            typeof(T).Name,
            id);

        var entity = await _inner.GetByIdAsync(id);

        _logger.LogInformation(
            "Retrieved {EntityType} with id {Id}",
            typeof(T).Name,
            id);

        return entity;
    }
}

[DecoratedBy<LoggingRepository<>>]
public class Repository<T> : IRepository<T> where T : class { }
```

### Caching Repository

```csharp
public class CachingRepository<T> : IRepository<T>
    where T : class, IEntity
{
    private readonly IRepository<T> _inner;
    private readonly IDistributedCache _cache;
    private readonly ISerializer _serializer;

    public async Task<T> GetByIdAsync(int id)
    {
        var key = $"{typeof(T).Name}:{id}";
        var cached = await _cache.GetStringAsync(key);

        if (cached != null)
            return _serializer.Deserialize<T>(cached);

        var entity = await _inner.GetByIdAsync(id);

        await _cache.SetStringAsync(
            key,
            _serializer.Serialize(entity),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        return entity;
    }
}

[DecoratedBy<CachingRepository<>>]
public class Repository<T> : IRepository<T>
    where T : class, IEntity { }
```

### Metrics Repository

```csharp
public class MetricsRepository<T> : IRepository<T> where T : class
{
    private readonly IRepository<T> _inner;
    private readonly IMeterFactory _meterFactory;
    private readonly Counter<long> _getCounter;
    private readonly Histogram<double> _getDuration;

    public MetricsRepository(
        IRepository<T> inner,
        IMeterFactory meterFactory)
    {
        _inner = inner;
        _meterFactory = meterFactory;

        var meter = _meterFactory.Create("Repository");
        _getCounter = meter.CreateCounter<long>("repository.get.count");
        _getDuration = meter.CreateHistogram<double>("repository.get.duration");
    }

    public async Task<T> GetByIdAsync(int id)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await _inner.GetByIdAsync(id);

            _getCounter.Add(1, new TagList
            {
                { "entity_type", typeof(T).Name },
                { "status", "success" }
            });

            return result;
        }
        catch
        {
            _getCounter.Add(1, new TagList
            {
                { "entity_type", typeof(T).Name },
                { "status", "error" }
            });
            throw;
        }
        finally
        {
            _getDuration.Record(sw.Elapsed.TotalMilliseconds, new TagList
            {
                { "entity_type", typeof(T).Name }
            });
        }
    }
}

[DecoratedBy<MetricsRepository<>>]
public class Repository<T> : IRepository<T> where T : class { }
```

## Limitations

### No Partially Closed Generics

DecoWeaver doesn't support partially closing generic types:

```csharp
// ❌ Not supported: Partially closed generic
[DecoratedBy<CachingKeyValueStore<string, >>]
public class KeyValueStore<TValue> : IKeyValueStore<string, TValue> { }

// ✅ Workaround: Create a new open generic
public class StringKeyValueStore<TValue> : IKeyValueStore<string, TValue> { }

[DecoratedBy<CachingStringKeyValueStore<>>]
public class StringKeyValueStore<TValue> { }
```

### Closed Generic Registration Required

DecoWeaver requires closed generic registration syntax:

```csharp
// ❌ Open generic registration - NOT intercepted by DecoWeaver:
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ✅ Closed generic registration - intercepted and decorated:
[DecoratedBy<CachingRepository<>>]
public class Repository<T> : IRepository<T> { }

services.AddScoped<IRepository<User>, Repository<User>>();
services.AddScoped<IRepository<Product>, Repository<Product>>();
```

Each closed registration is intercepted separately and gets its own decorator chain.

## Best Practices

1. **Match type constraints** between decorator and implementation
2. **Use `typeof(T)` for type-specific behavior** (caching keys, logging, etc.)
3. **Keep decorators generic** when possible for maximum reusability
4. **Consider performance** of runtime type closing
5. **Test with multiple type arguments** to ensure correctness

## Next Steps

- See [Examples](../examples/index.md) with open generic patterns
- Learn about [Multiple Decorators](multiple-decorators.md)
- Understand [How It Works](../core-concepts/how-it-works.md) under the hood