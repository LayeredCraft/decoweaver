namespace DecoWeaver;

internal static class AttributeNames
{
    // Full names with namespace (for ToDisplayString() comparisons)
    public const string DecoratedByAttribute = $"DecoWeaver.Attributes.{DecoratedByMetadataName}";
    public const string GenericDecoratedByAttribute = $"DecoWeaver.Attributes.{GenericDecoratedByMetadataName}";

    // Metadata names only (for pattern matching)
    public const string DecoratedByMetadataName = "DecoratedByAttribute";
    public const string GenericDecoratedByMetadataName = "DecoratedByAttribute`1";
}