# How It Works

DecoWeaver uses compile-time code generation to automatically apply the decorator pattern to your dependency injection registrations. This page explains the technical approach and pipeline.

## Overview

Traditional decorator registration requires manual factory functions that resolve dependencies and wrap them. DecoWeaver automates this entirely at build time using three key technologies:

1. **Incremental Source Generators** - Analyze your code during compilation
2. **C# Interceptors** - Rewrite DI registration calls at compile time
3. **Keyed Services** - Prevent circular dependencies when resolving decorators

## The DecoWeaver Pipeline

### Phase 1: Discovery

During compilation, DecoWeaver's source generator scans your code for:

**Decorated Implementations**:
```csharp
[DecoratedBy<LoggingRepository>]
public class UserRepository : IUserRepository { }
```

**DI Registrations**:
```csharp
services.AddScoped<IUserRepository, UserRepository>();
```

The generator uses two parallel discovery streams:
- **Generic Attribute Provider**: Finds `[DecoratedBy<T>]` attributes
- **Non-Generic Attribute Provider**: Finds `[DecoratedBy(typeof(T))]` attributes

### Phase 2: Analysis

For each decorated implementation, DecoWeaver:

1. **Validates** the decorator type implements the same interface
2. **Checks** that decorators accept the interface as a constructor parameter
3. **Orders** multiple decorators by their `Order` property
4. **Matches** DI registrations to decorated implementations

### Phase 3: Code Generation

DecoWeaver generates **interceptor methods** that rewrite your DI calls:

**Your Code**:
```csharp
services.AddScoped<IUserRepository, UserRepository>();
```

**Generated Interceptor**:
```csharp
file static class DecoWeaverInterceptors
{
    [InterceptsLocation(version: 1, data: "Program.cs|123|45")]
    public static IServiceCollection AddScoped_IUserRepository_UserRepository(
        this IServiceCollection services)
    {
        // Register undecorated implementation with a key
        services.AddKeyedScoped<IUserRepository, UserRepository>("...");

        // Register factory that applies decorators
        services.AddScoped<IUserRepository>(sp =>
        {
            var inner = sp.GetRequiredKeyedService<IUserRepository>("...");
            return new LoggingRepository(inner, sp.GetRequiredService<ILogger>());
        });

        return services;
    }
}
```

The `[InterceptsLocation]` attribute tells the C# compiler to redirect your original call to this generated method.

## Keyed Services Strategy

DecoWeaver uses .NET 8's keyed services feature to avoid circular dependencies:

```csharp
// 1. Register undecorated implementation with a unique key
services.AddKeyedScoped<IUserRepository, UserRepository>(
    "{IUserRepository}|{UserRepository}");

// 2. Register factory that resolves keyed service and wraps it
services.AddScoped<IUserRepository>(sp =>
{
    var inner = sp.GetRequiredKeyedService<IUserRepository>(
        "{IUserRepository}|{UserRepository}");
    return new LoggingRepository(inner, sp.GetRequiredService<ILogger>());
});
```

This prevents the container from trying to resolve `IUserRepository` recursively when constructing the decorator.

## Multiple Decorators

When you stack decorators, DecoWeaver applies them in order:

```csharp
[DecoratedBy<LoggingRepository>(Order = 1)]
[DecoratedBy<CachingRepository>(Order = 2)]
public class UserRepository : IUserRepository { }
```

**Generated Factory**:
```csharp
services.AddScoped<IUserRepository>(sp =>
{
    // Start with undecorated implementation
    var inner = sp.GetRequiredKeyedService<IUserRepository>("...");

    // Apply Order = 1 (innermost)
    inner = new LoggingRepository(inner, sp.GetRequiredService<ILogger>());

    // Apply Order = 2 (outermost)
    return new CachingRepository(inner, sp.GetRequiredService<ICache>());
});
```

The result: `CachingRepository` → `LoggingRepository` → `UserRepository`

## Generic Type Decoration

For closed generic registrations with open generic decorators, DecoWeaver generates interceptors that handle runtime type closing:

!!! warning "Registration Requirement"
    DecoWeaver requires **closed generic registrations** using the `AddScoped<TService, TImplementation>()` syntax. Open generic registrations using `AddScoped(typeof(...), typeof(...))` are **NOT supported**.

**Your Code**:
```csharp
[DecoratedBy<CachingRepository<>>]  // Open generic decorator
public class Repository<T> : IRepository<T> where T : class { }

// Register each closed generic type - required for DecoWeaver
services.AddScoped<IRepository<User>, Repository<User>>();
services.AddScoped<IRepository<Product>, Repository<Product>>();
```

**Generated Factory** (for each closed registration):
```csharp
// For IRepository<User> registration:
services.AddScoped<IRepository<User>>(sp =>
{
    // Get the undecorated implementation from keyed service
    var inner = sp.GetRequiredKeyedService<IRepository<User>>("...");

    // Close the open generic decorator at runtime: CachingRepository<> → CachingRepository<User>
    var decoratorType = typeof(CachingRepository<>).MakeGenericType(typeof(User));

    // Resolve decorator dependencies and construct
    var cache = sp.GetRequiredService<IMemoryCache>();
    return (IRepository<User>)Activator.CreateInstance(decoratorType, inner, cache);
});
```

Each closed generic registration gets its own interceptor with the decorator closed to the appropriate type at runtime.

## Incremental Generation

DecoWeaver uses incremental source generation for performance:

- **Syntax Predicates**: Fast checks without semantic analysis
- **Semantic Transformers**: Only run on candidate nodes
- **Caching**: Results cached between builds
- **Change Detection**: Only regenerates when relevant code changes

This ensures build performance stays fast even in large codebases.

## Compilation Safety

All generated code is validated by the C# compiler:

1. Interceptor methods must match the signature of the methods they intercept
2. Type safety is enforced at compile time
3. Missing dependencies cause build errors, not runtime failures

## Zero Runtime Overhead

Because decoration happens at compile time:

- No reflection at runtime
- No assembly scanning
- No registration overhead
- Factory functions are pre-compiled

The DI container sees only standard registrations with factory functions.

## Next Steps

- Understand the [Decorator Pattern](decorator-pattern.md) in the DI context
- Learn about [Ordering and Nesting](order-and-nesting.md) decorators
- Explore [Interceptors](../advanced/interceptors.md) in depth