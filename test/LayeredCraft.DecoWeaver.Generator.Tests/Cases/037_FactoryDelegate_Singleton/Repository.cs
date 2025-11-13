using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface ICache<T>
{
    void Set(string key, T value);
    T? Get(string key);
}

// Singleton lifetime with factory delegate and decorator
[DecoratedBy(typeof(LoggingCache<>))]
public class InMemoryCache<T> : ICache<T>
{
    public void Set(string key, T value)
    {
        Console.WriteLine($"[InMemoryCache] Setting {key}...");
    }

    public T? Get(string key)
    {
        Console.WriteLine($"[InMemoryCache] Getting {key}...");
        return default;
    }
}

public class LoggingCache<T> : ICache<T>
{
    private readonly ICache<T> _inner;

    public LoggingCache(ICache<T> inner)
    {
        _inner = inner;
    }

    public void Set(string key, T value)
    {
        Console.WriteLine($"[LoggingCache] Logging Set operation...");
        _inner.Set(key, value);
    }

    public T? Get(string key)
    {
        Console.WriteLine($"[LoggingCache] Logging Get operation...");
        return _inner.Get(key);
    }
}