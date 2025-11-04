# Source Generators

DecoWeaver is built as an incremental source generator that analyzes your code at compile time and generates interceptor code. This page explains how source generators work and how DecoWeaver implements them.

## What are Source Generators?

Source generators are a compile-time metaprogramming feature in .NET that allows you to:

1. **Analyze** user code during compilation
2. **Generate** additional C# source files
3. **Add** generated code to the compilation

Source generators run as part of the build process and have access to the full Roslyn API for code analysis.

## DecoWeaver's Generator Pipeline

DecoWeaver implements an incremental source generator with the following pipeline:

```
1. Language Version Check
   ↓
2. Discover Decorated Types
   ├─ Generic Attributes: [DecoratedBy<T>]
   └─ Non-Generic Attributes: [DecoratedBy(typeof(T))]
   ↓
3. Discover DI Registrations
   ├─ AddScoped<TService, TImpl>()
   ├─ AddScoped(typeof(Service<>), typeof(Impl<>))
   └─ AddSingleton/AddTransient variants
   ↓
4. Match Types to Registrations
   ↓
5. Generate Interceptor Code
```

### Phase 1: Language Version Check

DecoWeaver first checks that the project uses C# 11+:

```csharp
// Simplified version
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    var languageVersion = context.ParseOptionsProvider
        .Select((options, _) => options.LanguageVersion());

    if (languageVersion < LanguageVersion.CSharp11)
    {
        context.ReportDiagnostic(/* Error: C# 11 required */);
        return;
    }

    // Continue with generation...
}
```

### Phase 2: Discover Decorated Types

DecoWeaver uses two parallel providers to find decorated types:

**Generic Attribute Provider**:
```csharp
var genericDecorators = context.SyntaxProvider
    .CreateSyntaxProvider(
        // Predicate: Fast syntax-only check
        predicate: (node, _) => node is ClassDeclarationSyntax c &&
            c.AttributeLists.Any(),

        // Transform: Semantic analysis
        transform: (ctx, _) =>
        {
            var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node);
            var attributes = classSymbol.GetAttributes()
                .Where(a => a.AttributeClass.Name == "DecoratedByAttribute" &&
                           a.AttributeClass.IsGenericType);

            return new
            {
                Implementation = classSymbol,
                Decorators = attributes.Select(GetDecoratorType)
            };
        });
```

**Non-Generic Attribute Provider**:
```csharp
var nonGenericDecorators = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: (node, _) => node is ClassDeclarationSyntax c &&
            c.AttributeLists.Any(),

        transform: (ctx, _) =>
        {
            var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node);
            var attributes = classSymbol.GetAttributes()
                .Where(a => a.AttributeClass.Name == "DecoratedByAttribute" &&
                           !a.AttributeClass.IsGenericType);

            return new
            {
                Implementation = classSymbol,
                Decorators = attributes.Select(a => a.ConstructorArguments[0].Value)
            };
        });
```

### Phase 3: Discover DI Registrations

Find all DI registration calls in the codebase:

```csharp
var registrations = context.SyntaxProvider
    .CreateSyntaxProvider(
        // Predicate: Find method invocations
        predicate: (node, _) => node is InvocationExpressionSyntax inv &&
            inv.Expression is MemberAccessExpressionSyntax mae &&
            mae.Name.Identifier.ValueText.StartsWith("Add"),

        // Transform: Extract service and implementation types
        transform: (ctx, _) =>
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetSymbolInfo(invocation).Symbol;

            if (symbol?.Name is "AddScoped" or "AddSingleton" or "AddTransient")
            {
                // Extract TService and TImplementation from generic arguments
                var method = (IMethodSymbol)symbol;
                var serviceType = method.TypeArguments[0];
                var implType = method.TypeArguments.Length > 1
                    ? method.TypeArguments[1]
                    : null;

                return new
                {
                    ServiceType = serviceType,
                    ImplementationType = implType,
                    Lifetime = symbol.Name, // "AddScoped", "AddSingleton", etc.
                    Location = invocation.GetLocation()
                };
            }

            return null;
        });
```

### Phase 4: Combine and Match

Combine decorated types with their registrations:

