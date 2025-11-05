using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T item);
}

// Non-generic decorator on generic implementation
// AuditDecorator is closed (not generic) but wraps generic repository
[DecoratedBy(typeof(AuditDecorator))]
public sealed class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(DynamoDbRepository<>)}, type: {typeof(T).Name}");
    }
}

// Non-generic decorator that implements IRepository<Customer>
public sealed class AuditDecorator : IRepository<Customer>
{
    private readonly IRepository<Customer> _innerRepository;

    public AuditDecorator(IRepository<Customer> innerRepository)
    {
        _innerRepository = innerRepository;
    }

    public void Save(Customer item)
    {
        Console.WriteLine("Auditing save operation.");
        _innerRepository.Save(item);
    }
}