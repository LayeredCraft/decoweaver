namespace DecoWeaver.Sample;

public interface IRepository<T>
{
    void Save(T entity);
}

// No decorators - factory delegate should pass through to original method
public class Repository<T> : IRepository<T>
{
    public void Save(T entity)
    {
        Console.WriteLine($"[Repository] Saving {typeof(T).Name}...");
    }
}