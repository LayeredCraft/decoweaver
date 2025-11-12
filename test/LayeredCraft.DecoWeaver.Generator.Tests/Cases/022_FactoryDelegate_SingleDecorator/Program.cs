using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// This test verifies that factory delegate registrations ARE intercepted (Phase 1)
// Factory delegates have signature: AddScoped<T1, T2>(Func<IServiceProvider, T2>)
// DecoWeaver now supports both parameterless and factory delegate overloads
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>(sp =>
        new DynamoDbRepository<Customer>())
    .BuildServiceProvider();

// The interceptor should wrap the factory and apply decorators
var customerRepo = serviceProvider.GetRequiredService<IRepository<Customer>>();
Console.WriteLine($"Resolved: {customerRepo.GetType().Name}");

// Expected: CachingRepository<Customer> (decorated)
// The factory is preserved and the decorator is applied around the factory result

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}