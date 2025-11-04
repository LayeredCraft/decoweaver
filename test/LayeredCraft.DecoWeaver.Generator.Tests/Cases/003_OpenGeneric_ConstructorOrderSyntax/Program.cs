using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test constructor order syntax: [DecoratedBy(typeof(...), 1)]
// Verifies the pattern fix for reading positional order parameter
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>()
    .BuildServiceProvider();

var customerRepo = serviceProvider.GetRequiredService<IRepository<Customer>>();
Console.WriteLine($"Resolved: {customerRepo.GetType().Name}");
customerRepo.Save(new Customer { Id = 1, Name = "Test Customer" });

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}