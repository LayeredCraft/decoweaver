using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sculptor.Model;
using Sculptor.Roslyn;

namespace Sculptor.Providers;

internal readonly record struct OpenGenericRegistration(
    TypeDefId ServiceDef,
    TypeDefId ImplDef,
    string Lifetime, // "AddTransient" | "AddScoped" | "AddSingleton"
    string InterceptsData // "file|start|length"
);

internal static class OpenGenericRegistrationProvider
{
    private static readonly string[] AllowedContainingTypes =
    [
        "Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions",
        "Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions"
    ];

    internal static bool Predicate(SyntaxNode node, CancellationToken _)
        => node is InvocationExpressionSyntax;

    internal static OpenGenericRegistration? Transformer(GeneratorSyntaxContext ctx, CancellationToken _)
    {
        if (ctx.Node is not InvocationExpressionSyntax inv) return null;

        var symbol = ctx.SemanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
        symbol = symbol?.ReducedFrom ?? symbol?.OriginalDefinition;
        if (symbol is null) return null;

        // Allow both DI extension containers
        if (!AllowedContainingTypes.Contains(symbol.ContainingType?.ToDisplayString()))
            return null;

        var name = symbol.Name;
        if (name is not ("AddTransient" or "AddScoped" or "AddSingleton")) return null;

        // Extension-call: expect 2 args (Type service, Type implementation)
        var args = inv.ArgumentList.Arguments;
        if (args.Count < 2) return null;

        var svc  = TryGetTypeOfArg(ctx.SemanticModel, inv, indexFromEnd: 1);
        var impl = TryGetTypeOfArg(ctx.SemanticModel, inv, indexFromEnd: 0);
        if (svc is null || impl is null) return null;

        if (svc is not INamedTypeSymbol svcNamed || impl is not INamedTypeSymbol implNamed) return null;

        // Only match open generics typeof(Foo<>)
        if (!svcNamed.IsUnboundGenericType || !implNamed.IsUnboundGenericType) return null;

        // Hash-based interceptable location (Roslyn experimental)
#pragma warning disable RSEXPERIMENTAL002
        var il = ctx.SemanticModel.GetInterceptableLocation(inv, default);
#pragma warning restore RSEXPERIMENTAL002
        if (il is null) return null;

        return new OpenGenericRegistration(
            ServiceDef: svc.ToTypeId().Definition,
            ImplDef:    impl.ToTypeId().Definition,
            Lifetime: name, // "AddTransient" | "AddScoped" | "AddSingleton"
            InterceptsData: il.Data
        );
    }

    private static ITypeSymbol? TryGetTypeOfArg(SemanticModel model, InvocationExpressionSyntax inv, int indexFromEnd)
    {
        var args = inv.ArgumentList.Arguments;
        var expr = args[^ (indexFromEnd + 1)].Expression;
        return expr is TypeOfExpressionSyntax tof ? model.GetTypeInfo(tof.Type).Type : null;
    }
}
