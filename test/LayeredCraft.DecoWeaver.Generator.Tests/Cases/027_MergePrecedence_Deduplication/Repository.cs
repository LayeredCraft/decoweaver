namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    T Get(string id);
    void Save(T entity);
}

public class User { }
public class Product { }

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

// UserRepository: Assembly declares Caching@10, class declares Caching@20
// Expected: Deduplication - class-level wins, so only one CachingRepository at order 20
[DecoWeaver.Attributes.DecoratedBy(typeof(CachingRepository<>), Order = 20)]
public class UserRepository : IRepository<User>
{
    public User Get(string id) => throw new NotImplementedException();
    public void Save(User entity) => throw new NotImplementedException();
}

// ProductRepository: Assembly declares Logging@5, class declares Logging@15
// Expected: Deduplication - class-level wins, so only one LoggingRepository at order 15
[DecoWeaver.Attributes.DecoratedBy(typeof(LoggingRepository<>), Order = 15)]
public class ProductRepository : IRepository<Product>
{
    public Product Get(string id) => throw new NotImplementedException();
    public void Save(Product entity) => throw new NotImplementedException();
}