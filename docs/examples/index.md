# Examples

This section provides real-world examples of using Sculptor to apply the decorator pattern in different scenarios.

## Available Examples

### Caching

Learn how to add caching to your services with decorators:

- **[Memory Caching](caching.md#memory-caching)** - In-memory caching with `IMemoryCache`
- **[Distributed Caching](caching.md#distributed-caching)** - Redis/SQL Server caching
- **[Conditional Caching](caching.md#conditional-caching)** - Smart caching strategies
- **[Cache Invalidation](caching.md#cache-invalidation)** - Managing cache lifetime

[View Caching Examples →](caching.md)

### Logging

Add comprehensive logging to your services:

- **[Method Call Logging](logging.md#method-call-logging)** - Log method entry/exit
- **[Parameter Logging](logging.md#parameter-logging)** - Log parameters and results
- **[Structured Logging](logging.md#structured-logging)** - Use structured logging
- **[Performance Logging](logging.md#performance-logging)** - Log execution times

[View Logging Examples →](logging.md)

### Resilience

Build resilient services with retry and circuit breaker patterns:

- **[Retry Logic](resilience.md#retry-logic)** - Automatic retry with exponential backoff
- **[Circuit Breaker](resilience.md#circuit-breaker)** - Prevent cascading failures
- **[Timeout](resilience.md#timeout)** - Protect against hanging operations
- **[Fallback](resilience.md#fallback)** - Graceful degradation

[View Resilience Examples →](resilience.md)

### Validation

Validate inputs and business rules:

- **[Input Validation](validation.md#input-validation)** - Validate method parameters
- **[Business Rules](validation.md#business-rules)** - Apply business logic
- **[FluentValidation Integration](validation.md#fluentvalidation)** - Use FluentValidation
- **[Conditional Validation](validation.md#conditional-validation)** - Context-based validation

[View Validation Examples →](validation.md)

### Metrics and Telemetry

Monitor and measure your services:

- **[OpenTelemetry](metrics.md#opentelemetry)** - Distributed tracing with OpenTelemetry
- **[Custom Metrics](metrics.md#custom-metrics)** - Track custom metrics
- **[Performance Counters](metrics.md#performance-counters)** - Monitor performance
- **[Health Checks](metrics.md#health-checks)** - Service health monitoring

[View Metrics Examples →](metrics.md)

## Complete Example

Here's a complete example showing multiple decorators working together:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sculptor.Attributes;

// Service interface
public interface IOrderService
{
    Task<Order> GetOrderAsync(int orderId);
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
}

// Implementation
[DecoratedBy<ValidationDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<LoggingDecorator>(Order = 3)]
[DecoratedBy<MetricsDecorator>(Order = 4)]
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Order> GetOrderAsync(int orderId)
    {
        return await _repository.GetByIdAsync(orderId);
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items,
            Total = request.Items.Sum(i => i.Price * i.Quantity)
        };

        await _repository.SaveAsync(order);
        return order;
    }
}

// Validation decorator
public class ValidationDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly IValidator<CreateOrderRequest> _validator;

    public ValidationDecorator(
        IOrderService inner,
        IValidator<CreateOrderRequest> validator)
    {
        _inner = inner;
        _validator = validator;
    }

    public Task<Order> GetOrderAsync(int orderId)
    {
        if (orderId <= 0)
            throw new ArgumentException("Order ID must be positive", nameof(orderId));

        return _inner.GetOrderAsync(orderId);
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);
        return await _inner.CreateOrderAsync(request);
    }
}

// Caching decorator
public class CachingDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly IMemoryCache _cache;

    public CachingDecorator(IOrderService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Order> GetOrderAsync(int orderId)
    {
        var key = $"order:{orderId}";

        if (_cache.TryGetValue(key, out Order cached))
            return cached;

        var order = await _inner.GetOrderAsync(orderId);
        _cache.Set(key, order, TimeSpan.FromMinutes(5));
        return order;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = await _inner.CreateOrderAsync(request);
        _cache.Remove($"order:{order.Id}");
        return order;
    }
}

// Logging decorator
public class LoggingDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger<LoggingDecorator> _logger;

    public LoggingDecorator(IOrderService inner, ILogger<LoggingDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Order> GetOrderAsync(int orderId)
    {
        _logger.LogInformation("Getting order {OrderId}", orderId);

        try
        {
            var order = await _inner.GetOrderAsync(orderId);
            _logger.LogInformation("Retrieved order {OrderId}", orderId);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation(
            "Creating order for customer {CustomerId}",
            request.CustomerId);

        try
        {
            var order = await _inner.CreateOrderAsync(request);
            _logger.LogInformation("Created order {OrderId}", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating order for customer {CustomerId}",
                request.CustomerId);
            throw;
        }
    }
}

// Metrics decorator
public class MetricsDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly IMeterFactory _meterFactory;
    private readonly Counter<long> _getCounter;
    private readonly Counter<long> _createCounter;
    private readonly Histogram<double> _duration;

    public MetricsDecorator(IOrderService inner, IMeterFactory meterFactory)
    {
        _inner = inner;
        _meterFactory = meterFactory;

        var meter = meterFactory.Create("OrderService");
        _getCounter = meter.CreateCounter<long>("orders.get.count");
        _createCounter = meter.CreateCounter<long>("orders.create.count");
        _duration = meter.CreateHistogram<double>("orders.duration");
    }

    public async Task<Order> GetOrderAsync(int orderId)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var order = await _inner.GetOrderAsync(orderId);
            _getCounter.Add(1, new TagList { { "status", "success" } });
            return order;
        }
        catch
        {
            _getCounter.Add(1, new TagList { { "status", "error" } });
            throw;
        }
        finally
        {
            _duration.Record(
                sw.Elapsed.TotalMilliseconds,
                new TagList { { "method", "get" } });
        }
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var order = await _inner.CreateOrderAsync(request);
            _createCounter.Add(1, new TagList { { "status", "success" } });
            return order;
        }
        catch
        {
            _createCounter.Add(1, new TagList { { "status", "error" } });
            throw;
        }
        finally
        {
            _duration.Record(
                sw.Elapsed.TotalMilliseconds,
                new TagList { { "method", "create" } });
        }
    }
}

// Registration
var services = new ServiceCollection();

// Register dependencies
services.AddLogging();
services.AddMemoryCache();
services.AddMetrics();
services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
services.AddScoped<IOrderRepository, OrderRepository>();

// Register service - decorators automatically applied
services.AddScoped<IOrderService, OrderService>();

// Build and use
var provider = services.BuildServiceProvider();
var orderService = provider.GetRequiredService<IOrderService>();

// This request goes through: Metrics → Logging → Caching → Validation → OrderService
var order = await orderService.GetOrderAsync(123);
```

## Example Structure

Each example follows a consistent structure:

1. **Problem** - What problem the decorator solves
2. **Solution** - How the decorator is implemented
3. **Usage** - How to apply the decorator
4. **Complete Code** - Full working example
5. **Variations** - Alternative approaches or extensions

## Testing Examples

Most examples include test examples showing how to:

- Test decorators in isolation
- Test the full decorator chain
- Mock dependencies
- Verify decorator behavior

## Running Examples

All examples are runnable with minimal setup:

1. Create a new console application
2. Install required NuGet packages
3. Copy the example code
4. Run with `dotnet run`

## Contributing Examples

Have a great Sculptor example? We'd love to include it! See the [Contributing Guide](../contributing.md) for how to submit examples.

## Next Steps

- Start with [Logging Examples](logging.md) for basic decorator patterns
- Explore [Caching Examples](caching.md) for performance optimization
- Learn [Resilience Patterns](resilience.md) for production-ready services