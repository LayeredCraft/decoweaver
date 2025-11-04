# Order and Nesting

When you apply multiple decorators to a service, the order in which they wrap each other is critical. Sculptor gives you explicit control over decorator ordering.

## Understanding Decorator Order

Decorators wrap each other like layers of an onion:

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<MetricsDecorator>(Order = 3)]
public class UserRepository : IUserRepository { }
```

**Resulting wrapper chain**:
```
MetricsDecorator (outermost, Order = 3)
    → CachingDecorator (Order = 2)
        → LoggingDecorator (Order = 1)
            → UserRepository (innermost)
```

**Call flow**:
1. Request enters `MetricsDecorator`
2. Passes through `CachingDecorator`
3. Passes through `LoggingDecorator`
4. Reaches `UserRepository`
5. Response returns through the same chain in reverse

## The Order Property

The `Order` property on `[DecoratedBy]` controls decorator precedence:

```csharp
[DecoratedBy<DecoratorA>(Order = 1)]  // Applied first (closest to implementation)
[DecoratedBy<DecoratorB>(Order = 2)]  // Applied second
[DecoratedBy<DecoratorC>(Order = 3)]  // Applied third (outermost)
```

**Key rules**:
- Lower numbers are closer to the implementation
- Higher numbers are further from the implementation
- Default order is `0` if not specified
- Decorators with the same order are applied in attribute definition order

## Why Order Matters

Different orderings produce different behaviors. Consider logging and caching:

### Scenario 1: Logging Inside Caching

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
public class UserRepository : IUserRepository { }

// Chain: Cache → Logging → Repository
```

**Behavior**:
- Cache miss: Logs are written, database is queried
- Cache hit: Logs are NOT written, cache returns immediately
- **Use case**: Only log actual database operations

### Scenario 2: Caching Inside Logging

```csharp
[DecoratedBy<CachingDecorator>(Order = 1)]
[DecoratedBy<LoggingDecorator>(Order = 2)]
public class UserRepository : IUserRepository { }

// Chain: Logging → Cache → Repository
```

**Behavior**:
- Cache miss: Logs are written, database is queried
- Cache hit: Logs are written, cache returns
- **Use case**: Log all requests regardless of caching

## Common Ordering Patterns

### Logging → Caching → Retry → Service

```csharp
[DecoratedBy<RetryDecorator>(Order = 1)]      // Innermost
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<LoggingDecorator>(Order = 3)]    // Outermost
```

**Why this order?**
- Logging sees all requests (cache hits and misses)
- Caching avoids unnecessary retries
- Retries only happen on actual failures from the service

### Metrics → Circuit Breaker → Retry → Service

```csharp
[DecoratedBy<RetryDecorator>(Order = 1)]
[DecoratedBy<CircuitBreakerDecorator>(Order = 2)]
[DecoratedBy<MetricsDecorator>(Order = 3)]
```

**Why this order?**
- Metrics capture all attempts including circuit breaker trips
- Circuit breaker can prevent retry storms
- Retries only happen when circuit is closed

### Authorization → Validation → Logging → Service

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<ValidationDecorator>(Order = 2)]
[DecoratedBy<AuthorizationDecorator>(Order = 3)]
```

**Why this order?**
- Authorization checked first (fail fast)
- Validation happens only for authorized requests
- Logging records valid, authorized operations

## Visualizing Decorator Chains

### Example: Complete Observability Stack

```csharp
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<MetricsDecorator>(Order = 3)]
[DecoratedBy<TracingDecorator>(Order = 4)]
public class UserRepository : IUserRepository { }
```

**Request flow**:
```
1. TracingDecorator.GetByIdAsync(123)
   ↓ starts span
2. MetricsDecorator.GetByIdAsync(123)
   ↓ increments counter, starts timer
3. CachingDecorator.GetByIdAsync(123)
   ↓ cache miss
4. LoggingDecorator.GetByIdAsync(123)
   ↓ logs "Getting user 123"
5. UserRepository.GetByIdAsync(123)
   ↓ queries database
   ← returns User
6. LoggingDecorator
   ← logs "Retrieved user 123"
7. CachingDecorator
   ← stores in cache
8. MetricsDecorator
   ← records duration
9. TracingDecorator
   ← ends span
```

## Default Order Behavior

If you don't specify `Order`, it defaults to `0`:

```csharp
[DecoratedBy<DecoratorA>]           // Order = 0
[DecoratedBy<DecoratorB>(Order = 1)] // Order = 1
[DecoratedBy<DecoratorC>]           // Order = 0
```

When multiple decorators have the same order, they're applied in the order they appear:

```
DecoratorB (Order = 1, outermost)
    → DecoratorA (Order = 0, first defined)
        → DecoratorC (Order = 0, second defined)
            → Implementation
