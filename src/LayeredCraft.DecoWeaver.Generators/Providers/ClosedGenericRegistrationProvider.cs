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
    FactorySingleTypeParam
}

internal readonly record struct ClosedGenericRegistration(
    TypeDefId ServiceDef,
    TypeDefId ImplDef,
    string ServiceFqn, // Fully qualified closed type (e.g., "global::DecoWeaver.Sample.IRepository<global::DecoWeaver.Sample.Customer>")
    string ImplFqn, // Fully qualified closed type
    string Lifetime, // "AddTransient" | "AddScoped" | "AddSingleton"
    string InterceptsData, // "file|start|length"
    RegistrationKind Kind = RegistrationKind.Parameterless,
    string? FactoryParameterName = null // Parameter name from the original registration (e.g., "implementationFactory")
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
        if (name is not ("AddTransient" or "AddScoped" or "AddSingleton")) return null;

        // Must be generic method with 1 or 2 type arguments
        // 2 type args: AddScoped<TService, TImplementation>() or AddScoped<TService, TImplementation>(factory)
        // 1 type arg: AddScoped<TService>(factory)
        if (!symbol.IsGenericMethod) return null;
        if (symbol.TypeArguments.Length is not (1 or 2)) return null;

        // symbolToCheck is the unreduced (static extension) form, so it has IServiceCollection as first parameter
        // Accept:
        // - 1 param: Parameterless (IServiceCollection services)
        // - 2 params: Factory delegate (IServiceCollection services, Func<IServiceProvider, T> implementationFactory)
        // Reject: Keyed services, instance registrations, non-generic overloads
        var paramCount = symbolToCheck.Parameters.Length;
        if (paramCount is not (1 or 2)) return null;

        RegistrationKind kind;
        string? factoryParamName = null;

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

                // Verify factory return type matches TImplementation (second type arg)
                var implTypeArg = symbol.TypeArguments[1];
                if (!SymbolEqualityComparer.Default.Equals(funcReturnType, implTypeArg))
                {
                    // Some overloads allow Func<IServiceProvider, TService> instead of TImplementation
                    // This is valid, but we use the TImplementation from type args for decorator matching
                }
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
            FactoryParameterName: factoryParamName
        );
    }
}
