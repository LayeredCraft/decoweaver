// DecoWeaver/Emit/TemplateConstants.cs

namespace DecoWeaver.Emit;

/// <summary>
/// Constants for Scriban template resource paths.
/// Template names follow the format: "Templates.{FileName}.scriban"
/// </summary>
internal static class TemplateConstants
{
    // Unified template - emits entire DecoWeaverInterceptors file
    internal const string DecoWeaverInterceptors = "Templates.DecoWeaverInterceptors.scriban";
}