using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

// Single type parameter factory: AddScoped<T>(factory) where T is both service and implementation
[DecoratedBy(typeof(LoggingRepository<>))]
public class Repository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[Repository] Saving {typeof(T).Name}...");
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