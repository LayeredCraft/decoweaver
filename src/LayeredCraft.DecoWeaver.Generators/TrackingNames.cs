namespace DecoWeaver;

public static class TrackingNames
{
    // Settings / gates
    public const string Settings_LanguageVersionGate = nameof(Settings_LanguageVersionGate);
    public const string Diagnostics_CSharpVersion     = nameof(Diagnostics_CSharpVersion);

    // Attribute binding (DecoratedBy)
    public const string Attr_Generic_Predicate        = nameof(Attr_Generic_Predicate);
    public const string Attr_Generic_Transform        = nameof(Attr_Generic_Transform);
    public const string Attr_NonGeneric_Predicate     = nameof(Attr_NonGeneric_Predicate);
    public const string Attr_NonGeneric_Transform     = nameof(Attr_NonGeneric_Transform);
    public const string Attr_Generic_FilterNotNull    = nameof(Attr_Generic_FilterNotNull);
    public const string Attr_NonGeneric_FilterNotNull = nameof(Attr_NonGeneric_FilterNotNull);
    public const string Attr_ServiceDecoration_Transform = nameof(Attr_ServiceDecoration_Transform);
    public const string Attr_ServiceDecoration_FilterNotNull = nameof(Attr_ServiceDecoration_FilterNotNull);
    public const string Attr_ServiceDecoration_Stream = nameof(Attr_ServiceDecoration_Stream);
    public const string Attr_SkipAssemblyDecoration_Transform = nameof(Attr_SkipAssemblyDecoration_Transform);
    public const string Attr_SkipAssemblyDecoration_FilterNotNull = nameof(Attr_SkipAssemblyDecoration_FilterNotNull);
    public const string Attr_SkipAssemblyDecoration_Stream = nameof(Attr_SkipAssemblyDecoration_Stream);

    public const string Attr_Generic_Stream           = nameof(Attr_Generic_Stream);
    public const string Attr_NonGeneric_Stream        = nameof(Attr_NonGeneric_Stream);
    public const string Attr_All_Combined             = nameof(Attr_All_Combined);
    public const string Attr_Service_Collected        = nameof(Attr_Service_Collected);

    // Gated flow
    public const string Gate_Decorations              = nameof(Gate_Decorations);

    // Reduction / map building
    public const string Reduce_BuildDecorationMap     = nameof(Reduce_BuildDecorationMap);

    // Emission
    public const string Emit_DecorationDebug          = nameof(Emit_DecorationDebug);

    // Registration discovery
    public const string Reg_ClosedGeneric_Predicate = nameof(Reg_ClosedGeneric_Predicate);
    public const string Reg_ClosedGeneric_Transform = nameof(Reg_ClosedGeneric_Transform);
    public const string Reg_ClosedGeneric_Filter   = nameof(Reg_ClosedGeneric_Filter);
    public const string Reg_ClosedGeneric_Collect = nameof(Reg_ClosedGeneric_Collect);
    public const string Reg_SkipAssembly_Collect = nameof(Reg_SkipAssembly_Collect);

    // Emission
    public const string Emit_ClosedGenericInterceptors = nameof(Emit_ClosedGenericInterceptors);
    
    // Optional gate per-stream
    public const string Gate_Decorations_Generic    = nameof(Gate_Decorations_Generic);
    public const string Gate_Decorations_NonGeneric = nameof(Gate_Decorations_NonGeneric);
    public const string Gate_Decorations_Service = nameof(Gate_Decorations_Service);
    public const string Gate_Decorations_SkipAssembly = nameof(Gate_Decorations_SkipAssembly);
}