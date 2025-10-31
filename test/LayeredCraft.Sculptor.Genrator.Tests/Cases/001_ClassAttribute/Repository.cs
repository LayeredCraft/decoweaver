using Sculptor.Attributes;

namespace Sculptor.Sample;

public interface IRepository<T>
{
}

[DecoratedBy(typeof(CachingRepository<>))]
public sealed class DynamoDbRepository<T> : IRepository<T>
{
}

public sealed class CachingRepository<T> : IRepository<T>
{
    private readonly IRepository<T> _innerRepository;

    public CachingRepository(IRepository<T> innerRepository)
    {
        _innerRepository = innerRepository;
    }
}