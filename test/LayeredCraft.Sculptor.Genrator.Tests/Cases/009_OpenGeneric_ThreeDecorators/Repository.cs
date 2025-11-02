using Sculptor.Attributes;

namespace Sculptor.Sample;

public interface IRepository<T>
{
    void Save(T item);
}

// Three decorators for deep nesting test
// Order: Audit(1) -> Caching(2) -> Logging(3)
[DecoratedBy(typeof(AuditRepository<>), Order = 1)]
[DecoratedBy(typeof(CachingRepository<>), Order = 2)]
[DecoratedBy(typeof(LoggingRepository<>), Order = 3)]
public sealed class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(DynamoDbRepository<>)}, type: {typeof(T).Name}");
    }
}

public sealed class AuditRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _innerRepository;

    public AuditRepository(IRepository<T> innerRepository)
    {
        _innerRepository = innerRepository;
    }

    public void Save(T item)
    {
        Console.WriteLine("Auditing save operation.");
        _innerRepository.Save(item);
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