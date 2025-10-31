# LayeredCraft.Sculptor

**Compile-time decorator registration for .NET dependency injection**

[![NuGet](https://img.shields.io/nuget/v/Sculptor.svg)](https://www.nuget.org/packages/Sculptor/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/layered-craft/sculptor/build.yml?branch=main)](https://github.com/layered-craft/sculptor/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Overview

Sculptor is a .NET incremental source generator that brings **compile-time decorator registration** to your dependency injection setup. Using C# 11+ interceptors, it automatically wraps your service implementations with decorators at build time—eliminating runtime reflection, assembly scanning, and manual factory wiring.

Unlike runtime approaches (like Scrutor), Sculptor analyzes your code during compilation and generates optimized interceptor code that rewrites DI registrations. This results in:
- **Zero runtime overhead** - No reflection or assembly scanning at startup
- **Type-safe decorator chains** - Catches configuration errors at compile time
- **Open generic support** - Decorate `IRepository<T>` implementations seamlessly
- **Clear service composition** - Generated code is inspectable and debuggable

## Key Features

- **Attribute-driven decoration**: Simply mark your implementations with `[DecoratedBy<TDecorator>]`
- **Open generic decorators**: Support for `IRepository<T>`, `ICommand<T>`, and other generic patterns
- **Ordered decorator chains**: Control decorator application order with the `Order` property
- **Assembly-level decoration**: Apply decorators to all implementations of a service with `[DecorateService]`
- **Keyed services**: Leverages .NET 8+ keyed services to prevent circular dependencies
- **Incremental generation**: Fast rebuilds with Roslyn's incremental generator infrastructure

## Installation

Install the NuGet package:

```bash
dotnet add package Sculptor
```

Ensure your project uses C# 11 or later:

```xml
<PropertyGroup>
  <LangVersion>11</LangVersion>
  <!-- or <LangVersion>latest</LangVersion> -->
</PropertyGroup>
```

## Quick Start

### 1. Define your service and decorator

```csharp
using Sculptor.Attributes;

public interface IRepository<T>
{
    Task<T> GetByIdAsync(int id);
}

// Mark the implementation with the decorator to apply
[DecoratedBy(typeof(CachingRepository<>))]
public class DynamoDbRepository<T> : IRepository<T>
{
    public Task<T> GetByIdAsync(int id)
    {
        // Real implementation
    }
}

// The decorator wraps the inner implementation
public class CachingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;

    public CachingRepository(IRepository<T> inner)
    {
        _inner = inner;
    }

    public async Task<T> GetByIdAsync(int id)
    {
        // Check cache first, then delegate to _inner
    }
}
```

### 2. Register as usual

```csharp
var services = new ServiceCollection();

// Standard open generic registration
services.AddScoped(typeof(IRepository<>), typeof(DynamoDbRepository<>));

var provider = services.BuildServiceProvider();
```

### 3. Magic happens at compile time

Sculptor generates an interceptor that rewrites the `AddScoped` call to:
1. Register `DynamoDbRepository<>` as a keyed service
2. Register a factory that resolves the keyed service and wraps it with `CachingRepository<>`

When you resolve `IRepository<User>`, you automatically get:
```
CachingRepository<User>
  └─ DynamoDbRepository<User>
```

## Usage

### Basic Decorator (Generic Attribute)

Use the generic attribute when the decorator type is known at compile time:

```csharp
[DecoratedBy<LoggingDecorator>]
public class UserService : IUserService
{
    // Implementation
}
```

### Open Generic Decorators

Use `typeof()` for open generic decorators:

```csharp
[DecoratedBy(typeof(CachingRepository<>))]
[DecoratedBy(typeof(LoggingRepository<>), order: 1)]
public class SqlRepository<T> : IRepository<T>
{
    // Implementation
}
```

The `order` parameter controls application order (lower numbers are applied first/innermost).

### Multiple Decorators

Stack multiple decorators with different orders:

```csharp
[DecoratedBy(typeof(CachingRepository<>), order: 0)]
[DecoratedBy(typeof(LoggingRepository<>), order: 1)]
[DecoratedBy(typeof(RetryRepository<>), order: 2)]
public class HttpRepository<T> : IRepository<T>
{
    // Implementation
}
```

Results in:
```
RetryRepository<T>          (outermost - order: 2)
  └─ LoggingRepository<T>    (middle - order: 1)
      └─ CachingRepository<T> (innermost - order: 0)
          └─ HttpRepository<T> (implementation)
```

### Assembly-Level Decoration

Apply a decorator to all implementations of a service across an assembly:

```csharp
[assembly: DecorateService(typeof(IRepository<>), typeof(MetricsRepository<>), order: 100)]

namespace MyApp;

// This automatically gets MetricsRepository<> applied
public class UserRepository : IRepository<User> { }

// This too
public class ProductRepository : IRepository<Product> { }
```

### Controlling Decorator Order

The `Order` property determines decorator nesting (ascending order):

```csharp
[DecoratedBy<ValidationDecorator>(Order = 0)]  // Applied first (innermost)
[DecoratedBy<CachingDecorator>(Order = 50)]    // Applied second
[DecoratedBy<LoggingDecorator>(Order = 100)]   // Applied last (outermost)
public class OrderService : IOrderService { }
```

## How It Works

### The Interceptor Mechanism

1. **Discovery**: During compilation, Sculptor scans for:
   - Classes marked with `[DecoratedBy<T>]` or `[DecoratedBy(typeof(...))]`
   - Assembly-level `[DecorateService]` attributes
   - DI registration calls like `AddScoped(typeof(IRepo<>), typeof(Impl<>))`

2. **Code Generation**: Generates an interceptor file (`Sculptor.Interceptors.OpenGenerics.g.cs`) with:
   - Methods matching the signature of `ServiceCollectionServiceExtensions.AddScoped/Transient/Singleton`
   - `[InterceptsLocation]` attributes that redirect specific call sites to the generated methods

3. **Runtime Behavior**: The interceptor method:
   - Registers the undecorated implementation as a keyed service
   - Registers a factory that resolves the keyed implementation and wraps it with decorators
   - Decorators are applied in ascending order by `Order` property

### Generated Code Example

For the quick start example above, Sculptor generates something like:

```csharp
[InterceptsLocation(version: 1, data: "Program.cs|245|67")]
internal static IServiceCollection AddScoped(
    IServiceCollection services,
    Type serviceType,
    Type implementationType)
{
    if (implementationType == typeof(DynamoDbRepository<>))
    {
        var key = $"{serviceType.AssemblyQualifiedName}|{implementationType.AssemblyQualifiedName}";

        // Register undecorated implementation with key
        services.AddKeyedScoped(serviceType, key, implementationType);

        // Register factory that applies decorators
        services.AddScoped(serviceType, sp =>
        {
            var inner = sp.GetRequiredKeyedService(serviceType, key);
            var decorated = ActivatorUtilities.CreateInstance(sp,
                typeof(CachingRepository<>).MakeGenericType(serviceType.GetGenericArguments()),
                inner);
            return decorated;
        });

        return services;
    }

    // Fallback to original method for unknown types
    return ServiceCollectionServiceExtensions.AddScoped(services, serviceType, implementationType);
}
```

## Comparison with Scrutor

Sculptor and [Scrutor](https://github.com/khellang/Scrutor) both enable decorator patterns in .NET DI, but with different approaches:

| Aspect | Sculptor | Scrutor |
|--------|----------|---------|
| **Execution** | Compile-time code generation | Runtime reflection & scanning |
| **Performance** | Zero startup overhead | Reflection cost at startup |
| **Open Generics** | Full support via interceptors | Full support via runtime wrapping |
| **Type Safety** | Compile-time validation | Runtime validation |
| **Inspection** | Generated code is visible | Runtime-only registration |
| **Requirements** | C# 11+, .NET 8+ | .NET Standard 2.0+ |

Choose Sculptor when you want compile-time safety and zero reflection overhead. Choose Scrutor for broader compatibility or dynamic scenarios.

## Requirements

- **C# Language Version**: 11 or later (interceptors are a C# 11+ feature)
- **.NET Runtime**: .NET 8.0 or later (requires keyed services support)
- **.NET SDK Version**: 8.0.400 or later (ships with Visual Studio 2022 17.11+)
- **Target Framework**: netstandard2.0 or later (for the attributes package)
- **IDE Support**: Visual Studio 2022 17.11+, Rider 2023.1+, or VS Code with latest C# extension

> **Note**: To use source-generated dependency injection with interceptors, you must use at least version 8.0.400 of the .NET SDK. This ships with Visual Studio 2022 version 17.11 or higher.

## Attributes Reference

### `[DecoratedBy<TDecorator>]`

Generic attribute for type-safe decorator declaration.

```csharp
[DecoratedBy<LoggingDecorator>(Order = 0)]
public class MyService : IMyService { }
```

### `[DecoratedBy(Type decoratorType, int order = 0)]`

Non-generic variant supporting open generic types.

```csharp
[DecoratedBy(typeof(CachingRepository<>), order: 0)]
public class SqlRepository<T> : IRepository<T> { }
```

### `[assembly: DecorateService(Type serviceType, Type decoratorType, int order = 0)]`

Assembly-level decoration for all implementations of a service.

```csharp
[assembly: DecorateService(typeof(ICommand<>), typeof(ValidationCommand<>))]
```

### Properties

- **`Order`** (int): Controls decorator nesting order. Lower values are applied first (innermost). Default: 0.
- **`IsInterceptable`** (bool): Controls whether this decorator should be intercepted. Default: true. Set to false to skip interception for specific decorators.

## Troubleshooting

### "Interceptors are an experimental feature"

Interceptors are currently experimental. You may see compiler warnings. These can be suppressed:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);EXTEXP0001</NoWarn>
</PropertyGroup>
```

### No decorators are being applied

1. Verify you're using C# 11+ (`<LangVersion>11</LangVersion>`)
2. Ensure the generator package is referenced: `<PackageReference Include="Sculptor" />`
3. Check that your registration uses `AddScoped/Transient/Singleton(typeof(...), typeof(...))`
4. Look for generated files in `obj/Debug/[target]/generated/Sculptor/`

### Keyed services not available

Keyed services were introduced in .NET 8. If targeting earlier versions, you'll get compilation errors. Upgrade to .NET 8 or later.

### SDK version issues

If you encounter errors related to interceptors or source generators, ensure you're using .NET SDK 8.0.400 or later:

```bash
dotnet --version
```

If needed, download the latest SDK from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by [Scrutor](https://github.com/khellang/Scrutor) for runtime decorator registration
- Built on Roslyn's incremental source generators
- Uses C# interceptors (experimental feature introduced in C# 11)

---

**Made with ⚡ by the LayeredCraft team**