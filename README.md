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
- **Closed generic support** - Decorate specific instantiations like `IRepository<Customer>` and `IRepository<Order>`
- **Clear service composition** - Generated code is inspectable and debuggable

## Key Features

- **Attribute-driven decoration**: Simply mark your implementations with `[DecoratedBy<TDecorator>]` or `[DecoratedBy(typeof(...))]`
- **Closed generic registrations**: Support for `IRepository<Customer>`, `ICommand<CreateOrder>`, and other closed generic patterns
- **Open generic decorators**: Decorator classes can be open generics like `CachingRepository<>` that are closed at runtime
- **Ordered decorator chains**: Control decorator application order with the `Order` property
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

### 2. Register with closed generics

```csharp
var services = new ServiceCollection();

// Register closed generic types (specific instantiations)
services.AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>();
services.AddScoped<IRepository<Order>, DynamoDbRepository<Order>>();

var provider = services.BuildServiceProvider();
```

### 3. Magic happens at compile time

Sculptor generates an interceptor for each registration that:
1. Registers the undecorated implementation (e.g., `DynamoDbRepository<Customer>`) as a keyed service
2. Registers a factory that resolves the keyed service and wraps it with decorators
3. Closes open generic decorators at runtime using the service type's generic arguments

When you resolve `IRepository<Customer>`, you automatically get:
```
CachingRepository<Customer>
  └─ DynamoDbRepository<Customer>
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

### Open Generic Decorators with Closed Registrations

Use `typeof()` for open generic decorators. The decorator can be an open generic (`CachingRepository<>`) even though you register closed types:

```csharp
[DecoratedBy(typeof(CachingRepository<>))]
[DecoratedBy(typeof(LoggingRepository<>), order: 1)]
public class SqlRepository<T> : IRepository<T>
{
    // Implementation
}

// Register with closed types
services.AddScoped<IRepository<Customer>, SqlRepository<Customer>>();
services.AddScoped<IRepository<Order>, SqlRepository<Order>>();
```

The decorator will be closed at runtime to match the service type (e.g., `CachingRepository<Customer>`).

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

### Important: Closed Generic Registration Required

**Sculptor currently only supports closed generic registrations.** This means you must register specific type instantiations:

✅ **Supported:**
```csharp
services.AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>();
services.AddScoped<IRepository<Order>, DynamoDbRepository<Order>>();
```

❌ **Not Supported:**
```csharp
services.AddScoped(typeof(IRepository<>), typeof(DynamoDbRepository<>));
```

The decorator classes themselves can be open generics (like `CachingRepository<>`), and they will be closed at runtime to match the registered service type.

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
   - DI registration calls like `AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>()`

2. **Code Generation**: Generates an interceptor file (`Sculptor.Interceptors.ClosedGenerics.g.cs`) with:
   - One interceptor method per registration call site
   - Methods with generic signatures (`<TService, TImplementation>`) to match the original DI extension methods
   - `[InterceptsLocation]` attributes that redirect specific call sites to the generated methods
   - Method bodies use the concrete closed types (not the type parameters)

3. **Runtime Behavior**: The interceptor method:
   - Registers the undecorated implementation as a keyed service (e.g., `DynamoDbRepository<Customer>`)
   - Registers a factory that resolves the keyed implementation and wraps it with decorators
   - Open generic decorators are closed at runtime using `MakeGenericType` with the service type's generic arguments
   - Decorators are applied in ascending order by `Order` property

### Generated Code Example

For a registration like `services.AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>()`, Sculptor generates:

```csharp
[InterceptsLocation(version: 1, data: "e38EDFi...")]
/// <summary>Intercepted: ServiceCollectionServiceExtensions.AddScoped&lt;IRepository<Customer>, DynamoDbRepository<Customer>&gt;</summary>
internal static IServiceCollection AddScoped_0<TService, TImplementation>(this IServiceCollection services)
    where TService : class
    where TImplementation : class, TService
{
    // Register the undecorated implementation as a keyed service
    var key = DecoratorKeys.For(typeof(global::App.IRepository<Customer>), typeof(global::App.DynamoDbRepository<Customer>));
    services.AddKeyedScoped<global::App.IRepository<Customer>, global::App.DynamoDbRepository<Customer>>(key);

    // Register factory that applies decorators
    services.AddScoped<global::App.IRepository<Customer>>(sp =>
    {
        var current = (global::App.IRepository<Customer>)sp.GetRequiredKeyedService<global::App.IRepository<Customer>>(key)!;
        // Compose decorators (innermost to outermost)
        current = (global::App.IRepository<Customer>)DecoratorFactory.Create(sp,
            typeof(global::App.IRepository<Customer>),
            typeof(global::App.CachingRepository<>),
            current);
        return current;
    });
    return services;
}
```

**Key Points:**
- The method signature is generic (`<TService, TImplementation>`) to match the intercepted method
- The method body uses concrete types (`IRepository<Customer>`, `DynamoDbRepository<Customer>`)
- Open generic decorators (`CachingRepository<>`) are closed at runtime via `DecoratorFactory.Create`

## Comparison with Scrutor

Sculptor and [Scrutor](https://github.com/khellang/Scrutor) both enable decorator patterns in .NET DI, but with different approaches:

| Aspect | Sculptor | Scrutor |
|--------|----------|---------|
| **Execution** | Compile-time code generation | Runtime reflection & scanning |
| **Performance** | Zero startup overhead | Reflection cost at startup |
| **Registration Style** | Closed generics (`AddScoped<IRepo<T1>, Impl<T1>>()`) | Both open and closed generics |
| **Decorator Types** | Open generics supported (closed at runtime) | Open generics supported |
| **Type Safety** | Compile-time validation | Runtime validation |
| **Inspection** | Generated code is visible | Runtime-only registration |
| **Requirements** | C# 11+, .NET 8+ | .NET Standard 2.0+ |

Choose Sculptor when you want compile-time safety, zero reflection overhead, and are comfortable with closed generic registrations. Choose Scrutor for broader compatibility, open generic registrations, or dynamic scenarios.

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
3. **Check that your registration uses closed generics**: `AddScoped<IRepo<Customer>, Impl<Customer>>()`, not `AddScoped(typeof(IRepo<>), typeof(Impl<>))`
4. Look for generated files in `obj/Debug/[target]/generated/Sculptor/`
5. Verify the `[DecoratedBy]` attribute is on the implementation class (e.g., `DynamoDbRepository<T>`)

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