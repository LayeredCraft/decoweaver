using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test single type parameter factory delegate: AddScoped<T>(factory)
// Service and implementation are the same type
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<User>>(sp => new Repository<User>())
    .BuildServiceProvider();

// The decorator should be applied
var repo = serviceProvider.GetRequiredService<IRepository<User>>();
Console.WriteLine($"Resolved: {repo.GetType().Name}");

// Expected: LoggingRepository<User> (decorated)

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}