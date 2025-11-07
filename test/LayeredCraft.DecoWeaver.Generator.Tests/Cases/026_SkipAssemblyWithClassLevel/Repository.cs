using DecoWeaver.Attributes;

public interface IRepository<T>
{
    void Save(T entity);
}

// This repository opts out of assembly-level decorators (CachingRepository)
// but still has class-level decorators (ValidationRepository) that should apply
[SkipAssemblyDecoration]
[DecoratedBy(typeof(ValidationRepository<>), Order = 10)]
public sealed class SpecialRepository<T>(IRepository<T> inner) : IRepository<T>
{
    public void Save(T entity) => inner.Save(entity);
}

// Decorators
public sealed class CachingRepository<T>(IRepository<T> inner) : IRepository<T>
{
    public void Save(T entity) => inner.Save(entity);
}

public sealed class ValidationRepository<T>(IRepository<T> inner) : IRepository<T>
{
    public void Save(T entity) => inner.Save(entity);
}