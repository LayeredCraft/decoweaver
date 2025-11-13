using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test factory delegate without decorators
// Should pass through to original method, preserving the factory
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<Order>, Repository<Order>>(sp =>
        new Repository<Order>())
    .BuildServiceProvider();

// Should resolve the implementation directly without decoration
var repo = serviceProvider.GetRequiredService<IRepository<Order>>();
Console.WriteLine($"Resolved: {repo.GetType().Name}");

// Expected: Repository<Order> (no decorators)

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
}