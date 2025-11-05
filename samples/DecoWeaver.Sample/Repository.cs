using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

// ============================================================================
// Example 1: Open generic repositories with typeof() syntax
// Use [DecoratedBy(typeof(...))] for open generic decorators
// ============================================================================
public interface IRepository<T>
{
    void Save(T item);
}

[DecoratedBy(typeof(LoggingRepository<>), 2)]
[DecoratedBy(typeof(CachingRepository<>), 1)]
public sealed class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(DynamoDbRepository<>)}, type: {typeof(T).Name}");
    }
}

// ============================================================================
// Example 2: Concrete service with non-generic decorators
// Use [DecoratedBy<T>] for compile-time type safety with concrete decorators
// ============================================================================
public interface IUserService
{
    void CreateUser(string name);
}

[DecoratedBy<UserLoggingDecorator>(Order = 2)]
[DecoratedBy<UserAuditDecorator>(Order = 1)]
public sealed class UserService : IUserService
{
    public void CreateUser(string name)
    {
        Console.WriteLine($"Creating user: {name}");
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

public sealed class LoggingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _innerRepository;

    public LoggingRepository(IRepository<T> innerRepository)
    {
        _innerRepository = innerRepository;
    }

    public void Save(T item)
    {
        Console.WriteLine("Logging save operation.");
        _innerRepository.Save(item);
    }
}

// ============================================================================
// Concrete decorators for IUserService (demonstrates generic attribute syntax)
// ============================================================================
public sealed class UserAuditDecorator : IUserService
{
    private readonly IUserService _inner;

    public UserAuditDecorator(IUserService inner)
    {
        _inner = inner;
    }

    public void CreateUser(string name)
    {
        Console.WriteLine($"[AUDIT] User creation requested for: {name}");
        _inner.CreateUser(name);
    }
}

public sealed class UserLoggingDecorator : IUserService
{
    private readonly IUserService _inner;

    public UserLoggingDecorator(IUserService inner)
    {
        _inner = inner;
    }

    public void CreateUser(string name)
    {
        Console.WriteLine($"[LOG] CreateUser called with name: {name}");
        _inner.CreateUser(name);
    }
}