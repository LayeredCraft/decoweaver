using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// This test verifies that factory delegate registrations are NOT intercepted
// Factory delegates have a different signature: AddScoped<T1, T2>(Func<IServiceProvider, T2>)
// DecoWeaver should only intercept the parameterless overload: AddScoped<T1, T2>()
var serviceProvider = new ServiceCollection()
    .AddScoped<IRepository<Customer>, DynamoDbRepository<Customer>>(sp =>
        new DynamoDbRepository<Customer>())
    .BuildServiceProvider();

// Since this uses a factory delegate, NO interceptor should be generated
// The decorator should NOT be applied
var customerRepo = serviceProvider.GetRequiredService<IRepository<Customer>>();
Console.WriteLine($"Resolved: {customerRepo.GetType().Name}");

// Expected: DynamoDbRepository<Customer> (not decorated)
// If this were intercepted incorrectly, the factory would be lost

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}