```

## Negative Order Values

You can use negative numbers for fine-grained control:

```csharp
[DecoratedBy<InfrastructureDecorator>(Order = -10)]  // Very inner
[DecoratedBy<BusinessLogicDecorator>(Order = 0)]     // Middle
[DecoratedBy<ApiDecorator>(Order = 10)]              // Very outer
```

## Open Generic Decorator Ordering

Order works the same with open generic decorators:

```csharp
[DecoratedBy<CachingRepository<>>(Order = 1)]
[DecoratedBy<LoggingRepository<>>(Order = 2)]
public class Repository<T> : IRepository<T> { }
```

All closed generic instances get the same decorator order:
- `IRepository<User>` → `Logging → Caching → Repository<User>`
- `IRepository<Product>` → `Logging → Caching → Repository<Product>`

## Testing Decorator Order

Verify your decorator chain with integration tests:

```csharp
[Fact]
public async Task Decorators_AppliedInCorrectOrder()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddScoped<IUserRepository, UserRepository>();
    var provider = services.BuildServiceProvider();

    // Act
    var repo = provider.GetRequiredService<IUserRepository>();

    // Assert
    repo.Should().BeOfType<MetricsDecorator>();
    var metrics = (MetricsDecorator)repo;
    metrics.Inner.Should().BeOfType<CachingDecorator>();
    var caching = (CachingDecorator)metrics.Inner;
    caching.Inner.Should().BeOfType<LoggingDecorator>();
    var logging = (LoggingDecorator)caching.Inner;
    logging.Inner.Should().BeOfType<UserRepository>();
}
```

## Best Practices

### Start with Core Concerns Inner

Place core business concerns closest to the implementation:

```csharp
[DecoratedBy<TransactionDecorator>(Order = 1)]     // Core concern
[DecoratedBy<CachingDecorator>(Order = 2)]         // Performance
[DecoratedBy<LoggingDecorator>(Order = 3)]         // Observability
```

### Place Short-Circuits Outer

Decorators that can return early should be outermost:

```csharp
[DecoratedBy<RetryDecorator>(Order = 1)]           // Inner
[DecoratedBy<CircuitBreakerDecorator>(Order = 2)]  // Can short-circuit
[DecoratedBy<CachingDecorator>(Order = 3)]         // Can short-circuit
```

### Group Related Decorators

Keep related cross-cutting concerns together:

```csharp
// Observability stack
[DecoratedBy<LoggingDecorator>(Order = 10)]
[DecoratedBy<MetricsDecorator>(Order = 11)]
[DecoratedBy<TracingDecorator>(Order = 12)]

// Resilience stack
[DecoratedBy<RetryDecorator>(Order = 1)]
[DecoratedBy<CircuitBreakerDecorator>(Order = 2)]
[DecoratedBy<TimeoutDecorator>(Order = 3)]
```

### Document Your Strategy

Add comments explaining why decorators are ordered this way:

```csharp
// Order: Auth → Validation → Caching → Logging → Implementation
// Rationale:
// - Auth first (fail fast on unauthorized)
// - Validation before caching (don't cache invalid requests)
// - Caching before logging (log actual work, not cache hits)
[DecoratedBy<LoggingDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]
[DecoratedBy<ValidationDecorator>(Order = 3)]
[DecoratedBy<AuthorizationDecorator>(Order = 4)]
public class UserRepository : IUserRepository { }
```

## Common Pitfalls

### Pitfall 1: Caching Outside Validation

```csharp
// ❌ Bad: Can cache invalid requests
[DecoratedBy<ValidationDecorator>(Order = 1)]
[DecoratedBy<CachingDecorator>(Order = 2)]

// ✅ Good: Only cache validated requests
[DecoratedBy<CachingDecorator>(Order = 1)]
[DecoratedBy<ValidationDecorator>(Order = 2)]
```

### Pitfall 2: Metrics Inside Circuit Breaker

```csharp
// ❌ Bad: Metrics don't capture circuit breaker trips
[DecoratedBy<CircuitBreakerDecorator>(Order = 1)]
[DecoratedBy<MetricsDecorator>(Order = 2)]

// ✅ Good: Metrics capture all requests
[DecoratedBy<MetricsDecorator>(Order = 1)]
[DecoratedBy<CircuitBreakerDecorator>(Order = 2)]
```

### Pitfall 3: Retry Outside Circuit Breaker

```csharp
// ❌ Bad: Retries hammer an open circuit
[DecoratedBy<CircuitBreakerDecorator>(Order = 1)]
[DecoratedBy<RetryDecorator>(Order = 2)]

// ✅ Good: Circuit breaker prevents retry storms
[DecoratedBy<RetryDecorator>(Order = 1)]
[DecoratedBy<CircuitBreakerDecorator>(Order = 2)]
```

## Next Steps

- See [Examples](../examples/index.md) of real-world decorator chains
- Learn about [Multiple Decorators](../usage/multiple-decorators.md) usage
- Explore [Testing Strategies](../advanced/testing.md) for decorated services