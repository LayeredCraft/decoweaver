using DecoWeaver.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DecoWeaver.Providers;

internal enum RegistrationKind
{
    /// <summary>Parameterless: AddScoped&lt;T1, T2&gt;()</summary>
    Parameterless,
    /// <summary>Factory with two type params: AddScoped&lt;T1, T2&gt;(Func&lt;IServiceProvider, T2&gt;)</summary>
    FactoryTwoTypeParams,
    /// <summary>Factory with single type param: AddScoped&lt;T&gt;(Func&lt;IServiceProvider, T&gt;)</summary>
    FactorySingleTypeParam,
    /// <summary>Keyed parameterless: AddKeyedScoped&lt;T1, T2&gt;(object? serviceKey)</summary>
    KeyedParameterless,
    /// <summary>Keyed factory with two type params: AddKeyedScoped&lt;T1, T2&gt;(object? serviceKey, Func&lt;IServiceProvider, object?, T2&gt;)</summary>
    KeyedFactoryTwoTypeParams,
    /// <summary>Keyed factory with single type param: AddKeyedScoped&lt;T&gt;(object? serviceKey, Func&lt;IServiceProvider, object?, T&gt;)</summary>
    KeyedFactorySingleTypeParam
}

internal readonly record struct ClosedGenericRegistration(
    TypeDefId ServiceDef,
    TypeDefId ImplDef,
    string ServiceFqn, // Fully qualified closed type (e.g., "global::DecoWeaver.Sample.IRepository<global::DecoWeaver.Sample.Customer>")
    string ImplFqn, // Fully qualified closed type
    string Lifetime, // "AddTransient" | "AddScoped" | "AddSingleton" | "AddKeyedTransient" | "AddKeyedScoped" | "AddKeyedSingleton"
    string InterceptsData, // "file|start|length"
    RegistrationKind Kind = RegistrationKind.Parameterless,
    string? FactoryParameterName = null, // Parameter name from the original registration (e.g., "implementationFactory")
    string? ServiceKeyParameterName = null // Parameter name for keyed services (e.g., "serviceKey")
);

internal readonly record struct RegistrationValidationResult(
    RegistrationKind Kind,
    string? FactoryParameterName,
    string? ServiceKeyParameterName
);

/// <summary>
/// Discovers closed generic registrations like services.AddScoped&lt;IRepo&lt;T&gt;, SqlRepo&lt;T&gt;&gt;()
/// Open generic registrations with typeof() are NOT supported - decorators only work with closed types.
/// </summary>
internal static class ClosedGenericRegistrationProvider
{
    private static readonly string[] AllowedContainingTypes =
    [
        "Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions",
        "Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions"
    ];

    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is InvocationExpressionSyntax;

    internal static ClosedGenericRegistration? Transformer(GeneratorSyntaxContext ctx, CancellationToken _)
    {
        if (ctx.Node is not InvocationExpressionSyntax inv) return null;

        var symbol = ctx.SemanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
        if (symbol is null) return null;

        // For extension methods, we want the unreduced (static) form to check the containing type
        var symbolToCheck = symbol.ReducedFrom ?? symbol;

        // Allow both DI extension containers
        if (!AllowedContainingTypes.Contains(symbolToCheck.ContainingType?.ToDisplayString()))
            return null;

        var name = symbol.Name;
        var isKeyed = name is "AddKeyedTransient" or "AddKeyedScoped" or "AddKeyedSingleton";
        var isNonKeyed = name is "AddTransient" or "AddScoped" or "AddSingleton";

        if (!isKeyed && !isNonKeyed) return null;

        // Must be generic method with 1 or 2 type arguments
        if (!symbol.IsGenericMethod) return null;
        if (symbol.TypeArguments.Length is not (1 or 2)) return null;

        // Validate registration pattern and extract metadata
        var validationResult = isKeyed
            ? ValidateKeyedRegistration(symbolToCheck, symbol)
            : ValidateNonKeyedRegistration(symbolToCheck, symbol);

        if (validationResult is null) return null;

        // Extract service and implementation types
        var (svc, impl) = ExtractServiceAndImplementationTypes(symbol);
        if (svc is null || impl is null) return null;

        // Get interceptable location
        var interceptsData = GetInterceptsData(ctx.SemanticModel, inv);
        if (interceptsData is null) return null;

        // Generate fully qualified names for the closed types
        var serviceFqn = svc.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implFqn = impl.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new ClosedGenericRegistration(
            ServiceDef: TypeId.Create(svc).Definition,
            ImplDef: TypeId.Create(impl).Definition,
            ServiceFqn: serviceFqn,
            ImplFqn: implFqn,
            Lifetime: name,
            InterceptsData: interceptsData,
            Kind: validationResult.Value.Kind,
            FactoryParameterName: validationResult.Value.FactoryParameterName,
            ServiceKeyParameterName: validationResult.Value.ServiceKeyParameterName
        );
    }

