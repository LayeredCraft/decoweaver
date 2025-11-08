# Opt-Out Mechanisms

Sometimes you need to exclude decorators from certain implementations. DecoWeaver provides two opt-out mechanisms:

- **`[SkipAssemblyDecoration]`** - Opts out of ALL assembly-level decorators
- **`[DoNotDecorate(typeof(...))]`** - Surgically removes specific decorators

## SkipAssemblyDecoration Attribute

Use `[SkipAssemblyDecoration]` to opt out of **all** assembly-level decorators while keeping class-level decorators:

```csharp
// GlobalUsings.cs - Apply multiple decorators to all repositories
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>))]

// UserRepository.cs - Gets all three assembly-level decorators
public class UserRepository : IRepository<User> { }

// OrderRepository.cs - Completely opts out of assembly decorators
[SkipAssemblyDecoration]
public class OrderRepository : IRepository<Order> { }

// ProductRepository.cs - Opts out of assembly, adds class-level
[SkipAssemblyDecoration]
[DecoratedBy<ValidationRepository<Product>>]
public class ProductRepository : IRepository<Product> { }
```

**Result**:
- `UserRepository`: Logging → Caching → Metrics (all assembly-level)
- `OrderRepository`: No decorators at all
- `ProductRepository`: Only ValidationRepository (class-level only)

### When to Use SkipAssemblyDecoration

**Use this when:**
- The implementation needs to completely bypass all assembly-level decorators
- You want a "clean slate" to apply only specific class-level decorators
- The implementation has unique requirements incompatible with standard decorators
- Performance-critical code that should have zero decorator overhead

**Example - Performance Critical**:
```csharp
[assembly: DecorateService(typeof(IService<>), typeof(LoggingService<>))]
[assembly: DecorateService(typeof(IService<>), typeof(MetricsService<>))]

// High-throughput service - skip all observability
[SkipAssemblyDecoration]
public class HighThroughputService : IService<Data> { }
```

## DoNotDecorate Attribute

Sometimes you need to exclude decorators from certain implementations. DecoWeaver provides the `[DoNotDecorate]` attribute for fine-grained control over decorator application.

## Basic Syntax

Use `[DoNotDecorate(typeof(...))]` on an implementation class to exclude a specific decorator:

```csharp
[DoNotDecorate(typeof(CachingDecorator))]
public class UserRepository : IUserRepository
{
    // This implementation won't be cached
}
```

## When to Opt Out

### Excluding Assembly-Level Decorators

The primary use case is opting out of assembly-level decorators:

```csharp
// GlobalUsings.cs - Apply caching to all repositories
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

// UserRepository.cs - Gets caching
public class UserRepository : IRepository<User> { }

// OrderRepository.cs - Opts out of caching
[DoNotDecorate(typeof(CachingRepository<>))]
public class OrderRepository : IRepository<Order> { }
```

**Result**:
- `UserRepository`: Decorated with `CachingRepository<User>`
- `OrderRepository`: No decoration applied

### Implementation-Specific Requirements

Some implementations have unique constraints:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(TransactionRepository<>))]

// Most repositories need transactions
public class UserRepository : IRepository<User> { }

// But this one manages its own transactions
[DoNotDecorate(typeof(TransactionRepository<>))]
public class LegacyRepository : IRepository<Legacy>
{
    // Custom transaction management
}
```

### Performance-Critical Code

Opt out of observability decorators for hot paths:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IService<>), typeof(LoggingService<>))]
[assembly: DecorateService(typeof(IService<>), typeof(MetricsService<>))]

// Normal service - gets logging and metrics
public class UserService : IService<User> { }

// Performance-critical - minimal overhead
[DoNotDecorate(typeof(LoggingService<>))]
[DoNotDecorate(typeof(MetricsService<>))]
public class HighThroughputService : IService<Data> { }
```

### Testing Implementations

Opt out of decorators in test implementations:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

// Production implementation - gets caching
public class ProductionRepository : IRepository<User> { }

// Test implementation - no caching needed
[DoNotDecorate(typeof(CachingRepository<>))]
public class InMemoryRepository : IRepository<User> { }
```

## Multiple Opt-Outs

Apply multiple `[DoNotDecorate]` attributes to exclude multiple decorators:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>))]

// Opt out of caching and metrics, keep logging
[DoNotDecorate(typeof(CachingRepository<>))]
[DoNotDecorate(typeof(MetricsRepository<>))]
public class OrderRepository : IRepository<Order>
{
    // Gets LoggingRepository<Order> only
}
```

