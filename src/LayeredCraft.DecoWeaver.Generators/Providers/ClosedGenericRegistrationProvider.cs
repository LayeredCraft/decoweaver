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
        bool isKeyed = name is "AddKeyedTransient" or "AddKeyedScoped" or "AddKeyedSingleton";
        bool isNonKeyed = name is "AddTransient" or "AddScoped" or "AddSingleton";

        if (!isKeyed && !isNonKeyed) return null;

        // Must be generic method with 1 or 2 type arguments
        // 2 type args: AddScoped<TService, TImplementation>() or AddKeyedScoped<TService, TImplementation>(key)
        // 1 type arg: AddScoped<TService>(factory) or AddKeyedScoped<TService>(key, factory)
        if (!symbol.IsGenericMethod) return null;
        if (symbol.TypeArguments.Length is not (1 or 2)) return null;

        // symbolToCheck is the unreduced (static extension) form, so it has IServiceCollection as first parameter
        var paramCount = symbolToCheck.Parameters.Length;

        RegistrationKind kind;
        string? factoryParamName = null;
        string? serviceKeyParamName = null;

        if (!isKeyed)
        {
            // NON-KEYED SERVICES
            // Accept:
            // - 1 param: Parameterless (IServiceCollection services)
            // - 2 params: Factory delegate (IServiceCollection services, Func<IServiceProvider, T> implementationFactory)
            if (paramCount is not (1 or 2)) return null;

            if (paramCount == 1)
            {
                // Parameterless registration: AddScoped<T1, T2>()
                // Must have 2 type arguments
                if (symbol.TypeArguments.Length != 2) return null;
                kind = RegistrationKind.Parameterless;
            }
            else // paramCount == 2
            {
                // Factory delegate registration
                // Second parameter must be Func<IServiceProvider, T>
                var factoryParam = symbolToCheck.Parameters[1];

                // Check if parameter type is Func<IServiceProvider, TReturn>
                if (factoryParam.Type is not INamedTypeSymbol funcType) return null;
                if (funcType.OriginalDefinition.ToDisplayString() != "System.Func<T, TResult>") return null;
                if (funcType.TypeArguments.Length != 2) return null;

                var funcArgType = funcType.TypeArguments[0];
                var funcReturnType = funcType.TypeArguments[1];

                // First arg must be IServiceProvider
                if (funcArgType.ToDisplayString() != "System.IServiceProvider") return null;

                factoryParamName = factoryParam.Name;

                if (symbol.TypeArguments.Length == 2)
                {
                    // AddScoped<TService, TImplementation>(Func<IServiceProvider, TImplementation>)
                    kind = RegistrationKind.FactoryTwoTypeParams;
                }
                else // symbol.TypeArguments.Length == 1
                {
                    // AddScoped<TService>(Func<IServiceProvider, TService>)
                    kind = RegistrationKind.FactorySingleTypeParam;

                    // Verify factory return type matches TService (only type arg)
                    var svcTypeArg = symbol.TypeArguments[0];
                    if (!SymbolEqualityComparer.Default.Equals(funcReturnType, svcTypeArg))
                        return null; // Invalid signature
                }
            }
        }
        else
        {
            // KEYED SERVICES
            // Accept:
            // - 2 params: Keyed parameterless (IServiceCollection services, object? serviceKey)
            // - 3 params: Keyed factory delegate (IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, T> implementationFactory)
            if (paramCount is not (2 or 3)) return null;

            // Second parameter must be the service key (object?)
            var keyParam = symbolToCheck.Parameters[1];
            if (keyParam.Type.SpecialType != SpecialType.System_Object) return null;
            serviceKeyParamName = keyParam.Name;

            if (paramCount == 2)
            {
                // Keyed parameterless: AddKeyedScoped<T1, T2>(key)
                // Must have 2 type arguments
                if (symbol.TypeArguments.Length != 2) return null;
                kind = RegistrationKind.KeyedParameterless;
            }
            else // paramCount == 3
            {
                // Keyed factory delegate registration
                // Third parameter must be Func<IServiceProvider, object?, T>
                var factoryParam = symbolToCheck.Parameters[2];

                // Check if parameter type is Func<IServiceProvider, object?, TReturn>
                if (factoryParam.Type is not INamedTypeSymbol funcType) return null;
                if (funcType.OriginalDefinition.ToDisplayString() != "System.Func<T1, T2, TResult>") return null;
                if (funcType.TypeArguments.Length != 3) return null;

                var funcArg1Type = funcType.TypeArguments[0];
                var funcArg2Type = funcType.TypeArguments[1];
                var funcReturnType = funcType.TypeArguments[2];

                // First arg must be IServiceProvider
                if (funcArg1Type.ToDisplayString() != "System.IServiceProvider") return null;

                // Second arg must be object? (the key)
                if (funcArg2Type.SpecialType != SpecialType.System_Object) return null;

                factoryParamName = factoryParam.Name;

                if (symbol.TypeArguments.Length == 2)
                {
                    // AddKeyedScoped<TService, TImplementation>(key, Func<IServiceProvider, object?, TImplementation>)
                    kind = RegistrationKind.KeyedFactoryTwoTypeParams;
                }
                else // symbol.TypeArguments.Length == 1
                {
                    // AddKeyedScoped<TService>(key, Func<IServiceProvider, object?, TService>)
                    kind = RegistrationKind.KeyedFactorySingleTypeParam;

                    // Verify factory return type matches TService (only type arg)
                    var svcTypeArg = symbol.TypeArguments[0];
                    if (!SymbolEqualityComparer.Default.Equals(funcReturnType, svcTypeArg))
                        return null; // Invalid signature
                }
            }
        }

        // Extract service and implementation types
        INamedTypeSymbol? svc, impl;

        if (symbol.TypeArguments.Length == 2)
        {
            svc = symbol.TypeArguments[0] as INamedTypeSymbol;
            impl = symbol.TypeArguments[1] as INamedTypeSymbol;
        }
        else // symbol.TypeArguments.Length == 1
        {
            // For single type param factory: AddScoped<T>(factory)
            // Both service and implementation are the same type
            svc = symbol.TypeArguments[0] as INamedTypeSymbol;
            impl = svc;
        }

        if (svc is null || impl is null) return null;

        // Hash-based interceptable location (Roslyn experimental)
#pragma warning disable RSEXPERIMENTAL002
        var il = ctx.SemanticModel.GetInterceptableLocation(inv);
#pragma warning restore RSEXPERIMENTAL002
        if (il is null) return null;

        // Generate fully qualified names for the closed types
        // FullyQualifiedFormat includes global:: for ALL types including nested generic type arguments
        var serviceFqn = svc.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implFqn = impl.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new ClosedGenericRegistration(
            ServiceDef: TypeId.Create(svc).Definition,
            ImplDef: TypeId.Create(impl).Definition,
            ServiceFqn: serviceFqn,
            ImplFqn: implFqn,
            Lifetime: name,
            InterceptsData: il.Data,
            Kind: kind,
            FactoryParameterName: factoryParamName,
            ServiceKeyParameterName: serviceKeyParamName
        );
    }
}
