using System.Collections.Immutable;

namespace DecoWeaver.Util;

internal static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source)
        where T : IEquatable<T>
        => new(source);

    public static EquatableArray<T> ToEquatableArray<T>(this ImmutableArray<T> source)
        where T : IEquatable<T>
        => new(source);
}