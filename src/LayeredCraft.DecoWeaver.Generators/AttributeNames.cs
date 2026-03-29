namespace LayeredCraft.DecoWeaver;

internal static class AttributeNames
{
    // Full names with namespace (for ToDisplayString() comparisons)
    public const string DecoratedByAttribute = $"LayeredCraft.DecoWeaver.Attributes.{DecoratedByMetadataName}";
    public const string GenericDecoratedByAttribute = $"LayeredCraft.DecoWeaver.Attributes.{GenericDecoratedByMetadataName}";
    public const string ServiceDecoratedByAttribute = $"LayeredCraft.DecoWeaver.Attributes.{ServiceDecoratedByMetadataName}";
    public const string SkipAssemblyDecorationAttribute = $"LayeredCraft.DecoWeaver.Attributes.{SkipAssemblyDecorationMetadataName}";
    public const string DoNotDecorateAttribute = $"LayeredCraft.DecoWeaver.Attributes.{DoNotDecorateMetadataName}";

    // Metadata names only (for pattern matching)
    public const string DecoratedByMetadataName = "DecoratedByAttribute";
    public const string GenericDecoratedByMetadataName = "DecoratedByAttribute`1";
    public const string ServiceDecoratedByMetadataName = "DecorateServiceAttribute";
    public const string SkipAssemblyDecorationMetadataName = "SkipAssemblyDecorationAttribute";
    public const string DoNotDecorateMetadataName = "DoNotDecorateAttribute";
}