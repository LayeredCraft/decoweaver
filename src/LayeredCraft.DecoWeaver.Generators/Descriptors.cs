using Microsoft.CodeAnalysis;

namespace DecoWeaver;

internal static class Descriptors
{
    /// <summary>
    /// DECOW010: The project's C# language version is too low for DecoWeaver's interceptors.
    /// </summary>
    public static readonly DiagnosticDescriptor CSharpVersionTooLow = new(
        id: "DECOW010",
        title: "C# language version too low",
        messageFormat: "DecoWeaver requires C# 11 or newer (or LanguageVersion=default with a modern SDK). " +
                       "Set <LangVersion>latest</LangVersion> or enable preview features.",
        category: "DecoWeaver.Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "DecoWeaver relies on compiler features (interceptors/source-gen paths) that expect C# 11+.");

    /// <summary>
    /// DECOW020: Decorated implementation registered with factory delegate or additional parameters.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedRegistrationSignature = new(
        id: "DECOW020",
        title: "Unsupported DI registration signature for decorator",
        messageFormat: "The type '{0}' has [DecoratedBy] attributes but is registered using a factory delegate or additional parameters. " +
                       "DecoWeaver only supports the parameterless registration: AddScoped<TService, TImplementation>(). " +
                       "Remove the [DecoratedBy] attribute or use the parameterless registration.",
        category: "DecoWeaver.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "DecoWeaver can only intercept the parameterless AddScoped/AddTransient/AddSingleton<T1,T2>() overload. " +
                     "Factory delegates, keyed services, and instance registrations are not supported.");
}