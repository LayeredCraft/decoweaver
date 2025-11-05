# DecoWeaver

[![Build Status](https://github.com/layeredcraft/decoweaver/actions/workflows/build.yaml/badge.svg)](https://github.com/layeredcraft/decoweaver/actions/workflows/build.yaml)
[![NuGet](https://img.shields.io/nuget/v/DecoWeaver.svg)](https://www.nuget.org/packages/DecoWeaver/)
[![Downloads](https://img.shields.io/nuget/dt/DecoWeaver.svg)](https://www.nuget.org/packages/DecoWeaver/)

**DecoWeaver** is a compile-time decorator registration library for .NET dependency injection. It uses C# 11+ interceptors to automatically apply the decorator pattern at build time, eliminating runtime reflection and assembly scanning.

## Key Features

- **⚡ Zero Runtime Overhead**: Decorators applied at compile time using C# interceptors
- **🎯 Type-Safe**: Full compile-time validation with IntelliSense support
- **🔧 Simple API**: Just add `[DecoratedBy<T>]` attributes to your classes
- **🚀 Open Generic Support**: Works seamlessly with `IRepository<T>` patterns
- **📦 No Runtime Dependencies**: Only build-time source generator dependency
- **🔗 Order Control**: Explicit decorator ordering via `Order` property
- **✨ Clean Generated Code**: Readable, debuggable interceptor code

## Installation

```bash
dotnet add package DecoWeaver --prerelease
```

## Quick Start

### 1. Define Your Service and Implementation

```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
}

public class UserRepository : IUserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        // Your implementation
    }
}
```

### 2. Create a Decorator

```csharp
public class LoggingUserRepository : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly ILogger<LoggingUserRepository> _logger;

    public LoggingUserRepository(
        IUserRepository inner,
        ILogger<LoggingUserRepository> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        _logger.LogInformation("Getting user {UserId}", id);
        var user = await _inner.GetByIdAsync(id);
        _logger.LogInformation("Retrieved user {UserId}: {UserName}", id, user.Name);
        return user;
    }
}
```

### 3. Apply the Decorator Attribute

```csharp
using DecoWeaver.Attributes;

[DecoratedBy<LoggingUserRepository>]
public class UserRepository : IUserRepository
{
    // Your implementation
}
```

### 4. Register in DI Container

```csharp
var services = new ServiceCollection();

// DecoWeaver automatically wraps UserRepository with LoggingUserRepository
services.AddScoped<IUserRepository, UserRepository>();

var provider = services.BuildServiceProvider();
var repo = provider.GetRequiredService<IUserRepository>();
// Returns: LoggingUserRepository wrapping UserRepository
```

That's it! DecoWeaver handles the decoration automatically at compile time.

## How It Works

DecoWeaver uses a source generator to:

1. **Discover** `[DecoratedBy]` attributes on your classes at compile time
2. **Find** DI registration calls like `AddScoped<IService, Implementation>()`
3. **Generate** C# interceptor code that rewrites those calls
4. **Wrap** implementations with decorators using keyed services

The result is zero runtime overhead and fully type-safe decorator application.

## Multiple Decorators with Ordering

You can stack multiple decorators and control their order:

```csharp
[DecoratedBy<LoggingRepository>(Order = 1)]      // Applied first (innermost)
[DecoratedBy<CachingRepository>(Order = 2)]      // Applied second
[DecoratedBy<MetricsRepository>(Order = 3)]      // Applied third (outermost)
public class UserRepository : IUserRepository
{
    // Your implementation
}

// Result: MetricsRepository → CachingRepository → LoggingRepository → UserRepository
```

## Open Generic Support

DecoWeaver works seamlessly with open generic types:

```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
}

[DecoratedBy<CachingRepository<>>]  // Open generic decorator
public class Repository<T> : IRepository<T> where T : class
{
    // Your implementation
}

// Register open generic
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// All closed versions get decorated automatically
var userRepo = provider.GetRequiredService<IRepository<User>>();
// Returns: CachingRepository<User> wrapping Repository<User>
```

## Documentation

- **[Getting Started](getting-started/installation.md)** - Installation and setup
- **[Core Concepts](core-concepts/how-it-works.md)** - Understanding how DecoWeaver works
- **[Usage Guide](usage/class-level-decorators.md)** - Detailed attribute usage
- **[Examples](examples/index.md)** - Real-world usage patterns
- **[Advanced Topics](advanced/interceptors.md)** - Deep dive into internals

## Requirements

- **.NET SDK** with C# 11+ support (Visual Studio 2022 17.4+, Rider 2022.3+)
- **.NET 8.0+** runtime (for keyed services support)
- **Microsoft.Extensions.DependencyInjection** 8.0+

## Why DecoWeaver?

### Traditional Decorator Registration

```csharp
// Manual, error-prone, runtime overhead
services.AddScoped<IUserRepository>(sp =>
{
    var inner = new UserRepository(/* dependencies */);
    var logged = new LoggingRepository(inner, sp.GetRequiredService<ILogger>());
    var cached = new CachingRepository(logged, sp.GetRequiredService<ICache>());
    return cached;
});
```

### With DecoWeaver

```csharp
// Clean, compile-time, zero overhead
[DecoratedBy<CachingRepository>(Order = 2)]
[DecoratedBy<LoggingRepository>(Order = 1)]
public class UserRepository : IUserRepository { }

services.AddScoped<IUserRepository, UserRepository>(); // Done!
```

## Contributing

See the [Contributing Guide](contributing.md) for information on how to contribute to DecoWeaver.

## License

This project is licensed under the [MIT License](https://github.com/layeredcraft/decoweaver/blob/main/LICENSE).