using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test AddTransient with factory delegate
// Decorator should be applied to transient registrations
var serviceProvider = new ServiceCollection()
    .AddTransient<IRepository<Item>, Repository<Item>>(sp =>
        new Repository<Item>())
    .BuildServiceProvider();

// Should resolve with decorator
var repo = serviceProvider.GetRequiredService<IRepository<Item>>();
Console.WriteLine($"Resolved: {repo.GetType().Name}");

// Expected: MetricsRepository<Item> wrapping Repository<Item>

public class Item
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
}