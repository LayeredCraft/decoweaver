using DecoWeaver.Attributes;

namespace DecoWeaver.Sample;

// Test singleton lifetime with multiple generic decorators
public interface IUserService
{
    void CreateUser(string name);
}

[DecoratedBy<UserAuditDecorator>(Order = 1)]
[DecoratedBy<UserLoggingDecorator>(Order = 2)]
public sealed class UserService : IUserService
{
    public void CreateUser(string name)
    {
        Console.WriteLine($"Creating user: {name}");
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