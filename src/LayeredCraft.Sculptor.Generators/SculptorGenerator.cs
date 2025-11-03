using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sculptor.Emit;
using Sculptor.Model;
using Sculptor.Providers;
using Sculptor.Util;

namespace Sculptor;

[Generator]
public sealed class SculptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // --- Language version gate -----------------------------------------
        var csharpSufficient = context.CompilationProvider
            .Select(static (compilation, _) =>
                compilation is CSharpCompilation { LanguageVersion: LanguageVersion.Default or >= LanguageVersion.CSharp11 })
            .WithTrackingName(TrackingNames.Settings_LanguageVersionGate);

        context.RegisterSourceOutput(
            csharpSufficient.WithTrackingName(TrackingNames.Diagnostics_CSharpVersion),
            static (spc, ok) =>
            {
                if (!ok)
                    spc.ReportDiagnostic(Diagnostic.Create(Descriptors.CSharpVersionTooLow, Location.None));
            });

        // --- [DecoratedBy<T>] generic stream --------------------------------
        // Use ForAttributeWithMetadataName to handle multiple [DecoratedBy<T>] attributes on the same class
        var genericDecorations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Attributes.GenericDecoratedByAttribute,
                predicate: DecoratedByGenericProvider.Predicate,
                transform: DecoratedByGenericProvider.TransformMultiple)
            .SelectMany(static (decorators, _) => decorators.ToImmutableArray()) // Flatten IEnumerable<DecoratorToIntercept?>
            .WithTrackingName(TrackingNames.Attr_Generic_Transform)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Attr_Generic_FilterNotNull)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName(TrackingNames.Attr_Generic_Stream);

        // --- [DecoratedBy(typeof(...))] non-generic stream -------------------
        // Use CreateSyntaxProvider to handle multiple [DecoratedBy] attributes on the same class
        var nonGenericDecorations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, ct) => DecoratedByNonGenericProvider.Predicate(node, ct),
                transform: static (ctx, ct) => DecoratedByNonGenericProvider.TransformMultiple(ctx, ct))
            .SelectMany(static (decorators, _) => decorators.ToImmutableArray()) // Flatten IEnumerable<DecoratorToIntercept?>
            .WithTrackingName(TrackingNames.Attr_NonGeneric_Transform)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Attr_NonGeneric_FilterNotNull)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName(TrackingNames.Attr_NonGeneric_Stream);

        // ✅ Gate each VALUES stream before Collect()
        var genericGated = genericDecorations
            .Combine(csharpSufficient)
            .Where(static pair => pair.Right)
            .Select(static (pair, _) => pair.Left)
            .WithTrackingName(TrackingNames.Gate_Decorations_Generic);

        var nonGenericGated = nonGenericDecorations
            .Combine(csharpSufficient)
            .Where(static pair => pair.Right)
            .Select(static (pair, _) => pair.Left)
            .WithTrackingName(TrackingNames.Gate_Decorations_NonGeneric);

        // Collect both decoration streams
        var allDecorations = genericGated.Collect()
            .Combine(nonGenericGated.Collect())
            .WithTrackingName(TrackingNames.Attr_All_Combined);

        // --- Closed generic registration discovery -----------------------
        var closedGenericRegs = context.SyntaxProvider
            .CreateSyntaxProvider(
                ClosedGenericRegistrationProvider.Predicate,
                ClosedGenericRegistrationProvider.Transformer)
            .WithTrackingName(TrackingNames.Reg_ClosedGeneric_Transform)
            .Where(static r => r is not null)
            .WithTrackingName(TrackingNames.Reg_ClosedGeneric_Filter)
            .Select(static (r, _) => r!.Value);

        // Gate & collect registrations
        var closedGenericRegsCollected = closedGenericRegs
            .Combine(csharpSufficient)
            .Where(static pair => pair.Right)
            .Select(static (pair, _) => pair.Left) // back to values
            .Collect()
            .WithTrackingName(TrackingNames.Reg_ClosedGeneric_Collect);

        // Emit once we have decorations + registrations
        context.RegisterSourceOutput(
            allDecorations.Combine(closedGenericRegsCollected)
                .WithTrackingName(TrackingNames.Emit_ClosedGenericInterceptors),
            static (spc, pair) =>
            {
                var (genericDecos, nonGenericDecos) = pair.Left;
                var regs = pair.Right;

                var allDecos = genericDecos.AddRange(nonGenericDecos);
                var byImpl = BuildDecorationMap(allDecos); // Dictionary<TypeDefId, EquatableArray<TypeDefId>>

                // Only emit interceptors for registrations that have decorators
                var regsWithDecorators = regs
                    .Where(r => byImpl.TryGetValue(r.ImplDef, out var decos) && decos.Count > 0)
                    .ToEquatableArray();

                // Skip emission entirely if there are no registrations with decorators
                if (regsWithDecorators.Count == 0)
                    return;

                var source = InterceptorEmitter.EmitClosedGenericInterceptors(
                    registrations: regsWithDecorators,
                    decoratorsByImplementation: byImpl);

                spc.AddSource("Sculptor.Interceptors.ClosedGenerics.g.cs", source);
            });
    }

    private static Dictionary<TypeDefId, EquatableArray<TypeDefId>> BuildDecorationMap(
        ImmutableArray<DecoratorToIntercept> items)
    {
        var tmp = new Dictionary<TypeDefId, List<(int Order, TypeDefId Deco)>>();
        foreach (var d in items.Where(d => d.IsInterceptable))
        {
            if (!tmp.TryGetValue(d.ImplementationDef, out var list))
                tmp[d.ImplementationDef] = list = new();

            list.Add((d.Order, d.DecoratorDef));
        }

        var result = new Dictionary<TypeDefId, EquatableArray<TypeDefId>>(tmp.Count);
        foreach (var (impl, list) in tmp)
        {
            list.Sort(static (a, b) => a.Order.CompareTo(b.Order));
            var unique = list.Select(x => x.Deco).Distinct();
            result[impl] = new EquatableArray<TypeDefId>(unique);
        }
        return result;
    }
}
