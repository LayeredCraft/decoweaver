using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test multiple decorators with explicit order
// Order 1 (CachingRepository) should be innermost
// Order 2 (LoggingRepository) should be outermost
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