```csharp
var combined = genericDecorators
    .Combine(nonGenericDecorators)
    .Combine(registrations)
    .Select((data, _) =>
    {
        var (decorators, registrations) = data;

        // Match each registration to its decorators
        return registrations
            .Where(reg => HasDecorators(reg.ImplementationType, decorators))
            .Select(reg => new
            {
                Registration = reg,
                Decorators = GetDecoratorsFor(reg.ImplementationType, decorators)
                    .OrderBy(d => d.Order)
            });
    });
```

### Phase 5: Generate Code

Emit interceptor code for each decorated registration:

```csharp
context.RegisterSourceOutput(combined, (spc, decoratedRegistrations) =>
{
    var source = new StringBuilder();

    source.AppendLine("// <auto-generated/>");
    source.AppendLine("using Microsoft.Extensions.DependencyInjection;");
    source.AppendLine();
    source.AppendLine("file static class DecoWeaverInterceptors");
    source.AppendLine("{");

    foreach (var registration in decoratedRegistrations)
    {
        EmitInterceptor(source, registration);
    }

    source.AppendLine("}");

    spc.AddSource("DecoWeaver.Interceptors.g.cs", source.ToString());
});

void EmitInterceptor(StringBuilder sb, Registration registration)
{
    // Generate [InterceptsLocation] attribute
    var location = EncodeLocation(registration.Location);
    sb.AppendLine($"[InterceptsLocation(version: 1, data: \"{location}\")]");

    // Generate interceptor method
    sb.AppendLine($"public static IServiceCollection {GetMethodName(registration)}(");
    sb.AppendLine("    this IServiceCollection services)");
    sb.AppendLine("{");

    // Register keyed service
    sb.AppendLine($"    services.{registration.Lifetime}Keyed<{registration.ServiceType}, {registration.ImplementationType}>(");
    sb.AppendLine($"        \"{GetKey(registration)}\");");

    // Register factory with decorators
    sb.AppendLine($"    services.{registration.Lifetime}<{registration.ServiceType}>(sp =>");
    sb.AppendLine("    {");
    sb.AppendLine($"        var inner = sp.GetRequiredKeyedService<{registration.ServiceType}>(\"{GetKey(registration)}\");");

    // Apply decorators in order
    foreach (var decorator in registration.Decorators)
    {
        sb.AppendLine($"        inner = new {decorator.Type}(");
        sb.AppendLine("            inner,");

        // Resolve decorator dependencies
        foreach (var dep in decorator.Dependencies)
        {
            sb.AppendLine($"            sp.GetRequiredService<{dep}>(),");
        }

        sb.AppendLine("        );");
    }

    sb.AppendLine("        return inner;");
    sb.AppendLine("    });");
    sb.AppendLine();
    sb.AppendLine("    return services;");
    sb.AppendLine("}");
}
```

## Incremental Generation

DecoWeaver uses incremental generation for performance:

### Benefits

1. **Fast Builds**: Only regenerate when relevant code changes
2. **Editor Performance**: Minimal impact on IDE responsiveness
3. **Caching**: Results cached between builds

### How It Works

Incremental generators use a pipeline of transformations:

```csharp
var pipeline = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate,   // Fast syntax-only filter
        transform)   // Expensive semantic analysis
    .Collect()       // Combine results
    .Select()        // Transform
    .Where()         // Filter
    .Combine();      // Merge pipelines
```

Each stage is cached independently, so changes only invalidate affected stages.

### Performance Optimizations

**Syntax Predicates**:
```csharp
// ✅ Fast: Only checks syntax
predicate: (node, _) => node is ClassDeclarationSyntax c &&
    c.AttributeLists.Count > 0 &&
    c.AttributeLists.Any(al => al.Attributes.Any(
        a => a.Name.ToString().Contains("DecoratedBy")))

// ❌ Slow: Would require semantic model
predicate: (node, _) =>
{
    var symbol = semanticModel.GetDeclaredSymbol(node); // Don't do this
    return symbol.GetAttributes().Any();
}
```

**Equatable Types**:
```csharp
// Use equatable types for change detection
public record DecoratorInfo(
    string TypeName,
    int Order,
    EquatableArray<string> Dependencies);

// EquatableArray provides efficient equality comparison
```

## Diagnostics

DecoWeaver reports diagnostics for common errors:

