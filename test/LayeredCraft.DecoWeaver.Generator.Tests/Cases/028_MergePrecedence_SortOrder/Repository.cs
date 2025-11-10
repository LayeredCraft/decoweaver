namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    T Get(string id);
    void Save(T entity);
}

public class User { }

// Decorator types
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

// UserRepository:
// Assembly declares: Logging@10
// Class declares: Validation@10 (same order as assembly-level Logging)
// Expected sort order: Validation@10 (Class, Source=0), then Logging@10 (Assembly, Source=1)
[DecoWeaver.Attributes.DecoratedBy(typeof(ValidationRepository<>), Order = 10)]
public class UserRepository : IRepository<User>
{
    public User Get(string id) => throw new NotImplementedException();
    public void Save(User entity) => throw new NotImplementedException();
}