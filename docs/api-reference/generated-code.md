# Generated Code

!!! info "Coming Soon"
    This documentation is under development. Check back soon for detailed information about the structure of generated interceptor code.

## Overview

This page will cover:

- Structure of generated interceptor files
- How interceptors redirect DI registration calls
- Understanding `[InterceptsLocation]` attributes
- Generated factory methods for decorator chains
- Keyed service registration patterns

## Planned Content

- [ ] File naming and organization (`DecoWeaver.Interceptors.ClosedGenerics.g.cs`)
- [ ] Interceptor method signatures
- [ ] `[InterceptsLocation]` attribute usage
- [ ] Factory pattern for applying decorators
- [ ] Keyed service registration code
- [ ] How to read and debug generated code
- [ ] Generated code versioning

## Locating Generated Files

Generated interceptor code can be found in:
```
obj/Debug/{targetFramework}/generated/DecoWeaver/DecoWeaver.Generator/DecoWeaver.Interceptors.ClosedGenerics.g.cs
```

## IDE Support for Generated Files

- **Visual Studio 2022**: Enable "Show generated files" in Solution Explorer options
- **JetBrains Rider**: Files appear under "Generated Files" node
- **VS Code**: Generated files visible in obj/ directory

## See Also

- [Interceptors](../advanced/interceptors.md) - Understanding C# 11+ interceptors
- [How It Works](../core-concepts/how-it-works.md) - Generation process overview
- [Keyed Services](../advanced/keyed-services.md) - Internal keyed services strategy