```csharp
public static class Diagnostics
{
    public static DiagnosticDescriptor MissingCSharp11 = new(
        id: "SCULPT001",
        title: "C# 11 required",
        messageFormat: "DecoWeaver requires C# 11 or later. Set <LangVersion>11</LangVersion> in your project file.",
        category: "DecoWeaver",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DecoratorMissingInterface = new(
        id: "SCULPT002",
        title: "Decorator must implement service interface",
        messageFormat: "Decorator '{0}' does not implement '{1}'",
        category: "DecoWeaver",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DecoratorMissingConstructor = new(
        id: "SCULPT003",
        title: "Decorator must accept service interface in constructor",
        messageFormat: "Decorator '{0}' does not have a constructor accepting '{1}'",
        category: "DecoWeaver",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

// Report diagnostic
context.ReportDiagnostic(Diagnostic.Create(
    Diagnostics.MissingCSharp11,
    location));
```

## Debugging Source Generators

### Visual Studio

1. Set breakpoint in generator code
2. Right-click project → Properties → Debug
3. Launch profile: Roslyn Component
4. Start debugging (F5)

### Rider

1. Right-click generator project
2. Properties → Debug → Roslyn Component
3. Set breakpoints
4. Start debugging

### Logging

Add logging to generator:

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    context.RegisterSourceOutput(
        context.CompilationProvider,
        (spc, compilation) =>
        {
            var log = new StringBuilder();
            log.AppendLine("// Generator Debug Log");
            log.AppendLine($"// Compilation: {compilation.AssemblyName}");
            log.AppendLine($"// Language: {compilation.LanguageVersion}");

            spc.AddSource("Debug.log.g.cs", log.ToString());
        });
}
```

## Testing Generators

Test source generators with unit tests:

```csharp
[Fact]
public async Task GeneratesInterceptorForDecoratedType()
{
    // Arrange
    var source = @"
        using DecoWeaver.Attributes;

        [DecoratedBy<LoggingRepository>]
        public class UserRepository : IUserRepository { }

        public class LoggingRepository : IUserRepository
        {
            public LoggingRepository(IUserRepository inner) { }
        }
    ";

    var generator = new DecoWeaverGenerator();

    // Act
    var result = RunGenerator(source, generator);

    // Assert
    Assert.Single(result.GeneratedTrees);
    Assert.Contains("InterceptsLocation", result.GeneratedTrees[0].ToString());
    Assert.Contains("AddScoped", result.GeneratedTrees[0].ToString());
}
```

## Source Generator Best Practices

1. **Use incremental generation** for performance
2. **Keep predicates fast** - syntax-only checks
3. **Use equatable types** for change detection
4. **Report clear diagnostics** for user errors
5. **Handle edge cases** gracefully
6. **Test thoroughly** with unit tests
7. **Generate readable code** for debugging
8. **Document generated code** with comments

## Generator Output

DecoWeaver generates clean, readable code:

```csharp
// <auto-generated/>
// DecoWeaver v1.0.0
// https://github.com/layeredcraft/decoweaver

using System;
using System.CodeDom.Compiler;
using Microsoft.Extensions.DependencyInjection;

[GeneratedCode("DecoWeaver", "1.0.0")]
file static class DecoWeaverInterceptors
{
    [InterceptsLocation(version: 1, data: "Program.cs|245|67")]
    public static IServiceCollection AddScoped_IUserRepository_UserRepository(
        this IServiceCollection services)
    {
        // Register undecorated implementation as keyed service
        services.AddKeyedScoped<IUserRepository, UserRepository>(
            "IUserRepository|UserRepository");

        // Register factory that applies decorators
        services.AddScoped<IUserRepository>(sp =>
        {
            var inner = sp.GetRequiredKeyedService<IUserRepository>(
                "IUserRepository|UserRepository");

            // Apply LoggingRepository (Order = 1)
            inner = new LoggingRepository(
                inner,
                sp.GetRequiredService<ILogger<LoggingRepository>>());

            // Apply CachingRepository (Order = 2)
            inner = new CachingRepository(
                inner,
                sp.GetRequiredService<IMemoryCache>());

            return inner;
        });

        return services;
    }
}
```

## Next Steps

- Learn about [Interceptors](interceptors.md) in depth
- Understand [Testing Strategies](testing.md)
- See [How It Works](../core-concepts/how-it-works.md) overview