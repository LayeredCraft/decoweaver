using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test skip assembly does not decorate SqlRepository<T>
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>()
    .AddScoped<IRepository<Order>, SqlRepository<Order>>()
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