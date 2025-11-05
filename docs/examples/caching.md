# Caching Examples

Caching decorators improve performance by storing frequently accessed data in memory or distributed cache.

## Memory Caching

Basic in-memory caching with `IMemoryCache`:

```csharp
public class CachingUserService : IUserService
{
    private readonly IUserService _inner;
    private readonly IMemoryCache _cache;

    public CachingUserService(IUserService inner, IMemoryCache cache)
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

    public async Task SaveAsync(User user)
    {
        await _inner.SaveAsync(user);

        // Invalidate cache on save
        _cache.Remove($"user:{user.Id}");
    }
}

[DecoratedBy<CachingUserService>]
public class UserService : IUserService
{
    // Implementation
}

// Registration
services.AddMemoryCache();
services.AddScoped<IUserService, UserService>();
```

## Distributed Caching

Redis or SQL Server distributed caching:

```csharp
public class DistributedCachingService : IUserService
{
    private readonly IUserService _inner;
    private readonly IDistributedCache _cache;
    private readonly ISerializer _serializer;

    public DistributedCachingService(
        IUserService inner,
        IDistributedCache cache,
        ISerializer serializer)
    {
        _inner = inner;
        _cache = cache;
        _serializer = serializer;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var key = $"user:{id}";
        var cached = await _cache.GetStringAsync(key);

        if (cached != null)
            return _serializer.Deserialize<User>(cached);

        var user = await _inner.GetByIdAsync(id);

        await _cache.SetStringAsync(
            key,
            _serializer.Serialize(user),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            });

        return user;
    }

    public async Task SaveAsync(User user)
    {
        await _inner.SaveAsync(user);
        await _cache.RemoveAsync($"user:{user.Id}");
    }
}

[DecoratedBy<DistributedCachingService>]
public class UserService : IUserService
{
    // Implementation
}

// Registration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
services.AddScoped<ISerializer, JsonSerializer>();
services.AddScoped<IUserService, UserService>();
```

## Conditional Caching

Cache based on business rules:

```csharp
public class ConditionalCachingService : IUserService
{
    private readonly IUserService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConditionalCachingService> _logger;

    public ConditionalCachingService(
        IUserService inner,
        IMemoryCache cache,
        ILogger<ConditionalCachingService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var key = $"user:{id}";

        if (_cache.TryGetValue(key, out User cached))
        {
            _logger.LogDebug("Cache hit for user {UserId}", id);
            return cached;
        }

        _logger.LogDebug("Cache miss for user {UserId}", id);
        var user = await _inner.GetByIdAsync(id);

        // Only cache active users
        if (user.IsActive)
        {
            // Premium users get longer cache time
            var duration = user.IsPremium
                ? TimeSpan.FromMinutes(15)
                : TimeSpan.FromMinutes(5);

            _cache.Set(key, user, duration);
            _logger.LogDebug(
                "Cached user {UserId} for {Minutes} minutes",
                id,
                duration.TotalMinutes);
        }
        else
        {
            _logger.LogDebug("User {UserId} not cached (inactive)", id);
        }

        return user;
    }
}

[DecoratedBy<ConditionalCachingService>]
public class UserService : IUserService
{
    // Implementation
}
```

## Cache Invalidation

Sophisticated cache invalidation strategies:

```csharp
public class SmartCachingService : IUserService
{
    private readonly IUserService _inner;
    private readonly IMemoryCache _cache;
    private readonly ICacheInvalidationService _invalidation;

    public SmartCachingService(
        IUserService inner,
        IMemoryCache cache,
        ICacheInvalidationService invalidation)
    {
        _inner = inner;
        _cache = cache;
        _invalidation = invalidation;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var key = $"user:{id}";

        if (_cache.TryGetValue(key, out User cached))
        {
            // Check if cache is still valid
            if (!await _invalidation.IsInvalidatedAsync(key))
                return cached;

            _cache.Remove(key);
        }

        var user = await _inner.GetByIdAsync(id);

        _cache.Set(key, user, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        Console.WriteLine($"Cache entry {key} evicted: {reason}");
                    }
                }
            }
        });

        return user;
    }

    public async Task SaveAsync(User user)
    {
        await _inner.SaveAsync(user);

        // Invalidate this user's cache
        _cache.Remove($"user:{user.Id}");

        // Invalidate related caches
        await _invalidation.InvalidatePatternAsync($"user-search:*");
        await _invalidation.InvalidatePatternAsync($"user-list:*");
    }
}

public interface ICacheInvalidationService
{
    Task<bool> IsInvalidatedAsync(string key);
    Task InvalidateAsync(string key);
    Task InvalidatePatternAsync(string pattern);
}

[DecoratedBy<SmartCachingService>]
public class UserService : IUserService
{
    // Implementation
}
```

