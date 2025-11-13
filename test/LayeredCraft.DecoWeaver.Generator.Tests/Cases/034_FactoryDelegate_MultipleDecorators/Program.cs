using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test multiple decorators with factory delegate
// Decorators should be applied in order: CachingRepository (Order=1), LoggingRepository (Order=2)
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<Product>, DynamoDbRepository<Product>>(sp =>
        new DynamoDbRepository<Product>())
    .BuildServiceProvider();

// The decorators should be applied in the correct order
var repo = serviceProvider.GetRequiredService<IRepository<Product>>();
Console.WriteLine($"Resolved: {repo.GetType().Name}");

// Expected: LoggingRepository<Product> wrapping CachingRepository<Product> wrapping DynamoDbRepository<Product>

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}