using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T item);
}

// Test singleton lifetime with single decorator
[DecoratedBy(typeof(LoggingRepository<>))]
public sealed class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(DynamoDbRepository<>)}, type: {typeof(T).Name}");
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