# Logging Examples

Logging decorators capture method calls, parameters, results, and exceptions, providing observability into your services.

## Method Call Logging

Log method entry and exit with execution time:

```csharp
public class LoggingUserService : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<LoggingUserService> _logger;

    public LoggingUserService(IUserService inner, ILogger<LoggingUserService> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("Entering GetByIdAsync with id={Id}", id);

        try
        {
            var result = await _inner.GetByIdAsync(id);

            _logger.LogInformation(
                "Exiting GetByIdAsync with id={Id}, elapsed={ElapsedMs}ms",
                id,
                sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in GetByIdAsync with id={Id}, elapsed={ElapsedMs}ms",
                id,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}

[DecoratedBy<LoggingUserService>]
public class UserService : IUserService
{
    // Implementation
}
```

**Output**:
```
[12:34:56 INF] Entering GetByIdAsync with id=123
[12:34:56 INF] Exiting GetByIdAsync with id=123, elapsed=45ms
```

## Parameter Logging

Log method parameters and return values:

```csharp
public class DetailedLoggingService : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<DetailedLoggingService> _logger;

    public DetailedLoggingService(IUserService inner, ILogger<DetailedLoggingService> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _logger.LogInformation(
            "UpdateAsync called with user: {@User}",
            new { user.Id, user.Email, user.Name });

        var result = await _inner.UpdateAsync(user);

        _logger.LogInformation(
            "UpdateAsync returned: {@User}",
            new { result.Id, result.Email, result.Name, result.LastModified });

        return result;
    }
}

[DecoratedBy<DetailedLoggingService>]
public class UserService : IUserService
{
    // Implementation
}
```

**Output**:
```
[12:34:56 INF] UpdateAsync called with user: {"Id": 123, "Email": "user@example.com", "Name": "John"}
[12:34:56 INF] UpdateAsync returned: {"Id": 123, "Email": "user@example.com", "Name": "John", "LastModified": "2025-01-15T12:34:56Z"}
```

## Structured Logging

Use structured logging with correlation IDs:

```csharp
public class StructuredLoggingService : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<StructuredLoggingService> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public StructuredLoggingService(
        IUserService inner,
        ILogger<StructuredLoggingService> logger,
        IHttpContextAccessor httpContext)
    {
        _inner = inner;
        _logger = logger;
        _httpContext = httpContext;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var correlationId = _httpContext.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Method"] = nameof(GetByIdAsync),
            ["UserId"] = id
        }))
        {
            _logger.LogInformation("Fetching user from service");

            try
            {
                var user = await _inner.GetByIdAsync(id);

                _logger.LogInformation(
                    "User retrieved successfully: {UserEmail}",
                    user.Email);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user");
                throw;
            }
        }
    }
}

[DecoratedBy<StructuredLoggingService>]
public class UserService : IUserService
{
    // Implementation
}
```

**Output**:
```json
{
  "Timestamp": "2025-01-15T12:34:56Z",
  "Level": "Information",
  "Message": "Fetching user from service",
  "CorrelationId": "0HMQ8Q5F9V7H2",
  "Method": "GetByIdAsync",
  "UserId": 123
}
```

## Performance Logging

Log slow operations:

```csharp
public class PerformanceLoggingService : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<PerformanceLoggingService> _logger;
    private readonly IOptions<PerformanceOptions> _options;

    public PerformanceLoggingService(
        IUserService inner,
        ILogger<PerformanceLoggingService> logger,
        IOptions<PerformanceOptions> options)
    {
        _inner = inner;
        _logger = logger;
        _options = options;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            return await _inner.GetByIdAsync(id);
        }
        finally
        {
            sw.Stop();

            if (sw.ElapsedMilliseconds > _options.Value.SlowOperationThresholdMs)
            {
                _logger.LogWarning(
                    "SLOW OPERATION: GetByIdAsync took {ElapsedMs}ms (threshold: {ThresholdMs}ms) for id={Id}",
                    sw.ElapsedMilliseconds,
                    _options.Value.SlowOperationThresholdMs,
                    id);
            }
            else
            {
                _logger.LogDebug(
                    "GetByIdAsync completed in {ElapsedMs}ms for id={Id}",
                    sw.ElapsedMilliseconds,
                    id);
            }
        }
    }
}

public class PerformanceOptions
{
    public int SlowOperationThresholdMs { get; set; } = 1000;
}

[DecoratedBy<PerformanceLoggingService>]
public class UserService : IUserService
{
    // Implementation
}

// Configuration
services.Configure<PerformanceOptions>(config.GetSection("Performance"));
services.AddScoped<IUserService, UserService>();
```

## Generic Logging Decorator

Reusable logging decorator for any service:

