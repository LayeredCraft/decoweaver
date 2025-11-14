using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

[DecoratedBy(typeof(LoggingRepository<>), Order = 1)]
[DecoratedBy(typeof(CachingRepository<>), Order = 2)]
public class SqlRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[SQL] Saving {typeof(T).Name}...");
    }
}

public class LoggingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;

    public LoggingRepository(IRepository<T> inner)
    {
        _inner = inner;
    }

    public void Save(T entity)
    {
        Console.WriteLine($"[Log] Save called for {typeof(T).Name}");
        _inner.Save(entity);
    }
}

public class CachingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;

    public CachingRepository(IRepository<T> inner)
    {
        _inner = inner;
    }

    public void Save(T entity)
    {
        Console.WriteLine($"[Cache] Invalidating cache for {typeof(T).Name}");
        _inner.Save(entity);
    }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
