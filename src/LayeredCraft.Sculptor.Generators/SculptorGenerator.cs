using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sculptor.Emit;          // NEW
using Sculptor.Model;
using Sculptor.Providers;
using Sculptor.Util;          // NEW (EquatableArray extensions)

namespace Sculptor;

[Generator]
public sealed class SculptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // --- Language version gate -----------------------------------------
        var csharpSufficient = context.CompilationProvider
            .Select(static (compilation, _) =>
                compilation is CSharpCompilation { LanguageVersion: var lv } &&
                (lv == LanguageVersion.Default || lv >= LanguageVersion.CSharp11))
            .WithTrackingName(TrackingNames.Settings_LanguageVersionGate);

        context.RegisterSourceOutput(
            csharpSufficient.WithTrackingName(TrackingNames.Diagnostics_CSharpVersion),
            static (spc, ok) =>
            {
                if (!ok)
                    spc.ReportDiagnostic(Diagnostic.Create(Descriptors.CSharpVersionTooLow, Location.None));
            });

        // --- [DecoratedBy<T>] generic stream --------------------------------
        var genericDecorations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Attributes.GenericDecoratedByAttribute,
                predicate: DecoratedByGenericProvider.Predicate,
                transform: DecoratedByGenericProvider.Transformer)
            .WithTrackingName(TrackingNames.Attr_Generic_Transform)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Attr_Generic_FilterNotNull)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName(TrackingNames.Attr_Generic_Stream);

        // --- [DecoratedBy(typeof(...))] non-generic stream -------------------
        var nonGenericDecorations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Attributes.DecoratedByAttribute,
                predicate: DecoratedByNonGenericProvider.Predicate,
                transform: DecoratedByNonGenericProvider.Transformer)
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

        // --- NEW: open-generic registration discovery -----------------------
        var openGenericRegs = context.SyntaxProvider
            .CreateSyntaxProvider(
                OpenGenericRegistrationProvider.Predicate,
                OpenGenericRegistrationProvider.Transformer)
            .WithTrackingName(TrackingNames.Reg_OpenGeneric_Transform)
            .Where(static r => r is not null)
            .WithTrackingName(TrackingNames.Reg_OpenGeneric_Filter)
            .Select(static (r, _) => r!.Value);

        // Gate & collect registrations
        var openGenericRegsCollected = openGenericRegs
            .Combine(csharpSufficient)
            .Where(static pair => pair.Right)
            .Select(static (pair, _) => pair.Left) // back to values
            .Collect()
            .WithTrackingName(TrackingNames.Reg_OpenGeneric_Collect);

// Emit once we have decorations + registrations
        context.RegisterSourceOutput(
            allDecorations.Combine(openGenericRegsCollected)
                .WithTrackingName(TrackingNames.Emit_OpenGenericInterceptors),
            static (spc, pair) =>
            {
                var (genericDecos, nonGenericDecos) = pair.Left;
                var regs = pair.Right;

                var allDecos = genericDecos.AddRange(nonGenericDecos);
                var byImpl = BuildDecorationMap(allDecos); // Dictionary<TypeDefId, EquatableArray<TypeDefId>>

                var source = InterceptorEmitter.EmitOpenGenericInterceptors(
                    registrations: regs.ToEquatableArray(),
                    decoratorsByImplementation: byImpl);

                spc.AddSource("Sculptor.Interceptors.OpenGenerics.g.cs", source);
            });
    }

    private static Dictionary<TypeDefId, Sculptor.Util.EquatableArray<TypeDefId>> BuildDecorationMap(
        ImmutableArray<DecoratorToIntercept> items)
    {
        var tmp = new Dictionary<TypeDefId, List<(int Order, TypeDefId Deco)>>();
        foreach (var d in items)
        {
            if (!d.IsInterceptable) continue;

            if (!tmp.TryGetValue(d.ImplementationDef, out var list))
                tmp[d.ImplementationDef] = list = new();

            list.Add((d.Order, d.DecoratorDef));
        }

        var result = new Dictionary<TypeDefId, Sculptor.Util.EquatableArray<TypeDefId>>(tmp.Count);
        foreach (var (impl, list) in tmp)
        {
            list.Sort(static (a, b) => a.Order.CompareTo(b.Order));
            var unique = list.Select(x => x.Deco).Distinct().ToList();
            result[impl] = new Sculptor.Util.EquatableArray<TypeDefId>(unique);
        }
        return result;
    }
}
