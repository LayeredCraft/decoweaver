using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

// This decorator should NOT be applied because the registration uses a factory delegate
[DecoratedBy(typeof(CachingRepository<>))]
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
        Console.WriteLine($"[Cache] Checking cache for {typeof(T).Name}...");
        _inner.Save(entity);
    }
}