## Open Generic Matching

`[DoNotDecorate]` works with open generic types and matches all closed variants:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

// Opt out using open generic type
[DoNotDecorate(typeof(CachingRepository<>))]
public class UserRepository : IRepository<User> { }
```

Even though the assembly decorator will try to apply `CachingRepository<User>` (closed generic), the opt-out with `CachingRepository<>` (open generic) matches and excludes it.

**Type Matching Rules**:
- Open generic `[DoNotDecorate(typeof(Decorator<>))]` matches all closed generics
- Closed generic `[DoNotDecorate(typeof(Decorator<User>))]` matches only that specific closed type
- Non-generic types match exactly

## Combining with Class-Level Decorators

`[DoNotDecorate]` can also remove class-level decorators, though this is less common:

```csharp
// Base class with decorator
public abstract class BaseRepository<T> : IRepository<T>
{
    // ...
}

// Derived class opts out (though you could just not inherit the attribute)
[DoNotDecorate(typeof(CachingRepository<>))]
public class UserRepository : BaseRepository<User> { }
```

!!! note "Attribute Inheritance"
    `[DecoratedBy]` attributes are **not inherited**, so this scenario is rare. You'd typically only use `[DoNotDecorate]` to remove assembly-level decorators.

## Isolation Behavior

`[DoNotDecorate]` only affects the **specific implementation** it's applied to:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

// OrderRepository opts out
[DoNotDecorate(typeof(CachingRepository<>))]
public class OrderRepository : IRepository<Order> { }

// UserRepository still gets caching (not affected)
public class UserRepository : IRepository<User> { }
```

Each implementation is evaluated independently.

## How It Works

At compile time, DecoWeaver:

1. **Discovers** all `[DoNotDecorate]` attributes
2. **Collects** decorators from both class-level and assembly-level
3. **Filters out** decorators matching the `DoNotDecorate` directives
4. **Generates** interceptor code with only the remaining decorators

This happens at build time, so there's no runtime overhead.

## Common Patterns

### Selective Caching

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

// Read-heavy repositories get caching
public class ProductRepository : IRepository<Product> { }
public class CategoryRepository : IRepository<Category> { }

// Write-heavy repositories opt out
[DoNotDecorate(typeof(CachingRepository<>))]
public class OrderRepository : IRepository<Order> { }

[DoNotDecorate(typeof(CachingRepository<>))]
public class InventoryRepository : IRepository<Inventory> { }
```

### Environment-Specific Decorators

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IService<>), typeof(DetailedLoggingService<>))]

// Most services get detailed logging in dev
public class UserService : IService<User> { }

// But this one is too noisy
[DoNotDecorate(typeof(DetailedLoggingService<>))]
public class ChatService : IService<Message> { }
```

### Gradual Migration

When migrating to assembly-level decorators:

```csharp
// GlobalUsings.cs - New assembly-level decorator
[assembly: DecorateService(typeof(IRepository<>), typeof(NewCachingRepository<>))]

// New implementations use the new decorator
public class UserRepository : IRepository<User> { }

// Legacy implementations opt out (still using old approach)
[DoNotDecorate(typeof(NewCachingRepository<>))]
public class LegacyRepository : IRepository<Legacy>
{
    // Still using old caching mechanism
}
```

### Override Assembly Decisions

Opt out and apply a different decorator:

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(RedisCachingRepository<>))]

// Most repositories use Redis caching
public class UserRepository : IRepository<User> { }

// This one uses in-memory caching instead
[DoNotDecorate(typeof(RedisCachingRepository<>))]
[DecoratedBy<MemoryCachingRepository<Product>>]
public class ProductRepository : IRepository<Product> { }
```

## Troubleshooting

### Opt-Out Not Working

If `[DoNotDecorate]` isn't removing the decorator:

1. **Verify exact type match**: Type must exactly match the decorator being applied
2. **Check generic arity**: `CachingRepository<>` vs `CachingRepository<User>`
3. **Rebuild**: Opt-out changes require full rebuild
4. **Check assembly-level decorators**: Review what's actually being applied

### Type Name Confusion

```csharp
// ❌ Wrong: String name doesn't work
[DoNotDecorate("CachingRepository")]

