using Microsoft.CodeAnalysis;

namespace Sculptor;

internal static class Descriptors
{
    /// <summary>
    /// SCULPT010: The project’s C# language version is too low for Sculptor’s interceptors.
    /// </summary>
    public static readonly DiagnosticDescriptor CSharpVersionTooLow = new(
        id: "SCULPT010",
        title: "C# language version too low",
        messageFormat: "Sculptor requires C# 11 or newer (or LanguageVersion=default with a modern SDK). " +
                       "Set <LangVersion>latest</LangVersion> or enable preview features.",
        category: "Sculptor.Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Sculptor relies on compiler features (interceptors/source-gen paths) that expect C# 11+.");
}