    private static RegistrationValidationResult? ValidateNonKeyedRegistration(
        IMethodSymbol symbolToCheck,
        IMethodSymbol symbol)
    {
        // NON-KEYED SERVICES
        // Accept:
        // - 1 param: Parameterless (IServiceCollection services)
        // - 2 params: Factory delegate (IServiceCollection services, Func<IServiceProvider, T> implementationFactory)
        var paramCount = symbolToCheck.Parameters.Length;
        if (paramCount is not (1 or 2)) return null;

        if (paramCount == 1)
        {
            // Parameterless registration: AddScoped<T1, T2>()
            if (symbol.TypeArguments.Length != 2) return null;
            return new RegistrationValidationResult(RegistrationKind.Parameterless, null, null);
        }

        // Factory delegate registration (paramCount == 2)
        var factoryParam = symbolToCheck.Parameters[1];
        if (!ValidateFactoryDelegate(factoryParam, expectedArgCount: 2, out var funcReturnType))
            return null;

        var factoryParamName = factoryParam.Name;

        if (symbol.TypeArguments.Length == 2)
        {
            // AddScoped<TService, TImplementation>(Func<IServiceProvider, TImplementation>)
            return new RegistrationValidationResult(
                RegistrationKind.FactoryTwoTypeParams,
                factoryParamName,
                null);
        }

        // AddScoped<TService>(Func<IServiceProvider, TService>)
        // Verify factory return type matches TService (only type arg)
        var svcTypeArg = symbol.TypeArguments[0];
        if (!SymbolEqualityComparer.Default.Equals(funcReturnType, svcTypeArg))
            return null;

        return new RegistrationValidationResult(
            RegistrationKind.FactorySingleTypeParam,
            factoryParamName,
            null);
    }

    private static RegistrationValidationResult? ValidateKeyedRegistration(
        IMethodSymbol symbolToCheck,
        IMethodSymbol symbol)
    {
        // KEYED SERVICES
        // Accept:
        // - 2 params: Keyed parameterless (IServiceCollection services, object? serviceKey)
        // - 3 params: Keyed factory delegate (IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, T>)
        var paramCount = symbolToCheck.Parameters.Length;
        if (paramCount is not (2 or 3)) return null;

        // Second parameter must be the service key (object?)
        var keyParam = symbolToCheck.Parameters[1];
        if (keyParam.Type.SpecialType != SpecialType.System_Object) return null;
        var serviceKeyParamName = keyParam.Name;

        if (paramCount == 2)
        {
            // Keyed parameterless: AddKeyedScoped<T1, T2>(key)
            if (symbol.TypeArguments.Length != 2) return null;
            return new RegistrationValidationResult(
                RegistrationKind.KeyedParameterless,
                null,
                serviceKeyParamName);
        }

        // Keyed factory delegate registration (paramCount == 3)
        var factoryParam = symbolToCheck.Parameters[2];
        if (!ValidateKeyedFactoryDelegate(factoryParam, out var funcReturnType))
            return null;

        var factoryParamName = factoryParam.Name;

        if (symbol.TypeArguments.Length == 2)
        {
            // AddKeyedScoped<TService, TImplementation>(key, Func<IServiceProvider, object?, TImplementation>)
            return new RegistrationValidationResult(
                RegistrationKind.KeyedFactoryTwoTypeParams,
                factoryParamName,
                serviceKeyParamName);
        }

        // AddKeyedScoped<TService>(key, Func<IServiceProvider, object?, TService>)
        // Verify factory return type matches TService (only type arg)
        var svcTypeArg = symbol.TypeArguments[0];
        if (!SymbolEqualityComparer.Default.Equals(funcReturnType, svcTypeArg))
            return null;

        return new RegistrationValidationResult(
            RegistrationKind.KeyedFactorySingleTypeParam,
            factoryParamName,
            serviceKeyParamName);
    }

    private static bool ValidateFactoryDelegate(
        IParameterSymbol factoryParam,
        int expectedArgCount,
        out ITypeSymbol? funcReturnType)
    {
        funcReturnType = null;

        // Check if parameter type is Func<IServiceProvider, TReturn>
        if (factoryParam.Type is not INamedTypeSymbol funcType) return false;
        if (funcType.OriginalDefinition.ToDisplayString() != "System.Func<T, TResult>") return false;
        if (funcType.TypeArguments.Length != expectedArgCount) return false;

        var funcArgType = funcType.TypeArguments[0];

        // First arg must be IServiceProvider
        if (funcArgType.ToDisplayString() != "System.IServiceProvider") return false;

        funcReturnType = funcType.TypeArguments[1];
        return true;
    }

    private static bool ValidateKeyedFactoryDelegate(
        IParameterSymbol factoryParam,
        out ITypeSymbol? funcReturnType)
    {
        funcReturnType = null;

        // Check if parameter type is Func<IServiceProvider, object?, TReturn>
        if (factoryParam.Type is not INamedTypeSymbol funcType) return false;
        if (funcType.OriginalDefinition.ToDisplayString() != "System.Func<T1, T2, TResult>") return false;
        if (funcType.TypeArguments.Length != 3) return false;

        var funcArg1Type = funcType.TypeArguments[0];
        var funcArg2Type = funcType.TypeArguments[1];

        // First arg must be IServiceProvider
        if (funcArg1Type.ToDisplayString() != "System.IServiceProvider") return false;

        // Second arg must be object? (the key)
        if (funcArg2Type.SpecialType != SpecialType.System_Object) return false;

        funcReturnType = funcType.TypeArguments[2];
        return true;
    }

    private static (INamedTypeSymbol? Service, INamedTypeSymbol? Implementation) ExtractServiceAndImplementationTypes(
        IMethodSymbol symbol)
    {
        if (symbol.TypeArguments.Length == 2)
        {
            var svc = symbol.TypeArguments[0] as INamedTypeSymbol;
            var impl = symbol.TypeArguments[1] as INamedTypeSymbol;
            return (svc, impl);
        }

        // For single type param factory: AddScoped<T>(factory)
        // Both service and implementation are the same type
        var type = symbol.TypeArguments[0] as INamedTypeSymbol;
        return (type, type);
    }

    private static string? GetInterceptsData(SemanticModel semanticModel, InvocationExpressionSyntax inv)
    {
        // Hash-based interceptable location (Roslyn experimental)
#pragma warning disable RSEXPERIMENTAL002
        var il = semanticModel.GetInterceptableLocation(inv);
#pragma warning restore RSEXPERIMENTAL002
        return il?.Data;
    }
}