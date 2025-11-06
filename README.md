# DecoWeaver

**Compile-time decorator registration for .NET dependency injection**

[![NuGet](https://img.shields.io/nuget/v/DecoWeaver.svg)](https://www.nuget.org/packages/DecoWeaver/)
[![Build Status](https://github.com/LayeredCraft/decoweaver/actions/workflows/build.yaml/badge.svg)](https://github.com/LayeredCraft/decoweaver/actions/workflows/build.yaml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Documentation](https://img.shields.io/badge/docs-latest-blue)](https://layeredcraft.github.io/decoweaver/)

---

DecoWeaver is a .NET incremental source generator that brings **compile-time decorator registration** to your dependency injection setup. Using C# 11+ interceptors, it automatically wraps your service implementations with decorators at build time—eliminating runtime reflection, assembly scanning, and manual factory wiring.

**Why DecoWeaver?**
- ⚡ **Zero runtime overhead** - No reflection or assembly scanning at startup
- 🎯 **Type-safe** - Catches configuration errors at compile time
- 🚀 **Fast** - Incremental generation with Roslyn
- 🔧 **Simple** - Just add `[DecoratedBy<T>]` attributes
- 📦 **Clean** - Generates readable, debuggable interceptor code

📚 **[View Full Documentation](https://layeredcraft.github.io/decoweaver/)**

## Installation

Install the NuGet package:

```bash
dotnet add package DecoWeaver --prerelease
```

Ensure your project uses C# 11 or later:

```xml
<PropertyGroup>
  <LangVersion>11</LangVersion>
  <!-- or <LangVersion>latest</LangVersion> -->
</PropertyGroup>
```

## Quick Start

### 1. Mark your implementation with the decorator to apply

```csharp
using DecoWeaver.Attributes;

[DecoratedBy<LoggingRepository>]
public class UserRepository : IUserRepository
{
    // Your implementation
}
```

### 2. Register your service normally

```csharp
services.AddScoped<IUserRepository, UserRepository>();
```

### 3. That's it!

At compile time, DecoWeaver automatically generates interceptor code that wraps `UserRepository` with `LoggingRepository`. When you resolve `IUserRepository`, you'll get the decorated instance.

```
LoggingRepository
  └─ UserRepository
```

For more examples including open generics, multiple decorators, and ordering, see the [Quick Start Guide](https://layeredcraft.github.io/decoweaver/getting-started/quick-start/).

## Key Features

- **Multiple Decorators**: Stack multiple decorators with explicit ordering
- **Generic Type Decoration**: Decorate generic types like `IRepository<T>` with open generic decorators
- **Type-Safe**: Compile-time validation catches errors early
- **Zero Configuration**: No runtime registration or setup needed
- **Debuggable**: Generated code is readable and inspectable

Learn more in the [Core Concepts](https://layeredcraft.github.io/decoweaver/core-concepts/how-it-works/) documentation.

## Requirements

- C# 11+ (for interceptors support)
- .NET 8.0+ (for keyed services)
- .NET SDK 8.0.400+ (Visual Studio 2022 17.11+)

See the [Requirements](https://layeredcraft.github.io/decoweaver/getting-started/requirements/) page for full details.

## Documentation

📖 **[Full Documentation](https://layeredcraft.github.io/decoweaver/)** - Comprehensive guides and API reference

Key sections:
- [Installation](https://layeredcraft.github.io/decoweaver/getting-started/installation/) - Get started with DecoWeaver
- [Quick Start](https://layeredcraft.github.io/decoweaver/getting-started/quick-start/) - Your first decorator in 5 minutes
- [Core Concepts](https://layeredcraft.github.io/decoweaver/core-concepts/how-it-works/) - Understand how it works
- [Usage Guide](https://layeredcraft.github.io/decoweaver/usage/class-level-decorators/) - Detailed usage patterns
- [Examples](https://layeredcraft.github.io/decoweaver/examples/) - Real-world scenarios

## Contributing

Contributions are welcome! See our [Contributing Guide](https://layeredcraft.github.io/decoweaver/contributing/) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Made with ⚡ by the LayeredCraft team**