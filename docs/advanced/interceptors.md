# Interceptors

Sculptor uses C# 11's experimental interceptors feature to rewrite dependency injection registration calls at compile time. This page explains how interceptors work and how Sculptor uses them.

## What are Interceptors?

Interceptors are a C# 11 feature that allows you to redirect method calls to different implementations at compile time. They work by annotating a method with `[InterceptsLocation]` attributes that specify which call sites to intercept.

### Basic Example

```csharp
// Original code
Console.WriteLine("Hello");

// Interceptor
file static class Interceptors
{
    [InterceptsLocation(version: 1, data: "base64-encoded-hash")]
    public static void WriteLine(string message)
    {
        Console.WriteLine($"[LOG] {message}");
    }
}

// At compile time, the original call is redirected to the interceptor
```

When compiled, the original `Console.WriteLine("Hello")` call is redirected to the interceptor method, which adds a `[LOG]` prefix.

## How Sculptor Uses Interceptors

Sculptor intercepts DI registration calls to apply decorators automatically.

### Without Sculptor

Traditional manual decoration:

```csharp
services.AddScoped<IUserRepository>(sp =>
{
    var impl = new UserRepository(sp.GetRequiredService<IDbContext>());
    var cached = new CachingRepository(impl, sp.GetRequiredService<IMemoryCache>());
    var logged = new LoggingRepository(cached, sp.GetRequiredService<ILogger>());
    return logged;
});
```

### With Sculptor

Automatic decoration at compile time:

```csharp
[DecoratedBy<CachingRepository>]
[DecoratedBy<LoggingRepository>]
public class UserRepository : IUserRepository { }

// Your code
services.AddScoped<IUserRepository, UserRepository>();

// Sculptor generates an interceptor that rewrites this to:
services.AddScoped<IUserRepository>(/* factory with decorators */);
```

## Generated Interceptor Code

When you apply `[DecoratedBy]` attributes, Sculptor generates interceptor code like this:

```csharp
// Your code
services.AddScoped<IUserRepository, UserRepository>();

// Generated interceptor
file static class SculptorInterceptors
{
    [InterceptsLocation(version: 1, data: "base64-encoded-hash")]
    public static IServiceCollection AddScoped_IUserRepository_UserRepository(
        this IServiceCollection services)
    {
        // Register undecorated implementation with keyed service
        services.AddKeyedScoped<IUserRepository, UserRepository>(
            "IUserRepository|UserRepository");

        // Register factory that applies decorators
        services.AddScoped<IUserRepository>(sp =>
        {
            // Get undecorated implementation
            var inner = sp.GetRequiredKeyedService<IUserRepository>(
                "IUserRepository|UserRepository");

            // Apply decorators in order
            inner = new CachingRepository(
                inner,
                sp.GetRequiredService<IMemoryCache>());

            inner = new LoggingRepository(
                inner,
                sp.GetRequiredService<ILogger<LoggingRepository>>());

            return inner;
        });

        return services;
    }
}
```

### Key Components

1. **`[InterceptsLocation]`**: Tells the compiler which call site to intercept
2. **Keyed Service**: Registers the undecorated implementation
3. **Factory Registration**: Wraps the implementation with decorators
4. **File-Scoped Class**: Uses `file` keyword to prevent naming collisions

## InterceptsLocation Attribute

The `[InterceptsLocation]` attribute identifies which call sites to intercept:

```csharp
[InterceptsLocation(version: 1, data: "base64-encoded-hash")]
```

**Parameters**:
- `version`: Always `1` (format version)
- `data`: A base64-encoded hash that uniquely identifies the location of the code to intercept

The `data` parameter contains an encoded hash value that points to the specific call site in your source code. This hash is calculated by the source generator based on the file path and position of the method call.

### Multiple Locations

A single interceptor method can intercept multiple call sites:

```csharp
[InterceptsLocation(version: 1, data: "hash1")]
[InterceptsLocation(version: 1, data: "hash2")]
[InterceptsLocation(version: 1, data: "hash3")]
public static IServiceCollection AddScoped_IUserRepository_UserRepository(
    this IServiceCollection services)
{
    // Implementation
}
```

This allows Sculptor to intercept all registrations of `UserRepository` across your codebase.

## File-Scoped Types

Sculptor uses file-scoped types (`file` keyword) to prevent naming collisions:

```csharp
file static class SculptorInterceptors
{
    // This class is only visible within this file
}
```

This prevents conflicts when generating interceptors in multiple files.

## Viewing Generated Code

### Visual Studio

1. Build your project
2. Solution Explorer → Show All Files
3. Navigate to `obj/Debug/net8.0/generated/Sculptor.Generators/`
4. Open `Sculptor.Interceptors.g.cs`

### Rider

