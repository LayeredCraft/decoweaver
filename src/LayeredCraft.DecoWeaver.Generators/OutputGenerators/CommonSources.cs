// DecoWeaver/OutputGenerators/CommonSources.cs

using DecoWeaver.Emit;

namespace DecoWeaver.OutputGenerators;

/// <summary>
/// Generates common helper code used by all interceptors:
/// - InterceptsLocationAttribute (C# 12 interceptor support)
/// - DecoratorKeys (keyed service key generation)
/// - DecoratorFactory (runtime decorator instantiation with generic closing)
/// </summary>
internal static class CommonSources
{
    /// <summary>
    /// Generates the InterceptsLocationAttribute that enables C# 12 interceptors.
    /// This needs to be emitted at global namespace level.
    /// </summary>
    internal static string GenerateInterceptsLocationAttribute()
    {
        return TemplateHelper.Render(TemplateConstants.InterceptsLocationAttribute, (object?)null);
    }

    /// <summary>
    /// Generates the DecoratorKeys helper class for creating keyed service keys.
    /// This should be emitted inside the DecoWeaverInterceptors class.
    /// </summary>
    internal static string GenerateDecoratorKeys()
    {
        return TemplateHelper.Render(TemplateConstants.DecoratorKeys, (object?)null);
    }

    /// <summary>
    /// Generates the DecoratorFactory helper class for runtime decorator instantiation.
    /// This should be emitted inside the DecoWeaverInterceptors class.
    /// </summary>
    internal static string GenerateDecoratorFactory()
    {
        return TemplateHelper.Render(TemplateConstants.DecoratorFactory, (object?)null);
    }
}