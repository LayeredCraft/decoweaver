using DecoWeaver.Model;
using DecoWeaver.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DecoWeaver.Providers;

internal readonly record struct ClosedGenericRegistration(
    TypeDefId ServiceDef,
    TypeDefId ImplDef,
    string ServiceFqn, // Fully qualified closed type (e.g., "global::DecoWeaver.Sample.IRepository<global::DecoWeaver.Sample.Customer>")
    string ImplFqn, // Fully qualified closed type
    string Lifetime, // "AddTransient" | "AddScoped" | "AddSingleton"
    string InterceptsData // "file|start|length"
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

        // Must be generic method: AddScoped<TService, TImplementation>()
        // Use the original symbol (not OriginalDefinition) to get the constructed type arguments
        if (!symbol.IsGenericMethod || symbol.TypeArguments.Length != 2) return null;

        // Only match the parameterless overload: AddScoped<T1, T2>(this IServiceCollection services)
        // Reject factory delegates, keyed services, or instance registrations
        // symbolToCheck is the unreduced (static extension) form, so it has IServiceCollection as first parameter
        if (symbolToCheck.Parameters.Length != 1) return null;

        var svc = symbol.TypeArguments[0];
        var impl = symbol.TypeArguments[1];

        if (svc is not INamedTypeSymbol svcNamed || impl is not INamedTypeSymbol implNamed) return null;

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
            ServiceDef: svc.ToTypeId().Definition,
            ImplDef: impl.ToTypeId().Definition,
            ServiceFqn: serviceFqn,
            ImplFqn: implFqn,
            Lifetime: name,
            InterceptsData: il.Data
        );
    }
}
