namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

// No decorators - should pass through without interception
public class SqlRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[SQL] Saving {typeof(T).Name}...");
    }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
