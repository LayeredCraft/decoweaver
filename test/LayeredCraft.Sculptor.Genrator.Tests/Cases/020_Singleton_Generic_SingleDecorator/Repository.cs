using Sculptor.Attributes;

namespace Sculptor.Sample;

// Test singleton lifetime with generic decorator syntax
public interface IUserService
{
    void CreateUser(string name);
}

[DecoratedBy<UserLoggingDecorator>]
public sealed class UserService : IUserService
{
    public void CreateUser(string name)
    {
        Console.WriteLine($"Creating user: {name}");
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