1. Build your project
2. Solution Explorer → Show Generated Files
3. Expand the Sculptor.Generators node
4. Open `Sculptor.Interceptors.g.cs`

### Visual Studio Code

1. Build your project
2. Navigate to `obj/Debug/net8.0/generated/Sculptor.Generators/`
3. Open `Sculptor.Interceptors.g.cs`

## Interceptor Requirements

For interceptors to work correctly:

### C# Language Version

Interceptors require C# 11 or later:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <LangVersion>11</LangVersion> <!-- or "latest" -->
  </PropertyGroup>
</Project>
```

The Sculptor package automatically enables the interceptors feature, so you don't need to manually configure any experimental feature flags.

### Method Signature Match

Interceptor methods must exactly match the signature of the method they intercept:

```csharp
// Original method
public static IServiceCollection AddScoped<TService, TImplementation>(
    this IServiceCollection services)
    where TService : class
    where TImplementation : class, TService

// Interceptor must match exactly
[InterceptsLocation(...)]
public static IServiceCollection AddScoped<TService, TImplementation>(
    this IServiceCollection services)
    where TService : class
    where TImplementation : class, TService
{
    // Implementation
}
```

## Open Generic Interceptors

For open generic registrations, Sculptor generates interceptors that work with runtime types:

```csharp
// Your code
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Generated interceptor
[InterceptsLocation(version: 1, data: "base64-encoded-hash")]
public static IServiceCollection AddScoped_IRepository_Repository(
    this IServiceCollection services,
    Type serviceType,
    Type implementationType)
{
    services.AddKeyedScoped(
        serviceType,
        implementationType,
        "IRepository<>|Repository<>");

    services.AddScoped(serviceType, (sp, resolvedServiceType) =>
    {
        // Get type arguments at runtime
        var typeArgs = resolvedServiceType.GenericTypeArguments;

        // Close open generic types
        var closedImpl = implementationType.MakeGenericType(typeArgs);
        var inner = sp.GetRequiredKeyedService(resolvedServiceType, "...");

        // Apply decorators
        var decoratorType = typeof(CachingRepository<>).MakeGenericType(typeArgs);
        return Activator.CreateInstance(decoratorType, inner, /* dependencies */);
    });

    return services;
}
```

## Debugging Interceptors

### Compile-Time Errors

If an interceptor fails to apply, you'll see compile errors:

```
CS9137: The 'interceptors' preview feature is not enabled.
```

**Solution**: Enable C# 11 and interceptors in your project file.

```
CS9144: Cannot intercept method 'AddScoped' because it is not interceptable.
```

**Solution**: Ensure you're using `Microsoft.Extensions.DependencyInjection` 8.0+.

### Runtime Errors

If decorators aren't being applied:

1. Check that generated files exist in `obj/Debug/generated/`
2. Verify all decorator dependencies are registered
3. Ensure keyed services are supported (.NET 8+)
4. Rebuild the solution to regenerate interceptors

### Viewing Interceptor Locations

Add logging to see which interceptors are generated:

```csharp
// This is handled by Sculptor's source generator
// Check build output for diagnostic messages
```

## Performance

Interceptors have **zero runtime overhead**:

- No reflection at runtime
- No dynamic code generation
- No assembly scanning
- Direct method calls in generated code

The only cost is during compilation when interceptors are generated.

## Limitations

### Cannot Intercept Non-Public Methods

Interceptors only work with public methods:

```csharp
// ✅ Can intercept
public static IServiceCollection AddScoped<T, TImpl>(this IServiceCollection services)

// ❌ Cannot intercept
internal static IServiceCollection AddScoped<T, TImpl>(this IServiceCollection services)
```

### Cannot Intercept Virtual Methods

Virtual methods cannot be intercepted:

```csharp
// ✅ Can intercept
public static IServiceCollection AddScoped(...)

// ❌ Cannot intercept
public virtual IServiceCollection AddScoped(...)
```

### Cannot Intercept Generic Methods with Constraints

Some complex generic constraints may not work:

```csharp
// ✅ Works
public static void Method<T>() where T : class

// ⚠️ May not work
public static void Method<T>() where T : class, IDisposable, new()
```

## Experimental Feature Status

Interceptors are currently experimental in C# 11:

- API may change in future versions
- Some IDEs may show warnings
- Feature may be refined or stabilized in C# 12+

Sculptor uses interceptors safely within their documented capabilities. The feature is stable enough for production use with .NET 8+.

## Future of Interceptors

Microsoft is actively working on interceptors:

- Potential stabilization in future C# versions
- Improved IDE support
- Better debugging experience
- Additional use cases beyond DI

Sculptor will continue to support and leverage interceptors as the feature evolves.

## Next Steps

- Learn about [Source Generators](source-generators.md)
- Understand [How It Works](../core-concepts/how-it-works.md) in detail
- See [Testing Strategies](testing.md) for decorated services