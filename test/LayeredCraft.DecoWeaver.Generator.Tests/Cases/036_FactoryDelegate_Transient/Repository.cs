using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

// Transient lifetime with factory delegate and decorator
[DecoratedBy(typeof(MetricsRepository<>))]
public class Repository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[Repository] Saving {typeof(T).Name}...");
    }
}

public class MetricsRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;

    public MetricsRepository(IRepository<T> inner)
    {
        _inner = inner;
    }

    public void Save(T entity)
    {
        Console.WriteLine($"[Metrics] Recording save operation...");
        _inner.Save(entity);
    }
}