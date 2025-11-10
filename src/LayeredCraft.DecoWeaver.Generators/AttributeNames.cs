namespace DecoWeaver;

internal static class AttributeNames
{
    // Full names with namespace (for ToDisplayString() comparisons)
    public const string DecoratedByAttribute = $"DecoWeaver.Attributes.{DecoratedByMetadataName}";
    public const string GenericDecoratedByAttribute = $"DecoWeaver.Attributes.{GenericDecoratedByMetadataName}";
    public const string ServiceDecoratedByAttribute = $"DecoWeaver.Attributes.{ServiceDecoratedByMetadataName}";
    public const string SkipAssemblyDecorationAttribute = $"DecoWeaver.Attributes.{SkipAssemblyDecorationMetadataName}";
    public const string DoNotDecorateAttribute = $"DecoWeaver.Attributes.{DoNotDecorateMetadataName}";

    // Metadata names only (for pattern matching)
    public const string DecoratedByMetadataName = "DecoratedByAttribute";
    public const string GenericDecoratedByMetadataName = "DecoratedByAttribute`1";
    public const string ServiceDecoratedByMetadataName = "DecorateServiceAttribute";
    public const string SkipAssemblyDecorationMetadataName = "SkipAssemblyDecorationAttribute";
    public const string DoNotDecorateMetadataName = "DoNotDecorateAttribute";
}