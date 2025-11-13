# Instance Registrations

DecoWeaver supports decorating singleton instance registrations starting with version 1.0.4-beta. This allows you to apply decorators to pre-configured instances that you register directly with the DI container.

## Overview

Instance registrations let you register a pre-created instance directly with the DI container. DecoWeaver can intercept these registrations and apply decorators around your instance, just like it does for parameterless and factory delegate registrations.

## Supported Patterns

### Single Type Parameter with Instance

```csharp
// Register a pre-created instance
var instance = new SqlRepository<Customer>();
services.AddSingleton<IRepository<Customer>>(instance);

// DecoWeaver will apply decorators around the instance
var repo = serviceProvider.GetRequiredService<IRepository<Customer>>();
// Returns: LoggingRepository<Customer> wrapping SqlRepository<Customer> instance
```

## Limitations

### Singleton Only

Instance registrations are **only supported with `AddSingleton`**. This is a limitation of .NET's dependency injection framework itself:

```csharp
// ✅ Supported - AddSingleton with instance
services.AddSingleton<IRepository<Customer>>(instance);

// ❌ NOT supported - AddScoped doesn't have instance overload in .NET DI
services.AddScoped<IRepository<Customer>>(instance); // Compiler error

// ❌ NOT supported - AddTransient doesn't have instance overload in .NET DI
services.AddTransient<IRepository<Customer>>(instance); // Compiler error
```

The reason is that scoped and transient lifetimes are incompatible with instance registrations - they require creating new instances on each resolution or scope, which contradicts the concept of registering a pre-created instance.

## How It Works

When DecoWeaver encounters an instance registration:

1. **Instance Type Extraction**: The generator extracts the actual type of the instance from the argument expression
   ```csharp
   // User code:
   services.AddSingleton<IRepository<Customer>>(new SqlRepository<Customer>());

   // DecoWeaver sees:
   // - Service type: IRepository<Customer>
   // - Implementation type: SqlRepository<Customer> (extracted from "new SqlRepository<Customer>()")
   ```

2. **Keyed Service Registration**: The instance is registered directly as a keyed service
   ```csharp
   // Generated code:
   var key = DecoratorKeys.For(typeof(IRepository<Customer>), typeof(SqlRepository<Customer>));
   var capturedInstance = (IRepository<Customer>)(object)implementationInstance;
   services.AddKeyedSingleton<IRepository<Customer>>(key, capturedInstance);
   ```

3. **Decorator Application**: Decorators are applied around the keyed service
   ```csharp
   // Generated code:
   services.AddSingleton<IRepository<Customer>>(sp =>
   {
       var current = sp.GetRequiredKeyedService<IRepository<Customer>>(key);
       current = (IRepository<Customer>)DecoratorFactory.Create(
           sp, typeof(IRepository<Customer>), typeof(LoggingRepository<>), current);
       return current;
   });
   ```

## Examples

### Basic Instance Registration

```csharp
[DecoratedBy<LoggingRepository<>>]
public class SqlRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[SQL] Saving {typeof(T).Name}...");
    }
}

// Register pre-created instance
var instance = new SqlRepository<Customer>();
services.AddSingleton<IRepository<Customer>>(instance);

// The same instance is reused for all resolutions, but wrapped with decorators
var repo1 = serviceProvider.GetRequiredService<IRepository<Customer>>();
var repo2 = serviceProvider.GetRequiredService<IRepository<Customer>>();
// repo1 and repo2 both wrap the same SqlRepository<Customer> instance
```

### Multiple Decorators with Instance

```csharp
[DecoratedBy<CachingRepository<>>(Order = 1)]
[DecoratedBy<LoggingRepository<>>(Order = 2)]
public class SqlRepository<T> : IRepository<T> { /* ... */ }

var instance = new SqlRepository<Product>();
services.AddSingleton<IRepository<Product>>(instance);

// Resolved as: LoggingRepository wrapping CachingRepository wrapping instance
```

### Pre-Configured Instance

```csharp
// Useful when instance needs complex initialization
var connectionString = configuration.GetConnectionString("Production");
var instance = new SqlRepository<Order>(connectionString)
{
    CommandTimeout = TimeSpan.FromSeconds(30),
    EnableRetries = true
};

services.AddSingleton<IRepository<Order>>(instance);
// Decorators are applied, but the pre-configured instance is preserved
```

## Technical Details

### Type Extraction from Arguments

DecoWeaver uses Roslyn's semantic model to extract the actual type from the instance argument:

```csharp
// In ClosedGenericRegistrationProvider.cs
var args = inv.ArgumentList.Arguments;
if (args.Count >= 1)
{
    var instanceArg = args[0].Expression; // Extension methods don't include 'this' in ArgumentList
    var instanceType = semanticModel.GetTypeInfo(instanceArg).Type as INamedTypeSymbol;
    return (serviceType, instanceType); // e.g., (IRepository<Customer>, SqlRepository<Customer>)
}
```

### Direct Instance Registration

DecoWeaver uses the direct instance overload available in .NET DI for keyed singleton services:

```csharp
// DecoWeaver generates:
var key = DecoratorKeys.For(typeof(IRepository<Customer>), typeof(SqlRepository<Customer>));
var capturedInstance = (IRepository<Customer>)(object)implementationInstance;
services.AddKeyedSingleton<IRepository<Customer>>(key, capturedInstance);
```

This preserves the expected .NET DI disposal semantics - the container owns and disposes the instance when the container is disposed, just like non-keyed singleton instance registrations.

The double cast `(TService)(object)` ensures the generic type parameter `TService` is compatible with the captured instance.

## When to Use Instance Registrations

Instance registrations with DecoWeaver are useful when:

1. **Pre-configured Dependencies**: Your instance needs complex initialization that's easier to do outside of DI
2. **External Resources**: Registering wrappers around external resources (e.g., database connections, message queues)
3. **Testing/Mocking**: Registering test doubles or mocks with specific configurations
4. **Singleton State**: When you need a true singleton with decorators applied

## Alternatives

If you need more flexibility, consider these alternatives:

### Factory Delegates
```csharp
// More flexible than instances - can use IServiceProvider
services.AddSingleton<IRepository<Customer>>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new SqlRepository<Customer>(config.GetConnectionString("Default"));
});
```

### Parameterless with Constructor Injection
```csharp
// Let DI handle the construction
services.AddSingleton<IRepository<Customer>, SqlRepository<Customer>>();
// SqlRepository constructor receives dependencies from DI
```

## See Also

- [Factory Delegates](../usage/factory-delegates.md) - Using factory functions with decorators
- [Keyed Services](keyed-services.md) - How DecoWeaver uses keyed services internally
- [How It Works](../core-concepts/how-it-works.md) - Understanding the generation process
