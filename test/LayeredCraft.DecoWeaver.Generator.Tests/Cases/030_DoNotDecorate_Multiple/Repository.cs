namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    T Get(string id);
    void Save(T entity);
}

public class Order { public int Id { get; set; } }

// SqlRepository: Opts out of Caching and Validation, keeps only Logging
[DecoWeaver.Attributes.DoNotDecorate(typeof(CachingRepository<>))]
[DecoWeaver.Attributes.DoNotDecorate(typeof(ValidationRepository<>))]
public class SqlRepository<T> : IRepository<T>
{
    public T Get(string id) => throw new NotImplementedException();
    public void Save(T entity) => throw new NotImplementedException();
}

// Decorators
public class CachingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;
    public CachingRepository(IRepository<T> inner) => _inner = inner;
    public T Get(string id) => _inner.Get(id);
    public void Save(T entity) => _inner.Save(entity);
}

public class LoggingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;
    public LoggingRepository(IRepository<T> inner) => _inner = inner;
    public T Get(string id) => _inner.Get(id);
    public void Save(T entity) => _inner.Save(entity);
}

public class ValidationRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;
    public ValidationRepository(IRepository<T> inner) => _inner = inner;
    public T Get(string id) => _inner.Get(id);
    public void Save(T entity) => _inner.Save(entity);
}