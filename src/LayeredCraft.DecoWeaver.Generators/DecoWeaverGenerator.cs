using System.Collections.Immutable;
using DecoWeaver.Emit;
using DecoWeaver.Model;
using DecoWeaver.Providers;
using DecoWeaver.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DecoWeaver;

[Generator]
public sealed class DecoWeaverGenerator : IIncrementalGenerator
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
                AttributeNames.GenericDecoratedByAttribute,
                predicate: DecoratedByGenericProvider.Predicate,
                transform: DecoratedByGenericProvider.TransformMultiple)
            .SelectMany(static (decorators, _) => decorators.ToImmutableArray()) // Flatten IEnumerable<DecoratorToIntercept?>
            .WithTrackingName(TrackingNames.Attr_Generic_Transform)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Attr_Generic_FilterNotNull)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName(TrackingNames.Attr_Generic_Stream);

        // --- [DecoratedBy(typeof(...))] non-generic stream -------------------
        // Use ForAttributeWithMetadataName to handle multiple [DecoratedBy] attributes on the same class
        var nonGenericDecorations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeNames.DecoratedByAttribute,
                predicate: DecoratedByNonGenericProvider.Predicate,
                transform: DecoratedByNonGenericProvider.TransformMultiple)
            .SelectMany(static (decorators, _) => decorators.ToImmutableArray()) // Flatten IEnumerable<DecoratorToIntercept?>
            .WithTrackingName(TrackingNames.Attr_NonGeneric_Transform)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Attr_NonGeneric_FilterNotNull)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName(TrackingNames.Attr_NonGeneric_Stream);
        
        // --- [assembly: DecorateService(...)] stream ----------------------
        // Discovers assembly-level decorator declarations that apply to all implementations
        // of a service type within the same assembly. This provides default decoration rules
        // that can be overridden or opted-out of at the class level.
        var serviceDecorations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeNames.ServiceDecoratedByAttribute,
                predicate: ServiceDecoratedByProvider.Predicate,
                transform: ServiceDecoratedByProvider.TransformMultiple)
            .SelectMany(static (decorations, _) => decorations.ToImmutableArray()) // Flatten IEnumerable<ServiceDecoration?>
            .WithTrackingName(TrackingNames.Attr_ServiceDecoration_Transform)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Attr_ServiceDecoration_FilterNotNull)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName(TrackingNames.Attr_ServiceDecoration_Stream);
        
        // --- [SkipAssemblyDecorators] stream --------------------------------
        // Discovers implementations that have opted out of all assembly-level decorations.
        // These markers are used to filter out assembly-level rules during the merge phase,
        // while still allowing class-level decorations to be applied.
        var skipAssemblyDecorations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeNames.SkipAssemblyDecorationAttribute,
                predicate: SkipAssemblyDecoratorProvider.Predicate,
                transform: SkipAssemblyDecoratorProvider.Transform)
            .WithTrackingName(TrackingNames.Attr_SkipAssemblyDecoration_Transform)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Attr_SkipAssemblyDecoration_FilterNotNull)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName(TrackingNames.Attr_SkipAssemblyDecoration_Stream);
            
        
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
        
        var serviceGated = serviceDecorations
            .Combine(csharpSufficient)
            .Where(static pair => pair.Right)
            .Select(static (pair, _) => pair.Left)
            .WithTrackingName(TrackingNames.Gate_Decorations_Service);
        
        var skipAssemblyGated = skipAssemblyDecorations
            .Combine(csharpSufficient)
            .Where(static pair => pair.Right)
            .Select(static (pair, _) => pair.Left)
            .WithTrackingName(TrackingNames.Gate_Decorations_SkipAssembly);

        // Collect both class-level decoration streams (generic and non-generic)
        var classDecos = genericGated.Collect()
            .Combine(nonGenericGated.Collect())
            .Select(static (p, _) => p.Left.AddRange(p.Right));

        // Combine class-level and assembly-level decorations for the merge phase
        var allDecorations = classDecos
            .Combine(serviceGated.Collect())
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

        var skipAssemblyCollected = skipAssemblyGated
            .Collect()
            .WithTrackingName(TrackingNames.Reg_SkipAssembly_Collect);
        
        // Emit once we have decorations + registrations
        context.RegisterSourceOutput(
            allDecorations
                .Combine(skipAssemblyCollected)
                .Combine(closedGenericRegsCollected)
                .WithTrackingName(TrackingNames.Emit_ClosedGenericInterceptors),
            static (spc, pair) =>
            {
                var ((classDecos, serviceDecos), skipMarkers) = pair.Left;
                var regs = pair.Right;

                // Build a fast lookup of implementations that opted out via [SkipAssemblyDecorators]
                var skipped = new HashSet<TypeDefId>(skipMarkers.Select(m => m.ImplementationDef));

                // Apply SkipAssemblyDecorators: filter ONLY assembly-level decorations.
                // Class-level decorations are never filtered - they always apply regardless of opt-out status.
                var filteredServiceDecos = serviceDecos
                    .Where(d => !skipped.Contains(d.ImplementationDef))
                    .ToImmutableArray();

                // Merge class-level and (filtered) assembly-level decorations
                var allDecos = classDecos.AddRange(filteredServiceDecos);
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

                spc.AddSource("DecoWeaver.Interceptors.ClosedGenerics.g.cs", source);
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
