namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

// No decorators - should pass through to original method
public class PlainRepository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[Plain] Saving {typeof(T).Name}...");
    }
}

public class Data
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
