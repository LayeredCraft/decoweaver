// DecoWeaver/Emit/InterceptorEmitter.cs

using DecoWeaver.Model;
using DecoWeaver.OutputGenerators;
using DecoWeaver.Providers;
using DecoWeaver.Util;
using Microsoft.CodeAnalysis.CSharp;

namespace DecoWeaver.Emit;

/// <summary>Emits the interceptor source for DecoWeaver's open-generic decoration rewrite.</summary>
internal static class InterceptorEmitter
{
    public static string EmitClosedGenericInterceptors(
        EquatableArray<ClosedGenericRegistration> registrations,
        Dictionary<TypeDefId, EquatableArray<TypeDefId>> decoratorsByImplementation)
    {
        // Group registrations by lifetime to preserve ordering
        var byLifetime = registrations
            .GroupBy(r => r.Lifetime)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Convert all registrations to template models
        var models = new List<DecoWeaverInterceptorsSources.RegistrationModel>();
        var methodIndex = 0;

        foreach (var (lifetime, regs) in byLifetime)
        {
            foreach (var reg in regs)
            {
                var decorators = decoratorsByImplementation.TryGetValue(reg.ImplDef, out var decos) && decos.Count > 0
                    ? decos.Select(ToFqn).ToArray()
                    : Array.Empty<string>();

                models.Add(DecoWeaverInterceptorsSources.CreateRegistrationModel(
                    reg, lifetime, methodIndex++, decorators, Escape(reg.InterceptsData)));
            }
        }

        // Use the unified template for ALL registrations
        return DecoWeaverInterceptorsSources.Generate(models);
    }

    private static string ToFqn(TypeDefId t)
    {
        var ns = t.ContainingNamespaces is { Count: > 0 } ? string.Join(".", t.ContainingNamespaces) : null;
        var nest = t.ContainingTypes is { Count: > 0 } ? string.Join("+", t.ContainingTypes) : null;
        var head = ns is null ? "" : ns + ".";
        if (!string.IsNullOrEmpty(nest)) head += nest + ".";

        // Strip backtick notation (e.g., "DynamoDbRepository`1" -> "DynamoDbRepository")
        var metadataName = t.MetadataName;
        var backtickIndex = metadataName.IndexOf('`');
        if (backtickIndex >= 0)
            metadataName = metadataName.Substring(0, backtickIndex);

        // Add <> for open generic types with commas for multiple type parameters
        // Examples: Repository<>, Dictionary<,>, SomeType<,,>
        if (t.Arity > 0)
        {
            var commas = new string(',', t.Arity - 1);
            metadataName = $"{metadataName}<{commas}>";
        }

        // Prepend global:: to avoid namespace conflicts in generated code
        return $"global::{head}{metadataName}";
    }

    private static string Escape(string s) =>
        SymbolDisplay.FormatLiteral(s, quote: true);
}
