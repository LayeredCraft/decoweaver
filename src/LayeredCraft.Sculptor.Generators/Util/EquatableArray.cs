using System.Collections;
using System.Collections.Immutable;

namespace Sculptor.Util;

// Value-based equality wrapper for incremental caching & dictionary keys
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _array;

    public EquatableArray(IEnumerable<T> items) => _array = items?.ToArray();
    public EquatableArray(T[] array)            => _array = array;
    public EquatableArray(ImmutableArray<T> a)  => _array = a.IsDefaultOrEmpty ? null : a.ToArray();

    public int Count => _array?.Length ?? 0;
    public T this[int index] => _array![index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_array is null || other._array is null) return _array is null && other._array is null;
        if (_array.Length != other._array.Length) return false;
        for (int i = 0; i < _array.Length; i++)
            if (!_array[i].Equals(other._array[i])) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array is null) return 0;
        unchecked
        {
            int hash = 17;
            foreach (var item in _array)
                hash = (hash * 31) + (item?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator() => (_array ?? Array.Empty<T>()).AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}