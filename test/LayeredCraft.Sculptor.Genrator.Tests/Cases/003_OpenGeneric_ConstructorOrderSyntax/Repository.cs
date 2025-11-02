using Sculptor.Attributes;

namespace Sculptor.Sample;

public interface IRepository<T>
{
    void Save(T item);
}

// Using positional order parameter in constructor (ChatGPT fix verification)
[DecoratedBy(typeof(CachingRepository<>), 1)]
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