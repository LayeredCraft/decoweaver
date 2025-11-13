using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

[DecoratedBy(typeof(CachingRepository<>))]
public class ConfigurableRepository<T> : IRepository<T>
{
    private readonly string _connectionString;

    public ConfigurableRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Save(T entity)
    {
        Console.WriteLine($"[Configurable] Saving {typeof(T).Name} to {_connectionString}...");
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
        Console.WriteLine($"[Cache] Checking cache for {typeof(T).Name}...");
        _inner.Save(entity);
    }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
