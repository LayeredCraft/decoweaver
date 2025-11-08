# Quick Start

Get started with DecoWeaver in 5 minutes by creating a simple logging decorator.

## Step 1: Install DecoWeaver

```bash
dotnet add package DecoWeaver --prerelease
```

## Step 2: Create Your Service

```csharp
public interface IUserService
{
    Task<User> GetByIdAsync(int id);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

## Step 3: Create a Decorator

```csharp
using Microsoft.Extensions.Logging;

public class LoggingUserService : IUserService
{
    private readonly IUserService _inner;
    private readonly ILogger<LoggingUserService> _logger;

    public LoggingUserService(
        IUserService inner,
        ILogger<LoggingUserService> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        _logger.LogInformation("Getting user {UserId}", id);

        var user = await _inner.GetByIdAsync(id);

        _logger.LogInformation(
            "Retrieved user {UserId}: {UserName}",
            id,
            user.Name);

        return user;
    }
}
```

## Step 4: Apply the Decorator Attribute

Add the `[DecoratedBy]` attribute to your implementation:

```csharp
using DecoWeaver.Attributes;

[DecoratedBy<LoggingUserService>]
public class UserService : IUserService
{
    // Your implementation
}
```

## Step 5: Register in DI

Register your service normally - DecoWeaver handles the decoration automatically:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add logging
services.AddLogging();

// Register your service - DecoWeaver automatically applies the decorator
services.AddScoped<IUserService, UserService>();

// Build and resolve
var provider = services.BuildServiceProvider();
var userService = provider.GetRequiredService<IUserService>();

// userService is actually: LoggingUserService wrapping UserService
await userService.GetByIdAsync(123);
// Logs: "Getting user 123"
// Logs: "Retrieved user 123: John Doe"
```

## What Just Happened?

1. **Build Time**: DecoWeaver's source generator detected the `[DecoratedBy]` attribute
2. **Code Generation**: Generated an interceptor that rewrites the `AddScoped` call
3. **Runtime**: Your service is automatically wrapped with the logging decorator

## View Generated Code

To see the generated interceptor code:

**Visual Studio**: Solution Explorer → Show All Files → obj/Debug/net8.0/generated/DecoWeaver/

**Rider**: Solution Explorer → Generated Files node

## Alternative: Assembly-Level Decorators

Instead of applying decorators to each class individually, you can apply them to all implementations from one place:

```csharp
// In GlobalUsings.cs or any .cs file
using DecoWeaver.Attributes;

[assembly: DecorateService(typeof(IUserService), typeof(LoggingUserService))]

// Now ALL IUserService implementations automatically get logging
public class UserService : IUserService { }
public class AdminUserService : IUserService { }
```

This is ideal for cross-cutting concerns like logging, metrics, or caching that apply to many services. See [Assembly-Level Decorators](../usage/assembly-level-decorators.md) for details.

## Next Steps

Now that you have a basic decorator working, explore:

- **[Assembly-Level Decorators](../usage/assembly-level-decorators.md)** - Apply decorators globally
- **[Multiple Decorators](../usage/multiple-decorators.md)** - Stack decorators with ordering
- **[Open Generics](../usage/open-generics.md)** - Decorate `IRepository<T>` patterns
- **[Examples](../examples/index.md)** - Real-world decorator patterns

## Common First-Time Issues

!!! warning "Generator Not Running?"
    If decorators aren't being applied:

    1. Check C# language version is 11+ in your .csproj
    2. Clean and rebuild your solution
    3. Check for build errors in the Error List
    4. View generated files to verify interceptor was created

!!! tip "IntelliSense Not Working?"
    Rebuild your solution after adding attributes. IDEs need a successful build to recognize generated code.