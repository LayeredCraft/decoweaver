using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

[DecoratedBy(typeof(CachingRepository<>))]
public class SqlRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[SQL] Saving {typeof(T).Name}...");
    }
}

[DecoratedBy(typeof(CachingRepository<>))]
public class CosmosRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[Cosmos] Saving {typeof(T).Name}...");
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

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
