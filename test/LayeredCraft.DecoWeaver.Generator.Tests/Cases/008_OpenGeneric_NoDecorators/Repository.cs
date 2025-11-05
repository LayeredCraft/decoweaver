using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T item);
}

// No [DecoratedBy] attributes at all
// Should pass through to normal DI registration
public sealed class DynamoDbRepository<T> : IRepository<T>
{
    public void Save(T item)
    {
        Console.WriteLine($"Saving in {nameof(DynamoDbRepository<>)}, type: {typeof(T).Name}");
    }
}