using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

// Test mixed order syntax: positional vs named property
// Verifies both syntaxes work together correctly
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