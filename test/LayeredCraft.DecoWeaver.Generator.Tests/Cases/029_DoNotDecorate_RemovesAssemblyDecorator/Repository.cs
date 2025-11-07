namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    T Get(string id);
    void Save(T entity);
}

public class User { public int Id { get; set; } }
public class Order { public int Id { get; set; } }

// DynamoDbRepository: Should get decorated (no DoNotDecorate)
public class DynamoDbRepository<T> : IRepository<T>
{
    public T Get(string id) => throw new NotImplementedException();
    public void Save(T entity) => throw new NotImplementedException();
}

// SqlRepository: Should NOT be decorated (DoNotDecorate applied)
[DecoWeaver.Attributes.DoNotDecorate(typeof(CachingRepository<>))]
public class SqlRepository<T> : IRepository<T>
{
    public T Get(string id) => throw new NotImplementedException();
    public void Save(T entity) => throw new NotImplementedException();
}

// Decorator
public class CachingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _inner;
    public CachingRepository(IRepository<T> inner) => _inner = inner;
    public T Get(string id) => _inner.Get(id);
    public void Save(T entity) => _inner.Save(entity);
}