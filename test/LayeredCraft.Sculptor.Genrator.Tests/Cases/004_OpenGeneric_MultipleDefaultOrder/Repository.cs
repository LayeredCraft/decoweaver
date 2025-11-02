using Sculptor.Attributes;

namespace Sculptor.Sample;

public interface IRepository<T>
{
    void Save(T item);
}

// Multiple decorators without explicit order (both default to 0)
// Should apply in declaration order
[DecoratedBy(typeof(CachingRepository<>))]
[DecoratedBy(typeof(LoggingRepository<>))]
public sealed class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(DynamoDbRepository<>)}, type: {typeof(T).Name}");
    }
}

public sealed class CachingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _innerRepository;

    public CachingRepository(IRepository<T> innerRepository)
    {
        _innerRepository = innerRepository;
    }

    public void Save(T item)
    {
        Console.WriteLine("Saved item to cache.");
        _innerRepository.Save(item);
    }
}

public sealed class LoggingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _innerRepository;

    public LoggingRepository(IRepository<T> innerRepository)
    {
        _innerRepository = innerRepository;
    }

    public void Save(T item)
    {
        Console.WriteLine("Logging save operation.");
        _innerRepository.Save(item);
    }
}