## Cache-Aside Pattern

Implement cache-aside (lazy loading) with fallback:

```csharp
public class CacheAsideService : IUserService
{
    private readonly IUserService _inner;
    private readonly IDistributedCache _cache;
    private readonly ISerializer _serializer;
    private readonly ILogger<CacheAsideService> _logger;

    public CacheAsideService(
        IUserService inner,
        IDistributedCache cache,
        ISerializer serializer,
        ILogger<CacheAsideService> logger)
    {
        _inner = inner;
        _cache = cache;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var key = $"user:{id}";

        try
        {
            // Try to get from cache
            var cached = await _cache.GetStringAsync(key);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for {Key}", key);
                return _serializer.Deserialize<User>(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for {Key}, falling back to source", key);
        }

        _logger.LogDebug("Cache miss for {Key}, loading from source", key);

        // Cache miss - get from source
        var user = await _inner.GetByIdAsync(id);

        try
        {
            // Update cache
            await _cache.SetStringAsync(
                key,
                _serializer.Serialize(user),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

            _logger.LogDebug("Cached {Key} successfully", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write to cache for {Key}", key);
            // Don't fail the request if caching fails
        }

        return user;
    }
}

[DecoratedBy<CacheAsideService>]
public class UserService : IUserService
{
    // Implementation
}
```

## Open Generic Caching

Reusable caching decorator for `IRepository<T>`:

```csharp
public class CachingRepository<T> : IRepository<T>
    where T : class, IEntity
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

        // Get cache duration from entity attribute
        var duration = GetCacheDuration();
        _cache.Set(key, entity, duration);

        return entity;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var key = $"{typeof(T).Name}:all";

        if (_cache.TryGetValue(key, out IEnumerable<T> cached))
            return cached;

        var entities = await _inner.GetAllAsync();

        // Cache collections for shorter time
        _cache.Set(key, entities, TimeSpan.FromMinutes(1));

        return entities;
    }

    public async Task SaveAsync(T entity)
    {
        await _inner.SaveAsync(entity);

        // Invalidate individual and collection caches
        _cache.Remove($"{typeof(T).Name}:{entity.Id}");
        _cache.Remove($"{typeof(T).Name}:all");
    }

    private TimeSpan GetCacheDuration()
    {
        var attribute = typeof(T).GetCustomAttribute<CacheDurationAttribute>();
        return attribute?.Duration ?? TimeSpan.FromMinutes(5);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class CacheDurationAttribute : Attribute
{
    public TimeSpan Duration { get; }

    public CacheDurationAttribute(int minutes)
    {
        Duration = TimeSpan.FromMinutes(minutes);
    }
}

// Apply to repository
[DecoratedBy<CachingRepository<>>]
public class Repository<T> : IRepository<T> where T : class, IEntity
{
    // Implementation
}

// Configure cache duration per entity
[CacheDuration(10)]
public class User : IEntity { }

[CacheDuration(60)]
public class Product : IEntity { }

// Registration
services.AddMemoryCache();
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

## Hybrid Caching

Combine memory and distributed cache (L1 + L2):

```csharp
public class HybridCachingService : IUserService
{
    private readonly IUserService _inner;
    private readonly IMemoryCache _l1Cache;
    private readonly IDistributedCache _l2Cache;
    private readonly ISerializer _serializer;

