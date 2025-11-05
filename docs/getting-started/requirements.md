# Requirements

## .NET SDK

DecoWeaver requires:

- **.NET SDK 8.0.400+** for C# 11 language support and interceptor features
- **C# 11** or later (set via `<LangVersion>11</LangVersion>` or `<LangVersion>latest</LangVersion>`)

!!! warning "SDK Version Requirement"
    You must use at least version 8.0.400 of the .NET SDK to use source-generated dependency injection with interceptors. This ships with Visual Studio 2022 version 17.11 or higher.

## Runtime

- **.NET 8.0+** runtime (required for keyed services support)
- **Microsoft.Extensions.DependencyInjection 8.0+**

!!! info "Why .NET 8+?"
    DecoWeaver uses .NET 8's keyed services feature to prevent circular dependencies when applying decorators. This is essential for the decorator pattern to work correctly with dependency injection.

## C# Language Features

DecoWeaver relies on these C# features:

- **Interceptors** (C# 11, experimental) - Rewrites DI registration calls at compile time
- **File-scoped types** (C# 11) - Prevents namespace pollution in generated code
- **Generic attributes** (C# 11) - For `[DecoratedBy<TDecorator>]` syntax

## IDE Requirements

### Visual Studio

- **Visual Studio 2022 17.4+**
- Supports IntelliSense for generated code
- Enable "Show generated files" to view interceptor code

### JetBrains Rider

- **Rider 2022.3+**
- Full source generator support
- Generated files visible in Solution Explorer

### Visual Studio Code

- **C# Dev Kit extension**
- **OmniSharp** with .NET 8 SDK
- Some generated file viewing limitations

## Build Environment

- **MSBuild** or **dotnet CLI** for builds
- Source generators run during compilation
- No special build configuration needed

## Compatibility

| Feature | Minimum Version |
|---------|----------------|
| .NET SDK | 8.0 |
| Runtime | 8.0 |
| C# Language | 11 |
| Visual Studio | 2022 17.4+ |
| Rider | 2022.3+ |
| DI Library | Microsoft.Extensions.DependencyInjection 8.0+ |

## Verification

Verify your environment meets requirements:

```bash
# Check .NET SDK version
dotnet --version
# Should be 8.0.0 or higher

# Check C# language version in your project
cat MyProject.csproj | grep LangVersion
# Should show LangVersion 11 or later
```

## Next Steps

- Install DecoWeaver via [Installation Guide](installation.md)
- Create your first decorator with [Quick Start](quick-start.md)