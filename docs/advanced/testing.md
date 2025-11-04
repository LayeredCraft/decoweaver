# Testing Strategies

Testing services decorated with Sculptor follows standard .NET testing practices. This guide shows how to test decorators individually and in combination.

## Testing Philosophy

Decorators should be testable in isolation:

- **Unit tests**: Test each decorator independently with mocked inner implementations
- **Integration tests**: Test the full decorator chain with real dependencies
- **End-to-end tests**: Test the complete service with all decorators applied

## Unit Testing Decorators

Test decorators in isolation by mocking the inner implementation:

```csharp
using NSubstitute;
using Xunit;

public class CachingRepositoryTests
{
    [Fact]
    public async Task CachesResults()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var decorator = new CachingRepository(inner, cache);

        var user = new User { Id = 123, Name = "John" };
        inner.GetByIdAsync(123).Returns(user);

        // Act
        var result1 = await decorator.GetByIdAsync(123);
        var result2 = await decorator.GetByIdAsync(123);

        // Assert
        Assert.Equal(user, result1);
        Assert.Equal(user, result2);
        await inner.Received(1).GetByIdAsync(123); // Called only once
    }

    [Fact]
    public async Task InvalidatesCacheOnSave()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var decorator = new CachingRepository(inner, cache);

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

## Testing with Test Doubles

Use test doubles (mocks, stubs, fakes) for decorator dependencies:

```csharp
public class LoggingRepositoryTests
{
    [Fact]
    public async Task LogsMethodCalls()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var logger = Substitute.For<ILogger<LoggingRepository>>();
        var decorator = new LoggingRepository(inner, logger);

        inner.GetByIdAsync(123).Returns(new User { Id = 123 });

        // Act
        await decorator.GetByIdAsync(123);

        // Assert
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Getting user")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task LogsExceptions()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var logger = Substitute.For<ILogger<LoggingRepository>>();
        var decorator = new LoggingRepository(inner, logger);

        inner.GetByIdAsync(123).Throws(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => decorator.GetByIdAsync(123));

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
```

## Integration Testing

Test decorators with real dependencies:

```csharp
public class UserServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DecoratorChain_WorksEndToEnd()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // Act - this goes through the full decorator chain
        var user = await userService.GetByIdAsync(123);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(123, user.Id);

        // Verify side effects (logs, metrics, etc.)
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserService>>();
        // Verify logs were written
    }
}
```

## Testing Decorator Order

Verify decorators are applied in the correct order:

```csharp
public class DecoratorOrderTests
{
    [Fact]
    public void Decorators_AppliedInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddMemoryCache();
        services.AddScoped<IUserRepository, UserRepository>();

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetRequiredService<IUserRepository>();

        // Assert - unwrap the decorator chain
        Assert.IsType<MetricsRepository>(service);

        var metrics = (MetricsRepository)service;
        Assert.IsType<CachingRepository>(metrics.Inner);

        var caching = (CachingRepository)metrics.Inner;
        Assert.IsType<LoggingRepository>(caching.Inner);

        var logging = (LoggingRepository)caching.Inner;
        Assert.IsType<UserRepository>(logging.Inner);
    }
}
```

## Testing with In-Memory Providers

Use in-memory implementations for integration tests:

```csharp
public class RepositoryTests
{
    [Fact]
    public async Task CachingDecorator_WorksWithInMemoryDatabase()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        services.AddMemoryCache();
        services.AddLogging();
        services.AddScoped<IUserRepository, UserRepository>();

        var provider = services.BuildServiceProvider();

        // Act
        using (var scope = provider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var user = new User { Id = 1, Name = "John" };
            await repo.SaveAsync(user);
        }

        using (var scope = provider.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // This should hit the cache
            var retrieved = await repo.GetByIdAsync(1);

            Assert.NotNull(retrieved);
            Assert.Equal("John", retrieved.Name);
        }
    }
}
```

## Testing Decorator Side Effects

Verify decorator side effects (logging, metrics, cache hits):

```csharp
public class DecoratorSideEffectsTests
{
    [Fact]
    public async Task CachingDecorator_RecordsMetrics()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var metrics = new TestMetrics();
        var decorator = new CachingRepository(inner, cache, metrics);

        inner.GetByIdAsync(123).Returns(new User { Id = 123 });

        // Act
        await decorator.GetByIdAsync(123); // Miss
        await decorator.GetByIdAsync(123); // Hit

        // Assert
        Assert.Equal(1, metrics.CacheMisses);
        Assert.Equal(1, metrics.CacheHits);
    }

    [Fact]
    public async Task LoggingDecorator_WritesStructuredLogs()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
        var logger = loggerFactory.CreateLogger<LoggingRepository>();
        var decorator = new LoggingRepository(inner, logger);

        inner.GetByIdAsync(123).Returns(new User { Id = 123 });

        // Act
        await decorator.GetByIdAsync(123);

        // Assert
        // Verify structured logs in your logging provider
    }
}

public class TestMetrics
{
    public int CacheHits { get; private set; }
    public int CacheMisses { get; private set; }

    public void RecordCacheHit() => CacheHits++;
    public void RecordCacheMiss() => CacheMisses++;
}
```

## Testing Exception Handling

Verify decorators handle exceptions correctly:

```csharp
public class ExceptionHandlingTests
{
    [Fact]
    public async Task RetryDecorator_RetriesOnTransientErrors()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var decorator = new RetryRepository(inner, maxRetries: 3);

        var attempts = 0;
        inner.GetByIdAsync(123).Returns(async _ =>
        {
            attempts++;
            if (attempts < 3)
                throw new TransientException("Database timeout");

            return new User { Id = 123 };
        });