    public HybridCachingService(
        IUserService inner,
        IMemoryCache l1Cache,
        IDistributedCache l2Cache,
        ISerializer serializer)
    {
        _inner = inner;
        _l1Cache = l1Cache;
        _l2Cache = l2Cache;
        _serializer = serializer;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var key = $"user:{id}";

        // Try L1 cache (memory)
        if (_l1Cache.TryGetValue(key, out User l1Cached))
            return l1Cached;

        // Try L2 cache (distributed)
        var l2Cached = await _l2Cache.GetStringAsync(key);
        if (l2Cached != null)
        {
            var user = _serializer.Deserialize<User>(l2Cached);

            // Promote to L1 cache
            _l1Cache.Set(key, user, TimeSpan.FromMinutes(2));

            return user;
        }

        // Cache miss - load from source
        var result = await _inner.GetByIdAsync(id);

        // Write to both caches
        _l1Cache.Set(key, result, TimeSpan.FromMinutes(2));

        await _l2Cache.SetStringAsync(
            key,
            _serializer.Serialize(result),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

        return result;
    }

    public async Task SaveAsync(User user)
    {
        await _inner.SaveAsync(user);

        var key = $"user:{user.Id}";

        // Invalidate both caches
        _l1Cache.Remove(key);
        await _l2Cache.RemoveAsync(key);
    }
}

[DecoratedBy<HybridCachingService>]
public class UserService : IUserService
{
    // Implementation
}
```

## Cache Warming

Pre-populate cache on startup:

```csharp
public class CacheWarmingHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CacheWarmingHostedService> _logger;

    public CacheWarmingHostedService(
        IServiceProvider services,
        ILogger<CacheWarmingHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cache warming");

        using var scope = _services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Warm commonly accessed users
        var popularUserIds = new[] { 1, 2, 3, 5, 10 };

        foreach (var id in popularUserIds)
        {
            try
            {
                await userService.GetByIdAsync(id);
                _logger.LogInformation("Warmed cache for user {UserId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm cache for user {UserId}", id);
            }
        }

        _logger.LogInformation("Cache warming completed");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Registration
services.AddMemoryCache();
services.AddScoped<IUserService, UserService>();
services.AddHostedService<CacheWarmingHostedService>();
```

## Testing

Test caching decorators:

```csharp
public class CachingServiceTests
{
    [Fact]
    public async Task CachesResults()
    {
        // Arrange
        var inner = Substitute.For<IUserService>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var decorator = new CachingUserService(inner, cache);

        var user = new User { Id = 123, Name = "John" };
        inner.GetByIdAsync(123).Returns(user);

        // Act
        var result1 = await decorator.GetByIdAsync(123);
        var result2 = await decorator.GetByIdAsync(123);

        // Assert
        Assert.Equal(user, result1);
        Assert.Equal(user, result2);
        await inner.Received(1).GetByIdAsync(123); // Only called once
    }

    [Fact]
    public async Task InvalidatesCacheOnSave()
    {
        // Arrange
        var inner = Substitute.For<IUserService>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var decorator = new CachingUserService(inner, cache);

        var user = new User { Id = 123, Name = "John" };
        inner.GetByIdAsync(123).Returns(user);

        // Act
        await decorator.GetByIdAsync(123); // Cache
        await decorator.SaveAsync(user);   // Invalidate
        await decorator.GetByIdAsync(123); // Re-fetch

        // Assert
        await inner.Received(2).GetByIdAsync(123); // Called twice
    }
}
```

## Best Practices

1. **Choose appropriate cache duration** based on data volatility
2. **Invalidate cache on writes** to maintain consistency
3. **Handle cache failures gracefully** - don't fail requests
4. **Use cache keys consistently** across your application
5. **Monitor cache hit rates** to optimize performance
6. **Consider memory pressure** - don't cache everything
7. **Use distributed cache** for multi-instance scenarios
8. **Implement cache warming** for frequently accessed data

## Next Steps

- Explore [Logging Examples](logging.md)
- Learn about [Resilience Patterns](resilience.md)
- See [Complete Example](index.md#complete-example) with multiple decorators