using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

// Multiple decorators applied in order with factory delegate
[DecoratedBy(typeof(CachingRepository<>), Order = 1)]
[DecoratedBy(typeof(LoggingRepository<>), Order = 2)]
public class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[DynamoDB] Saving {typeof(T).Name}...");
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
        Console.WriteLine($"[Cache] Checking cache...");
        _inner.Save(entity);
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
        Console.WriteLine($"[Logging] Before save...");
        _inner.Save(entity);
        Console.WriteLine($"[Logging] After save...");
    }
}