        // Act
        var result = await decorator.GetByIdAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task CircuitBreakerDecorator_OpensAfterFailures()
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var decorator = new CircuitBreakerRepository(inner, threshold: 3);

        inner.GetByIdAsync(Arg.Any<int>()).Throws(new Exception("Service unavailable"));

        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<Exception>(() => decorator.GetByIdAsync(i));
        }

        // Circuit should be open now
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => decorator.GetByIdAsync(123));
    }
}
```

## Testing with AutoFixture

Generate test data with AutoFixture:

```csharp
using AutoFixture;
using AutoFixture.Xunit2;

public class UserRepositoryTests
{
    [Theory]
    [AutoData]
    public async Task SaveAsync_PersistsUser(User user)
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var decorator = new CachingRepository(inner, cache);

        // Act
        await decorator.SaveAsync(user);

        // Assert
        await inner.Received(1).SaveAsync(user);
    }

    [Theory]
    [InlineAutoData(1)]
    [InlineAutoData(2)]
    [InlineAutoData(3)]
    public async Task GetByIdAsync_WorksForMultipleIds(int id, User user)
    {
        // Arrange
        var inner = Substitute.For<IUserRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var decorator = new CachingRepository(inner, cache);

        user.Id = id;
        inner.GetByIdAsync(id).Returns(user);

        // Act
        var result = await decorator.GetByIdAsync(id);

        // Assert
        Assert.Equal(id, result.Id);
    }
}
```

## Snapshot Testing

Use snapshot testing for generated interceptor code:

```csharp
using VerifyXunit;

public class GeneratorTests
{
    [Fact]
    public async Task GeneratesCorrectInterceptor()
    {
        // Arrange
        var source = @"
            [DecoratedBy<LoggingRepository>]
            public class UserRepository : IUserRepository { }
        ";

        var generator = new SculptorGenerator();

        // Act
        var result = RunGenerator(source, generator);

        // Assert - verify generated code matches snapshot
        await Verifier.Verify(result.GeneratedTrees[0].ToString())
            .UseDirectory("Snapshots");
    }
}
```

## Performance Testing

Test decorator performance impact:

```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class DecoratorBenchmarks
{
    private IUserRepository _undecorated;
    private IUserRepository _withCaching;
    private IUserRepository _withLogging;
    private IUserRepository _withBoth;

    [GlobalSetup]
    public void Setup()
    {
        _undecorated = new UserRepository();
        _withCaching = new CachingRepository(_undecorated, new MemoryCache());
        _withLogging = new LoggingRepository(_undecorated, logger);
        _withBoth = new CachingRepository(
            new LoggingRepository(_undecorated, logger),
            new MemoryCache());
    }

    [Benchmark(Baseline = true)]
    public async Task Undecorated()
    {
        await _undecorated.GetByIdAsync(123);
    }

    [Benchmark]
    public async Task WithCaching()
    {
        await _withCaching.GetByIdAsync(123);
    }

    [Benchmark]
    public async Task WithLogging()
    {
        await _withLogging.GetByIdAsync(123);
    }

    [Benchmark]
    public async Task WithBoth()
    {
        await _withBoth.GetByIdAsync(123);
    }
}
```

## Test Helpers

Create test helpers for common scenarios:

```csharp
public static class TestHelpers
{
    public static IServiceProvider CreateServiceProvider(
        Action<IServiceCollection> configure = null)
    {
        var services = new ServiceCollection();

        // Default test services
        services.AddLogging(b => b.AddDebug());
        services.AddMemoryCache();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        var mock = new Mock<ILogger<T>>();

        mock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        return mock;
    }

    public static void VerifyLogged<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        string message)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

// Usage
[Fact]
public async Task UsesTestHelpers()
{
    var provider = TestHelpers.CreateServiceProvider(services =>
    {
        services.AddScoped<IUserRepository, UserRepository>();
    });

    var logger = TestHelpers.CreateLogger<LoggingRepository>();

    // Test...
}
```

## Best Practices

1. **Test decorators in isolation** with mocked inner implementations
2. **Test decorator chains** with integration tests
3. **Verify side effects** (logs, metrics, cache behavior)
4. **Test exception handling** in decorators
5. **Use in-memory providers** for database-dependent decorators
6. **Performance test** critical decorator chains
7. **Snapshot test** generated interceptor code
8. **Create test helpers** for common setup
9. **Mock external dependencies** (HTTP, message queues)
10. **Test with realistic data** using AutoFixture or similar tools

## Common Testing Patterns

### Arrange-Act-Assert

```csharp
[Fact]
public async Task FollowsAAA()
{
    // Arrange
    var inner = Substitute.For<IUserRepository>();
    var decorator = new CachingRepository(inner, new MemoryCache());

    // Act
    var result = await decorator.GetByIdAsync(123);

    // Assert
    Assert.NotNull(result);
}
```

### Builder Pattern for Tests

```csharp
public class UserRepositoryBuilder
{
    private IUserRepository _inner = Substitute.For<IUserRepository>();
    private IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private ILogger _logger = Substitute.For<ILogger>();

    public UserRepositoryBuilder WithInner(IUserRepository inner)
    {
        _inner = inner;
        return this;
    }

    public UserRepositoryBuilder WithCache(IMemoryCache cache)
    {
        _cache = cache;
        return this;
    }

    public IUserRepository Build()
    {
        return new CachingRepository(
            new LoggingRepository(_inner, _logger),
            _cache);
    }
}

// Usage
[Fact]
public async Task UsesBuilder()
{
    var repo = new UserRepositoryBuilder()
        .WithCache(myCache)
        .Build();

    // Test...
}
```

## Next Steps

- Learn about [Interceptors](interceptors.md)
- Understand [Source Generators](source-generators.md)
- See [Examples](../examples/index.md) with tests