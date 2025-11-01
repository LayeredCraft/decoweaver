using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

// NOTE: Sculptor requires closed generic registrations to apply decorators.
// Open generic registrations like AddScoped(typeof(IRepository<>), typeof(DynamoDbRepository<>))
// are not supported and will fall through to standard DI registration without decorators.
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>()
    .AddScoped<IRepository<Order>, DynamoDbRepository<Order>>()
    .BuildServiceProvider();

// Test with Customer repository
var customerRepo = serviceProvider.GetRequiredService<IRepository<Customer>>();
Console.WriteLine($"Resolved: {customerRepo.GetType().Name}");
customerRepo.Save(new Customer { Id = 1, Name = "Test Customer" });
Console.WriteLine();

// Test with Order repository
var orderRepo = serviceProvider.GetRequiredService<IRepository<Order>>();
Console.WriteLine($"Resolved: {orderRepo.GetType().Name}");
orderRepo.Save(new Order { Id = 1, Total = 99.99m });

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
}