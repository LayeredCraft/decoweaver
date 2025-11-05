using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

// Mix of generic and non-generic decorator syntax on the same class
// This demonstrates that both syntaxes can coexist
// Order: Validation(1, non-generic) -> Audit(2, generic) -> Logging(3, generic) -> UserService
public interface IUserService
{
    void CreateUser(string name);
}

[DecoratedBy(typeof(UserValidationDecorator), 1)]      // Non-generic syntax
[DecoratedBy<UserAuditDecorator>(Order = 2)]           // Generic syntax
[DecoratedBy<UserLoggingDecorator>(Order = 3)]         // Generic syntax
public sealed class UserService : IUserService
{
    public void CreateUser(string name)
    {
        Console.WriteLine($"Creating user: {name}");
    }
}

public sealed class UserValidationDecorator : IUserService
{
    private readonly IUserService _inner;

    public UserValidationDecorator(IUserService inner)
    {
        _inner = inner;
    }

    public void CreateUser(string name)
    {
        Console.WriteLine("[VALIDATE] Validating user data");
        _inner.CreateUser(name);
    }
}

public sealed class UserAuditDecorator : IUserService
{
    private readonly IUserService _inner;

    public UserAuditDecorator(IUserService inner)
    {
        _inner = inner;
    }

    public void CreateUser(string name)
    {
        Console.WriteLine("[AUDIT] User creation requested");
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
        Console.WriteLine("[LOG] CreateUser called");
        _inner.CreateUser(name);
    }
}