// ✅ Correct: Use typeof
[DoNotDecorate(typeof(CachingRepository<>))]
```

### Namespace Mismatch

```csharp
// GlobalUsings.cs
[assembly: DecorateService(typeof(IRepository<>), typeof(Company.Infrastructure.CachingRepository<>))]

// ❌ Wrong: Namespace mismatch
[DoNotDecorate(typeof(CachingRepository<>))]

// ✅ Correct: Full namespace
[DoNotDecorate(typeof(Company.Infrastructure.CachingRepository<>))]
```

Use fully qualified type names when necessary.

## Best Practices

1. **Use sparingly** - If many implementations opt out, reconsider the assembly-level decorator
2. **Document why** - Add comments explaining the opt-out reason
3. **Prefer assembly-level for most cases** - Only opt out when truly necessary
4. **Consider alternatives** - Sometimes a different interface or implementation pattern is better
5. **Group opt-outs** - If several implementations opt out of the same decorator, consider a marker interface

## Anti-Patterns

### Over-Using Opt-Out

```csharp
// ❌ Bad: Most implementations opt out
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]

[DoNotDecorate(typeof(CachingRepository<>))]
public class Repo1 : IRepository<T1> { }

[DoNotDecorate(typeof(CachingRepository<>))]
public class Repo2 : IRepository<T2> { }

// Only one gets caching
public class Repo3 : IRepository<T3> { }
```

**Better approach**: Remove assembly-level decorator, use class-level where needed.

### Opt-Out as Default

```csharp
// ❌ Bad: Using opt-out to avoid applying decorators
[DoNotDecorate(typeof(Decorator1))]
[DoNotDecorate(typeof(Decorator2))]
[DoNotDecorate(typeof(Decorator3))]
public class CleanRepository : IRepository<T> { }
```

**Better approach**: Don't apply assembly-level decorators if most implementations don't need them.

## Choosing Between Opt-Out Mechanisms

| Attribute | Scope | Use Case |
|-----------|-------|----------|
| **`[SkipAssemblyDecoration]`** | Removes ALL assembly decorators | Clean slate, performance critical, completely different decoration strategy |
| **`[DoNotDecorate(typeof(...))]`** | Removes specific decorator(s) | Opt out of 1-2 decorators while keeping others |

### Decision Tree

```
Need to opt out?
├─ Remove ALL assembly decorators?
│  └─ Use [SkipAssemblyDecoration]
│
└─ Remove specific decorator(s)?
   ├─ Remove 1-3 decorators? → Use [DoNotDecorate]
   ├─ Remove most decorators? → Use [SkipAssemblyDecoration] + class-level
   └─ Complex mix? → Reconsider assembly-level approach
```

### Examples

**Scenario 1: Completely Different Decoration**
```csharp
// Most services get standard observability
[assembly: DecorateService(typeof(IService<>), typeof(LoggingService<>))]
[assembly: DecorateService(typeof(IService<>), typeof(MetricsService<>))]

// This service has custom observability
[SkipAssemblyDecoration]
[DecoratedBy<CustomTracingService>]
public class SpecialService : IService<Data> { }
```

**Scenario 2: Opt Out of One Decorator**
```csharp
[assembly: DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>))]

// Keep logging and metrics, skip caching
[DoNotDecorate(typeof(CachingRepository<>))]
public class OrderRepository : IRepository<Order> { }
```

## Comparison: Class-Level vs Assembly-Level

| Approach | When to Use |
|----------|-------------|
| **No assembly decorator** | Decorator applies to few implementations |
| **Assembly decorator** | Decorator applies to most implementations |
| **Assembly + SkipAssemblyDecoration** | Most use assembly, few need clean slate |
| **Assembly + DoNotDecorate** | Most use assembly, some need surgical exclusions |
| **Class-level only** | Implementation-specific decorators |

## See Also

- [Assembly-Level Decorators](assembly-level-decorators.md) - Understanding assembly-level decoration
- [Class-Level Decorators](class-level-decorators.md) - Applying decorators to individual classes
- [Multiple Decorators](multiple-decorators.md) - Managing decorator chains
- [API Reference](../api-reference/attributes.md) - Complete attribute documentation
