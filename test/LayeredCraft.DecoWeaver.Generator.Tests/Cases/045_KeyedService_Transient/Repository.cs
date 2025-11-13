using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

[DecoratedBy(typeof(LoggingRepository<>))]
public class MemoryRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[Memory] Saving {typeof(T).Name}...");
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
        Console.WriteLine($"[Log] Save called for {typeof(T).Name}");
        _inner.Save(entity);
    }
}

public class Event
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
}
