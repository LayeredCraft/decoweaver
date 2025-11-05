# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LayeredCraft.DecoWeaver is a .NET incremental source generator that enables **compile-time decorator registration** for dependency injection. It uses C# 11+ interceptors to automatically wrap service implementations with decorators at build time, eliminating runtime reflection and assembly scanning.

## Build Commands

```bash
# Restore NuGet packages
dotnet restore

# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Build in release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

## Project Structure

The solution contains four main projects:

1. **src/LayeredCraft.DecoWeaver.Attributes** (netstandard2.0)
   - Defines `[DecoratedBy<T>]` and `[DecoratedBy(typeof(...))]` attributes
   - Consumer-facing API with minimal dependencies
   - Uses Polyfill for modern C# features on older targets

2. **src/LayeredCraft.DecoWeaver.Generators** (netstandard2.0)
   - The incremental source generator implementation
   - Depends on Microsoft.CodeAnalysis.CSharp 4.14.0
   - Emits interceptor code to `DecoWeaver.Interceptors.ClosedGenerics.g.cs`

3. **samples/DecoWeaver.Sample** (net10.0)
   - Demonstrates open generic decorator registration
   - References both Attributes and Generators projects

4. **test/LayeredCraft.DecoWeaver.Generator.Tests** (net8.0)
   - XUnit v3 tests with snapshot verification infrastructure
   - Uses AutoFixture + NSubstitute for test data/mocking
   - Multi-framework BCL reference assemblies for accurate compilation testing

## High-Level Architecture

### Source Generator Pipeline

The generator follows an incremental generation pipeline with these key stages:

1. **Language Version Gate**: Validates C# 11+ required for interceptors
2. **Attribute Discovery**: Two parallel streams discover decorators:
   - `DecoratedByGenericProvider`: Handles `[DecoratedBy<TDecorator>]`
   - `DecoratedByNonGenericProvider`: Handles `[DecoratedBy(typeof(...))]`
3. **Registration Discovery**: `OpenGenericRegistrationProvider` finds DI registrations like `AddScoped(typeof(IRepo<>), typeof(SqlRepo<>))`
4. **Data Combination**: Maps implementations to their decorators, ordered by priority
5. **Code Emission**: Generates interceptor methods with `[InterceptsLocation]` attributes

### Key Design Patterns

**Type Identity System**:
- `TypeDefId`: Definition-only identity (no type arguments) - stable across compilations
- `TypeId`: Full type with type arguments
- Handles open generics, nested types, and type parameters

**Incremental Optimizations**:
- Predicates use syntax-only checks (no semantic model)
- Transformers are pure functions with semantic analysis
- `EquatableArray<T>` for efficient change detection
- Language gate applied before expensive `Collect()` operations

**Interceptor Generation**:
- Generates methods that match the signature of `ServiceCollectionServiceExtensions.AddScoped/Transient/Singleton`
- Uses `[InterceptsLocation]` to redirect specific call sites
- Registers implementation with keyed service to prevent circular resolution
- Factory registration applies decorators in ascending order (by Order property)

### Decorator Application Logic

For each decorated implementation, the generator:
1. Registers the undecorated implementation as a keyed service
2. Registers a factory that resolves the keyed service and wraps it with decorators
3. Decorators are applied in ascending order (innermost to outermost)
4. Open generic decorators are closed at runtime via `MakeGenericType`

## Testing Approach

### Test Case Organization

Tests live in `/test/Cases/{NNN}_{Description}/` directories:
- Each case contains multiple .cs files (e.g., Repository.cs, Program.cs)
- Cases are excluded from compilation but copied to output directory
- `GlobalUsings.g.cs` applied to all test cases

### Test Infrastructure

**GeneratorTestHelpers.cs**:
- `RunFromCases()`: Core harness that creates compilation, adds references, runs generator
- `RecompileWithGeneratedTrees()`: Re-parses generated code to validate correctness
- Multi-framework BCL references (Net80/90/100) for accurate testing

**Test Patterns**:
- Use `[GeneratorAutoData]` attribute for parameterized tests
- Call `VerifyGlue.VerifySourcesAsync()` to run generator and assert no compilation errors
- Snapshot testing infrastructure present (currently validates via compilation success)

### Adding New Test Cases

1. Create directory `/test/Cases/{NextNumber}_{Description}/`
2. Add source files demonstrating the scenario
3. Create xunit test method with `[GeneratorAutoData]`
4. Call `VerifyGlue.VerifySourcesAsync(generator, casePaths)`

## Development Workflow

### Working on Generator Code

1. Modify files in `/src/LayeredCraft.DecoWeaver.Generators/`
2. Key files to understand:
   - `DecoWeaverGenerator.cs`: Main pipeline orchestration
   - `Providers/`: Syntax/semantic analysis for discovery
   - `Emit/InterceptorEmitter.cs`: Code generation logic
   - `Model/`: Data structures passed through pipeline
3. Run tests to validate changes
4. Debug builds emit generated files to `obj/Debug/netstandard2.0/generated/`

### Code Organization

- `/Providers/`: Discovery logic (predicates + transformers)
- `/Model/`: Immutable data structures for pipeline
- `/Emit/`: Code generation
- `/Roslyn/`: Roslyn API adapters and type system handling
- `/Util/`: Shared utilities (EquatableArray)

### Debugging

- Attach debugger to test process
- Use `TrackingNames` constants to identify pipeline stages in debugger
- Check `obj/` output directory for generated files when debugging generator itself
- Set breakpoints in provider transformers to inspect semantic analysis

## Important Technical Details

### C# Interceptors Feature

- Requires C# 11+ (experimental feature)
- `[InterceptsLocation(version: 1, data: "file|start|length")]` redirects specific call sites
- File-scoped types (`file sealed class`) prevent collisions
- Multiple `[InterceptsLocation]` attributes on one method intercept multiple call sites

### Keyed Services Strategy

- Keyed services (NET 8+) prevent infinite recursion during resolution
- Key format: `"{ServiceAssemblyQualifiedName}|{ImplAssemblyQualifiedName}"`
- Undecorated implementation registered with key
- Factory registration wraps keyed service with decorators

### Open Generic Support

- Generator discovers open generic registrations: `AddScoped(typeof(IRepo<>), typeof(SqlRepo<>))`
- Decorator types can be open generics: `[DecoratedBy<CachingRepo<>>]`
- Runtime closing via `MakeGenericType` when service is resolved
- Type arguments extracted from the service type being resolved

### Attribute Compilation

- Attributes marked with `[Conditional("DECOWEAVER_EMIT_ATTRIBUTE_METADATA")]`
- Don't exist at runtime - zero metadata footprint
- Only affect compile-time behavior

## Requirements

- .NET SDK with C# 11+ support
- Consuming projects must set `<LangVersion>` to 11, latest, or preview
- Keyed services feature requires .NET 8+ runtime
- Test project requires .NET 8 SDK

## Naming Conventions

- Diagnostics: `DECOW###` prefix
- Tracking names: `Category_Subcategory_Action` format
- Type identity: `TypeDefId` (definition), `TypeId` (with args)
- Error handling: Return `null` from transformers on invalid input