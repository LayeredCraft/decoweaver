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
                compilation is CSharpCompilation
                {
                    LanguageVersion: LanguageVersion.Default or >= LanguageVersion.CSharp11
                })
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
            .SelectMany(static (decorators, _) =>
                decorators.ToImmutableArray()) // Flatten IEnumerable<DecoratorToIntercept?>
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
            .SelectMany(static (decorators, _) =>
                decorators.ToImmutableArray()) // Flatten IEnumerable<DecoratorToIntercept?>
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
            .SelectMany(static (decorations, _) =>
                decorations.ToImmutableArray()) // Flatten IEnumerable<ServiceDecoration?>
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

        // Collect class-level decorations (generic + non-generic combined)
        var allDecorations = genericGated.Collect()
            .Combine(nonGenericGated.Collect())
            .WithTrackingName(TrackingNames.Attr_All_Combined);

        // Collect assembly-level service decorations separately (to be matched against registrations)
        var serviceDecosCollected = serviceGated.Collect()
            .WithTrackingName(TrackingNames.Attr_Service_Collected);


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

        // Combine all inputs using Select to create a clean named tuple structure
        var allInputs = allDecorations
            .Combine(serviceDecosCollected)
            .Combine(skipAssemblyCollected)
            .Combine(closedGenericRegsCollected)
            .Select(static (data, _) => (
                GenericDecos: data.Left.Left.Left.Left,
                NonGenericDecos: data.Left.Left.Left.Right,
                ServiceDecos: data.Left.Left.Right,
                SkipMarkers: data.Left.Right,
                Registrations: data.Right
            ));

        // Emit once we have decorations + registrations
        context.RegisterSourceOutput(
            allInputs.WithTrackingName(TrackingNames.Emit_ClosedGenericInterceptors),
            (spc, inputs) =>
            {
                // Merge class-level decorations (generic + non-generic)
                var classDecos = inputs.GenericDecos.Concat(inputs.NonGenericDecos).ToImmutableArray();

                // Build a fast lookup of implementations that opted out via [SkipAssemblyDecorators]
                var skipped = new HashSet<TypeDefId>(inputs.SkipMarkers.Select(m => m.ImplementationDef));

                // Convert ServiceDecoration → DecoratorToIntercept by matching service types with registrations
                var assemblyDecos = new List<DecoratorToIntercept>();
                foreach (var matchingDecos in inputs.Registrations.Select(reg => inputs.ServiceDecos
                             .Where(sd =>
                                 sd.ServiceDef.Equals(reg.ServiceDef) && sd.AssemblyName == reg.ImplDef.AssemblyName)
                             .Where(_ => !skipped.Contains(reg.ImplDef))
                             .Select(sd => new DecoratorToIntercept(
                                 ImplementationDef: reg.ImplDef,
                                 DecoratorDef: sd.DecoratorDef,
                                 Order: sd.Order,
                                 IsInterceptable: true))))
                {
                    assemblyDecos.AddRange(matchingDecos);
                }

                // Build decoration map with proper merge/precedence logic (class-level and assembly-level kept separate)
                var byImpl = BuildDecorationMap(classDecos, assemblyDecos.ToImmutableArray());

                // Only emit interceptors for registrations that have decorators
                var regsWithDecorators = inputs.Registrations
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
        ImmutableArray<DecoratorToIntercept> classDecorators,
        ImmutableArray<DecoratorToIntercept> assemblyDecorators)
    {
        // Build intermediate structure: ImplementationDef -> List<MergedDecoration>
        var grouped = new Dictionary<TypeDefId, List<MergedDecoration>>();

        // Add class-level decorators
        foreach (var d in classDecorators.Where(d => d.IsInterceptable))
        {
            if (!grouped.TryGetValue(d.ImplementationDef, out var list))
                grouped[d.ImplementationDef] = list = new();

            list.Add(new MergedDecoration(
                DecoratorDef: d.DecoratorDef,
                Order: d.Order,
                Source: DecorationSource.Class,
                IsInterceptable: d.IsInterceptable));
        }

        // Add assembly-level decorators (always IsInterceptable: true for now)
        foreach (var d in assemblyDecorators.Where(d => d.IsInterceptable))
        {
            if (!grouped.TryGetValue(d.ImplementationDef, out var list))
                grouped[d.ImplementationDef] = list = new();

            list.Add(new MergedDecoration(
                DecoratorDef: d.DecoratorDef,
                Order: d.Order,
                Source: DecorationSource.Assembly,
                IsInterceptable: true));
        }

        // Deduplicate and sort each implementation's decorators
        var result = new Dictionary<TypeDefId, EquatableArray<TypeDefId>>();
        foreach (var (impl, decorations) in grouped)
        {
            // Group by DecoratorDef to find duplicates, then take the one with lowest Source (Class=0 wins over Assembly=1)
            var deduplicated = decorations
                .GroupBy(m => m.DecoratorDef)
                .Select(g => g.MinBy(m => m.Source)!) // Class (0) wins over Assembly (1)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Source)
                .ThenBy(m => GetSortableTypeName(m.DecoratorDef))
                .ToList();

            // TODO: Apply DoNotDecorateDirective filtering here (Priority 3)
            // Filter out decorators that match any DoNotDecorateDirective for this implementation

            // Extract just the DecoratorDef for emission
            result[impl] = new EquatableArray<TypeDefId>(deduplicated.Select(m => m.DecoratorDef));
        }

        return result;
    }

    /// <summary>
    /// Generates a stable fully-qualified name for a type definition, used as a tiebreaker
    /// when sorting decorators with the same Order and Source.
    /// </summary>
    private static string GetSortableTypeName(TypeDefId typeDefId)
    {
        var parts = new List<string>();

        if (typeDefId.ContainingNamespaces.Count > 0)
            parts.Add(string.Join(".", typeDefId.ContainingNamespaces));

        if (typeDefId.ContainingTypes.Count > 0)
            parts.Add(string.Join("+", typeDefId.ContainingTypes));

        parts.Add(typeDefId.MetadataName);

        return string.Join(".", parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}
