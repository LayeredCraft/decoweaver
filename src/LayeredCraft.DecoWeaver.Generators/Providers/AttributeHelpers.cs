using Microsoft.CodeAnalysis;

namespace DecoWeaver.Providers;

/// <summary>
/// Shared helper methods for reading attribute arguments across all providers.
/// </summary>
internal static class AttributeHelpers
{
    /// <summary>
    /// Gets a boolean named argument from an attribute, returning the default value if not found.
    /// </summary>
    internal static bool GetBoolNamedArg(AttributeData attr, string name, bool defaultValue)
    {
        foreach (var (n, v) in attr.NamedArguments)
            if (n == name && v.Value is bool b) return b;
        return defaultValue;
    }

    /// <summary>
    /// Gets an integer named argument from an attribute, returning the default value if not found.
    /// </summary>
    internal static int GetIntNamedArg(AttributeData attr, string name, int defaultValue)
    {
        foreach (var (n, v) in attr.NamedArguments)
            if (n == name && v.Value is int i) return i;
        return defaultValue;
    }
}