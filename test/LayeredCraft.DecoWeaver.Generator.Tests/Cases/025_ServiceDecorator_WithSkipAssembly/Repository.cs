using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T item);
}

public sealed class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(DynamoDbRepository<>)}, type: {typeof(T).Name}");
    }
}

[SkipAssemblyDecoration]
public sealed class SqlRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(SqlRepository<>)}, type: {typeof(T).Name}");
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
