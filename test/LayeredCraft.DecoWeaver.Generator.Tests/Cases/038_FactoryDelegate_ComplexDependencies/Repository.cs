using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface ILogger
{
    void Log(string message);
}

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"[ConsoleLogger] {message}");
}

public interface IRepository<T>
{
    void Save(T entity);
}

// Factory with complex dependencies resolved from IServiceProvider
[DecoratedBy(typeof(CachingRepository<>))]
public class Repository<T> : IRepository<T>
{
    private readonly ILogger _logger;

    public Repository(ILogger logger)
    {
        _logger = logger;
    }

    public void Save(T entity)
    {
        _logger.Log($"Saving {typeof(T).Name}...");
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