```csharp
public class LoggingDecorator<TService> where TService : class
{
    private readonly TService _inner;
    private readonly ILogger _logger;

    public LoggingDecorator(TService inner, ILogger<LoggingDecorator<TService>> logger)
    {
        _inner = inner;
        _logger = logger;

        // Create proxy at runtime
        var proxyGenerator = new ProxyGenerator();
        var interceptor = new LoggingInterceptor<TService>(_inner, _logger);

        Proxy = (TService)proxyGenerator.CreateInterfaceProxyWithTarget(
            typeof(TService),
            _inner,
            interceptor);
    }

    public TService Proxy { get; }
}

public class LoggingInterceptor<TService> : IInterceptor
{
    private readonly TService _target;
    private readonly ILogger _logger;

    public LoggingInterceptor(TService target, ILogger logger)
    {
        _target = target;
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var methodName = invocation.Method.Name;
        var parameters = string.Join(", ", invocation.Arguments.Select(a => a?.ToString() ?? "null"));

        _logger.LogInformation(
            "{Service}.{Method}({Parameters})",
            typeof(TService).Name,
            methodName,
            parameters);

        try
        {
            invocation.Proceed();

            if (invocation.ReturnValue != null)
            {
                _logger.LogInformation(
                    "{Service}.{Method} returned: {ReturnValue}",
                    typeof(TService).Name,
                    methodName,
                    invocation.ReturnValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{Service}.{Method} threw exception",
                typeof(TService).Name,
                methodName);
            throw;
        }
    }
}

// Can be applied to any service
[DecoratedBy<LoggingDecorator<IUserService>>]
public class UserService : IUserService { }

[DecoratedBy<LoggingDecorator<IOrderService>>]
public class OrderService : IOrderService { }
```

## Conditional Logging

Log based on configuration or environment:

```csharp
public class ConditionalLoggingService : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<ConditionalLoggingService> _logger;
    private readonly IHostEnvironment _environment;

    public ConditionalLoggingService(
        IUserService inner,
        ILogger<ConditionalLoggingService> logger,
        IHostEnvironment environment)
    {
        _inner = inner;
        _logger = logger;
        _environment = environment;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        // Only log in development
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("GetByIdAsync called with id={Id}", id);
        }

        var result = await _inner.GetByIdAsync(id);

        // Always log in production if user is admin
        if (_environment.IsProduction() && result.IsAdmin)
        {
            _logger.LogWarning("Admin user accessed: {Email}", result.Email);
        }

        return result;
    }
}

[DecoratedBy<ConditionalLoggingService>]
public class UserService : IUserService
{
    // Implementation
}
```

## Sensitive Data Filtering

Prevent logging sensitive information:

```csharp
public class SafeLoggingService : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<SafeLoggingService> _logger;

    public SafeLoggingService(IUserService inner, ILogger<SafeLoggingService> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> AuthenticateAsync(string email, string password)
    {
        // Never log passwords
        _logger.LogInformation("AuthenticateAsync called for email={Email}", email);

        try
        {
            var user = await _inner.AuthenticateAsync(email, password);

            // Log safe properties only
            _logger.LogInformation(
                "User authenticated: {UserId}, {Email}",
                user.Id,
                user.Email);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Authentication failed for email={Email}",
                email);
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        // Create safe version for logging
        var safeUser = new
        {
            user.Id,
            user.Email,
            user.Name,
            PasswordHash = "[REDACTED]",
            user.LastModified
        };

        _logger.LogInformation("Updating user: {@User}", safeUser);

        var result = await _inner.UpdateAsync(user);

        _logger.LogInformation("User updated successfully: {UserId}", result.Id);

        return result;
    }
}

[DecoratedBy<SafeLoggingService>]
public class UserService : IUserService
{
    // Implementation
}
```

## Testing

Test logging decorators:

```csharp
public class LoggingServiceTests
{
    [Fact]
    public async Task LogsMethodEntry()
    {
        // Arrange
        var inner = Substitute.For<IUserService>();
        var logger = Substitute.For<ILogger<LoggingUserService>>();
        var decorator = new LoggingUserService(inner, logger);

        inner.GetByIdAsync(123).Returns(new User { Id = 123 });

        // Act
        await decorator.GetByIdAsync(123);

        // Assert
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Entering GetByIdAsync")),
            null,
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task LogsExceptions()
    {
        // Arrange
        var inner = Substitute.For<IUserService>();
        var logger = Substitute.For<ILogger<LoggingUserService>>();
        var decorator = new LoggingUserService(inner, logger);

        inner.GetByIdAsync(123).Throws(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => decorator.GetByIdAsync(123));

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }
}
```

## Best Practices

1. **Use structured logging** with named parameters
2. **Log at appropriate levels** (Debug, Information, Warning, Error)
3. **Never log sensitive data** (passwords, tokens, PII)
4. **Use log scopes** for correlation
5. **Consider performance** - don't log too verbosely in production
6. **Use semantic logging** - log why, not just what
7. **Include correlation IDs** for distributed tracing

## Next Steps

- Explore [Caching Examples](caching.md)
- Learn about [Metrics and Telemetry](metrics.md)
- See [Complete Example](index.md#complete-example) with multiple decorators